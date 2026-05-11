using CrmSystem.Api.Controllers;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class UpdDtoValidator : AbstractValidator<UpdDto>
{
    public UpdDtoValidator()
    {
        RuleFor(x => x.Number).MaximumLength(32);
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Дата обязательна");
        RuleFor(x => x.CounterpartyId).NotEmpty();
        RuleFor(x => x.Lines).NotNull().Must(l => l.Count > 0).WithMessage("УПД должен содержать хотя бы одну позицию");
        RuleForEach(x => x.Lines).SetValidator(new UpdLineDtoValidator());
    }
}

public sealed class UpdLineDtoValidator : AbstractValidator<UpdLineDto>
{
    public UpdLineDtoValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.NameSnapshot).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Qty).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
