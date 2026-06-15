using eNote.Domain.Enums;

namespace eNote.Application.Features.Lectures
{
    public class AttendanceDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public AttendanceStatus AttendanceStatus { get; set; }
    }
}
