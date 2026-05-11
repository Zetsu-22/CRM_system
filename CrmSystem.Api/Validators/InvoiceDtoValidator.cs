using CrmSystem.Api.Controllers;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class InvoiceDtoValidator : AbstractValidator<InvoiceDto>
{
    public InvoiceDtoValidator()
    {
        RuleFor(x => x.Number).MaximumLength(32);
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Дата обязательна");
        RuleFor(x => x.CounterpartyId).NotEmpty();
        RuleFor(x => x.Lines).NotNull().Must(l => l.Count > 0).WithMessage("Счёт должен содержать хотя бы одну позицию");
        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineDtoValidator());
    }
}

public sealed class InvoiceLineDtoValidator : AbstractValidator<InvoiceLineDto>
{
    public InvoiceLineDtoValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.NameSnapshot).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0);
    }
}
