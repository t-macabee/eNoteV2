using eNote.API.Configuration;
using eNote.API.Extensions;
using eNote.Application.Features.Payments;
using eNote.Infrastructure.Payments.Stripe;
using eNote.Infrastructure.Identity;
using eNote.Worker.Rentals;
using System.Reflection;
using System.Text.RegularExpressions;

namespace eNote.Tests.Configuration;

public sealed class EnvExampleTests
{
    private const string ExampleFileName = ".env.docker.example";
    private const string ComposeFileName = "docker-compose.yml";

    private static readonly HashSet<string> ExternalKeys =
    [
        "MSSQL_SA_PASSWORD",
        "STRIPE_PUBLISHABLE_KEY"
    ];

    [Fact]
    public void EveryOptionKey_IsInTheExample()
    {
        var exampleKeys = ReadActiveKeys(ExamplePath()).ToHashSet(StringComparer.Ordinal);

        var missing = new List<string>();

        foreach (var option in ReadCodeOptionProperties())
        {
            if (option.Key == "Seed__Enabled")
            {
                continue;
            }

            var required = option.IsArray ? $"{option.Key}__0" : option.Key;

            if (!exampleKeys.Contains(required))
            {
                missing.Add(required);
            }
        }

        Assert.True(missing.Count == 0, $"Keys the code reads but {ExampleFileName} does not define: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryExampleKey_IsReadByTheCode()
    {
        var codeKeys = ReadCodeOptionProperties()
            .Select(option => option.Key)
            .ToHashSet(StringComparer.Ordinal);

        var unknown = ReadActiveKeys(ExamplePath())
            .Select(NormalizeKey)
            .Where(key => !codeKeys.Contains(key) && !ExternalKeys.Contains(key))
            .Distinct()
            .ToList();

        Assert.True(unknown.Count == 0, $"Keys in {ExampleFileName} that the code does not read: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void EveryComposeVariable_IsInTheExample()
    {
        var examplePath = ExamplePath();
        var declaredKeys = ReadActiveKeys(examplePath)
            .Concat(ReadCommentedKeys(examplePath))
            .ToHashSet(StringComparer.Ordinal);

        var compose = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ComposeFileName));
        var usedKeys = Regex.Matches(compose, @"\$\{([A-Z_][A-Z0-9_]*)")
            .Select(match => match.Groups[1].Value)
            .Distinct();

        var missing = usedKeys.Where(key => !declaredKeys.Contains(key)).ToList();

        Assert.True(missing.Count == 0, $"Variables used by {ComposeFileName} but missing from {ExampleFileName}: {string.Join(", ", missing)}");
    }

    private static string ExamplePath() => Path.Combine(FindRepositoryRoot(), ExampleFileName);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ExampleFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find {ExampleFileName} above {AppContext.BaseDirectory}.");
    }

    private static List<string> ReadActiveKeys(string path)
    {
        var keys = new List<string>();

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');

            if (separator < 0)
            {
                continue;
            }

            keys.Add(trimmed[..separator].Trim());
        }

        return keys;
    }

    private static List<string> ReadCommentedKeys(string path) =>
        [.. File.ReadLines(path)
            .Select(line => Regex.Match(line, @"^#\s*([A-Z_][A-Z0-9_]*)="))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)];

    private static List<(string Key, bool IsArray)> ReadCodeOptionProperties()
    {
        var assemblies = new[]
        {
            typeof(CorsOriginsOptions).Assembly,
            typeof(PaymentOptions).Assembly,
            typeof(StripeOptions).Assembly,
            typeof(JwtOptions).Assembly,
            typeof(RentalPickupExpiryService).Assembly
        };

        var options = new List<(string Key, bool IsArray)>();

        foreach (var type in assemblies.Distinct().SelectMany(assembly => assembly.GetTypes()))
        {
            if (!type.IsPublic || type.IsAbstract)
            {
                continue;
            }

            var sectionField = type.GetField("SectionName", BindingFlags.Public | BindingFlags.Static);

            if (sectionField is null || !sectionField.IsLiteral || sectionField.FieldType != typeof(string))
            {
                continue;
            }

            var sectionName = (string)sectionField.GetRawConstantValue()!;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.SetMethod is null || !property.SetMethod.IsPublic)
                {
                    continue;
                }

                options.Add(($"{sectionName}__{property.Name}", property.PropertyType == typeof(string[])));
            }
        }

        return options;
    }

    private static string NormalizeKey(string key) => Regex.Replace(key, @"__\d+$", string.Empty);
}
