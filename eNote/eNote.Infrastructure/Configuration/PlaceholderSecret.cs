namespace eNote.Infrastructure.Configuration;

internal static class PlaceholderSecret
{
    public static bool IsPlaceholder(string value) =>
        value.Contains("change-this-in-production", StringComparison.OrdinalIgnoreCase)
        || value.Contains("fake_local_dev_placeholder", StringComparison.OrdinalIgnoreCase)
        || value == "ThisIsASecretKeyThatIsAtLeast32CharactersLong!"
        || value.StartsWith("sk_test_...", StringComparison.Ordinal)
        || value.StartsWith("whsec_...", StringComparison.Ordinal);
}
