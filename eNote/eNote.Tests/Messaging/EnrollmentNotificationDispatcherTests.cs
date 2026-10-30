using eNote.Contracts.Enrollments;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Enums;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class EnrollmentNotificationDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DispatchStatusChangedAsync_CarriesEnrollmentCourseAndStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new EnrollmentNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchStatusChangedAsync(31, 47, 5, "Guitar 101", EnrollmentStatus.Active, null);
        await context.SaveChangesAsync();

        var message = await ReadSingleAsync<EnrollmentStatusChanged>(context);
        Assert.Equal(31, message.EnrollmentId);
        Assert.Equal(47, message.CourseId);
        Assert.Equal(5, message.StudentUserId);
    }

    [Fact]
    public async Task DispatchTuitionPaidAsync_CarriesEnrollmentCourseAndStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new EnrollmentNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchTuitionPaidAsync(31, 47, 5, "Guitar 101", 5000, "bam", Now.AddDays(30));
        await context.SaveChangesAsync();

        var message = await ReadSingleAsync<TuitionPaymentNotification>(context);
        Assert.Equal(31, message.EnrollmentId);
        Assert.Equal(47, message.CourseId);
        Assert.Equal(5, message.StudentUserId);
    }

    [Fact]
    public async Task DispatchTuitionRefundedAsync_CarriesEnrollmentCourseAndStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new EnrollmentNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchTuitionRefundedAsync(31, 47, 5, "Guitar 101", 5000, "bam");
        await context.SaveChangesAsync();

        var message = await ReadSingleAsync<TuitionPaymentNotification>(context);
        Assert.Equal(31, message.EnrollmentId);
        Assert.Equal(47, message.CourseId);
        Assert.Equal(5, message.StudentUserId);
    }

    private static async Task<T> ReadSingleAsync<T>(ENoteContext context)
    {
        var row = await context.Set<NotificationOutbox>().AsNoTracking().SingleAsync();

        return JsonSerializer.Deserialize<T>(row.PayloadJson, JsonOptions)!;
    }
}
