using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UserProvisionRequestValidator : AbstractValidator<UserProvisionRequest>
{
    public UserProvisionRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Ispravna email adresa je obavezna.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.");
        RuleFor(x => x.Role).NotEmpty().Must(BeKnownRole).WithMessage("Nepoznata uloga.");
        RuleFor(x => x.MusicStoreId).GreaterThan(0).When(x => x.MusicStoreId.HasValue).WithMessage("MusicStoreId mora biti veći od 0.");
    }

    private static bool BeKnownRole(string role) =>
        role is AppRoles.Administrator or AppRoles.Instructor or AppRoles.Student or AppRoles.StoreEmployee;
}
