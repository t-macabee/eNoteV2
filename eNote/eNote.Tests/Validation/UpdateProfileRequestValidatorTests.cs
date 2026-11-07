using eNote.Application.Features.Identity.Users;
using eNote.Application.Validation.Identity;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Validation;

public sealed class UpdateProfileRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly UpdateProfileRequestValidator _validator = new(new FixedClock(Now));

    [Fact]
    public void Validate_RefusesAFutureBirthDate_WithAPlainMessage()
    {
        var result = _validator.Validate(new UpdateProfileRequest
        {
            Email = "a@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateTime(2026, 7, 2)
        });

        var error = Assert.Single(result.Errors);
        Assert.Equal("Datum rođenja ne smije biti u budućnosti.", error.ErrorMessage);
    }

    [Fact]
    public void Validate_AcceptsABirthDateOfToday()
    {
        var result = _validator.Validate(new UpdateProfileRequest
        {
            Email = "a@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateTime(2026, 7, 1)
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Doe", "Ime je obavezno.")]
    [InlineData("   ", "Doe", "Ime je obavezno.")]
    [InlineData("Jane", "", "Prezime je obavezno.")]
    [InlineData("Jane", "   ", "Prezime je obavezno.")]
    public void Validate_RefusesAnEmptyName(string firstName, string lastName, string expectedMessage)
    {
        var result = _validator.Validate(new UpdateProfileRequest
        {
            Email = "a@example.com",
            FirstName = firstName,
            LastName = lastName
        });

        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedMessage);
    }
}
