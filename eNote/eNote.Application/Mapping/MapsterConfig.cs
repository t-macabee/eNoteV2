using eNote.Application.DTOs;
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
        }
    }
}
