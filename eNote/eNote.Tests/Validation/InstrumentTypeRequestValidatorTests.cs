using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Application.Validation.Rentals;

namespace eNote.Tests.Validation;

public sealed class InstrumentTypeRequestValidatorTests
{
    private readonly InstrumentTypeRequestValidator _validator = new();

    [Theory]
    [InlineData(30)]
    [InlineData(30.01)]
    [InlineData(45.50)]
    [InlineData(99999999.99)]
    public void Validate_AcceptsFeeWithinRange(decimal monthlyFee)
    {
        var request = new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = monthlyFee };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(29.99)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsFeeBelowMinimum(decimal monthlyFee)
    {
        var request = new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = monthlyFee };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentTypeRequest.MonthlyFee) && e.ErrorMessage == "Mjesečna naknada mora biti najmanje 30 KM.");
    }

    [Fact]
    public void Validate_RejectsFeeAboveMaximum()
    {
        var request = new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 100000000m };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentTypeRequest.MonthlyFee) && e.ErrorMessage == "Mjesečna naknada ne smije biti veća od 99999999.99.");
    }

    [Fact]
    public void Validate_RejectsFeeWithMoreThanTwoDecimals()
    {
        var request = new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45.999m };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentTypeRequest.MonthlyFee) && e.ErrorMessage == "Mjesečna naknada može imati najviše dvije decimale.");
    }

    [Fact]
    public void Validate_RejectsEmptyType()
    {
        var request = new InstrumentTypeRequest { Type = "", MonthlyFee = 30m };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentTypeRequest.Type));
    }

    [Fact]
    public void Validate_RejectsTypeLongerThanMaximum()
    {
        var request = new InstrumentTypeRequest { Type = new string('a', 101), MonthlyFee = 30m };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(InstrumentTypeRequest.Type));
    }
}
