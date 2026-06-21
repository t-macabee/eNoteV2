using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Instruments;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Recommendations.Services;

public sealed class RecommendationService(IAppDbContext context, IMapper mapper, ICurrentUserService currentUserService, IUserContextResolver resolver, IClock clock) : IRecommendationService
{
    private const double RentalWeight = 0.40;
    private const double ViewWeight = 0.30;
    private const double SimilarityWeight = 0.20;
    private const double PopularityWeight = 0.10;

    public async Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        count = NormalizeCount(count);

        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var userId = currentUserService.UserId;

        var candidates = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var userRentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.StudentProfileId == student.Id && InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .Select(x => new UserRentalSnapshot(x.InstrumentId, x.Instrument.InstrumentTypeId, x.Instrument.Manufacturer))
            .ToListAsync(cancellationToken);

        HashSet<int> rentedInstrumentIds = [.. userRentals.Select(x => x.InstrumentId)];

        Dictionary<int, InstrumentViewSnapshot> viewMap = await context.Set<InstrumentView>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.InstrumentId, x => new InstrumentViewSnapshot(x.ViewCount, x.LastViewedAt), cancellationToken);

        Dictionary<int, int> globalRentalCounts = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
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

        var collaborativeInstrumentIds = await BuildCollaborativeInstrumentIdsAsync(student.Id, rentedInstrumentIds, cancellationToken);

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

        var userId = currentUserService.UserId;

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
                && InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .Select(x => x.StudentProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (similarStudentIds.Count == 0)
        {
            return [];
        }

        var collaborativeIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => similarStudentIds.Contains(x.StudentProfileId) && InstrumentRentalStatusSets.History.Contains(x.RentalStatus) && !rentedInstrumentIds.Contains(x.InstrumentId))
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
            return ownHistoryScore * 0.6 + collaborativeScore * 0.4;
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
            return 0.35;
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
            reasons.Add($"Na osnovu vaÅ¡e historije najma ({instrument.InstrumentType.Type}).");
        }

        if (collaborativeInstrumentIds.Contains(instrument.Id))
        {
            reasons.Add("Studenti sa sliÄnim izborima najma biraju ovaj instrument.");
        }

        if (viewScore >= 0.5)
        {
            reasons.Add("Pregledali ste ovaj instrument ili sliÄne modele.");
        }

        if (similarityScore >= 0.6)
        {
            reasons.Add("SliÄan vaÅ¡im prethodnim izborima proizvoÄ‘aÄa ili vrste.");
        }

        if (popularityScore >= 0.5)
        {
            reasons.Add("Popularan meÄ‘u studentima.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("PreporuÄeno na osnovu dostupnosti i ukupnog interesovanja.");
        }

        return reasons;
    }

    private static int NormalizeCount(int count) => count < 1 ? 1 : count > 20 ? 20 : count;

    private sealed record UserRentalSnapshot(int InstrumentId, int InstrumentTypeId, string Manufacturer);

    private sealed record InstrumentViewSnapshot(int ViewCount, DateTime LastViewedAt);

    private sealed record ScoredRecommendation(Instrument Instrument, double Score, List<string> Reasons);
}
