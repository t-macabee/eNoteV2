using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Lectures.Services
{
    public interface ILectureService
    {
        Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search);
        Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search);
        Task<LectureDto> GetByIdForInstructorAsync(int id);
        Task<LectureDto> GetByIdForStudentAsync(int id);
        Task<LectureDto> CreateAsync(LectureCreateRequest request);
        Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request);
        Task DeleteAsync(int id);
        Task<LectureDto> CancelAsync(int id);
    }
}
