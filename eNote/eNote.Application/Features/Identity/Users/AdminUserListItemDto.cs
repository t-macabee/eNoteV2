namespace eNote.Application.Features.Identity.Users;

public sealed class AdminUserListItemDto
{
    public int AppUserId { get; init; }
    public string Username { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Role { get; init; }
    public bool IsActive { get; init; }
    public int? StudentId { get; init; }
    public DateTime? MembershipPaidUntil { get; init; }
    public DateTime? EnrollmentDate { get; init; }
    public string? StoreName { get; init; }
    public bool? IsManager { get; init; }
}
