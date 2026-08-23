using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;

namespace eNote.API.Middleware;

public sealed class TenantInitializationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, ICurrentActor actor)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            try { await actor.GetCurrentStoreIdAsync(httpContext.RequestAborted); }
            catch (StoreNotResolvedException) {  }
        }

        await next(httpContext);
    }
}
