using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Lectures.Services;

public interface ILectureAttendanceService
{
    Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search);
    Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request);
}
