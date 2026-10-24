using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class LectureCancelRequestValidator : AbstractValidator<LectureCancelRequest>
{
    public LectureCancelRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(Messages.LectureCancelReasonRequired)
            .Must(reason => reason.Trim().Length is >= 3 and <= 500).WithMessage(Messages.LectureCancelReasonLength);
    }
}
