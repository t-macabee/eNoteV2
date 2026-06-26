using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class GradeAssignmentRequestValidator : AbstractValidator<GradeAssignmentRequest>
{
    public GradeAssignmentRequestValidator()
    {
        RuleFor(x => x.Grade).InclusiveBetween(0, 100);
    }
}
