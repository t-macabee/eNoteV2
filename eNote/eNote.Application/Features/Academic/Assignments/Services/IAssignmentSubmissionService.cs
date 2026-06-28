using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request, CancellationToken cancellationToken = default);
}
