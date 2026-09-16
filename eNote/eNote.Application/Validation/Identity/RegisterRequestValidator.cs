using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.FirstName).MaximumLength(256);
        RuleFor(x => x.LastName).MaximumLength(256);
    }
}
