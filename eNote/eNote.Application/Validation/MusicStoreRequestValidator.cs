using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class MusicStoreRequestValidator : AbstractValidator<MusicStoreRequest>
{
    public MusicStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessHours).NotEmpty().MaximumLength(50);
    }
}
