using eNote.Application.Features.Communication.Events;
using FluentValidation;

namespace eNote.Application.Validation.Communication;

public sealed class EventRequestValidator : AbstractValidator<EventRequest>
{
    public EventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Naslov je obavezan.").MaximumLength(150).WithMessage("Naslov ne smije biti duži od 150 karaktera.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Opis je obavezan.").MaximumLength(4000).WithMessage("Opis ne smije biti duži od 4000 karaktera.");
        RuleFor(x => x.StartsAt).NotEmpty().WithMessage("Vrijeme početka je obavezno.");
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Vrijeme završetka mora biti nakon vremena početka.")
            .When(x => x.EndsAt.HasValue);
        RuleFor(x => x.AddressId).GreaterThan(0).When(x => x.AddressId.HasValue);
        RuleFor(x => x.CourseId).GreaterThan(0).When(x => x.CourseId.HasValue);
        RuleFor(x => x.InstructorId).GreaterThan(0).When(x => x.InstructorId.HasValue);
    }
}
