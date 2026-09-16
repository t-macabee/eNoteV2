using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;

namespace eNote.API.Middleware;

public sealed class TenantInitializationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, IStoreContext storeContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await storeContext.GetCurrentStoreIdAsync(httpContext.RequestAborted);
            }
            catch (StoreNotResolvedException)
            {
                if (httpContext.User.IsInRole(AppRoles.StoreEmployee))
                {
                    throw new AuthorizationException(Messages.Forbidden);
                }
            }
        }

        await next(httpContext);
    }
}
