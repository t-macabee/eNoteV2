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

            Assert.NotEmpty(ValidatorFactory.Instantiate(validatorType).CreateDescriptor().Rules);
        }
    }
}
