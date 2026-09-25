using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

// 500 = ReasonPromptDialog maxLength (UI/enote_desktop/lib/widgets/reason_prompt_dialog.dart:44)
public sealed class EnrollmentRejectRequestValidator : AbstractValidator<EnrollmentRejectRequest>
{
    public EnrollmentRejectRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
