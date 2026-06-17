namespace eNote.Application.Features.Users
{
    public class UpdateMembershipRequest
    {
        /// Null clears the membership (marks as unpaid)
        public DateTime? PaidUntil { get; set; }
    }
}
