using CrmSystem.Application.Items.Dtos;
using FluentValidation;

namespace CrmSystem.Api.Validators;

public sealed class ItemDtoValidator : AbstractValidator<ItemDto>
{
    public ItemDtoValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Sku).MaximumLength(64);
        RuleFor(x => x.Barcode).MaximumLength(64);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(32);
    }
}
