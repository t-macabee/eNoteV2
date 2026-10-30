using eNote.Contracts.Assignments;
using eNote.Contracts.Communication;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class SubmissionNotificationDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchGradedAsync_EnqueuesTheGradedSubmission()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new SubmissionNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchGradedAsync(submissionId: 4, studentUserId: 7, assignmentTitle: "Zadatak 1", grade: 3);
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal(NotificationMessageTypes.SubmissionGraded, row.MessageType);

        var payload = JsonSerializer.Deserialize<SubmissionGraded>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(4, payload.SubmissionId);
        Assert.Equal(7, payload.StudentUserId);
        Assert.Equal(3, payload.Grade);
        Assert.Equal("Zadaća ocijenjena", payload.Title);
        Assert.Equal("Vaša zadaća 'Zadatak 1' je ocijenjena. Ocjena: 3.", payload.Body);
        Assert.Equal(Now, payload.OccurredAtUtc);
    }
}
