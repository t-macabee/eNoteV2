using Asp.Versioning;
using eNote.Infrastructure.Payments.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace eNote.API.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments/stripe/webhook")]
[AllowAnonymous]
public sealed class StripeWebhookController(StripeWebhookService webhookService) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        string json;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(cancellationToken);
        }

        var signature = Request.Headers["Stripe-Signature"].ToString();

        await webhookService.HandleAsync(json, signature, cancellationToken);

        return Ok();
    }
}
