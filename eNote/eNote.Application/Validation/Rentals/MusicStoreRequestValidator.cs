using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public sealed class MusicStoreRequestValidator : AbstractValidator<MusicStoreRequest>
{
    public MusicStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().WithMessage("Naziv prodavnice je obavezan.").MaximumLength(100).WithMessage("Naziv ne smije biti duži od 100 karaktera.");
        RuleFor(x => x.BusinessHours).NotEmpty().WithMessage("Radno vrijeme je obavezno (npr. 09:00-17:00).").MaximumLength(50).WithMessage("Radno vrijeme ne smije biti duže od 50 karaktera.");
    }
}
