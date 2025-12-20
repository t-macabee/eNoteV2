using eNote.Domain.Entities.Instruments;
using eNote.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed
{
    internal static class ModelBuilderSeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            SeedAddresses(modelBuilder);
            SeedInstrumentTypes(modelBuilder);
        }

        private static void SeedAddresses(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>().HasData(
                new Address { Id = 1, City = "Sarajevo", Street = "Bistrik", Number = "12" },
                new Address { Id = 2, City = "Sarajevo", Street = "Maršala Tita", Number = "15" },
                new Address { Id = 3, City = "Sarajevo", Street = "Mula Mustafe Bašeskije", Number = "8" },
                new Address { Id = 4, City = "Sarajevo", Street = "Obala Kulina bana", Number = "18" },
                new Address { Id = 5, City = "Sarajevo", Street = "Veliki Alifakovac", Number = "14" }
            );
        }

        private static void SeedInstrumentTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InstrumentType>().HasData(
                new InstrumentType { Id = 1, Type = "Žičani" },
                new InstrumentType { Id = 2, Type = "Udaraljke" },
                new InstrumentType { Id = 3, Type = "Limeni" },
                new InstrumentType { Id = 4, Type = "Tipke" },
                new InstrumentType { Id = 5, Type = "Dodatna oprema" }
            );
        }
    }
}
