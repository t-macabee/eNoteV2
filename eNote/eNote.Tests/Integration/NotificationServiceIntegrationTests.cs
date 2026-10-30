using eNote.Application.Features.Communication.Notifications.Services;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class NotificationServiceIntegrationTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MarkAllReadAsync_MarksOnlyCurrentUsersNotificationsRead()
    {
        var database = await fixture.CreateDatabaseAsync();
        int currentUserId;
        int otherUserId;

        await using (var context = database.CreateContext())
        {
            currentUserId = await AddUserAsync(context, "current");
            otherUserId = await AddUserAsync(context, "other");

            context.Set<Notification>().AddRange(
                new Notification(currentUserId, "Current unread one", "body", Now),
                new Notification(currentUserId, "Current unread two", "body", Now.AddMinutes(1)),
                new Notification(otherUserId, "Other unread one", "body", Now),
                new Notification(otherUserId, "Other unread two", "body", Now.AddMinutes(1)));
            await context.SaveChangesAsync();
        }

        await using (var serviceContext = database.CreateContext())
        {
            var service = new NotificationService(serviceContext, TestMapper.Create(), new StubCurrentActor(userId: currentUserId));

            var result = await service.MarkAllReadAsync();

            Assert.Equal(0, result.UnreadCount);
        }

        await using var verifyContext = database.CreateContext();
        var currentRows = await verifyContext.Set<Notification>()
            .Where(x => x.UserId == currentUserId)
            .ToListAsync();
        var otherRows = await verifyContext.Set<Notification>()
            .Where(x => x.UserId == otherUserId)
            .ToListAsync();

        Assert.Equal(2, currentRows.Count);
        Assert.All(currentRows, row => Assert.True(row.IsRead));
        Assert.Equal(2, otherRows.Count);
        Assert.All(otherRows, row => Assert.False(row.IsRead));
    }

    private static async Task<int> AddUserAsync(ENoteContext context, string userName)
    {
        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@example.com",
            NormalizedEmail = $"{userName}@example.com".ToUpperInvariant(),
            IsActive = true
        };

        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }
}
