using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class EnrollmentReasonRequestValidator : AbstractValidator<EnrollmentReasonRequest>
{
    public EnrollmentReasonRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(Enrollment.DecisionNoteMaxLength);
    }
}
