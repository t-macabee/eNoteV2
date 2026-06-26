namespace eNote.Application.Features.Identity.Users;

public class UpdateMembershipRequest
{
    // Null briše članstvo (označava kao neplaćeno)
    public DateTime? PaidUntil { get; set; }
}
