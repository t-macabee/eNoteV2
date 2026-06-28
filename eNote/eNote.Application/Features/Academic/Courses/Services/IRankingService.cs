using eNote.Application.Features.Academic.Courses;

namespace eNote.Application.Features.Academic.Courses.Services;

public interface IRankingService
{
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId, CancellationToken cancellationToken = default);
}
