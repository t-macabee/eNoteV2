using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UpdateMembershipRequestValidator : AbstractValidator<UpdateMembershipRequest>
{
    public UpdateMembershipRequestValidator()
    {
        RuleFor(x => x.PaidUntil)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.PaidUntil.HasValue)
            .WithMessage("PaidUntil mora biti u budućnosti.");
    }
}
