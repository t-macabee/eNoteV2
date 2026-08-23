using MapsterMapper;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class StoreAnnouncementService(IAppDbContext context, IClock clock, ICurrentActor actor, IFileStorageService fileStorage, IMapper mapper)
{
    public async Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        var entity = AnnouncementBuilder.Build(request, null, storeId, clock, actor);

        context.Set<Announcement>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        var entity = await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        return await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .Where(a => a.MusicStoreId == storeId)
            .ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt), cancellationToken);
    }

    public async Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForStoreAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(ct);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }
}
