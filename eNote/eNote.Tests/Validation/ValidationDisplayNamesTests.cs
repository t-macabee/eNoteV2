using eNote.API.Extensions;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Validation;
using eNote.Application.Validation.Communication;
using eNote.Application.Validation.Identity;
using eNote.Tests.TestUtils;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace eNote.Tests.Validation;

public sealed class ValidationDisplayNamesTests : IDisposable
{
    private readonly CultureInfo? _originalCulture = ValidatorOptions.Global.LanguageManager.Culture;
    private readonly Func<Type, MemberInfo, LambdaExpression, string?>? _originalResolver = ValidatorOptions.Global.DisplayNameResolver;

    public static IEnumerable<object[]> Validators()
    {
        var assembly = typeof(LoginRequestValidator).Assembly;

        return assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && IsValidator(t))
            .OrderBy(t => t.FullName)
            .Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(Validators))]
    public void EveryRuleProperty_HasABosnianDisplayName(Type validatorType)
    {
        var validator = Instantiate(validatorType);

        foreach (var rule in validator.CreateDescriptor().Rules)
        {
            // Object-level rules (RuleFor(x => x)) carry no property name and keep their own message.
            if (string.IsNullOrEmpty(rule.PropertyName))
            {
                continue;
            }

            Assert.True(
                ValidationDisplayNames.DisplayNames.ContainsKey(rule.PropertyName),
                $"{validatorType.FullName} uses '{rule.PropertyName}' without a Bosnian display name.");
        }
    }

    [Fact]
    public void EmptyTitle_UsesTheBosnianPropertyName()
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("bs");
        ValidatorOptions.Global.DisplayNameResolver = ValidationDisplayNames.Resolve;

        var result = new AnnouncementRequestValidator().Validate(new AnnouncementRequest(string.Empty, "Sadržaj"));

        Assert.Contains("'Naslov' ne smije biti prazan.", result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void AddApplicationValidation_ConfiguresTheBosnianDisplayNameResolver()
    {
        new ServiceCollection().AddApplicationValidation();

        var result = new AnnouncementRequestValidator().Validate(new AnnouncementRequest(string.Empty, "Sadržaj"));

        Assert.Contains("'Naslov' ne smije biti prazan.", result.Errors.Select(e => e.ErrorMessage));
    }

    public void Dispose()
    {
        ValidatorOptions.Global.LanguageManager.Culture = _originalCulture;
        ValidatorOptions.Global.DisplayNameResolver = _originalResolver;
    }

    private static bool IsValidator(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

    private static IValidator Instantiate(Type validatorType)
    {
        var constructor = validatorType
            .GetConstructors()
            .MinBy(c => c.GetParameters().Length)
            ?? throw new InvalidOperationException($"Validator has no public constructor: {validatorType.FullName}");

        var arguments = constructor.GetParameters()
            .Select(p => CreateArgument(p.ParameterType))
            .ToArray();

        return (IValidator)constructor.Invoke(arguments);
    }

    private static object CreateArgument(Type type) => type == typeof(IClock)
        ? new FixedClock(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc))
        : throw new InvalidOperationException($"Validator constructor dependency is not supported by the display-name test: {type.FullName}");
}
