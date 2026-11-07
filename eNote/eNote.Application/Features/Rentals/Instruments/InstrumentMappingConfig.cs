using Mapster;

namespace eNote.Application.Features.Rentals.Instruments;

public sealed class InstrumentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Instrument, InstrumentDto>()
            .Map(dest => dest.ManufacturerName, src => src.Manufacturer.Name)
            .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
            .Map(dest => dest.MonthlyFee, src => src.InstrumentType.MonthlyFee)
            .Map(dest => dest.MusicStore, src => src.MusicStore.StoreName);
    }
}
