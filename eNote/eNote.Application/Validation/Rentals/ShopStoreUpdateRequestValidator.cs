using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class ShopStoreUpdateRequestValidator : AbstractValidator<ShopStoreUpdateRequest>
{
    public ShopStoreUpdateRequestValidator()
    {
        RuleFor(x => x.BusinessHours)
            .NotEmpty().WithMessage("Radno vrijeme je obavezno (npr. 09:00-17:00).")
            .MaximumLength(50).WithMessage("Radno vrijeme ne smije biti duže od 50 karaktera.");

        RuleFor(x => x.PhoneNumber)
            .PhoneNumber().When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
