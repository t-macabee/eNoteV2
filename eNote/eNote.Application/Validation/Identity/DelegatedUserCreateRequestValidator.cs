using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class DelegatedUserCreateRequestValidator : AbstractValidator<DelegatedUserCreateRequest>
{
    public DelegatedUserCreateRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.").MaximumLength(256).WithMessage("Korisničko ime ne smije biti duže od 256 znakova.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Ispravna email adresa je obavezna.").MaximumLength(256).WithMessage("Email adresa ne smije biti duža od 256 znakova.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.");
        RuleFor(x => x.FirstName).MaximumLength(256);
        RuleFor(x => x.LastName).MaximumLength(256);
    }
}
