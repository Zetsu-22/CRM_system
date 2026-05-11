using CrmSystem.Api.Controllers;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class CompanyDtoValidator : AbstractValidator<CompanyDto>
{
    public CompanyDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Inn).Matches(@"^\d{10}$|^\d{12}$").When(x => !string.IsNullOrWhiteSpace(x.Inn))
            .WithMessage("ИНН должен быть 10 или 12 цифр");
        RuleFor(x => x.Kpp).Matches(@"^\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.Kpp))
            .WithMessage("КПП должен быть 9 цифр");
        RuleFor(x => x.Ogrn).Matches(@"^\d{13}$|^\d{15}$").When(x => !string.IsNullOrWhiteSpace(x.Ogrn))
            .WithMessage("ОГРН должен быть 13 или 15 цифр");
        RuleFor(x => x.Bik).Matches(@"^\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.Bik))
            .WithMessage("БИК должен быть 9 цифр");
    }
}
