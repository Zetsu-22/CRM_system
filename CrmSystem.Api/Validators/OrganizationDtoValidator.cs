using CrmSystem.Api.Controllers;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class OrganizationDtoValidator : AbstractValidator<OrganizationDto>
{
    public OrganizationDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Address).MaximumLength(1024);
    }
}
