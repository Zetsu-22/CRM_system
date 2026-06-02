using System.Text.Json;
using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Orders.Pricing;
using CrmSystem.Application.Upds.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Upds;

public sealed class UpdsService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IUpdsService
{
    public async Task<Result<IReadOnlyList<Upd>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<Upd>>.Success(Array.Empty<Upd>());

        var query = db.Upds
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Lines)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query.OrderByDescending(x => x.Date).ToListAsync(ct);
        return Result<IReadOnlyList<Upd>>.Success(list);
    }

    public async Task<Result<Upd>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Upds
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

        return entity is null ? Error.NotFound() : Result<Upd>.Success(entity);
    }

    public async Task<Result<Upd>> CreateAsync(UpdDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new Upd
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
            CounterpartyId = dto.CounterpartyId,
            OrderId = dto.OrderId,
            InvoiceId = dto.InvoiceId,
            Number = dto.Number,
            Date = dto.Date,
            Function = dto.Function,
            Status = dto.Status,
            ShippingDate = dto.ShippingDate,
            Comment = dto.Comment,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var line in dto.Lines)
            entity.Lines.Add(BuildLine(entity.Id, line, now));

        ApplyTotals(entity);

        // Phase 4.3 — снапшоты
        await FillSnapshotsAsync(entity, dto.CounterpartyId, ct);

        db.Upds.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Upd>> UpdateAsync(Guid id, UpdDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var entity = await db.Upds
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        var now = clock.UtcNow;
        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.OrganizationId = dto.OrganizationId;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.OrderId = dto.OrderId;
        entity.InvoiceId = dto.InvoiceId;
        entity.Function = dto.Function;
        entity.Status = dto.Status;
        entity.ShippingDate = dto.ShippingDate;
        entity.Comment = dto.Comment;
        entity.UpdatedAt = now;

        // Hard-delete old lines then recreate — consistent with OrdersService pattern.
        await db.UpdLines
            .IgnoreQueryFilters()
            .Where(x => x.UpdId == id)
            .ExecuteDeleteAsync(ct);

        var newLines = dto.Lines.Select(l => BuildLine(id, l, now)).ToList();
        db.UpdLines.AddRange(newLines);

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

        var entity = await db.Upds
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task FillSnapshotsAsync(Upd entity, Guid counterpartyId, CancellationToken ct)
    {
        var cp = await db.Counterparties.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == counterpartyId, ct);
        if (cp is not null)
        {
            entity.CounterpartyInnSnapshot = cp.Inn;
            entity.CounterpartyKppSnapshot = cp.Kpp;
        }

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

    private static UpdLine BuildLine(Guid updId, UpdLineDto line, DateTimeOffset now)
    {
        var totals = OrderTotalsCalculator.CalculateLine(line.Qty, line.UnitPrice, line.VatRateSnapshot);
        return new UpdLine
        {
            Id = Guid.NewGuid(),
            UpdId = updId,
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

    private static void ApplyTotals(Upd upd)
    {
        upd.TotalNet   = upd.Lines.Sum(l => l.Net);
        upd.TotalVat   = upd.Lines.Sum(l => l.Vat);
        upd.TotalGross = upd.Lines.Sum(l => l.Gross);
    }
}
