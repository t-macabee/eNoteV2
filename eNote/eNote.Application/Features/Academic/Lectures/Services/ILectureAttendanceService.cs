namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureAttendanceService
{
    Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search, CancellationToken cancellationToken = default);
}
