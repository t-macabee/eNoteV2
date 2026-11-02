using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;

namespace eNote.Application.Features.Home.Services;

public sealed class StudentHomeService(
    LectureService lectures,
    AssignmentService assignments,
    RentalQueryService rentals,
    CourseService courses,
    IClock clock)
{
    private const int OpenAssignmentCount = 3;
    private const int CurrentRentalCount = 5;

    public async Task<StudentHomeDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var nextLecture = await lectures.GetPagedForStudentAsync(new LectureSearchObject
        {
            From = now,
            LectureStatus = LectureStatus.Scheduled,
            Page = 1,
            PageSize = 1,
            IncludeTotalCount = false
        }, cancellationToken);

        var openAssignments = await assignments.GetForStudentAsync(new AssignmentSearchObject
        {
            DueAfter = now,
            IsSubmitted = false,
            Page = 1,
            PageSize = OpenAssignmentCount,
            IncludeTotalCount = false
        }, cancellationToken);

        var approvedRentals = await rentals.GetPagedForStudentAsync(new InstrumentRentalSearchObject
        {
            RentalStatus = InstrumentRentalStatus.Approved,
            PageSize = CurrentRentalCount,
            IncludeTotalCount = false
        }, cancellationToken);

        var activeRentals = await rentals.GetPagedForStudentAsync(new InstrumentRentalSearchObject
        {
            RentalStatus = InstrumentRentalStatus.Active,
            PageSize = CurrentRentalCount,
            IncludeTotalCount = false
        }, cancellationToken);

        var enrolledCourses = await courses.GetPagedForStudentAsync(new CourseSearchObject
        {
            EnrolledOnly = true,
            PageSize = PagingLimits.MaxPageSize,
            IncludeTotalCount = false
        }, cancellationToken);

        var debt = await rentals.GetDebtForStudentAsync(cancellationToken);

        return new StudentHomeDto
        {
            NextLecture = nextLecture.Items.FirstOrDefault(),
            OpenAssignments = [.. openAssignments.Items],
            CurrentRentals = [.. approvedRentals.Items, .. activeRentals.Items],
            TuitionDueCourses = [.. enrolledCourses.Items.Where(c =>
                c.EnrollmentStatus == EnrollmentStatus.Active
                && !c.IsFree
                && (c.PaidUntil is null || c.PaidUntil < now))],
            Debt = debt
        };
    }
}
