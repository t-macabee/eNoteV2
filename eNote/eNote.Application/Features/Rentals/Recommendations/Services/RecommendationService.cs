using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.Recommendations.Services;

public sealed class RecommendationService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IClock clock) : IRecommendationService
{
    private const double RentalWeight = 0.40;
    private const double ViewWeight = 0.30;
    private const double SimilarityWeight = 0.20;
    private const double PopularityWeight = 0.10;
    private const double OwnRentalHistoryWeight = 0.60;
    private const double CollaborativeRentalWeight = 0.40;
    private const double TypeViewFallbackScore = 0.35;
    private const int CandidatePoolSize = 80;

    public async Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        count = NormalizeCount(count);

        var studentId = await actor.GetCurrentStudentIdAsync();

        var userId = actor.UserId;

        var userRentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.StudentProfileId == studentId && (x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active || x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly))
            .Select(x => new UserRentalSnapshot(x.InstrumentId, x.Instrument.InstrumentTypeId, x.Instrument.Manufacturer))
            .ToListAsync(cancellationToken);

        HashSet<int> rentedInstrumentIds = [.. userRentals.Select(x => x.InstrumentId)];

        Dictionary<int, InstrumentViewSnapshot> viewMap = await context.Set<InstrumentView>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.InstrumentId, x => new InstrumentViewSnapshot(x.ViewCount, x.LastViewedAt), cancellationToken);

        Dictionary<int, int> globalRentalCounts = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => (x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active || x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly))
            .GroupBy(x => x.InstrumentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var maxGlobalRentals = globalRentalCounts.Values.DefaultIfEmpty(0).Max();

        var maxUserViews = viewMap.Values.Select(x => x.ViewCount).DefaultIfEmpty(0).Max();

        var userTypeCounts = userRentals
            .GroupBy(x => x.InstrumentTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var preferredTypeId = userTypeCounts
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .Cast<int?>()
            .FirstOrDefault();

        var preferredManufacturer = userRentals
            .GroupBy(x => x.Manufacturer)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var collaborativeInstrumentIds = await BuildCollaborativeInstrumentIdsAsync(studentId, rentedInstrumentIds, cancellationToken);

        var preferredTypeIds = userTypeCounts.Keys.ToList();

        var candidates = await LoadCandidateInstrumentsAsync(
            preferredTypeIds, collaborativeInstrumentIds, count, cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        List<ScoredRecommendation> scored = [];

        foreach (Instrument instrument in candidates)
        {
            var rentalScore = ComputeRentalScore(instrument, userTypeCounts, collaborativeInstrumentIds);
            var viewScore = ComputeViewScore(instrument, viewMap, maxUserViews, userTypeCounts);

            var similarityScore = ComputeSimilarityScore(instrument, preferredTypeId, preferredManufacturer);
            var popularityScore = maxGlobalRentals == 0 ? 0 : (double)globalRentalCounts.GetValueOrDefault(instrument.Id) / maxGlobalRentals;
            var totalScore = rentalScore * RentalWeight + viewScore * ViewWeight + similarityScore * SimilarityWeight + popularityScore * PopularityWeight;

            var reasons = BuildReasons(rentalScore, viewScore, similarityScore, popularityScore, instrument, preferredTypeId, collaborativeInstrumentIds);

            scored.Add(new ScoredRecommendation(instrument, totalScore, reasons));
        }

        return [.. scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Instrument.IsAvailable)
            .ThenBy(x => x.Instrument.Id)
            .Take(count)
            .Select(x => new InstrumentRecommendationDto
            {
                Instrument = mapper.Map<InstrumentDto>(x.Instrument),
                Score = Math.Round(x.Score, 4),
                Reasons = x.Reasons
            })];
    }

    public async Task RecordInstrumentViewAsync(int instrumentId, CancellationToken cancellationToken = default)
    {
        var instrumentExists = await context.Set<Instrument>()
            .AnyAsync(x => x.Id == instrumentId && x.IsActive, cancellationToken);

        if (!instrumentExists)
        {
            throw new NotFoundException(Messages.InstrumentNotFound);
        }

        var userId = actor.UserId;

        var now = clock.UtcNow;

        var view = await context.Set<InstrumentView>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.InstrumentId == instrumentId, cancellationToken);

        if (view is null)
        {
            context.Set<InstrumentView>().Add(new InstrumentView(userId, instrumentId, now));
        }
        else
        {
            view.RecordView(now);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Instrument>> LoadCandidateInstrumentsAsync(IReadOnlyList<int> preferredTypeIds, HashSet<int> collaborativeInstrumentIds, int count, CancellationToken cancellationToken)
    {
        var poolSize = Math.Max(count * 12, CandidatePoolSize);

        var popularIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => (x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active || x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly))
            .GroupBy(x => x.InstrumentId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(poolSize / 2)
            .ToListAsync(cancellationToken);

        List<int> preferredIds = preferredTypeIds.Count == 0 ? [] : await context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.IsActive && preferredTypeIds.Contains(x.InstrumentTypeId))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(poolSize / 2)
                .ToListAsync(cancellationToken);

        var candidateIds = preferredIds
            .Concat(collaborativeInstrumentIds)
            .Concat(popularIds)
            .Distinct()
            .Take(poolSize)
            .ToList();

        if (candidateIds.Count < count)
        {
            var fillerIds = await context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.IsActive && !candidateIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(poolSize - candidateIds.Count)
                .ToListAsync(cancellationToken);

            candidateIds.AddRange(fillerIds);
        }

        return await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .Where(x => candidateIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    private async Task<HashSet<int>> BuildCollaborativeInstrumentIdsAsync(int studentId, HashSet<int> rentedInstrumentIds, CancellationToken cancellationToken)
    {
        if (rentedInstrumentIds.Count == 0)
        {
            return [];
        }

        var similarStudentIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => rentedInstrumentIds.Contains(x.InstrumentId)
                && x.StudentProfileId != studentId
                && (x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active || x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly))
            .Select(x => x.StudentProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (similarStudentIds.Count == 0)
        {
            return [];
        }

        var collaborativeIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => similarStudentIds.Contains(x.StudentProfileId) && (x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active || x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly) && !rentedInstrumentIds.Contains(x.InstrumentId))
            .Select(x => x.InstrumentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. collaborativeIds];
    }

    private static double ComputeRentalScore(Instrument instrument, Dictionary<int, int> userTypeCounts, HashSet<int> collaborativeInstrumentIds)
    {
        double ownHistoryScore = 0;

        if (userTypeCounts.TryGetValue(instrument.InstrumentTypeId, out var typeCount) && typeCount > 0)
        {
            var maxTypeCount = userTypeCounts.Values.Max();

            ownHistoryScore = (double)typeCount / maxTypeCount;
        }

        var collaborativeScore = collaborativeInstrumentIds.Contains(instrument.Id) ? 1 : 0;

        if (ownHistoryScore > 0 && collaborativeScore > 0)
        {
            return ownHistoryScore * OwnRentalHistoryWeight + collaborativeScore * CollaborativeRentalWeight;
        }

        return Math.Max(ownHistoryScore, collaborativeScore);
    }

    private static double ComputeViewScore(Instrument instrument, Dictionary<int, InstrumentViewSnapshot> viewMap, int maxUserViews, Dictionary<int, int> userTypeCounts)
    {
        if (viewMap.TryGetValue(instrument.Id, out var directView) && maxUserViews > 0)
        {
            return (double)directView.ViewCount / maxUserViews;
        }

        if (userTypeCounts.ContainsKey(instrument.InstrumentTypeId))
        {
            return TypeViewFallbackScore;
        }

        return 0;
    }

    private static double ComputeSimilarityScore(Instrument instrument, int? preferredTypeId, string? preferredManufacturer)
    {
        if (preferredTypeId is null)
        {
            return 0;
        }

        if (string.Equals(instrument.Manufacturer, preferredManufacturer, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (instrument.InstrumentTypeId == preferredTypeId)
        {
            return 0.6;
        }

        return 0;
    }

    private static List<string> BuildReasons(double rentalScore, double viewScore, double similarityScore, double popularityScore, Instrument instrument, int? preferredTypeId, HashSet<int> collaborativeInstrumentIds)
    {
        List<string> reasons = [];

        if (rentalScore >= 0.5 && preferredTypeId == instrument.InstrumentTypeId)
        {
            reasons.Add($"Na osnovu vaše historije najma ({instrument.InstrumentType.Type}).");
        }

        if (collaborativeInstrumentIds.Contains(instrument.Id))
        {
            reasons.Add("Studenti sa sličnim izborima najma biraju ovaj instrument.");
        }

        if (viewScore >= 0.5)
        {
            reasons.Add("Pregledali ste ovaj instrument ili slične modele.");
        }

        if (similarityScore >= 0.6)
        {
            reasons.Add("Sličan vašim prethodnim izborima proizvođača ili vrste.");
        }

        if (popularityScore >= 0.5)
        {
            reasons.Add("Popularan među studentima.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Preporučeno na osnovu dostupnosti i ukupnog interesovanja.");
        }

        return reasons;
    }

    private static int NormalizeCount(int count) => count < 1 ? 1 : count > 20 ? 20 : count;

    private sealed record UserRentalSnapshot(int InstrumentId, int InstrumentTypeId, string Manufacturer);

    private sealed record InstrumentViewSnapshot(int ViewCount, DateTime LastViewedAt);

    private sealed record ScoredRecommendation(Instrument Instrument, double Score, List<string> Reasons);
}
