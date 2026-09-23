using eNote.Application.Features.Identity.Instructors;
using MapsterMapper;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class InstructorAnnouncementService(IAppDbContext context, IClock clock, ICurrentUserContext currentUser, InstructorAccessService instructorAccess, IMapper mapper, IAnnouncementNotificationDispatcher notificationDispatcher)
{
    public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId, cancellationToken))
        {
            throw new BusinessException(Messages.AnnouncementCourseForbidden);
        }

        var entity = AnnouncementBuilder.Build(request, courseId, null, clock, currentUser);

        return await context.ExecuteInTransactionAsync(async () =>
        {
            context.Set<Announcement>().Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            var enrolledStudentUserIds = await context.Set<Enrollment>()
                .Where(e => e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active)
                .Select(e => e.Student.AppUserId)
                .ToListAsync(cancellationToken);

            await notificationDispatcher.DispatchPublishedAsync(entity.Id, entity.Title, enrolledStudentUserIds);
            await context.SaveChangesAsync(cancellationToken);

            return mapper.Map<AnnouncementDto>(entity);
        }, cancellationToken);
    }

    public async Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, cancellationToken)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search, CancellationToken cancellationToken = default)
    {
        return await (await GetCourseAnnouncementQueryAsync(courseId, cancellationToken)).ApplySearch(search).ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt), cancellationToken);
    }

    public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, cancellationToken, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, cancellationToken, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IQueryable<Announcement>> GetCourseAnnouncementQueryAsync(int courseId, CancellationToken cancellationToken, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        return instructorAccess.CourseAnnouncementsFor(courseId, instructorId, track);
    }
}
