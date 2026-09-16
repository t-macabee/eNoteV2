using FluentValidation;

namespace eNote.Application.Validation.Identity;

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 znakova.")
            .Matches(@"\d").WithMessage("Lozinka mora sadržavati najmanje jednu cifru.")
            .Matches(@"\p{Lu}").WithMessage("Lozinka mora sadržavati najmanje jedno veliko slovo.")
            .Matches(@"\p{Ll}").WithMessage("Lozinka mora sadržavati najmanje jedno malo slovo.")
            .Matches(@"[^\p{L}\p{Nd}]").WithMessage("Lozinka mora sadržavati najmanje jedan specijalni znak.");
    }
}
