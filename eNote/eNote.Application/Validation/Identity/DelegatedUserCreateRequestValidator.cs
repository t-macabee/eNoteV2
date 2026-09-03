using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class DelegatedUserCreateRequestValidator : AbstractValidator<DelegatedUserCreateRequest>
{
    public DelegatedUserCreateRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Ispravna email adresa je obavezna.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.");
    }
}
