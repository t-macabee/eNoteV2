using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Lectures;

namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureService
{
    Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search, CancellationToken cancellationToken = default);
    Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search, CancellationToken cancellationToken = default);
    Task<LectureDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default);
    Task<LectureDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default);
    Task<LectureDto> CreateAsync(LectureCreateRequest request, CancellationToken cancellationToken = default);
    Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<LectureDto> CancelAsync(int id, CancellationToken cancellationToken = default);
}
