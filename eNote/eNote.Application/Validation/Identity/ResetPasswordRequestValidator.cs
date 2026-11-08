using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage(Messages.EmailFormat);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).Password();
    }
}
