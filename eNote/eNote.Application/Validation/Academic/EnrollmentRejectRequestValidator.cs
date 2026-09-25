using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

// 500 = _NotePromptDialog maxLength (UI/enote_desktop/lib/features/store_employee/rental/rental_transition_action_row.dart:196)
public sealed class EnrollmentRejectRequestValidator : AbstractValidator<EnrollmentRejectRequest>
{
    public EnrollmentRejectRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
