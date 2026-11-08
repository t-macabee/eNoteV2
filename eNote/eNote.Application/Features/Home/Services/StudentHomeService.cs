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
    private const int TuitionDueCourseCount = 5;

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

        var currentRentals = await rentals.GetPagedForStudentAsync(new InstrumentRentalSearchObject
        {
            RentalStatuses = [InstrumentRentalStatus.Approved, InstrumentRentalStatus.Active],
            PageSize = CurrentRentalCount,
            IncludeTotalCount = false
        }, cancellationToken);

        var tuitionDueCourses = await courses.GetPagedForStudentAsync(new CourseSearchObject
        {
            EnrolledOnly = true,
            TuitionDueOnly = true,
            PageSize = TuitionDueCourseCount,
            IncludeTotalCount = false
        }, cancellationToken);

        var debt = await rentals.GetDebtForStudentAsync(cancellationToken);

        return new StudentHomeDto
        {
            NextLecture = nextLecture.Items.FirstOrDefault(),
            OpenAssignments = [.. openAssignments.Items],
            CurrentRentals = [.. currentRentals.Items],
            TuitionDueCourses = [.. tuitionDueCourses.Items],
            Debt = debt
        };
    }
}
