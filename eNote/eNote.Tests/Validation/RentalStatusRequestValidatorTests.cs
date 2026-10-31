using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Validation.Rentals;

namespace eNote.Tests.Validation;

public sealed class RentalStatusRequestValidatorTests
{
    private readonly RentalStatusRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsNoteOf500Characters()
    {
        var request = new RentalStatusRequest
        {
            Note = new string('a', 500)
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsNoteOf501Characters()
    {
        var request = new RentalStatusRequest
        {
            Note = new string('a', 501)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RentalStatusRequest.Note));
    }
}
