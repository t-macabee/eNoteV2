using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Domain.Entities.Rentals;
using Mapster;

namespace eNote.Application.Features.Shared;

public sealed class InstrumentRentalMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InstrumentRental, InstrumentRentalDto>()
            .Map(x => x.InstrumentModel, x => x.Instrument.Model)
            .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
            .Map(x => x.MusicStoreId, x => x.Instrument.MusicStoreId)
            .Map(x => x.StoreName, x => x.Instrument.MusicStore.StoreName)
            .Map(x => x.StudentUserId, x => x.StudentProfile.AppUserId);
    }
}