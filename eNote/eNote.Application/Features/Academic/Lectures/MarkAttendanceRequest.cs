using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class MarkAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; }
}
