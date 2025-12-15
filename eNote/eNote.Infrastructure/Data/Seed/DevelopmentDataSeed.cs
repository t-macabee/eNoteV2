using eNote.Domain.Entities;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed
{
    public static class DevelopmentDataSeed
    {
        public static async Task SeedAsync(ENoteContext context)
        {
            if (await context.Courses.AnyAsync())
                return;

            await SeedCourses(context);
            await SeedLectures(context);
            await SeedInstruments(context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedCourses(ENoteContext context)
        {
            var instructorId = await context.Instructors
                .Select(i => i.Id)
                .FirstAsync();

            context.Courses.AddRange(
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
        }

        private static async Task SeedLectures(ENoteContext context)
        {            
            var courses = await context.Courses
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
        }

        private static async Task SeedInstruments(ENoteContext context)
        {
            var shopId = await context.MusicShops
                .Select(s => s.Id)
                .FirstAsync();

            var stringTypeId = await context.InstrumentTypes
                .Where(x => x.Type == "Žičani")
                .Select(x => x.Id)
                .FirstAsync();

            var percussionTypeId = await context.InstrumentTypes
                .Where(x => x.Type == "Udaraljke")
                .Select(x => x.Id)
                .FirstAsync();

            var brassTypeId = await context.InstrumentTypes
                .Where(x => x.Type == "Limeni") 
                .Select(x => x.Id)
                .FirstAsync();

            var keysTypeId = await context.InstrumentTypes
                .Where(x => x.Type == "Tipke")
                .Select(x => x.Id)
                .FirstAsync();

            var accessoriesTypeId = await context.InstrumentTypes
                .Where(x => x.Type == "Dodatna oprema") 
                .Select(x => x.Id)
                .FirstAsync();

            context.Instruments.AddRange(               
                new Instrument
                {
                    Manufacturer = "Fender",
                    Model = "Stratocaster",
                    Description = "Klasična električna gitara poznata po svojoj svestranosti i glatkoj svirljivosti.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/strat.webp"
                },
                new Instrument
                {
                    Manufacturer = "Gibson",
                    Model = "Les Paul",
                    Description = "Legendarna električna gitara omiljena zbog bogatog tona i održavanja.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/les-paul.webp"
                },
                new Instrument
                {
                    Manufacturer = "Ibanez",
                    Model = "RG",
                    Description = "Visokoperformansna električna gitara popularna među rok i metal sviračima.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/rg.webp"
                },
                new Instrument
                {
                    Manufacturer = "PRS",
                    Model = "Custom 24",
                    Description = "Visokokvalitetna električna gitara poznata po svojoj prelijepoj izradi i zvuku.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/prs.webp"
                },
                new Instrument
                {
                    Manufacturer = "Yamaha",
                    Model = "Pacifica",
                    Description = "Svestrana električna gitara pogodna za različite žanrove.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/pacifica.webp"
                },
                new Instrument
                {
                    Manufacturer = "Jackson",
                    Model = "Dinky",
                    Description = "Električna gitara dizajnirana za brzo sviranje i snažan zvuk.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/dinky.webp"
                },
                new Instrument
                {
                    Manufacturer = "Taylor",
                    Model = "214ce",
                    Description = "Svestrana i lijepo izrađena akustična gitara, poznata po svom svijetlom i artikulisanom tonu.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/214ce.webp"
                },
                new Instrument
                {
                    Manufacturer = "Martin",
                    Model = "D-28",
                    Description = "Ikonična dreadnought gitara sa bogatom historijom, poznata po svom dubokom, rezonantnom basu i jasnim visokim tonovima.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/d-28.webp"
                },
                new Instrument
                {
                    Manufacturer = "Gibson",
                    Model = "J-45",
                    Description = "Često nazivan \"radnim konjem\" među akustičnim gitarama, ovaj dreadnought sa zaobljenim ramenima pruža topao, blag ton koji je savršen za kantautore.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/j-45.webp"
                },
                new Instrument
                {
                    Manufacturer = "Seagull",
                    Model = "S6",
                    Description = "S6 proizvodi topao, bogat zvuk sa blago rustičnim karakterom, što je čini omiljenom među muzičarima koji sviraju folk i roots muziku.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/s6.webp"
                },
                new Instrument
                {
                    Manufacturer = "Fender",
                    Model = "Precision Bass",
                    Description = "Industrijski standard bas gitara poznata po dubokom, udarnom zvuku.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/precision.webp"
                },
                new Instrument
                {
                    Manufacturer = "Gibson",
                    Model = "Thunderbird",
                    Description = "Ikonična bas gitara poznata po jedinstvenom dizajnu i snažnom zvuku.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/thunderbird.webp"
                },
                new Instrument
                {
                    Manufacturer = "Music Man",
                    Model = "StingRay",
                    Description = "Legendarna električna bas gitara, prepoznatljiva po svom moćnom, artikulisanom zvuku, elegantnom dizajnu i vrhunskoj svirljivosti.",
                    MusicShopId = shopId,
                    InstrumentTypeId = stringTypeId,
                    ImagePath = "instruments/stingray.webp"
                },

                new Instrument
                {
                    Manufacturer = "Pearl",
                    Model = "Export",
                    Description = "Pristupačan bubanj set savršen za početnike i srednje napredne bubnjare.",
                    MusicShopId = shopId,
                    InstrumentTypeId = percussionTypeId,
                    ImagePath = "instruments/export.webp"
                },
                new Instrument
                {
                    Manufacturer = "Tama",
                    Model = "Imperialstar",
                    Description = "Svestran bubanj set sa izvrsnom izradom i zvukom.",
                    MusicShopId = shopId,
                    InstrumentTypeId = percussionTypeId,
                    ImagePath = "instruments/imperialstar.webp"
                },
                new Instrument
                {
                    Manufacturer = "Ludwig",
                    Model = "Breakbeats",
                    Description = "Kompaktni bubanj set dizajniran za prenosivost i odličan ton.",
                    MusicShopId = shopId,
                    InstrumentTypeId = percussionTypeId,
                    ImagePath = "instruments/breakbeats.webp"
                },

                new Instrument
                {
                    Manufacturer = "Yamaha",
                    Model = "YAS-280",
                    Description = "Popularni saksofon među studentima i srednje naprednim sviračima.",
                    MusicShopId = shopId,
                    InstrumentTypeId = brassTypeId,
                    ImagePath = "instruments/yas.webp"
                },
                new Instrument
                {
                    Manufacturer = "Bach",
                    Model = "Stradivarius",
                    Description = "Profesionalni trombon poznat po bogatom tonu i preciznoj intonaciji.",
                    MusicShopId = shopId,
                    InstrumentTypeId = brassTypeId,
                    ImagePath = "instruments/stradivarius.webp"
                },

                new Instrument
                {
                    Manufacturer = "Korg",
                    Model = "Minilogue",
                    Description = "Analogni sintisajzer poznat po svom bogatom, toplom zvuku.",
                    MusicShopId = shopId,
                    InstrumentTypeId = keysTypeId,
                    ImagePath = "instruments/minilogue.webp"
                },
                new Instrument
                {
                    Manufacturer = "Roland",
                    Model = "Juno-DS",
                    Description = "Svestrani sintisajzer popularan za žive nastupe i studijsku upotrebu.",
                    MusicShopId = shopId,
                    InstrumentTypeId = keysTypeId,
                    ImagePath = "instruments/juno-ds.webp"
                },

                new Instrument
                {
                    Manufacturer = "Fender",
                    Model = "Blues Junior IV",
                    Description = "Kompaktno, ali snažno cijevno pojačalo koje pruža klasičan Fender ton sa dodanom modernom svestranošću.",
                    MusicShopId = shopId,
                    InstrumentTypeId = accessoriesTypeId,
                    ImagePath = "instruments/blues-junior.webp"
                },
                new Instrument
                {
                    Manufacturer = "Marshall",
                    Model = "DSL40CR-DS",
                    Description = "Vrlo svestrano cijevno pojačalo koje nudi sve, od klasične rock distorzije do žestokih solaža.",
                    MusicShopId = shopId,
                    InstrumentTypeId = accessoriesTypeId,
                    ImagePath = "instruments/dsl40cr.webp"
                },
                new Instrument
                {
                    Manufacturer = "Vox",
                    Model = "AC15C1",
                    Description = "Poznato po svojim svijetlim čistim tonovima i karakterističnom \"Top Boost\" overdrive efektu, savršeno je za one koji traže vintage britanski zvuk.",
                    MusicShopId = shopId,
                    InstrumentTypeId = accessoriesTypeId,
                    ImagePath = "instruments/ac15c1.webp"
                },
                new Instrument
                {
                    Manufacturer = "Orange",
                    Model = "Rocker 15",
                    Description = "Idealno je za kućne probe i manje nastupe, nudeći niz tonova od čistog do prljavog sa jednostavnim i preglednim kontrolama.",
                    MusicShopId = shopId,
                    InstrumentTypeId = accessoriesTypeId,
                    ImagePath = "instruments/rocker-15.webp"
                },
                new Instrument
                {
                    Manufacturer = "Boss",
                    Model = "Katana-100 MkII",
                    Description = "Moderno digitalno pojačalo koje kombinuje veliku snagu sa nevjerovatnom svestranošću.",
                    MusicShopId = shopId,
                    InstrumentTypeId = accessoriesTypeId,
                    ImagePath = "instruments/katana-100.webp"
                }
            );
        }
    }
}
