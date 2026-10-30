namespace eNote.Application.Features.Communication.Announcements.Services;

internal static class AnnouncementAudience
{
    public const EnrollmentStatus CourseEnrollmentStatus = EnrollmentStatus.Active;

    public static readonly InstrumentRentalStatus[] StoreRentalStatuses =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    ];

    public static IQueryable<Enrollment> CourseEnrollments(IAppDbContext context) =>
        context.Set<Enrollment>().Where(e => e.EnrollmentStatus == CourseEnrollmentStatus);

    public static IQueryable<InstrumentRental> StoreRentals(IAppDbContext context) =>
        context.Set<InstrumentRental>()
            .Where(r => StoreRentalStatuses.Contains(r.RentalStatus));
}
