using eNote.Application.Common.Time;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

internal static class CommunicationSeed
{
    public static async Task SeedAnnouncements(ENoteContext context, IClock clock)
    {
        if (await context.Set<Announcement>().AnyAsync())
        {
            return;
        }

        var courses = await context.Set<Course>()
            .OrderBy(c => c.Id)
            .Take(2)
            .ToListAsync();

        var defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);
        var now = clock.UtcNow;

        var announcements = new List<Announcement>();

        if (courses.Count >= 1)
        {
            announcements.Add(new Announcement(
                "Uvodni materijali za kurs",
                "Materijali za pripremu su dostupni u sekciji kursa.",
                courses[0].Id,
                null,
                now.AddDays(-20)));

            announcements.Add(new Announcement(
                "Priprema za harmoniju",
                "Mole se studenti da ponove gradivo o ljestvicama prije narednog časa.",
                courses[0].Id,
                null,
                now.AddDays(-5)));
        }

        if (courses.Count >= 2)
        {
            announcements.Add(new Announcement(
                "Preporučene vježbe za gitaru",
                "Vježbajte koordinaciju prstiju prema postavljenim primjerima.",
                courses[1].Id,
                null,
                now.AddDays(-10)));

            announcements.Add(new Announcement(
                "Raspored praktičnih časova",
                "Naredni čas gitare fokusira se na improvizaciju.",
                courses[1].Id,
                null,
                now.AddDays(-2)));
        }

        announcements.Add(new Announcement(
            "Popust na opremu za studente",
            "Svi studenti ostvaruju popust na dodatnu opremu tokom ovog mjeseca.",
            null,
            defaultStoreId,
            now.AddDays(-7)));

        announcements.Add(new Announcement(
            "Novi instrumenti na stanju",
            "U radnju su stigli novi modeli gitara i pojačala.",
            null,
            defaultStoreId,
            now.AddDays(-1)));

        context.Set<Announcement>().AddRange(announcements);
        await context.SaveChangesAsync();
    }

    public static async Task SeedEvents(ENoteContext context, IClock clock)
    {
        if (await context.Set<Event>().AnyAsync())
        {
            return;
        }

        var now = clock.UtcNow;
        var today = now.Date;

        var courses = await context.Set<Course>()
            .OrderBy(c => c.Id)
            .Take(2)
            .ToListAsync();

        var instructor = await context.Set<Instructor>()
            .OrderBy(i => i.Id)
            .FirstOrDefaultAsync();

        var events = new List<Event>
        {
            new(
                "Godišnji koncert škole",
                "Godišnji koncert učenika i profesora muzičke škole.",
                today.AddDays(20).AddHours(19),
                today.AddDays(20).AddHours(21),
                addressId: 4,
                courseId: null,
                instructorId: null),
            new(
                "Otvoreni čas gitare",
                "Prezentacija tehnika sviranja gitare i radionica za sve zainteresovane.",
                today.AddDays(9).AddHours(17),
                today.AddDays(9).AddHours(18).AddMinutes(30),
                addressId: 2,
                courseId: null,
                instructorId: null)
        };

        if (courses.Count >= 2 && instructor != null)
        {
            events.Add(new(
                "Radionica improvizacije",
                "Specijalizovana radionica improvizacije za polaznike naprednog kursa.",
                today.AddDays(15).AddHours(18),
                today.AddDays(15).AddHours(20),
                addressId: 3,
                courseId: courses[1].Id,
                instructorId: instructor.Id));
        }

        context.Set<Event>().AddRange(events);
        await context.SaveChangesAsync();
    }

    public static async Task SeedNotifications(ENoteContext context, IClock clock)
    {
        if (await context.Set<Notification>().AnyAsync())
        {
            return;
        }

        var studentUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "student");
        if (studentUser == null)
        {
            return;
        }

        var now = clock.UtcNow;

        var rental3 = await context.Set<InstrumentRental>()
            .Include(r => r.Instrument)
            .FirstOrDefaultAsync(r => r.StudentProfile.AppUserId == studentUser.Id && r.Instrument.Model == "Les Paul");

        var firstC1Assignment = await context.Set<Assignment>()
            .Include(a => a.Lecture)
            .ThenInclude(l => l.Course)
            .Where(a => a.Lecture.Course.Name == "Osnove teorije muzike")
            .OrderBy(a => a.DueAt)
            .FirstOrDefaultAsync();

        var submission1 = firstC1Assignment != null
            ? await context.Set<AssignmentSubmission>()
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.AssignmentId == firstC1Assignment.Id && s.Student.AppUserId == studentUser.Id)
            : null;

        var announcementD5 = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Title == "Priprema za harmoniju");

        var notifications = new List<Notification>();

        if (rental3 != null)
        {
            var notif1 = new Notification(
                studentUser.Id,
                "Zahtjev odobren",
                $"Vaš zahtjev za instrument {rental3.Instrument.Model} je odobren. Mjesečna naknada: {rental3.Fee:F2} KM.",
                now.AddDays(-11),
                rentalId: rental3.Id);
            notif1.MarkRead();
            notifications.Add(notif1);
        }

        if (submission1 != null)
        {
            var notif2 = new Notification(
                studentUser.Id,
                "Zadaća ocijenjena",
                $"Vaša zadaća '{submission1.Assignment.Title}' je ocijenjena. Ocjena: {submission1.Grade}.",
                now.AddDays(-3),
                submissionId: submission1.Id);
            notifications.Add(notif2);
        }

        if (announcementD5 != null)
        {
            var notif3 = new Notification(
                studentUser.Id,
                "Nova obavijest na kursu",
                announcementD5.Title,
                now.AddDays(-5),
                announcementId: announcementD5.Id);
            notifications.Add(notif3);
        }

        context.Set<Notification>().AddRange(notifications);
        await context.SaveChangesAsync();
    }
}
