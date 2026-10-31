using FluentValidation;

namespace eNote.Tests.TestUtils;

public static class ValidatorFactory
{
    public static IValidator Instantiate(Type validatorType)
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
        : throw new InvalidOperationException($"Validator constructor dependency is not supported by the validator tests: {type.FullName}");
}
