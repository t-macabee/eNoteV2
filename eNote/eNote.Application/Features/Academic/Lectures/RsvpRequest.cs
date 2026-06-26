using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class RsvpRequest
{
    [Required]
    public bool Confirm { get; set; }
    public string? Note { get; set; }
}
