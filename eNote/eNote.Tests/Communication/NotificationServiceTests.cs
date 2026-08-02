using eNote.Application.Features.Communication.Notifications;
using eNote.Application.Features.Communication.Notifications.Services;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Communication;

public sealed class NotificationServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnread_ForCurrentUser()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(student: null);
        context.Set<Notification>().AddRange(
            new Notification(1, "Unread", "body", Now),
            new Notification(1, "Read", "body", Now),
            new Notification(2, "Other user", "body", Now));
        await context.SaveChangesAsync();
        context.Set<Notification>().Single(n => n.Title == "Read").MarkRead();
        await context.SaveChangesAsync();
        var service = new NotificationService(context, TestMapper.Create(), actor);

        var result = await service.GetUnreadCountAsync();

        Assert.Equal(1, result.UnreadCount);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyCurrentUsersNotifications()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(student: null);
        context.Set<Notification>().AddRange(
            new Notification(1, "Mine", "body", Now),
            new Notification(2, "Theirs", "body", Now));
        await context.SaveChangesAsync();
        var service = new NotificationService(context, TestMapper.Create(), actor);

        var result = await service.GetPagedAsync(new NotificationSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Mine", dto.Title);
    }

    [Fact]
    public async Task MarkReadAsync_Throws_WhenNotificationBelongsToOtherUser()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(student: null);
        context.Set<Notification>().Add(new Notification(2, "Theirs", "body", Now));
        await context.SaveChangesAsync();
        var service = new NotificationService(context, TestMapper.Create(), actor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.MarkReadAsync(1));
    }

    [Fact]
    public async Task MarkReadAsync_MarksOwnNotificationRead()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(student: null);
        var notification = new Notification(1, "Mine", "body", Now);
        context.Set<Notification>().Add(notification);
        await context.SaveChangesAsync();
        var service = new NotificationService(context, TestMapper.Create(), actor);

        var dto = await service.MarkReadAsync(notification.Id);

        Assert.True(dto.IsRead);
        var updated = await context.Set<Notification>().SingleAsync(n => n.Id == notification.Id);
        Assert.True(updated.IsRead);
    }
}
