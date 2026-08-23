using MapsterMapper;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class StudentAnnouncementFeedService(IAppDbContext context, ICurrentActor actor, IMapper mapper)
{
    public async Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.MusicStore)
            .Where(a =>
                (a.CourseId != null && context.Set<Enrollment>().Any(e =>
                    e.StudentId == studentId &&
                    e.EnrollmentStatus == EnrollmentStatus.Active &&
                    e.CourseId == a.CourseId)) ||
                (a.MusicStoreId != null && context.Set<InstrumentRental>().Any(r =>
                    r.StudentProfileId == studentId &&
                    (r.RentalStatus == InstrumentRentalStatus.Approved || r.RentalStatus == InstrumentRentalStatus.Active || r.RentalStatus == InstrumentRentalStatus.Completed || r.RentalStatus == InstrumentRentalStatus.ReturnedEarly) &&
                    r.Instrument.MusicStoreId == a.MusicStoreId)));

        return await query.ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt), cancellationToken);
    }
}
