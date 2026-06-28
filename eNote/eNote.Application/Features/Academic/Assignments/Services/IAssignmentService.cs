using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default);
    Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default);
    Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, CancellationToken cancellationToken = default);
}
