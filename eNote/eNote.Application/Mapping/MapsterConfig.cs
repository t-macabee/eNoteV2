using eNote.Application.Common.DTOs;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.Instruments.DTOs;
using eNote.Application.Features.Instruments.Requests;
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
                .Map(x => x.MusicShopName, x => x.Instrument.MusicShop.StoreName)
                .Map(x => x.StudentProfileId, x => x.StudentProfileId);

            config.NewConfig<InstrumentUpdateRequest, Instrument>()
                .IgnoreNullValues(true);
        }
    }
}
