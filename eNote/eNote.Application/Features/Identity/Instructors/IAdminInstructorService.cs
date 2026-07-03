namespace eNote.Application.Features.Identity.Instructors;

public interface IAdminInstructorService
{
    Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search, CancellationToken cancellationToken = default);
    Task<InstructorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
