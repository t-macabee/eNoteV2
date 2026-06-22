using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Assignments.Services;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, int page, int pageSize);
    Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request);
}
