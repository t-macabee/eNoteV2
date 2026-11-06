using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Rentals.Recommendations.Services;

public sealed class RecommendationService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser, IStudentContext students, IClock clock, ILogger<RecommendationService> logger)
{
    private const double RentalWeight = 0.40;
    private const double ViewWeight = 0.30;
    private const double SimilarityWeight = 0.20;
    private const double PopularityWeight = 0.10;
    private const double OwnRentalHistoryWeight = 0.60;
    private const double CollaborativeRentalWeight = 0.40;
    private const double TypeViewFallbackScore = 0.35;
    private const int CandidatePoolSize = 80;
    private const int ScoreDecimals = 4;

    public const int DefaultCount = 5;
    public const int MaxCount = 20;
    private const int PoolSizePerResult = 12;
    private const double TypeSimilarityScore = 0.6;
    private const double ReasonScoreThreshold = 0.5;

    private static readonly InstrumentRentalStatus[] CountedRentalStatuses =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    ];

    public async Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = DefaultCount, CancellationToken cancellationToken = default)
    {
        count = NormalizeCount(count);

        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var userId = currentUser.UserId;

        var recommendable = RecommendableInstruments(studentId);

        var userRentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.StudentProfileId == studentId && CountedRentalStatuses.Contains(x.RentalStatus))
            .Select(x => new UserRentalSnapshot(x.InstrumentId, x.Instrument.InstrumentTypeId, x.Instrument.Manufacturer))
            .ToListAsync(cancellationToken);

        HashSet<int> rentedInstrumentIds = [.. userRentals.Select(x => x.InstrumentId)];

        Dictionary<int, int> viewMap = await context.Set<InstrumentView>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && recommendable.Any(i => i.Id == x.InstrumentId))
            .ToDictionaryAsync(x => x.InstrumentId, x => x.ViewCount, cancellationToken);

        Dictionary<int, int> globalRentalCounts = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => CountedRentalStatuses.Contains(x.RentalStatus) && recommendable.Any(i => i.Id == x.InstrumentId))
            .GroupBy(x => x.InstrumentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var maxGlobalRentals = globalRentalCounts.Values.DefaultIfEmpty(0).Max();

        var maxUserViews = viewMap.Values.DefaultIfEmpty(0).Max();

        var userTypeCounts = userRentals
            .GroupBy(x => x.InstrumentTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var maxTypeCount = userTypeCounts.Values.DefaultIfEmpty(0).Max();

        HashSet<string> rentedManufacturers = new(userRentals.Select(x => x.Manufacturer), StringComparer.OrdinalIgnoreCase);

        var collaborativeInstrumentIds = await BuildCollaborativeInstrumentIdsAsync(studentId, rentedInstrumentIds, recommendable, cancellationToken);

        HashSet<int> collaborativeSet = [.. collaborativeInstrumentIds];

        var candidates = await LoadCandidateInstrumentsAsync(
            recommendable, [.. userTypeCounts.Keys], collaborativeInstrumentIds, viewMap, count, globalRentalCounts, cancellationToken);

        List<ScoredRecommendation> scored = [];

        foreach (var instrument in candidates)
        {
            var ownHistoryScore = userTypeCounts.TryGetValue(instrument.InstrumentTypeId, out var typeCount)
                ? (double)typeCount / maxTypeCount
                : 0;
            var rentalScore = ownHistoryScore * OwnRentalHistoryWeight + (collaborativeSet.Contains(instrument.Id) ? CollaborativeRentalWeight : 0);
            var viewScore = ComputeViewScore(instrument, viewMap, maxUserViews, userTypeCounts);

            var similarityScore = ComputeSimilarityScore(instrument, rentedManufacturers, userTypeCounts);
            var popularityScore = maxGlobalRentals == 0 ? 0 : (double)globalRentalCounts.GetValueOrDefault(instrument.Id) / maxGlobalRentals;
            var totalScore = rentalScore * RentalWeight + viewScore * ViewWeight + similarityScore * SimilarityWeight + popularityScore * PopularityWeight;

            var reasons = BuildReasons(ownHistoryScore, viewScore, similarityScore, popularityScore, instrument, collaborativeSet);

            scored.Add(new ScoredRecommendation(instrument, totalScore, reasons));
        }

        return [.. scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Instrument.Id)
            .Take(count)
            .Select(x =>
            {
                var dto = mapper.Map<InstrumentDto>(x.Instrument);
                dto.IsAvailable = true;
                return new InstrumentRecommendationDto
                {
                    Instrument = dto,
                    Score = Math.Round(x.Score, ScoreDecimals),
                    Reasons = x.Reasons
                };
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

        var userId = currentUser.UserId;

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

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.InstrumentViewUserIdInstrumentIdUniqueIndex))
        {
            logger.LogInformation(ex, "Duplicate view for user {UserId} and instrument {InstrumentId} ignored", userId, instrumentId);
        }
    }

    private IQueryable<Instrument> RecommendableInstruments(int studentId) =>
        context.Set<Instrument>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .WhereHasNoBlockingRental()
            .Where(x => !x.InstrumentRentals.Any(r => r.StudentProfileId == studentId && r.RentalStatus == InstrumentRentalStatus.Pending));

    private async Task<List<Instrument>> LoadCandidateInstrumentsAsync(IQueryable<Instrument> recommendable, List<int> preferredTypeIds, IReadOnlyList<int> collaborativeInstrumentIds, Dictionary<int, int> viewMap, int count, Dictionary<int, int> globalRentalCounts, CancellationToken cancellationToken)
    {
        var poolSize = Math.Max(count * PoolSizePerResult, CandidatePoolSize);

        var viewedIds = viewMap
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => x.Key)
            .Take(poolSize / 4)
            .ToList();

        var popularIds = globalRentalCounts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => x.Key)
            .Take(poolSize / 2)
            .ToList();

        var preferredIds = await recommendable
            .Where(x => preferredTypeIds.Contains(x.InstrumentTypeId))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(poolSize / 2)
            .ToListAsync(cancellationToken);

        var candidateIds = preferredIds
            .Concat(viewedIds)
            .Concat(collaborativeInstrumentIds)
            .Concat(popularIds)
            .Distinct()
            .Take(poolSize)
            .ToList();

        if (candidateIds.Count < count)
        {
            var fillerIds = await recommendable
                .Where(x => !candidateIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(count - candidateIds.Count)
                .ToListAsync(cancellationToken);

            candidateIds.AddRange(fillerIds);
        }

        return await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .Where(x => candidateIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> BuildCollaborativeInstrumentIdsAsync(int studentId, HashSet<int> rentedInstrumentIds, IQueryable<Instrument> recommendable, CancellationToken cancellationToken)
    {
        var similarStudentIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => rentedInstrumentIds.Contains(x.InstrumentId)
                && x.StudentProfileId != studentId
                && CountedRentalStatuses.Contains(x.RentalStatus))
            .Select(x => x.StudentProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (similarStudentIds.Count == 0)
        {
            return [];
        }

        return await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => similarStudentIds.Contains(x.StudentProfileId) && CountedRentalStatuses.Contains(x.RentalStatus) && !rentedInstrumentIds.Contains(x.InstrumentId) && recommendable.Any(i => i.Id == x.InstrumentId))
            .GroupBy(x => x.InstrumentId)
            .Select(g => new { InstrumentId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.InstrumentId)
            .Select(x => x.InstrumentId)
            .ToListAsync(cancellationToken);
    }

    private static double ComputeViewScore(Instrument instrument, Dictionary<int, int> viewMap, int maxUserViews, Dictionary<int, int> userTypeCounts)
    {
        var directScore = viewMap.TryGetValue(instrument.Id, out var viewCount) ? (double)viewCount / maxUserViews : 0;
        var fallbackScore = userTypeCounts.ContainsKey(instrument.InstrumentTypeId) ? TypeViewFallbackScore : 0;

        return Math.Max(directScore, fallbackScore);
    }

    private static double ComputeSimilarityScore(Instrument instrument, HashSet<string> rentedManufacturers, Dictionary<int, int> userTypeCounts)
    {
        if (rentedManufacturers.Contains(instrument.Manufacturer))
        {
            return 1;
        }

        if (userTypeCounts.ContainsKey(instrument.InstrumentTypeId))
        {
            return TypeSimilarityScore;
        }

        return 0;
    }

    private static List<string> BuildReasons(double ownHistoryScore, double viewScore, double similarityScore, double popularityScore, Instrument instrument, HashSet<int> collaborativeInstrumentIds)
    {
        List<string> reasons = [];

        if (ownHistoryScore >= ReasonScoreThreshold)
        {
            reasons.Add($"Na osnovu vaše historije najma ({instrument.InstrumentType.Type}).");
        }

        if (collaborativeInstrumentIds.Contains(instrument.Id))
        {
            reasons.Add("Studenti sa sličnim izborima najma biraju ovaj instrument.");
        }

        if (viewScore >= ReasonScoreThreshold)
        {
            reasons.Add("Pregledali ste ovaj instrument.");
        }

        if (similarityScore >= TypeSimilarityScore)
        {
            reasons.Add("Sličan vašim prethodnim izborima proizvođača ili vrste.");
        }

        if (popularityScore >= ReasonScoreThreshold)
        {
            reasons.Add("Popularan među studentima.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Preporučeno na osnovu dostupnosti i ukupnog interesovanja.");
        }

        return reasons;
    }

    private static int NormalizeCount(int count) => Math.Clamp(count, 1, MaxCount);

    private sealed record UserRentalSnapshot(int InstrumentId, int InstrumentTypeId, string Manufacturer);

    private sealed record ScoredRecommendation(Instrument Instrument, double Score, List<string> Reasons);
}
