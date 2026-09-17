using Mapster;

namespace eNote.Application.Features.Academic.Tuition;

public sealed class TuitionPaymentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CoursePayment, CoursePaymentDto>()
            .Map(dest => dest.PaymentIntentId, src => src.StripePaymentIntentId)
            .Map(dest => dest.AmountCents, src => src.AmountChargedCents);
    }
}
