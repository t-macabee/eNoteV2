using eNote.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class MarkAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
    [Required]
    public AttendanceStatus AttendanceStatus { get; set; }
}
