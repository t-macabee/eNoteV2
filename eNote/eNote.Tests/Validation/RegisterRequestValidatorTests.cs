using eNote.Application.Features.Identity.Auth;
using eNote.Application.Validation.Identity;

namespace eNote.Tests.Validation;

public sealed class RegisterRequestValidatorTests
{
    private static RegisterRequest Request(string firstName, string lastName) => new()
    {
        Username = "jdoe",
        Email = "jdoe@example.com",
        Password = "Password1!",
        FirstName = firstName,
        LastName = lastName
    };

    [Theory]
    [InlineData("", "Doe", "Ime je obavezno.")]
    [InlineData("   ", "Doe", "Ime je obavezno.")]
    [InlineData("Jane", "", "Prezime je obavezno.")]
    [InlineData("Jane", "   ", "Prezime je obavezno.")]
    public void Validate_RefusesAnEmptyName(string firstName, string lastName, string expectedMessage)
    {
        var result = new RegisterRequestValidator().Validate(Request(firstName, lastName));

        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_RefusesANameLongerThanTheLimit_WithAMessageThatStatesTheLimit()
    {
        var tooLong = new string('a', IdentityPolicy.PersonNameMaxLength + 1);

        var result = new RegisterRequestValidator().Validate(Request(tooLong, "Doe"));

        Assert.Contains(result.Errors, e => e.ErrorMessage == $"Ime može imati najviše {IdentityPolicy.PersonNameMaxLength} znakova.");
    }

    [Fact]
    public void Validate_AcceptsNames()
    {
        var result = new RegisterRequestValidator().Validate(Request("Jane", "Doe"));

        Assert.True(result.IsValid);
    }
}
