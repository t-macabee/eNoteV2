using Mapster;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public sealed class InstrumentRentalMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InstrumentRental, InstrumentRentalDto>()
            .Map(x => x.InstrumentModel, x => x.Instrument.Model)
            .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
            .Map(x => x.MusicStoreId, x => x.Instrument.MusicStoreId)
            .Map(x => x.StoreName, x => x.Instrument.MusicStore.StoreName)
            .Map(x => x.StudentUserId, x => x.StudentProfile.AppUserId)
            .Map(x => x.InstrumentImagePath, x => x.Instrument.ImagePath)
            .Map(x => x.IsPaid, x => x.IsPaid)
            .Map(x => x.AmountPaid, x => x.AmountPaid)
            .Map(x => x.PaidAt, x => x.PaidAt);
    }
}
