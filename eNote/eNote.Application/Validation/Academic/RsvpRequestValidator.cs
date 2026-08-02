using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class RsvpRequestValidator : AbstractValidator<RsvpRequest>
{
    public RsvpRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("Napomena može imati najviše 500 znakova.");
    }
}
