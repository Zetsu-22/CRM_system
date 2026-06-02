using CrmSystem.Application.Orders.Dtos;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class SalesOrderDtoValidator : AbstractValidator<SalesOrderDto>
{
    public SalesOrderDtoValidator()
    {
        RuleFor(x => x.Number).MaximumLength(32);
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Дата обязательна");
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.CounterpartyId).NotEmpty();
        RuleFor(x => x.Lines).NotNull().Must(l => l.Count > 0).WithMessage("Заказ должен содержать хотя бы одну позицию");
        RuleForEach(x => x.Lines).SetValidator(new SalesOrderLineDtoValidator());
    }
}

public sealed class SalesOrderLineDtoValidator : AbstractValidator<SalesOrderLineDto>
{
    public SalesOrderLineDtoValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.NameSnapshot).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0);
    }
}
