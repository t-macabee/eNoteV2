using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;

namespace eNote.API.Middleware;

public sealed class TenantInitializationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, ICurrentActor actor)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            try { await actor.GetCurrentStoreIdAsync(httpContext.RequestAborted); }
            catch (BusinessException ex) when (ex.Message == Messages.ActiveEmployeeStoreNotFound) { /* Not a store employee; tenant filter will match nothing — safe */ }
        }

        await next(httpContext);
    }
}
