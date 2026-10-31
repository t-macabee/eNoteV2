using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public sealed class RentalOptions
{
    public const string SectionName = "Rentals";

    [Range(typeof(decimal), "0.01", "10", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Rentals__OverduePenaltyMultiplier must be between 0.01 and 10.")]
    public decimal OverduePenaltyMultiplier { get; set; }
}
