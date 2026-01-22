using eNote.Application.DTOs;
using eNote.Application.Requests.Instruments;
using eNote.Domain.Entities;
using Mapster;

namespace eNote.Application.Mapping
{
    public static class MapsterConfig
    {
        public static void RegisterMappings(TypeAdapterConfig config)
        {
            config.NewConfig<Address, AddressDto>();

            config.NewConfig<Instrument, InstrumentDto>()                
                .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
                .Map(dest => dest.MusicShop, src => src.MusicShop.StoreName)
                .Map(dest => dest.ImagePath, src => src.ImagePath);

            TypeAdapterConfig<InstrumentRental, InstrumentRentalDto>.NewConfig()
                .Map(x => x.InstrumentModel, x => x.Instrument.Model)
                .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
                .Map(x => x.MusicShopId, x => x.Instrument.MusicShopId)
                .Map(x => x.MusicShopName, x => x.Instrument.MusicShop.StoreName);

            config.NewConfig<InstrumentUpdateRequest, Instrument>()
                .IgnoreNullValues(true);
        }
    }
}
