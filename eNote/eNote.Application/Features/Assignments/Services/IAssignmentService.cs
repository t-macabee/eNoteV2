using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Assignments.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search);
    Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId);
    Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request);
    Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request);
    Task DeleteAsync(int lectureId, int assignmentId);
    Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search);
    Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId);
}
