using System.Text.RegularExpressions;
using FluentValidation;

namespace eNote.Application.Validation.Rentals;

public static class BusinessHoursRules
{
    private static readonly Regex Pattern = new(@"^([01]\d|2[0-3]):[0-5]\d\s*-\s*([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string?> BusinessHours<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Radno vrijeme je obavezno (npr. 09:00-17:00).")
            .MaximumLength(MusicStore.BusinessHoursMaxLength).WithMessage($"Radno vrijeme ne smije biti duže od {MusicStore.BusinessHoursMaxLength} karaktera.")
            .Must(v => string.IsNullOrWhiteSpace(v) || Pattern.IsMatch(v)).WithMessage(Messages.BusinessHoursFormat);
    }
}
