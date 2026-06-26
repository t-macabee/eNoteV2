using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Lectures;

namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureAttendanceService
{
    Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search);
    Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request);
}
