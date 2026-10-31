using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(IdentityPolicy.UserNameMaxLength);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(IdentityPolicy.EmailMaxLength);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.FirstName).MaximumLength(IdentityPolicy.PersonNameMaxLength);
        RuleFor(x => x.LastName).MaximumLength(IdentityPolicy.PersonNameMaxLength);
    }
}
