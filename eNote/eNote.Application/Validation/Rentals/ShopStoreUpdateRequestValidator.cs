using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class ShopStoreUpdateRequestValidator : AbstractValidator<ShopStoreUpdateRequest>
{
    public ShopStoreUpdateRequestValidator()
    {
        RuleFor(x => x.BusinessHours).BusinessHours();

        RuleFor(x => x.PhoneNumber)
            .PhoneNumber().When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
