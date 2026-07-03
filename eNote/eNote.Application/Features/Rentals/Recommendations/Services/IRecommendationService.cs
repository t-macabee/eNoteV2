namespace eNote.Application.Features.Rentals.Recommendations.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = 5, CancellationToken cancellationToken = default);
    Task RecordInstrumentViewAsync(int instrumentId, CancellationToken cancellationToken = default);
}
