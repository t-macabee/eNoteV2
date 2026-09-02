namespace eNote.Application.Features.Identity.Students;

public class StudentDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }

    public DateTime EnrollmentDate { get; set; }
    public DateTime? MembershipPaidUntil { get; set; }
}
