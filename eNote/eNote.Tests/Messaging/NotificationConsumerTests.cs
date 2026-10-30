using eNote.Application.Common.Persistence;
using eNote.Contracts.Enrollments;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class NotificationConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AnnouncementPublishedConsumer_StoresNotificationForStudent()
    {
        var message = new AnnouncementPublished(9, 5, "Nova obavijest na kursu", "Dobrodošli na kurs", Now);

        var notification = await ConsumeAsync<AnnouncementPublishedConsumer, AnnouncementPublished>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(9, notification.AnnouncementId);
        Assert.Equal("Nova obavijest na kursu", notification.Title);
        Assert.Equal("Dobrodošli na kurs", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task EnrollmentStatusChangedConsumer_StoresNotificationForStudent()
    {
        var message = new EnrollmentStatusChanged(31, 47, 5, "Upis odobren", "Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", Now);

        var notification = await ConsumeAsync<EnrollmentStatusChangedConsumer, EnrollmentStatusChanged>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(31, notification.EnrollmentId);
        Assert.Equal(47, notification.CourseId);
        Assert.Equal("Upis odobren", notification.Title);
        Assert.Equal("Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task TuitionPaymentConsumer_StoresNotificationForStudent()
    {
        var message = new TuitionPaymentNotification(31, 47, 5, "Školarina plaćena", "Školarina za kurs 'Guitar 101' je plaćena.", Now);

        var notification = await ConsumeAsync<TuitionPaymentConsumer, TuitionPaymentNotification>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(31, notification.EnrollmentId);
        Assert.Equal(47, notification.CourseId);
        Assert.Equal("Školarina plaćena", notification.Title);
        Assert.Equal("Školarina za kurs 'Guitar 101' je plaćena.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task LectureCancelledConsumer_StoresNotificationForStudent()
    {
        var message = new LectureCancelled(3, 5, "Harmonija", "Predavanje otkazano", "Predavanje Harmonija je otkazano.", Now);

        var notification = await ConsumeAsync<LectureCancelledConsumer, LectureCancelled>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(3, notification.LectureId);
        Assert.Equal("Predavanje otkazano", notification.Title);
        Assert.Equal("Predavanje Harmonija je otkazano.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task RentalRefundedConsumer_StoresNotificationForStudent()
    {
        var message = new RentalRefunded(1, 5, 9, 5000, "bam", "Stratocaster", "Uplata vraćena", "Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", Now);

        var notification = await ConsumeAsync<RentalRefundedConsumer, RentalRefunded>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(1, notification.RentalId);
        Assert.Equal("Uplata vraćena", notification.Title);
        Assert.Equal("Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task RentalStatusChangedConsumer_StoresNotificationForRecipient()
    {
        var message = new RentalStatusChanged(9, 5, 11, null, "Active", "Stratocaster", "Status iznajmljivanja", "Iznajmljivanje je aktivirano.", Now);

        var notification = await ConsumeAsync<RentalStatusChangedConsumer, RentalStatusChanged>(message);

        Assert.Equal(11, notification.UserId);
        Assert.Equal(9, notification.RentalId);
        Assert.Equal("Status iznajmljivanja", notification.Title);
        Assert.Equal("Iznajmljivanje je aktivirano.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task SubmissionGradedConsumer_StoresNotificationForStudent()
    {
        var message = new SubmissionGraded(4, 5, "Zadatak 1", 5, "Zadatak ocijenjen", "Zadatak 1 je ocijenjen ocjenom 5.", Now);

        var notification = await ConsumeAsync<SubmissionGradedConsumer, SubmissionGraded>(message);

        Assert.Equal(5, notification.UserId);
        Assert.Equal(4, notification.SubmissionId);
        Assert.Equal("Zadatak ocijenjen", notification.Title);
        Assert.Equal("Zadatak 1 je ocijenjen ocjenom 5.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    private static async Task<Notification> ConsumeAsync<TConsumer, TMessage>(TMessage message)
        where TConsumer : class, IConsumer<TMessage>
        where TMessage : class
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        await using var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<TConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<TMessage>());
        }
        finally
        {
            await harness.Stop();
        }

        return await context.Set<Notification>().SingleAsync();
    }
}
