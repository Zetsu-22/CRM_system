using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Pricing;

/// <summary>
/// Чистые функции расчёта сумм для строк и заказа целиком.
/// Не зависит ни от EF Core, ни от DTO — принимает примитивы и возвращает структуры.
/// </summary>
public static class OrderTotalsCalculator
{
    /// <summary>Считает Net/Vat/Gross для одной позиции (НДС включён сверху).</summary>
    public static LineTotals CalculateLine(decimal qty, decimal unitPrice, VatRate vatRate)
    {
        var net = qty * unitPrice;
        var rate = (int)vatRate / 100m;
        var vat = net * rate;
        return new LineTotals(net, vat, net + vat);
    }

    /// <summary>Суммирует строки заказа в общие итоги.</summary>
    public static OrderTotals CalculateOrder(IEnumerable<LineTotals> lines)
    {
        decimal totalNet = 0, totalVat = 0, totalGross = 0;
        foreach (var l in lines)
        {
            totalNet   += l.Net;
            totalVat   += l.Vat;
            totalGross += l.Gross;
        }
        return new OrderTotals(totalNet, totalVat, totalGross);
    }
}

public readonly record struct LineTotals(decimal Net, decimal Vat, decimal Gross);
public readonly record struct OrderTotals(decimal TotalNet, decimal TotalVat, decimal TotalGross);
