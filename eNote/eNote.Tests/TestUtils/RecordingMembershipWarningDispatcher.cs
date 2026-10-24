using eNote.Application.Features.Identity.Students.Services;

namespace eNote.Tests.TestUtils;

public sealed class RecordingMembershipWarningDispatcher : IMembershipWarningDispatcher
{
    public List<(int StudentUserId, DateTime? MembershipPaidUntil, string? Message)> Warnings { get; } = [];

    public Task DispatchWarningAsync(int studentUserId, DateTime? membershipPaidUntil, string? message)
    {
        Warnings.Add((studentUserId, membershipPaidUntil, message));
        return Task.CompletedTask;
    }
}
