using System.Text.RegularExpressions;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public static class PhoneRules
{
    private static readonly Regex Pattern = new(@"^\+?[0-9]{6,15}$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(30).WithMessage(Messages.PhoneNumberTooLong)
            .Must(v => v is null || Pattern.IsMatch(v.Replace(" ", ""))).WithMessage(Messages.PhoneNumberFormat);
    }
}
