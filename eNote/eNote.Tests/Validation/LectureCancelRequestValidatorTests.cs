using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Validation.Academic;

namespace eNote.Tests.Validation;

public sealed class LectureCancelRequestValidatorTests
{
    private readonly LectureCancelRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsMissingReason()
    {
        var result = _validator.Validate(new LectureCancelRequest { Reason = "   " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LectureCancelRequest.Reason) && e.ErrorMessage == Messages.LectureCancelReasonRequired);
    }

    [Fact]
    public void Validate_RejectsReasonShorterThanThreeCharacters()
    {
        var result = _validator.Validate(new LectureCancelRequest { Reason = "ab" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LectureCancelRequest.Reason) && e.ErrorMessage == Messages.LectureCancelReasonLength);
    }

    [Fact]
    public void Validate_RejectsReasonThatIsTooShortAfterTrimming()
    {
        var result = _validator.Validate(new LectureCancelRequest { Reason = "  a " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LectureCancelRequest.Reason) && e.ErrorMessage == Messages.LectureCancelReasonLength);
    }

    [Fact]
    public void Validate_RejectsReasonLongerThanFiveHundredCharacters()
    {
        var result = _validator.Validate(new LectureCancelRequest { Reason = new string('a', 501) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LectureCancelRequest.Reason) && e.ErrorMessage == Messages.LectureCancelReasonLength);
    }

    [Fact]
    public void Validate_AcceptsReasonWithinBounds()
    {
        var result = _validator.Validate(new LectureCancelRequest { Reason = "Instrument je pokvaren" });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
