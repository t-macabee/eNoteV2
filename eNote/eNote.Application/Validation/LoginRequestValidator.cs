using eNote.Application.Features.Auth;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Lozinka je obavezna.");
    }
}
