using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Lectures.Services.Interfaces
{
    public interface ILectureService
    {
        Task<LectureDto> GetByIdAsync(int id, int requesterId);
        Task<PagedResult<LectureDto>> GetPagedAsync(int page, int pageSize, int requesterId);
        Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request);
        Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request);
    }
}
