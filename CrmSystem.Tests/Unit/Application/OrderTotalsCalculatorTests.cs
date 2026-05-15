using CrmSystem.Application.Orders.Pricing;
using CrmSystem.Domain;
using FluentAssertions;

namespace CrmSystem.Tests.Unit.Application;

public sealed class OrderTotalsCalculatorTests
{
    [Theory]
    [InlineData(2, 100, VatRate.Vat22, 200, 44, 244)]
    [InlineData(1, 1000, VatRate.Vat10, 1000, 100, 1100)]
    [InlineData(3, 50, VatRate.Vat0, 150, 0, 150)]
    [InlineData(2, 100, VatRate.Vat5, 200, 10, 210)]
    public void CalculateLine_Computes_NetVatGross(decimal qty, decimal price, VatRate rate, decimal net, decimal vat, decimal gross)
    {
        var totals = OrderTotalsCalculator.CalculateLine(qty, price, rate);
        totals.Net.Should().Be(net);
        totals.Vat.Should().Be(vat);
        totals.Gross.Should().Be(gross);
    }

    [Fact]
    public void CalculateOrder_Sums_All_Lines()
    {
        var lines = new[]
        {
            new LineTotals(100m, 20m, 120m),
            new LineTotals(200m, 40m, 240m),
            new LineTotals(50m,  5m,  55m),
        };

        var totals = OrderTotalsCalculator.CalculateOrder(lines);

        totals.TotalNet.Should().Be(350m);
        totals.TotalVat.Should().Be(65m);
        totals.TotalGross.Should().Be(415m);
    }

    [Fact]
    public void CalculateOrder_Empty_Returns_Zeros()
    {
        var totals = OrderTotalsCalculator.CalculateOrder(Array.Empty<LineTotals>());
        totals.TotalNet.Should().Be(0);
        totals.TotalVat.Should().Be(0);
        totals.TotalGross.Should().Be(0);
    }
}
