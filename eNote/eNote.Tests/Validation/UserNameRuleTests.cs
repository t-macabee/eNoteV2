using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Validation.Identity;

namespace eNote.Tests.Validation;

public sealed class UserNameRuleTests
{
    private const string ExpectedMessage = "Korisničko ime smije sadržavati samo slova, cifre i znakove - . _ @ +";

    private static RegisterRequest Register(string username) =>
        new() { Username = username, Email = "a@example.com", Password = "Password1!", FirstName = "Test", LastName = "User" };

    private static DelegatedUserCreateRequest Delegated(string username) =>
        new() { Username = username, Email = "a@example.com", Password = "Password1!", FirstName = "Test", LastName = "User" };

    private static UserProvisionRequest Provision(string username) =>
        new() { Username = username, Email = "a@example.com", Password = "Password1!", Role = "Student", FirstName = "Test", LastName = "User" };

    [Theory]
    [InlineData("ime prezime")]
    [InlineData("ime/prezime")]
    [InlineData("ime!")]
    public void Validators_RefuseAUserName_WithACharacterIdentityRefuses(string username)
    {
        Assert.Contains(new RegisterRequestValidator().Validate(Register(username)).Errors, e => e.ErrorMessage == ExpectedMessage);
        Assert.Contains(new DelegatedUserCreateRequestValidator().Validate(Delegated(username)).Errors, e => e.ErrorMessage == ExpectedMessage);
        Assert.Contains(new UserProvisionRequestValidator().Validate(Provision(username)).Errors, e => e.ErrorMessage == ExpectedMessage);
    }

    [Theory]
    [InlineData("đuro")]
    [InlineData("Čedomir.Šišić_1")]
    [InlineData("  jdoe  ")]
    public void Validators_AcceptAUserName_WithAllowedCharacters(string username)
    {
        Assert.True(new RegisterRequestValidator().Validate(Register(username)).IsValid);
        Assert.True(new DelegatedUserCreateRequestValidator().Validate(Delegated(username)).IsValid);
        Assert.True(new UserProvisionRequestValidator().Validate(Provision(username)).IsValid);
    }
}
