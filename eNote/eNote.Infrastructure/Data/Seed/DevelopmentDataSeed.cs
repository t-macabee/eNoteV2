using eNote.Application.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

public static class DevelopmentDataSeed
{
    public static async Task SeedAsync(ENoteContext context, IClock clock)
    {
        await MusicStoreSeed.SeedStores(context);
        await CourseSeed.SeedCourses(context);
        await LectureSeed.SeedLectures(context);
        await InstrumentSeed.SeedInstruments(context);
        await EnrollmentSeed.SeedEnrollments(context);
        await StudentMembershipSeed.SeedMemberships(context, clock);
    }
}

internal static class MusicStoreSeed
{
    // IdentitySeed.StoreSeed always creates exactly one default store first
    // (see EnsureDefaultStoreAsync), so ">1" is the right idempotency check
    // here rather than "any at all".
    public static async Task SeedStores(ENoteContext context)
    {
        if (await context.Set<MusicStore>().CountAsync() > 1)
        {
            return;
        }

        context.Set<MusicStore>().AddRange(
            new MusicStore("Muzička radnja Mostar", "08:00-16:00", addressId: 6),
            new MusicStore("Muzička radnja Banja Luka", "10:00-18:00", addressId: 7)
        );

        await context.SaveChangesAsync();
    }
}

internal static class StudentMembershipSeed
{
    public static async Task SeedMemberships(ENoteContext context, IClock clock)
    {
        var students = await context.Set<Student>()
            .Where(s => s.MembershipPaidUntil == null)
            .ToListAsync();

        if (students.Count == 0)
        {
            return;
        }

        var paidUntil = clock.UtcNow.AddYears(1);

        foreach (Student student in students)
        {
            student.UpdateMembership(paidUntil);
        }

        await context.SaveChangesAsync();
    }
}

internal static class CourseSeed
{
    public static async Task SeedCourses(ENoteContext context)
    {
        if (await context.Set<Course>().AnyAsync())
        {
            return;
        }

        var instructorId = await context.Set<Instructor>()
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .FirstAsync();

        var c1 = new Course("Osnove teorije muzike", "Uvod u osnove teorije muzike.", 800, DateTime.SpecifyKind(new DateTime(2024, 8, 10), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(2024, 10, 10), DateTimeKind.Utc), instructorId);
        c1.SetPublishedStatus(true);

        var c2 = new Course("Napredne tehnike gitare", "Napredne tehnike i improvizacija.", 800, DateTime.SpecifyKind(new DateTime(2024, 9, 12), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(2024, 10, 12), DateTimeKind.Utc), instructorId);
        c2.SetPublishedStatus(true);

        context.Set<Course>().AddRange(c1, c2);
        await context.SaveChangesAsync();
    }
}

internal static class LectureSeed
{
    public static async Task SeedLectures(ENoteContext context)
    {
        if (await context.Set<Lecture>().AnyAsync())
        {
            return;
        }

        var courses = await context.Set<Course>()
            .OrderBy(c => c.Id)
            .Take(2)
            .ToListAsync();

        if (courses.Count < 2)
        {
            return;
        }

        context.AddRange(
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 90, DateTime.SpecifyKind(new DateTime(2024, 8, 11, 19, 30, 0), DateTimeKind.Utc), LectureType.Theoretical, null, courses[0].Id),
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 60, DateTime.SpecifyKind(new DateTime(2024, 8, 19, 19, 30, 0), DateTimeKind.Utc), LectureType.Theoretical, null, courses[1].Id)
        );

        await context.SaveChangesAsync();
    }
}

internal static class InstrumentSeed
{
    public static async Task SeedInstruments(ENoteContext context)
    {
        if (await context.Set<Instrument>().AnyAsync())
        {
            return;
        }

        var shopId = await context.Set<MusicStore>()
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstAsync();

        const int stringTypeId = 1;
        const int percussionTypeId = 2;
        const int brassTypeId = 3;
        const int keysTypeId = 4;
        const int accessoriesTypeId = 5;

        Instrument[] allInstruments = [.. new[]
        {
            StringInstruments.GetInstruments(shopId, stringTypeId),
            PercussionInstruments.GetInstruments(shopId, percussionTypeId),
            BrassInstruments.GetInstruments(shopId, brassTypeId),
            KeysInstruments.GetInstruments(shopId, keysTypeId),
            AccessoriesInstruments.GetInstruments(shopId, accessoriesTypeId)
        }.SelectMany(x => x)];

        context.Set<Instrument>().AddRange(allInstruments);

        await context.SaveChangesAsync();
    }
}

internal static class StringInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Stratocaster", "Fender", "Klasična električna gitara poznata po svojoj svestranosti i glatkoj svirljivosti.", "instruments/strat.webp", typeId, shopId),
        new Instrument("Les Paul", "Gibson", "Legendarna električna gitara omiljena zbog bogatog tona i održavanja.", "instruments/les-paul.webp", typeId, shopId),
        new Instrument("RG", "Ibanez", "Visokoperformansna električna gitara popularna među rok i metal sviračima.", "instruments/rg.webp", typeId, shopId),
        new Instrument("Custom 24", "PRS", "Visokokvalitetna električna gitara poznata po svojoj prelijepoj izradi i zvuku.", "instruments/prs.webp", typeId, shopId),
        new Instrument("Pacifica", "Yamaha", "Svestrana električna gitara pogodna za različite žanrove.", "instruments/pacifica.webp", typeId, shopId),
        new Instrument("Dinky", "Jackson", "Električna gitara dizajnirana za brzo sviranje i snažan zvuk.", "instruments/dinky.webp", typeId, shopId),
        new Instrument("214ce", "Taylor", "Svestrana i lijepo izrađena akustična gitara, poznata po svom svijetlom i artikulisanom tonu.", "instruments/214ce.webp", typeId, shopId),
        new Instrument("D-28", "Martin", "Ikonična dreadnought gitara sa bogatom historijom, poznata po svom dubokom, rezonantnom basu i jasnim visokim tonovima.", "instruments/d-28.webp", typeId, shopId),
        new Instrument("J-45", "Gibson", "Često nazivan \"radnim konjem\" među akustičnim gitarama, ovaj dreadnought sa zaobljenim ramenima pruža topao, blag ton koji je savršen za kantautore.", "instruments/j-45.webp", typeId, shopId),
        new Instrument("S6", "Seagull", "S6 proizvodi topao, bogat zvuk sa blago rustičnim karakterom, što je čini omiljenom među muzičarima koji sviraju folk i roots muziku.", "instruments/s6.webp", typeId, shopId),
        new Instrument("Precision Bass", "Fender", "Industrijski standard bas gitara poznata po dubokom, udarnom zvuku.", "instruments/precision.webp", typeId, shopId),
        new Instrument("Thunderbird", "Gibson", "Ikonična bas gitara poznata po jedinstvenom dizajnu i snažnom zvuku.", "instruments/thunderbird.webp", typeId, shopId),
        new Instrument("StingRay", "Music Man", "Legendarna električna bas gitara, prepoznatljiva po svom moćnom, artikulisanom zvuku, elegantnom dizajnu i vrhunskoj svirljivosti.", "instruments/stingray.webp", typeId, shopId)
    ];
}

internal static class PercussionInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Export", "Pearl", "Pristupačan bubanj set savršen za početnike i srednje napredne bubnjare.", "instruments/export.webp", typeId, shopId),
        new Instrument("Imperialstar", "Tama", "Svestran bubanj set sa izvrsnom izradom i zvukom.", "instruments/imperialstar.webp", typeId, shopId),
        new Instrument("Breakbeats", "Ludwig", "Kompaktni bubanj set dizajniran za prenosivost i odličan ton.", "instruments/breakbeats.webp", typeId, shopId)
    ];
}

internal static class BrassInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("YAS-280", "Yamaha", "Popularni saksofon među studentima i srednje naprednim sviračima.", "instruments/yas.webp", typeId, shopId),
        new Instrument("Stradivarius", "Bach", "Profesionalni trombon poznat po bogatom tonu i preciznoj intonaciji.", "instruments/stradivarius.webp", typeId, shopId)
    ];
}

internal static class KeysInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Minilogue", "Korg", "Analogni sintisajzer poznat po svom bogatom, toplom zvuku.", "instruments/minilogue.webp", typeId, shopId),
        new Instrument("Juno-DS", "Roland", "Svestrani sintisajzer popularan za žive nastupe i studijsku upotrebu.", "instruments/juno-ds.webp", typeId, shopId)
    ];
}

internal static class AccessoriesInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Blues Junior IV", "Fender", "Kompaktno, ali snažno cijevno pojačalo koje pruža klasičan Fender ton sa dodanom modernom svestranošću.", "instruments/blues-junior.webp", typeId, shopId),
        new Instrument("DSL40CR-DS", "Marshall", "Vrlo svestrano cijevno pojačalo koje nudi sve, od klasične rock distorzije do žestokih solaža.", "instruments/dsl40cr.webp", typeId, shopId),
        new Instrument("AC15C1", "Vox", "Poznato po svojim svijetlim čistim tonovima i karakterističnom \"Top Boost\" overdrive efektu, savršeno je za one koji traže vintage britanski zvuk.", "instruments/ac15c1.webp", typeId, shopId),
        new Instrument("Rocker 15", "Orange", "Idealno je za kućne probe i manje nastupe, nudeći niz tonova od čistog do prljavog sa jednostavnim i preglednim kontrolama.", "instruments/rocker-15.webp", typeId, shopId),
        new Instrument("Katana-100 MkII", "Boss", "Moderno digitalno pojačalo koje kombinuje veliku snagu sa nevjerovatnom svestranošću.", "instruments/katana-100.webp", typeId, shopId)
    ];
}

internal static class EnrollmentSeed
{
    public static async Task SeedEnrollments(ENoteContext context)
    {
        if (await context.Set<Enrollment>().AnyAsync())
        {
            return;
        }

        var studentId = await context.Set<Student>()
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (studentId == 0)
        {
            return;
        }

        var courseIds = await context.Set<Course>()
            .Where(c => c.IsPublished)
            .Select(c => c.Id)
            .ToListAsync();

        List<Enrollment> enrollments = [.. courseIds.Select(courseId => new Enrollment(studentId, courseId, EnrollmentStatus.Active))];

        context.Set<Enrollment>().AddRange(enrollments);
        await context.SaveChangesAsync();
    }
}
