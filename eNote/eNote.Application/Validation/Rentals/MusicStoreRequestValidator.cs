using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class MusicStoreRequestValidator : AbstractValidator<MusicStoreRequest>
{
    public MusicStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().WithMessage("Naziv prodavnice je obavezan.").MaximumLength(MusicStore.StoreNameMaxLength).WithMessage($"Naziv ne smije biti duži od {MusicStore.StoreNameMaxLength} karaktera.");
        RuleFor(x => x.BusinessHours).BusinessHours();
        RuleFor(x => x.PhoneNumber).PhoneNumber().When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.AddressId).GreaterThan(0).When(x => x.AddressId.HasValue);
    }
}
