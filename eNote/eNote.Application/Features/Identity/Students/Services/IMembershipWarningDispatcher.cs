namespace eNote.Application.Features.Identity.Students.Services;

public interface IMembershipWarningDispatcher
{
    Task DispatchWarningAsync(int studentUserId, DateTime? membershipPaidUntil, string? message);
}
