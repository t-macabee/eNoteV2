using MapsterMapper;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class StudentAnnouncementFeedService(IAppDbContext context, IStudentContext students, IMapper mapper)
{
    public async Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var query = VisibleTo(studentId).ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt), cancellationToken);
    }

    public async Task<AnnouncementDto> GetByIdForStudentAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var entity = await VisibleTo(studentId)
            .FirstOrDefaultAsync(a => a.Id == announcementId, cancellationToken) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    private IQueryable<Announcement> VisibleTo(int studentId)
    {
        return context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.MusicStore)
            .Where(a =>
                (a.CourseId != null && context.Set<Enrollment>().Any(e =>
                    e.EnrollmentStatus == AnnouncementAudience.CourseEnrollmentStatus &&
                    e.StudentId == studentId &&
                    e.CourseId == a.CourseId)) ||
                (a.MusicStoreId != null && context.Set<InstrumentRental>().Any(r =>
                    AnnouncementAudience.StoreRentalStatuses.Contains(r.RentalStatus) &&
                    r.StudentProfileId == studentId &&
                    r.MusicStoreId == a.MusicStoreId)));
    }
}
