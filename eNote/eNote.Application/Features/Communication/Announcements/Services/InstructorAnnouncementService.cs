using eNote.Application.Features.Identity.Instructors;
using MapsterMapper;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class InstructorAnnouncementService(IAppDbContext context, IClock clock, ICurrentUserContext currentUser, InstructorAccessService instructorAccess, IFileStorageService fileStorage, IMapper mapper)
{
    public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId, cancellationToken))
        {
            throw new BusinessException(Messages.AnnouncementCourseForbidden);
        }

        var entity = AnnouncementBuilder.Build(request, courseId, null, clock, currentUser);

        context.Set<Announcement>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search, CancellationToken cancellationToken = default)
    {
        return await (await GetCourseAnnouncementQueryAsync(courseId)).ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt), cancellationToken);
    }

    public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);
        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }

    private async Task<IQueryable<Announcement>> GetCourseAnnouncementQueryAsync(int courseId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        return instructorAccess.CourseAnnouncementsFor(courseId, instructorId, track);
    }
}
