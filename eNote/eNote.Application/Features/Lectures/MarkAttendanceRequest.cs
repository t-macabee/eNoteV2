using eNote.Domain.Enums;

namespace eNote.Application.Features.Lectures
{
    public class MarkAttendanceRequest
    {
        public int StudentId { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
    }
}
