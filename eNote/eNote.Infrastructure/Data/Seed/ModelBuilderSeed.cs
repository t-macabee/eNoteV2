using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

internal static class ModelBuilderSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedCities(modelBuilder);
        SeedAddresses(modelBuilder);
        SeedInstrumentTypes(modelBuilder);
    }

    private static void SeedCities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>().HasData(
            new City { Id = 1, Name = "Sarajevo" },
            new City { Id = 2, Name = "Mostar" },
            new City { Id = 3, Name = "Banja Luka" },
            new City { Id = 4, Name = "Tuzla" },
            new City { Id = 5, Name = "Zenica" }
        );
    }

    private static void SeedAddresses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>().HasData(
            new Address { Id = 1, CityId = 1, Street = "Bistrik", Number = "12" },
            new Address { Id = 2, CityId = 1, Street = "Maršala Tita", Number = "15" },
            new Address { Id = 3, CityId = 1, Street = "Mula Mustafe Bašeskije", Number = "8" },
            new Address { Id = 4, CityId = 1, Street = "Obala Kulina bana", Number = "18" },
            new Address { Id = 5, CityId = 1, Street = "Veliki Alifakovac", Number = "14" }
        );
    }

    private static void SeedInstrumentTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstrumentType>().HasData(
            new InstrumentType { Id = 1, Type = "Žičani", MonthlyFee = 45m },
            new InstrumentType { Id = 2, Type = "Udaraljke", MonthlyFee = 35m },
            new InstrumentType { Id = 3, Type = "Limeni", MonthlyFee = 55m },
            new InstrumentType { Id = 4, Type = "Tipke", MonthlyFee = 65m },
            new InstrumentType { Id = 5, Type = "Dodatna oprema", MonthlyFee = 15m }
        );
    }
}
