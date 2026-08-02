using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class MarkAttendanceRequestValidator : AbstractValidator<MarkAttendanceRequest>
{
    public MarkAttendanceRequestValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("StudentId mora biti veći od 0.");
        RuleFor(x => x.AttendanceStatus).IsInEnum().WithMessage("Nepoznat status prisustva.");
    }
}
