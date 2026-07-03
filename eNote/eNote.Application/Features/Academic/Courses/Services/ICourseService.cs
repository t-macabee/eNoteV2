namespace eNote.Application.Features.Academic.Courses.Services;

public interface ICourseService
{
    Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default);
    Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search, CancellationToken cancellationToken = default);
    Task<CourseDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> CreateAsync(CourseRequest request, CancellationToken cancellationToken = default);
    Task<CourseDto> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
