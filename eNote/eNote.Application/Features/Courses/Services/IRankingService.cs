namespace eNote.Application.Features.Courses.Services;

public interface IRankingService
{
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId);
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId);
}
