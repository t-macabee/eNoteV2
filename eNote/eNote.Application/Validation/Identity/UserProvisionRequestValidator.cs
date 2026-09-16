using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UserProvisionRequestValidator : AbstractValidator<UserProvisionRequest>
{
    public UserProvisionRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.").MaximumLength(256).WithMessage("Korisničko ime ne smije biti duže od 256 znakova.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Ispravna email adresa je obavezna.").MaximumLength(256).WithMessage("Email adresa ne smije biti duža od 256 znakova.");
        RuleFor(x => x.FirstName).MaximumLength(256);
        RuleFor(x => x.LastName).MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.");
        RuleFor(x => x.Role).NotEmpty().Must(BeKnownRole).WithMessage("Nepoznata uloga.");
        RuleFor(x => x.MusicStoreId)
            .NotNull().WithMessage("Prodavnica je obavezna za uposlenika radnje.")
            .GreaterThan(0).WithMessage("MusicStoreId mora biti veći od 0.")
            .When(x => x.Role == AppRoles.StoreEmployee);
        RuleFor(x => x.MusicStoreId)
            .GreaterThan(0).WithMessage("MusicStoreId mora biti veći od 0.")
            .When(x => x.Role != AppRoles.StoreEmployee && x.MusicStoreId.HasValue);
    }

    private static bool BeKnownRole(string role) =>
        role is AppRoles.Administrator or AppRoles.Instructor or AppRoles.Student or AppRoles.StoreEmployee;
}
