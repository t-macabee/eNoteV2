using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Lectures.Services
{
    public interface ILectureService
    {
        Task<LectureDto> GetByIdAsync(int id, int requesterId);
        Task<PagedResult<LectureDto>> GetPagedAsync(int page, int pageSize, int requesterId);
        Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request);
        Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request);
    }

    public class RsvpResponse
    {
        public int LectureId { get; set; }
        public int StudentId { get; set; }
        public bool Confirmed { get; set; }
    }
}
