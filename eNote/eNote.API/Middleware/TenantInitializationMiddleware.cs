using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;

namespace eNote.API.Middleware;

public sealed class TenantInitializationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, IStoreContext storeContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            try { await storeContext.GetCurrentStoreIdAsync(httpContext.RequestAborted); }
            catch (StoreNotResolvedException) {  }
        }

        await next(httpContext);
    }
}
