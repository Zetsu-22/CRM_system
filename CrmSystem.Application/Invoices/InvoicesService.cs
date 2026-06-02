using System.Text.Json;
using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Invoices.Dtos;
using CrmSystem.Application.Orders.Pricing;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Invoices;

public sealed class InvoicesService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IInvoicesService
{
    public async Task<Result<IReadOnlyList<Invoice>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<Invoice>>.Success(Array.Empty<Invoice>());

        var query = db.Invoices
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Lines)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query.OrderByDescending(x => x.Date).ToListAsync(ct);
        return Result<IReadOnlyList<Invoice>>.Success(list);
    }

    public async Task<Result<Invoice>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Invoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

        return entity is null ? Error.NotFound() : Result<Invoice>.Success(entity);
    }

    public async Task<Result<Invoice>> CreateAsync(InvoiceDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
            CounterpartyId = dto.CounterpartyId,
            OrderId = dto.OrderId,
            Number = dto.Number,
            Date = dto.Date,
            Status = dto.Status,
            Comment = dto.Comment,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var line in dto.Lines)
            entity.Lines.Add(BuildLine(entity.Id, line, now));

        ApplyTotals(entity);

        // Phase 4.3 — снапшоты
        await FillSnapshotsAsync(entity, dto.CounterpartyId, ct);

        db.Invoices.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Invoice>> UpdateAsync(Guid id, InvoiceDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var entity = await db.Invoices
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        var now = clock.UtcNow;
        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.OrganizationId = dto.OrganizationId;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.OrderId = dto.OrderId;
        entity.Status = dto.Status;
        entity.Comment = dto.Comment;
        entity.UpdatedAt = now;

        // Hard-delete old lines then recreate — consistent with OrdersService pattern.
        await db.InvoiceLines
            .IgnoreQueryFilters()
            .Where(x => x.InvoiceId == id)
            .ExecuteDeleteAsync(ct);

        var newLines = dto.Lines.Select(l => BuildLine(id, l, now)).ToList();
        db.InvoiceLines.AddRange(newLines);

        entity.TotalNet   = newLines.Sum(l => l.Net);
        entity.TotalVat   = newLines.Sum(l => l.Vat);
        entity.TotalGross = newLines.Sum(l => l.Gross);

        await db.SaveChangesAsync(ct);

        // Reload lines for response
        await db.Entry(entity).Collection(x => x.Lines).LoadAsync(ct);
        return entity;
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();

        var entity = await db.Invoices
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task FillSnapshotsAsync(Invoice entity, Guid counterpartyId, CancellationToken ct)
    {
        // Counterparty INN/KPP snapshot
        var cp = await db.Counterparties.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == counterpartyId, ct);
        if (cp is not null)
        {
            entity.CounterpartyInnSnapshot = cp.Inn;
            entity.CounterpartyKppSnapshot = cp.Kpp;
        }

        // Company bank requisites snapshot
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cu.CompanyId, ct);
        if (company is not null)
        {
            entity.BankRequisitesSnapshotJson = JsonSerializer.Serialize(new
            {
                bankName = company.BankName,
                bik = company.Bik,
                accountNumber = company.AccountNumber,
                corrAccount = company.CorrAccount,
            });
        }

        // Calculation snapshot — строки с расчётами
        entity.CalculationSnapshotJson = JsonSerializer.Serialize(entity.Lines.Select(l => new
        {
            itemId = l.ItemId,
            name = l.NameSnapshot,
            qty = l.Qty,
            unitPrice = l.UnitPrice,
            vatRate = (int)l.VatRateSnapshot,
            net = l.Net,
            vat = l.Vat,
            gross = l.Gross,
        }));
    }

    private static InvoiceLine BuildLine(Guid invoiceId, InvoiceLineDto line, DateTimeOffset now)
    {
        var totals = OrderTotalsCalculator.CalculateLine(line.Qty, line.UnitPrice, line.VatRateSnapshot);
        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ItemId = line.ItemId,
            NameSnapshot = line.NameSnapshot,
            VatRateSnapshot = line.VatRateSnapshot,
            Qty = line.Qty,
            UnitPrice = line.UnitPrice,
            DiscountType = line.DiscountType,
            DiscountValue = line.DiscountValue,
            Net = totals.Net,
            Vat = totals.Vat,
            Gross = totals.Gross,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static void ApplyTotals(Invoice invoice)
    {
        invoice.TotalNet   = invoice.Lines.Sum(l => l.Net);
        invoice.TotalVat   = invoice.Lines.Sum(l => l.Vat);
        invoice.TotalGross = invoice.Lines.Sum(l => l.Gross);
    }
}
