using eNote.Application.Features.ReferenceData.Addresses;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
    }
}
