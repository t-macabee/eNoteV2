using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed
{
    public static class DevelopmentDataSeed
    {
        public static async Task SeedAsync(ENoteContext context)
        {
            await CourseSeed.SeedCourses(context);
            await LectureSeed.SeedLectures(context);
            await InstrumentSeed.SeedInstruments(context);
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

            context.Set<Course>().AddRange(
                new Course
                {
                    Name = "Osnove teorije muzike",
                    Description = "Uvod u osnove teorije muzike.",
                    Price = 800,
                    InstructorId = instructorId,
                    StartDate = new DateTime(2024, 8, 10),
                    EndDate = new DateTime(2024, 10, 10)
                },
                new Course
                {
                    Name = "Napredne tehnike gitare",
                    Description = "Napredne tehnike i improvizacija.",
                    Price = 800,
                    InstructorId = instructorId,
                    StartDate = new DateTime(2024, 9, 12),
                    EndDate = new DateTime(2024, 10, 12)
                }
            );

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
                new Lecture
                {
                    Name = "Uvodno predavanje",
                    Location = "Amfiteatar gradskog BKC-a",
                    Duration = 90,
                    LectureTime = new DateTime(2024, 8, 11, 19, 30, 0),
                    CourseId = courses[0].Id,
                    LectureType = LectureType.Theoretical,
                    LectureStatus = LectureStatus.Scheduled
                },
                new Lecture
                {
                    Name = "Uvodno predavanje",
                    Location = "Amfiteatar gradskog BKC-a",
                    Duration = 60,
                    LectureTime = new DateTime(2024, 8, 19, 19, 30, 0),
                    CourseId = courses[1].Id,
                    LectureType = LectureType.Theoretical,
                    LectureStatus = LectureStatus.Scheduled
                }
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
            new Instrument
            {
                Manufacturer = "Fender",
                Model = "Stratocaster",
                Description = "Klasična električna gitara poznata po svojoj svestranosti i glatkoj svirljivosti.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/strat.webp"
            },
            new Instrument
            {
                Manufacturer = "Gibson",
                Model = "Les Paul",
                Description = "Legendarna električna gitara omiljena zbog bogatog tona i održavanja.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/les-paul.webp"
            },
            new Instrument
            {
                Manufacturer = "Ibanez",
                Model = "RG",
                Description = "Visokoperformansna električna gitara popularna među rok i metal sviračima.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/rg.webp"
            },
            new Instrument
            {
                Manufacturer = "PRS",
                Model = "Custom 24",
                Description = "Visokokvalitetna električna gitara poznata po svojoj prelijepoj izradi i zvuku.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/prs.webp"
            },
            new Instrument
            {
                Manufacturer = "Yamaha",
                Model = "Pacifica",
                Description = "Svestrana električna gitara pogodna za različite žanrove.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/pacifica.webp"
            },
            new Instrument
            {
                Manufacturer = "Jackson",
                Model = "Dinky",
                Description = "Električna gitara dizajnirana za brzo sviranje i snažan zvuk.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/dinky.webp"
            },
            new Instrument
            {
                Manufacturer = "Taylor",
                Model = "214ce",
                Description = "Svestrana i lijepo izrađena akustična gitara, poznata po svom svijetlom i artikulisanom tonu.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/214ce.webp"
            },
            new Instrument
            {
                Manufacturer = "Martin",
                Model = "D-28",
                Description = "Ikonična dreadnought gitara sa bogatom historijom, poznata po svom dubokom, rezonantnom basu i jasnim visokim tonovima.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/d-28.webp"
            },
            new Instrument
            {
                Manufacturer = "Gibson",
                Model = "J-45",
                Description = "Često nazivan \"radnim konjem\" među akustičnim gitarama, ovaj dreadnought sa zaobljenim ramenima pruža topao, blag ton koji je savršen za kantautore.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/j-45.webp"
            },
            new Instrument
            {
                Manufacturer = "Seagull",
                Model = "S6",
                Description = "S6 proizvodi topao, bogat zvuk sa blago rustičnim karakterom, što je čini omiljenom među muzičarima koji sviraju folk i roots muziku.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/s6.webp"
            },
            new Instrument
            {
                Manufacturer = "Fender",
                Model = "Precision Bass",
                Description = "Industrijski standard bas gitara poznata po dubokom, udarnom zvuku.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/precision.webp"
            },
            new Instrument
            {
                Manufacturer = "Gibson",
                Model = "Thunderbird",
                Description = "Ikonična bas gitara poznata po jedinstvenom dizajnu i snažnom zvuku.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/thunderbird.webp"
            },
            new Instrument
            {
                Manufacturer = "Music Man",
                Model = "StingRay",
                Description = "Legendarna električna bas gitara, prepoznatljiva po svom moćnom, artikulisanom zvuku, elegantnom dizajnu i vrhunskoj svirljivosti.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/stingray.webp"
            }
        ];
    }

    internal static class PercussionInstruments
    {
        public static Instrument[] GetInstruments(int shopId, int typeId) =>
        [
            new Instrument
            {
                Manufacturer = "Pearl",
                Model = "Export",
                Description = "Pristupačan bubanj set savršen za početnike i srednje napredne bubnjare.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/export.webp"
            },
            new Instrument
            {
                Manufacturer = "Tama",
                Model = "Imperialstar",
                Description = "Svestran bubanj set sa izvrsnom izradom i zvukom.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/imperialstar.webp"
            },
            new Instrument
            {
                Manufacturer = "Ludwig",
                Model = "Breakbeats",
                Description = "Kompaktni bubanj set dizajniran za prenosivost i odličan ton.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/breakbeats.webp"
            }
        ];
    }

    internal static class BrassInstruments
    {
        public static Instrument[] GetInstruments(int shopId, int typeId) =>
        [
            new Instrument
            {
                Manufacturer = "Yamaha",
                Model = "YAS-280",
                Description = "Popularni saksofon među studentima i srednje naprednim sviračima.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/yas.webp"
            },
            new Instrument
            {
                Manufacturer = "Bach",
                Model = "Stradivarius",
                Description = "Profesionalni trombon poznat po bogatom tonu i preciznoj intonaciji.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/stradivarius.webp"
            }
        ];
    }

    internal static class KeysInstruments
    {
        public static Instrument[] GetInstruments(int shopId, int typeId) =>
        [
            new Instrument
            {
                Manufacturer = "Korg",
                Model = "Minilogue",
                Description = "Analogni sintisajzer poznat po svom bogatom, toplom zvuku.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/minilogue.webp"
            },
            new Instrument
            {
                Manufacturer = "Roland",
                Model = "Juno-DS",
                Description = "Svestrani sintisajzer popularan za žive nastupe i studijsku upotrebu.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/juno-ds.webp"
            }
        ];
    }

    internal static class AccessoriesInstruments
    {
        public static Instrument[] GetInstruments(int shopId, int typeId) =>
        [
            new Instrument
            {
                Manufacturer = "Fender",
                Model = "Blues Junior IV",
                Description = "Kompaktno, ali snažno cijevno pojačalo koje pruža klasičan Fender ton sa dodanom modernom svestranošću.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/blues-junior.webp"
            },
            new Instrument
            {
                Manufacturer = "Marshall",
                Model = "DSL40CR-DS",
                Description = "Vrlo svestrano cijevno pojačalo koje nudi sve, od klasične rock distorzije do žestokih solaža.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/dsl40cr.webp"
            },
            new Instrument
            {
                Manufacturer = "Vox",
                Model = "AC15C1",
                Description = "Poznato po svojim svijetlim čistim tonovima i karakterističnom \"Top Boost\" overdrive efektu, savršeno je za one koji traže vintage britanski zvuk.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/ac15c1.webp"
            },
            new Instrument
            {
                Manufacturer = "Orange",
                Model = "Rocker 15",
                Description = "Idealno je za kućne probe i manje nastupe, nudeći niz tonova od čistog do prljavog sa jednostavnim i preglednim kontrolama.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/rocker-15.webp"
            },
            new Instrument
            {
                Manufacturer = "Boss",
                Model = "Katana-100 MkII",
                Description = "Moderno digitalno pojačalo koje kombinuje veliku snagu sa nevjerovatnom svestranošću.",
                MusicStoreId = shopId,
                InstrumentTypeId = typeId,
                ImagePath = "instruments/katana-100.webp"
            }
        ];
    }
}
