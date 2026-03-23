using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/dev")]
[Authorize]
public sealed class DevSeedController(CrmDbContext db, CurrentUserService cu, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("seed-test-company")]
    public async Task<IActionResult> SeedTestCompany([FromBody] SeedTestCompanyDto dto, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        var companyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? "Тестовая компния" : dto.CompanyName.Trim();

        var now = DateTimeOffset.UtcNow;

        var company = await db.Companies.FirstOrDefaultAsync(x => x.Name == companyName, ct);
        if (company is null)
        {
            company = new Company
            {
                Id = Guid.NewGuid(),
                Name = companyName,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync(ct);
        }

        var companyId = company.Id;

        var hasSeed = await db.Items.AnyAsync(x => x.CompanyId == companyId && x.Sku.StartsWith("TEST-"), ct);
        if (hasSeed && !dto.Force)
        {
            return Ok(new
            {
                message = "Seed already exists. Pass { force: true } to generate additional documents.",
                companyId,
            });
        }

        var org1 = await EnsureOrganization(companyId, "Основная организация", true, now, ct);
        _ = await EnsureOrganization(companyId, "Филиал 1", false, now, ct);

        var wh = await EnsureWarehouse(companyId, "Основной склад", true, now, ct);

        var customer = await EnsureCounterparty(companyId, "ООО Покупатель", CounterpartyType.Customer, now, ct);
        var supplier = await EnsureCounterparty(companyId, "ООО Поставщик", CounterpartyType.Supplier, now, ct);
        var both = await EnsureCounterparty(companyId, "ИП Партнёр", CounterpartyType.Both, now, ct);

        var items = await EnsureItems(companyId, now, ct);

        await EnsureStockBalances(wh.Id, items.Where(i => i.IsStockItem).Take(5).ToList(), now, ct);

        var createdOrders = new List<SalesOrder>();

        for (var i = 1; i <= 3; i++)
        {
            var number = $"TST-ORD-{DateTime.UtcNow:yyyyMMdd}-{i:000}";
            if (await db.Orders.AnyAsync(x => x.CompanyId == companyId && x.Number == number, ct)) continue;

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CounterpartyId = customer.Id,
                Number = number,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = OrderStatus.Processing,
                Comment = "Тестовый заказ",
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };

            var lineItems = items.Take(3).ToArray();
            order.Lines.Add(BuildOrderLine(order.Id, wh.Id, lineItems[0], 2, 1500m, now));
            order.Lines.Add(BuildOrderLine(order.Id, wh.Id, lineItems[1], 1, 4990m, now));
            order.Lines.Add(BuildOrderLine(order.Id, null, lineItems[2], 3, 500m, now));

            RecalcOrderTotals(order);

            db.Orders.Add(order);
            createdOrders.Add(order);
        }

        await db.SaveChangesAsync(ct);

        var createdInvoices = new List<Invoice>();

        for (var i = 1; i <= 2; i++)
        {
            var number = $"TST-INV-{DateTime.UtcNow:yyyyMMdd}-{i:000}";
            if (await db.Invoices.AnyAsync(x => x.CompanyId == companyId && x.Number == number, ct)) continue;

            var order = createdOrders.ElementAtOrDefault(i - 1) ?? await db.Orders.Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            if (order is null) break;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CounterpartyId = customer.Id,
                OrderId = order.Id,
                Number = number,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = InvoiceStatus.Issued,
                Comment = "Тестовый счёт",
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };

            var invItem1 = items[0];
            var invItem2 = items[1];
            invoice.Lines.Add(BuildInvoiceLine(invoice.Id, invItem1, 1, 1500m, now));
            invoice.Lines.Add(BuildInvoiceLine(invoice.Id, invItem2, 1, 4990m, now));

            RecalcInvoiceTotals(invoice);

            db.Invoices.Add(invoice);
            createdInvoices.Add(invoice);
        }

        await db.SaveChangesAsync(ct);

        var updNumber = $"TST-UPD-{DateTime.UtcNow:yyyyMMdd}-001";
        if (!await db.Upds.AnyAsync(x => x.CompanyId == companyId && x.Number == updNumber, ct))
        {
            var invoice = createdInvoices.FirstOrDefault();
            var order = createdOrders.FirstOrDefault();
            if (invoice is not null && order is not null)
            {
                var upd = new Upd
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    CounterpartyId = customer.Id,
                    OrderId = order.Id,
                    InvoiceId = invoice.Id,
                    Number = updNumber,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Function = UpdFunction.ShippingAndInvoice,
                    Status = UpdStatus.Registered,
                    Comment = "Тестовая УПД",
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false,
                };

                upd.Lines.Add(BuildUpdLine(upd.Id, wh.Id, items[0], 1, 1500m, now));
                upd.Lines.Add(BuildUpdLine(upd.Id, wh.Id, items[1], 1, 4990m, now));

                RecalcUpdTotals(upd);

                db.Upds.Add(upd);
                await db.SaveChangesAsync(ct);
            }
        }

        for (var i = 1; i <= 2; i++)
        {
            var exists = await db.Payments.AnyAsync(x => x.CompanyId == companyId && x.ExternalId == $"TST-PAY-{DateTime.UtcNow:yyyyMMdd}-{i:000}", ct);
            if (exists) continue;

            var invoice = createdInvoices.ElementAtOrDefault(i - 1) ?? createdInvoices.FirstOrDefault();
            if (invoice is null) break;

            var payAmount = i == 1 ? invoice.TotalGross / 2m : invoice.TotalGross;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CounterpartyId = customer.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Type = PaymentType.Incoming,
                PaymentMethod = PaymentMethod.Bank,
                Status = PaymentStatus.Posted,
                Amount = payAmount,
                ExternalId = $"TST-PAY-{DateTime.UtcNow:yyyyMMdd}-{i:000}",
                BankReference = "TEST-BANK-REF",
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };

            payment.Allocations.Add(new PaymentAllocation
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                Amount = payAmount,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            });

            db.Payments.Add(payment);

            invoice.PaidAmount += payAmount;
            if (invoice.PaidAmount >= invoice.TotalGross)
                invoice.Status = InvoiceStatus.Closed;

            invoice.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            companyId,
            companyName = company.Name,
            organizationsCreated = 2,
            counterparties = new[] { customer.Id, supplier.Id, both.Id },
            itemsCreated = items.Count,
            ordersCreated = createdOrders.Count,
            invoicesCreated = createdInvoices.Count,
            message = "Seed completed"
        });
    }

    private async Task<Organization> EnsureOrganization(Guid companyId, string name, bool isDefault, DateTimeOffset now, CancellationToken ct)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == name, ct);
        if (org is not null) return org;

        org = new Organization
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            IsDefault = isDefault,
            Address = "Тестовый адрес",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);
        return org;
    }

    private async Task<Warehouse> EnsureWarehouse(Guid companyId, string name, bool isDefault, DateTimeOffset now, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == name, ct);
        if (wh is not null) return wh;

        wh = new Warehouse
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            IsDefault = isDefault,
            Address = "Тестовый склад",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync(ct);
        return wh;
    }

    private async Task<Counterparty> EnsureCounterparty(Guid companyId, string name, CounterpartyType type, DateTimeOffset now, CancellationToken ct)
    {
        var cp = await db.Counterparties.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == name, ct);
        if (cp is not null) return cp;

        cp = new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Type = type,
            Status = CounterpartyStatus.Active,
            PaymentTermsDays = 7,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
        cp.Addresses.Add(new CounterpartyAddress
        {
            Id = Guid.NewGuid(),
            CounterpartyId = cp.Id,
            Type = CounterpartyAddressType.Shipping,
            AddressLine = "Тестовый адрес доставки",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        });
        cp.Contacts.Add(new CounterpartyContact
        {
            Id = Guid.NewGuid(),
            CounterpartyId = cp.Id,
            Name = "Иван",
            Phone = "+7 900 000-00-00",
            Email = "test@example.com",
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        });

        db.Counterparties.Add(cp);
        await db.SaveChangesAsync(ct);
        return cp;
    }

    private async Task<List<Item>> EnsureItems(Guid companyId, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await db.Items.Where(x => x.CompanyId == companyId && x.Sku.StartsWith("TEST-")).OrderBy(x => x.Sku).ToListAsync(ct);
        if (existing.Count >= 10) return existing;

        var list = new List<Item>();
        for (var i = 1; i <= 10; i++)
        {
            var sku = $"TEST-{i:000}";
            var item = await db.Items.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Sku == sku, ct);
            if (item is not null)
            {
                list.Add(item);
                continue;
            }

            item = new Item
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Type = i % 4 == 0 ? ItemType.Service : ItemType.Product,
                Sku = sku,
                Name = i % 4 == 0 ? $"Услуга {i}" : $"Товар {i}",
                Unit = i % 4 == 0 ? "усл" : "шт",
                VatRate = VatRate.Vat22,
                IsStockItem = i % 4 != 0,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };
            db.Items.Add(item);
            list.Add(item);
        }

        await db.SaveChangesAsync(ct);
        return list;
    }

    private async Task EnsureStockBalances(Guid warehouseId, List<Item> items, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var it in items)
        {
            var exists = await db.StockBalances.AnyAsync(x => x.WarehouseId == warehouseId && x.ItemId == it.Id, ct);
            if (exists) continue;

            db.StockBalances.Add(new StockBalance
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                ItemId = it.Id,
                QtyOnHand = 100,
                QtyReserved = 0,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static SalesOrderLine BuildOrderLine(Guid orderId, Guid? warehouseId, Item item, decimal qty, decimal unitPrice, DateTimeOffset now)
    {
        var net = qty * unitPrice;
        var vat = net * 0.22m;
        var gross = net + vat;

        return new SalesOrderLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ItemId = item.Id,
            WarehouseId = warehouseId,
            NameSnapshot = item.Name,
            VatRateSnapshot = item.VatRate,
            Qty = qty,
            UnitPrice = unitPrice,
            DiscountType = DiscountType.None,
            DiscountValue = 0,
            Net = net,
            Vat = vat,
            Gross = gross,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    private static void RecalcOrderTotals(SalesOrder order)
    {
        order.TotalNet = order.Lines.Sum(x => x.Net);
        order.TotalVat = order.Lines.Sum(x => x.Vat);
        order.TotalGross = order.Lines.Sum(x => x.Gross);
    }

    private static InvoiceLine BuildInvoiceLine(Guid invoiceId, Item item, decimal qty, decimal unitPrice, DateTimeOffset now)
    {
        var net = qty * unitPrice;
        var vat = net * 0.22m;
        var gross = net + vat;

        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ItemId = item.Id,
            NameSnapshot = item.Name,
            VatRateSnapshot = item.VatRate,
            Qty = qty,
            UnitPrice = unitPrice,
            DiscountType = DiscountType.None,
            DiscountValue = 0,
            Net = net,
            Vat = vat,
            Gross = gross,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    private static void RecalcInvoiceTotals(Invoice invoice)
    {
        invoice.TotalNet = invoice.Lines.Sum(x => x.Net);
        invoice.TotalVat = invoice.Lines.Sum(x => x.Vat);
        invoice.TotalGross = invoice.Lines.Sum(x => x.Gross);
        invoice.PaidAmount = invoice.PaidAmount;
    }

    private static UpdLine BuildUpdLine(Guid updId, Guid? warehouseId, Item item, decimal qty, decimal unitPrice, DateTimeOffset now)
    {
        var net = qty * unitPrice;
        var vat = net * 0.22m;
        var gross = net + vat;

        return new UpdLine
        {
            Id = Guid.NewGuid(),
            UpdId = updId,
            ItemId = item.Id,
            WarehouseId = warehouseId,
            NameSnapshot = item.Name,
            VatRateSnapshot = item.VatRate,
            Qty = qty,
            UnitPrice = unitPrice,
            DiscountType = DiscountType.None,
            DiscountValue = 0,
            Net = net,
            Vat = vat,
            Gross = gross,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    private static void RecalcUpdTotals(Upd upd)
    {
        upd.TotalNet = upd.Lines.Sum(x => x.Net);
        upd.TotalVat = upd.Lines.Sum(x => x.Vat);
        upd.TotalGross = upd.Lines.Sum(x => x.Gross);
    }
}

public sealed record SeedTestCompanyDto(string? CompanyName, bool Force);
