using eNote.Application.Validation.Identity;
using eNote.Tests.TestUtils;
using FluentValidation;

namespace eNote.Tests.Validation;

public sealed class ValidatorCoverageTests
{
    [Fact]
    public void EveryValidatorInTheApplicationAssembly_IsRegisteredAndDefinesRules()
    {
        var assembly = typeof(LoginRequestValidator).Assembly;
        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IValidator)))
            .ToList();

        var registeredTypes = AssemblyScanner
            .FindValidatorsInAssemblyContaining<LoginRequestValidator>()
            .Select(r => r.ValidatorType)
            .ToHashSet();

        Assert.NotEmpty(validatorTypes);

        foreach (var validatorType in validatorTypes)
        {
            Assert.True(
                registeredTypes.Contains(validatorType),
                $"Validator is not registered by the assembly scan: {validatorType.FullName}");

            Assert.NotEmpty(Instantiate(validatorType).CreateDescriptor().Rules);
        }
    }

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
        : throw new InvalidOperationException($"Validator constructor dependency is not supported by the coverage test: {type.FullName}");
}
