using eNote.Application.Features.Identity.Students;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class SendMembershipWarningRequestValidator : AbstractValidator<SendMembershipWarningRequest>
{
    public SendMembershipWarningRequestValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage(Messages.MembershipWarningMessageTooLong);
    }
}
