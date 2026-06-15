using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Lectures.Services.Interfaces
{
    public interface ILectureService
    {
        Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(int instructorUserId, int page, int pageSize);
        Task<PagedResult<LectureDto>> GetPagedForStudentAsync(int page, int pageSize);
        Task<LectureDto> GetByIdForInstructorAsync(int id, int instructorUserId);
        Task<LectureDto> GetByIdForStudentAsync(int id, int studentUserId);
        Task<LectureDto> CreateAsync(int teacherId, LectureCreateRequest request);
        Task<LectureDto> UpdateAsync(int id, int instructorUserId, LectureUpdateRequest request);
        Task DeleteAsync(int id, int instructorUserId);
        Task<LectureDto> CancelAsync(int id, int instructorUserId);
        Task<RsvpResponse> RsvpAsync(int lectureId, int studentUserId, RsvpRequest request);
        Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, int instructorUserId, int page, int pageSize);
        Task<AttendanceDto> MarkAttendanceAsync(int lectureId, int instructorUserId, MarkAttendanceRequest request);
    }
}
