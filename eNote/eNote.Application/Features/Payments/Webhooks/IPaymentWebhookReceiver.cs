namespace eNote.Application.Features.Payments.Webhooks;

public interface IPaymentWebhookReceiver
{
    Task HandleAsync(string rawJson, string signatureHeader, CancellationToken cancellationToken = default);
}
