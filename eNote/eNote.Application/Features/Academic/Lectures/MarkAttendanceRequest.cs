namespace eNote.Application.Features.Academic.Lectures;

public class MarkAttendanceRequest
{
    public int StudentId { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; }
}
