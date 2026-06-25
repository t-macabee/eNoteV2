using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Instructors;

public interface IAdminInstructorService
{
    Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search);
    Task<InstructorDto> GetByIdAsync(int id);
}
