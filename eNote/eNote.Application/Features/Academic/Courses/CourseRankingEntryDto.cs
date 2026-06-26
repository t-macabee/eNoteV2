namespace eNote.Application.Features.Academic.Courses;

public class CourseRankingEntryDto
{
    public int Rank { get; set; }
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public double? AverageGrade { get; set; }
    public int GradedSubmissions { get; set; }
}
