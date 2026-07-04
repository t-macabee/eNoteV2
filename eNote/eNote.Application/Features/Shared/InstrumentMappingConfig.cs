using eNote.Application.Features.Rentals.Instruments;
using Mapster;

namespace eNote.Application.Features.Shared;

public sealed class InstrumentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Instrument, InstrumentDto>()
            .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
            .Map(dest => dest.MusicStore, src => src.MusicStore.StoreName);
    }
}