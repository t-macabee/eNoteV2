using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(IdentityPolicy.UserNameMaxLength).Must(IdentityPolicy.HasOnlyAllowedUserNameCharacters).WithMessage(Messages.UsernameInvalidCharacters);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage(Messages.EmailFormat).MaximumLength(IdentityPolicy.EmailMaxLength);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(Messages.FirstNameRequired).MaximumLength(IdentityPolicy.PersonNameMaxLength).WithMessage(Messages.FirstNameTooLong);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(Messages.LastNameRequired).MaximumLength(IdentityPolicy.PersonNameMaxLength).WithMessage(Messages.LastNameTooLong);
    }
}
