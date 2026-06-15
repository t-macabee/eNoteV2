using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Assignments.Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, int instructorUserId, int page, int pageSize);
        Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, int instructorUserId);
        Task<AssignmentDto> CreateAsync(int lectureId, int instructorUserId, AssignmentCreateRequest request);
        Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, int instructorUserId, AssignmentUpdateRequest request);
        Task DeleteAsync(int lectureId, int assignmentId, int instructorUserId);
        Task<PagedResult<AssignmentDto>> GetForStudentAsync(int studentUserId, int page, int pageSize);
        Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, int studentUserId);
        Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, int studentUserId, AssignmentSubmitRequest request);
        Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, int instructorUserId, int page, int pageSize);
        Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, int instructorUserId, GradeAssignmentRequest request);
    }
}
