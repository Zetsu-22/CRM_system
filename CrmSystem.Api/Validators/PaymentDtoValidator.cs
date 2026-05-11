using CrmSystem.Api.Controllers;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class PaymentDtoValidator : AbstractValidator<PaymentDto>
{
    public PaymentDtoValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Дата обязательна");
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
