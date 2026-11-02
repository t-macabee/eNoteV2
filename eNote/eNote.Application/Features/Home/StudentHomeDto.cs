using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Home;

public class StudentHomeDto
{
    public LectureDto? NextLecture { get; set; }
    public List<AssignmentDto> OpenAssignments { get; set; } = [];
    public List<InstrumentRentalDto> CurrentRentals { get; set; } = [];
    public List<CourseDto> TuitionDueCourses { get; set; } = [];
    public RentalDebtDto Debt { get; set; } = new();
}
