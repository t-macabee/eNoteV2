using eNote.Application.Common.DTOs;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.Instruments;
using eNote.Domain.Entities;
using Mapster;

namespace eNote.Application.Mapping
{
    public sealed class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Address, AddressDto>();

            config.NewConfig<Instrument, InstrumentDto>()
                .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
                .Map(dest => dest.MusicStore, src => src.MusicStore.StoreName)
                .Map(dest => dest.ImagePath, src => src.ImagePath);

            TypeAdapterConfig<InstrumentRental, InstrumentRentalDto>.NewConfig()
                .Map(x => x.InstrumentModel, x => x.Instrument.Model)
                .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
                .Map(x => x.MusicStoreId, x => x.Instrument.MusicStoreId)
                .Map(x => x.StoreName, x => x.Instrument.MusicStore.StoreName)
                .Map(x => x.StudentProfileId, x => x.StudentProfileId)
                .Map(x => x.Fee, x => x.Fee);

            config.NewConfig<InstrumentUpdateRequest, Instrument>().IgnoreNullValues(true);

            config.Compile();
        }
    }
}
