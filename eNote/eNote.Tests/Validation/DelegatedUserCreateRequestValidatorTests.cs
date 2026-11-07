using eNote.Application.Features.Identity.Users;
using eNote.Application.Validation.Identity;

namespace eNote.Tests.Validation;

public sealed class DelegatedUserCreateRequestValidatorTests
{
    private static DelegatedUserCreateRequest Request(string firstName, string lastName) => new()
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
        var result = new DelegatedUserCreateRequestValidator().Validate(Request(firstName, lastName));

        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_AcceptsNames()
    {
        var result = new DelegatedUserCreateRequestValidator().Validate(Request("Jane", "Doe"));

        Assert.True(result.IsValid);
    }
}
