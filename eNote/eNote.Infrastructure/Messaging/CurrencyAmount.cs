namespace eNote.Infrastructure.Messaging;

internal static class CurrencyAmount
{
    private const string LocalCurrencyCode = "bam";
    private const string LocalCurrencyLabel = "KM";

    public static string Format(long cents, string currency) => $"{cents / 100m:F2} {Label(currency)}";

    public static string Local(decimal amount) => $"{amount:F2} {LocalCurrencyLabel}";

    private static string Label(string currency) =>
        string.IsNullOrEmpty(currency) || currency.Equals(LocalCurrencyCode, StringComparison.OrdinalIgnoreCase) ? LocalCurrencyLabel
        : currency.ToUpperInvariant();
}
