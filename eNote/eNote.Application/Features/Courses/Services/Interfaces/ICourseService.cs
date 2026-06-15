using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Courses.Services.Interfaces
{
    public interface ICourseService
    {
        Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(int instructorUserId, int page, int pageSize);
        Task<PagedResult<CourseDto>> GetPagedForStudentAsync(int page, int pageSize);
        Task<CourseDto> GetByIdForInstructorAsync(int id, int instructorUserId);
        Task<CourseDto> GetByIdForStudentAsync(int id, int studentUserId);
        Task<CourseDto> CreateAsync(int instructorUserId, CourseCreateRequest request);
        Task<CourseDto> UpdateAsync(int id, int instructorUserId, CourseUpdateRequest request);
        Task DeleteAsync(int id, int instructorUserId);
        Task EnrollAsync(int courseId, int studentUserId);
        Task UnenrollAsync(int courseId, int studentUserId);
    }
}
