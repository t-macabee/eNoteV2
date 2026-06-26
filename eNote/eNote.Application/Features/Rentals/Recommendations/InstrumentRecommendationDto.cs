using eNote.Application.Features.Rentals.Instruments;

namespace eNote.Application.Features.Rentals.Recommendations;

public class InstrumentRecommendationDto
{
    public InstrumentDto Instrument { get; set; } = null!;
    public double Score { get; set; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}
