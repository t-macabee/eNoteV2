using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Validation.Identity;

namespace eNote.Tests.Validation;

public sealed class SendMembershipWarningRequestValidatorTests
{
    private readonly SendMembershipWarningRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsEmptyMessage()
    {
        var result = _validator.Validate(new SendMembershipWarningRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsMessageLongerThanFiveHundredCharacters()
    {
        var result = _validator.Validate(new SendMembershipWarningRequest { Message = new string('a', 501) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendMembershipWarningRequest.Message) && e.ErrorMessage == Messages.MembershipWarningMessageTooLong);
    }
}
