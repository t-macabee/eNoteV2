using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class EnrollmentRejectRequestValidator : AbstractValidator<EnrollmentRejectRequest>
{
    public EnrollmentRejectRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
