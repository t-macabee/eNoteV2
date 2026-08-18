using eNote.Application.Common.Time;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

public static class DevelopmentDataSeed
{
    public static async Task SeedAsync(ENoteContext context, IClock clock)
    {
        await CourseSeed.SeedCourses(context);
        await LectureSeed.SeedLectures(context);
        await InstrumentSeed.SeedInstruments(context);
        await EnrollmentSeed.SeedEnrollments(context);
        await StudentMembershipSeed.SeedMemberships(context, clock);
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

        var c1 = new Course("Osnove teorije muzike", "Uvod u osnove teorije muzike.", 800, new DateTime(2024, 8, 10), new DateTime(2024, 10, 10), instructorId);
        c1.SetPublishedStatus(true);

        var c2 = new Course("Napredne tehnike gitare", "Napredne tehnike i improvizacija.", 800, new DateTime(2024, 9, 12), new DateTime(2024, 10, 12), instructorId);
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
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 90, new DateTime(2024, 8, 11, 19, 30, 0), LectureType.Theoretical, null, courses[0].Id),
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 60, new DateTime(2024, 8, 19, 19, 30, 0), LectureType.Theoretical, null, courses[1].Id)
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

        // ponytail: IDs are stable - seeded via HasData with explicit values in ModelBuilderSeed
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
        new Instrument("Stratocaster", "Fender", "KlasiÄna elektriÄna gitara poznata po svojoj svestranosti i glatkoj svirljivosti.", "instruments/strat.webp", typeId, shopId),
        new Instrument("Les Paul", "Gibson", "Legendarna elektriÄna gitara omiljena zbog bogatog tona i odrÅ¾avanja.", "instruments/les-paul.webp", typeId, shopId),
        new Instrument("RG", "Ibanez", "Visokoperformansna elektriÄna gitara popularna meÄ‘u rok i metal sviraÄima.", "instruments/rg.webp", typeId, shopId),
        new Instrument("Custom 24", "PRS", "Visokokvalitetna elektriÄna gitara poznata po svojoj prelijepoj izradi i zvuku.", "instruments/prs.webp", typeId, shopId),
        new Instrument("Pacifica", "Yamaha", "Svestrana elektriÄna gitara pogodna za razliÄite Å¾anrove.", "instruments/pacifica.webp", typeId, shopId),
        new Instrument("Dinky", "Jackson", "ElektriÄna gitara dizajnirana za brzo sviranje i snaÅ¾an zvuk.", "instruments/dinky.webp", typeId, shopId),
        new Instrument("214ce", "Taylor", "Svestrana i lijepo izraÄ‘ena akustiÄna gitara, poznata po svom svijetlom i artikulisanom tonu.", "instruments/214ce.webp", typeId, shopId),
        new Instrument("D-28", "Martin", "IkoniÄna dreadnought gitara sa bogatom historijom, poznata po svom dubokom, rezonantnom basu i jasnim visokim tonovima.", "instruments/d-28.webp", typeId, shopId),
        new Instrument("J-45", "Gibson", "ÄŒesto nazivan \"radnim konjem\" meÄ‘u akustiÄnim gitarama, ovaj dreadnought sa zaobljenim ramenima pruÅ¾a topao, blag ton koji je savrÅ¡en za kantautore.", "instruments/j-45.webp", typeId, shopId),
        new Instrument("S6", "Seagull", "S6 proizvodi topao, bogat zvuk sa blago rustiÄnim karakterom, Å¡to je Äini omiljenom meÄ‘u muziÄarima koji sviraju folk i roots muziku.", "instruments/s6.webp", typeId, shopId),
        new Instrument("Precision Bass", "Fender", "Industrijski standard bas gitara poznata po dubokom, udarnom zvuku.", "instruments/precision.webp", typeId, shopId),
        new Instrument("Thunderbird", "Gibson", "IkoniÄna bas gitara poznata po jedinstvenom dizajnu i snaÅ¾nom zvuku.", "instruments/thunderbird.webp", typeId, shopId),
        new Instrument("StingRay", "Music Man", "Legendarna elektriÄna bas gitara, prepoznatljiva po svom moÄ‡nom, artikulisanom zvuku, elegantnom dizajnu i vrhunskoj svirljivosti.", "instruments/stingray.webp", typeId, shopId)
    ];
}

internal static class PercussionInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Export", "Pearl", "PristupaÄan bubanj set savrÅ¡en za poÄetnike i srednje napredne bubnjare.", "instruments/export.webp", typeId, shopId),
        new Instrument("Imperialstar", "Tama", "Svestran bubanj set sa izvrsnom izradom i zvukom.", "instruments/imperialstar.webp", typeId, shopId),
        new Instrument("Breakbeats", "Ludwig", "Kompaktni bubanj set dizajniran za prenosivost i odliÄan ton.", "instruments/breakbeats.webp", typeId, shopId)
    ];
}

internal static class BrassInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("YAS-280", "Yamaha", "Popularni saksofon meÄ‘u studentima i srednje naprednim sviraÄima.", "instruments/yas.webp", typeId, shopId),
        new Instrument("Stradivarius", "Bach", "Profesionalni trombon poznat po bogatom tonu i preciznoj intonaciji.", "instruments/stradivarius.webp", typeId, shopId)
    ];
}

internal static class KeysInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Minilogue", "Korg", "Analogni sintisajzer poznat po svom bogatom, toplom zvuku.", "instruments/minilogue.webp", typeId, shopId),
        new Instrument("Juno-DS", "Roland", "Svestrani sintisajzer popularan za Å¾ive nastupe i studijsku upotrebu.", "instruments/juno-ds.webp", typeId, shopId)
    ];
}

internal static class AccessoriesInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Blues Junior IV", "Fender", "Kompaktno, ali snaÅ¾no cijevno pojaÄalo koje pruÅ¾a klasiÄan Fender ton sa dodanom modernom svestranoÅ¡Ä‡u.", "instruments/blues-junior.webp", typeId, shopId),
        new Instrument("DSL40CR-DS", "Marshall", "Vrlo svestrano cijevno pojaÄalo koje nudi sve, od klasiÄne rock distorzije do Å¾estokih solaÅ¾a.", "instruments/dsl40cr.webp", typeId, shopId),
        new Instrument("AC15C1", "Vox", "Poznato po svojim svijetlim Äistim tonovima i karakteristiÄnom \"Top Boost\" overdrive efektu, savrÅ¡eno je za one koji traÅ¾e vintage britanski zvuk.", "instruments/ac15c1.webp", typeId, shopId),
        new Instrument("Rocker 15", "Orange", "Idealno je za kuÄ‡ne probe i manje nastupe, nudeÄ‡i niz tonova od Äistog do prljavog sa jednostavnim i preglednim kontrolama.", "instruments/rocker-15.webp", typeId, shopId),
        new Instrument("Katana-100 MkII", "Boss", "Moderno digitalno pojaÄalo koje kombinuje veliku snagu sa nevjerovatnom svestranoÅ¡Ä‡u.", "instruments/katana-100.webp", typeId, shopId)
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
