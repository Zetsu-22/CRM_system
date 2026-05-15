using CrmSystem.Application.Orders;
using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Domain;
using FluentAssertions;

namespace CrmSystem.Tests.Unit.Application;

public sealed class OrderStatusTransitionsTests
{
    // ── IsFinal / IsLocked ──────────────────────────────────────────────
    [Theory]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Processing, false)]
    public void IsFinal_Returns_Correct(OrderStatus s, bool expected) =>
        OrderStatusTransitions.IsFinal(s).Should().Be(expected);

    [Theory]
    [InlineData(OrderStatus.DeliveringByTransport, true)]
    [InlineData(OrderStatus.Delivering, true)]
    [InlineData(OrderStatus.AwaitingPickup, true)]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Processing, false)]
    [InlineData(OrderStatus.Picking, false)]
    public void IsLocked_Returns_Correct(OrderStatus s, bool expected) =>
        OrderStatusTransitions.IsLocked(s).Should().Be(expected);

    // ── selfPickup branch: после Picking → AwaitingPickup ──────────────
    [Fact]
    public void SelfPickup_Skips_Transport_From_Picking() =>
        OrderStatusTransitions.GetNextStatus(OrderStatus.Picking, DeliveryTypes.SelfPickup, false)
            .Should().Be(OrderStatus.AwaitingPickup);

    // ── companyDelivery branch ─────────────────────────────────────────
    [Fact]
    public void CompanyDelivery_Picking_Goes_To_Delivering() =>
        OrderStatusTransitions.GetNextStatus(OrderStatus.Picking, DeliveryTypes.CompanyDelivery, false)
            .Should().Be(OrderStatus.Delivering);

    // ── transportCompany branch ────────────────────────────────────────
    [Fact]
    public void TransportCompany_DeliveringByTransport_Goes_To_AwaitingPickup() =>
        OrderStatusTransitions.GetNextStatus(OrderStatus.DeliveringByTransport, DeliveryTypes.TransportCompany, false)
            .Should().Be(OrderStatus.AwaitingPickup);

    // ── Manufacturing появляется только при дефиците ───────────────────
    // BillConfirmed → next по enum это Manufacturing.
    [Fact]
    public void BillConfirmed_Without_Shortage_Skips_Manufacturing()
    {
        var next = OrderStatusTransitions.GetNextStatus(OrderStatus.BillConfirmed, DeliveryTypes.SelfPickup, needsManufacturing: false);
        next.Should().Be(OrderStatus.Picking);
    }

    [Fact]
    public void BillConfirmed_With_Shortage_Goes_To_Manufacturing()
    {
        var next = OrderStatusTransitions.GetNextStatus(OrderStatus.BillConfirmed, DeliveryTypes.SelfPickup, needsManufacturing: true);
        next.Should().Be(OrderStatus.Manufacturing);
    }

    // ── Линейный переход ───────────────────────────────────────────────
    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.WaitingForPayment)]
    [InlineData(OrderStatus.WaitingForPayment, OrderStatus.BillConfirmed)]
    public void Linear_Transition(OrderStatus from, OrderStatus expected) =>
        OrderStatusTransitions.GetNextStatus(from, null, false).Should().Be(expected);

    // ── Финальные статусы ──────────────────────────────────────────────
    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Final_Status_Has_No_Next(OrderStatus s) =>
        OrderStatusTransitions.GetNextStatus(s, null, false).Should().BeNull();
}
