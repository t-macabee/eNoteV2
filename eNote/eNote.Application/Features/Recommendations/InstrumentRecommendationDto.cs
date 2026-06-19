using eNote.Application.Features.Instruments;

namespace eNote.Application.Features.Recommendations;

public class InstrumentRecommendationDto
{
    public InstrumentDto Instrument { get; set; } = null!;
    public double Score { get; set; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}
