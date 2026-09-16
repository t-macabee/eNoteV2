using eNote.Application.Common.Localization;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UpdateMembershipRequestValidator : AbstractValidator<UpdateMembershipRequest>
{
    public UpdateMembershipRequestValidator(IClock clock)
    {
        RuleFor(x => x.PaidUntil)
            .GreaterThan(_ => clock.UtcNow)
            .When(x => x.PaidUntil.HasValue)
            .WithMessage(Messages.MembershipPaidUntilFuture);
    }
}
