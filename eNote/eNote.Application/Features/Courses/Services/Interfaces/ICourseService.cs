using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Courses.Services.Interfaces
{
    public interface ICourseService
    {
        Task<CourseDto> GetByIdAsync(int id, int requesterId);
        Task<PagedResult<CourseDto>> GetPagedAsync(int page, int pageSize, int requesterId);
        Task<CourseDto> CreateAsync(int instructorUserId, CourseCreateRequest request);
        Task EnrollAsync(int courseId, int studentUserId);
    }
}
