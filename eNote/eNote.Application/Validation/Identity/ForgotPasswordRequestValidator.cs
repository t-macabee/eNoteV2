using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
