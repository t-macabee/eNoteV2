using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class GradeAssignmentRequestValidator : AbstractValidator<GradeAssignmentRequest>
{
    public GradeAssignmentRequestValidator()
    {
        RuleFor(x => x.Grade).InclusiveBetween(0, 100).WithMessage(Messages.AssignmentInvalidGrade);
        RuleFor(x => x.Feedback).MaximumLength(1000).WithMessage(Messages.AssignmentFeedbackTooLong);
    }
}
