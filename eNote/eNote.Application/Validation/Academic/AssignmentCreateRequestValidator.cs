using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class AssignmentCreateRequestValidator : AbstractValidator<AssignmentCreateRequest>
{
    public AssignmentCreateRequestValidator(IClock clock)
    {
        Include(new AssignmentRequestValidator());

        RuleFor(x => x.DueAt)
            .GreaterThan(_ => clock.UtcNow)
            .WithMessage(Messages.AssignmentDueAtMustBeFuture);
    }
}
