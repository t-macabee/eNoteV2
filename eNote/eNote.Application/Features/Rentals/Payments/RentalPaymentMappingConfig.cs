using Mapster;

namespace eNote.Application.Features.Rentals.Payments;

public sealed class RentalPaymentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RentalPayment, RentalPaymentDto>()
            .Map(dest => dest.RentalId, src => src.InstrumentRentalId)
            .Map(dest => dest.PaymentIntentId, src => src.StripePaymentIntentId)
            .Map(dest => dest.AmountCents, src => src.AmountChargedCents);
    }
}
