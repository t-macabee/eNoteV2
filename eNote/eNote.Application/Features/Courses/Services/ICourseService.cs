using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Courses.Services
{
    public interface ICourseService
    {
        Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search);
        Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search);
        Task<CourseDto> GetByIdForInstructorAsync(int id);
        Task<CourseDto> GetByIdForStudentAsync(int id);
        Task<CourseDto> CreateAsync(CourseRequest request);
        Task<CourseDto> UpdateAsync(int id, CourseRequest request);
        Task DeleteAsync(int id);
    }
}
