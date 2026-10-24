using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Validation.Identity;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Validation;

public sealed class RecordMembershipPaymentRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly RecordMembershipPaymentRequestValidator _validator = new(new FixedClock(Now));

    private static RecordMembershipPaymentRequest ValidRequest() => new()
    {
        Amount = 50m,
        ReceiptNumber = "CL-2026-1",
        Months = 1
    };

    [Fact]
    public void Validate_AcceptsRequestWithMonths()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsNonPositiveAmount()
    {
        var request = ValidRequest();
        request.Amount = 0m;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.Amount) && e.ErrorMessage == Messages.MembershipAmountInvalid);
    }

    [Fact]
    public void Validate_RejectsMissingReceiptNumber()
    {
        var request = ValidRequest();
        request.ReceiptNumber = "   ";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.ReceiptNumber) && e.ErrorMessage == Messages.MembershipReceiptNumberRequired);
    }

    [Fact]
    public void Validate_RejectsReceiptNumberLongerThanFiftyCharacters()
    {
        var request = ValidRequest();
        request.ReceiptNumber = new string('1', 51);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.ReceiptNumber) && e.ErrorMessage == Messages.MembershipReceiptNumberLength);
    }

    [Fact]
    public void Validate_RejectsRequestWithoutPeriodEndOrMonths()
    {
        var request = ValidRequest();
        request.Months = null;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == Messages.MembershipPeriodSelectionRequired);
    }

    [Fact]
    public void Validate_RejectsRequestWithBothPeriodEndAndMonths()
    {
        var request = ValidRequest();
        request.PeriodEnd = new DateTime(2026, 9, 1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == Messages.MembershipPeriodSelectionRequired);
    }

    [Fact]
    public void Validate_RejectsMonthsOutsideRange()
    {
        var request = ValidRequest();
        request.Months = 25;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.Months) && e.ErrorMessage == Messages.MembershipMonthsRange);
    }

    [Fact]
    public void Validate_RejectsPeriodEndOutsideTheTenYearLimit()
    {
        var request = ValidRequest();
        request.Months = null;
        request.PeriodEnd = Now.Date.AddYears(10).AddDays(1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.PeriodEnd) && e.ErrorMessage == Messages.MembershipPeriodEndRange);
    }

    [Fact]
    public void Validate_RejectsPeriodEndBeforeToday()
    {
        var request = ValidRequest();
        request.Months = null;
        request.PeriodEnd = Now.Date.AddDays(-1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.PeriodEnd) && e.ErrorMessage == Messages.MembershipPeriodEndRange);
    }

    [Fact]
    public void Validate_RejectsPaidAtInTheFuture()
    {
        var request = ValidRequest();
        request.PaidAt = Now.Date.AddDays(1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.PaidAt) && e.ErrorMessage == Messages.MembershipPaidAtRange);
    }

    [Fact]
    public void Validate_RejectsPaidAtOlderThanTenYears()
    {
        var request = ValidRequest();
        request.PaidAt = Now.Date.AddYears(-10).AddDays(-1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.PaidAt) && e.ErrorMessage == Messages.MembershipPaidAtRange);
    }

    [Fact]
    public void Validate_AcceptsDatesAtTheLimits()
    {
        var request = ValidRequest();
        request.Months = null;
        request.PeriodEnd = Now.Date.AddYears(10);
        request.PaidAt = Now.Date.AddYears(-10);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsNoteLongerThanFiveHundredCharacters()
    {
        var request = ValidRequest();
        request.Note = new string('a', 501);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordMembershipPaymentRequest.Note) && e.ErrorMessage == Messages.MembershipNoteTooLong);
    }
}
