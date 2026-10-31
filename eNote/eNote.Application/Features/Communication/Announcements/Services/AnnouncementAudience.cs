using eNote.Application.Features.Academic;

namespace eNote.Application.Features.Communication.Announcements.Services;

internal static class AnnouncementAudience
{
    public static readonly InstrumentRentalStatus[] StoreRentalStatuses =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    ];

    public static IQueryable<Enrollment> CourseEnrollments(IAppDbContext context, DateTime utcNow) =>
        context.Set<Enrollment>().Where(StudentEnrollmentExtensions.IsCurrentMember(utcNow));

    public static IQueryable<InstrumentRental> StoreRentals(IAppDbContext context) =>
        context.Set<InstrumentRental>()
            .Where(r => StoreRentalStatuses.Contains(r.RentalStatus));
}
