namespace eNote.Application.Features.Academic.Lectures;

public class AttendanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;
    public AttendanceStatus AttendanceStatus { get; set; }
}
