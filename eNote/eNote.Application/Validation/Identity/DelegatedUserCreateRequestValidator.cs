using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class DelegatedUserCreateRequestValidator : AbstractValidator<DelegatedUserCreateRequest>
{
    public DelegatedUserCreateRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.").MaximumLength(IdentityPolicy.UserNameMaxLength).WithMessage($"Korisničko ime ne smije biti duže od {IdentityPolicy.UserNameMaxLength} znakova.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Ispravna email adresa je obavezna.").MaximumLength(IdentityPolicy.EmailMaxLength).WithMessage($"Email adresa ne smije biti duža od {IdentityPolicy.EmailMaxLength} znakova.");
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.FirstName).MaximumLength(IdentityPolicy.PersonNameMaxLength);
        RuleFor(x => x.LastName).MaximumLength(IdentityPolicy.PersonNameMaxLength);
    }
}
