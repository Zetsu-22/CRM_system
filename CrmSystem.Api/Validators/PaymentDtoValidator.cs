using CrmSystem.Application.Payments.Dtos;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class PaymentDtoValidator : AbstractValidator<PaymentDto>
{
    public PaymentDtoValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Дата обязательна");
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
