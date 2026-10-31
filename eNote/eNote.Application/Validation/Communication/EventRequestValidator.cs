using eNote.Application.Features.Communication.Events;
using FluentValidation;

namespace eNote.Application.Validation.Communication;

public sealed class EventRequestValidator : AbstractValidator<EventRequest>
{
    public EventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Naslov je obavezan.").MaximumLength(Event.TitleMaxLength).WithMessage($"Naslov ne smije biti duži od {Event.TitleMaxLength} karaktera.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Opis je obavezan.").MaximumLength(Event.DescriptionMaxLength).WithMessage($"Opis ne smije biti duži od {Event.DescriptionMaxLength} karaktera.");
        RuleFor(x => x.StartsAt).NotEmpty().WithMessage("Vrijeme početka je obavezno.");
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage(Messages.EventEndsBeforeStarts)
            .When(x => x.EndsAt.HasValue);
        RuleFor(x => x.AddressId).GreaterThan(0).When(x => x.AddressId.HasValue);
    }
}
