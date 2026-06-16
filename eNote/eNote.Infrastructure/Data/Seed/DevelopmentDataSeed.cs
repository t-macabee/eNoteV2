using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eNote.Infrastructure.Data.Seed
{
    public static class DevelopmentDataSeed
    {
        public static async Task SeedAsync(ENoteContext context)
        {
            await CourseSeed.SeedCourses(context);
            await LectureSeed.SeedLectures(context);
            await InstrumentSeed.SeedInstruments(context);
            await EnrollmentSeed.SeedEnrollments(context);
        }
    }

    internal static class CourseSeed
    {
        public static async Task SeedCourses(ENoteContext context)
        {
            if (await context.Set<Course>().AnyAsync())
                return;

            var instructorId = await context.Set<Instructor>()
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
                return;

            var courses = await context.Set<Course>()
                .OrderBy(c => c.Id)
                .Take(2)
                .ToListAsync();

            if (courses.Count < 2)
                return;

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
                return;

            var shopId = await context.Set<MusicStore>()
                .Select(s => s.Id)
                .FirstAsync();

            var stringTypeId = await context.Set<InstrumentType>()
                .Where(x => x.Type == "Žičani")
                .Select(x => x.Id)
                .FirstAsync();

            var percussionTypeId = await context.Set<InstrumentType>()
                .Where(x => x.Type == "Udaraljke")
                .Select(x => x.Id)
                .FirstAsync();

            var brassTypeId = await context.Set<InstrumentType>()
                .Where(x => x.Type == "Limeni")
                .Select(x => x.Id)
                .FirstAsync();

            var keysTypeId = await context.Set<InstrumentType>()
                .Where(x => x.Type == "Tipke")
                .Select(x => x.Id)
                .FirstAsync();

            var accessoriesTypeId = await context.Set<InstrumentType>()
                .Where(x => x.Type == "Dodatna oprema")
                .Select(x => x.Id)
                .FirstAsync();

            var allInstruments = new[]
            {
                StringInstruments.GetInstruments(shopId, stringTypeId),
                PercussionInstruments.GetInstruments(shopId, percussionTypeId),
                BrassInstruments.GetInstruments(shopId, brassTypeId),
                KeysInstruments.GetInstruments(shopId, keysTypeId),
                AccessoriesInstruments.GetInstruments(shopId, accessoriesTypeId)
            }.SelectMany(x => x).ToArray();

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
                return;

            var studentId = await context.Set<Student>()
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (studentId == 0)
                return;

            var courseIds = await context.Set<Course>()
                .Where(c => c.IsPublished)
                .Select(c => c.Id)
                .ToListAsync();

            var enrollments = courseIds
                .Select(courseId => new Enrollment(studentId, courseId, EnrollmentStatus.Active))
                .ToList();

            context.Set<Enrollment>().AddRange(enrollments);
            await context.SaveChangesAsync();
        }
    }
}