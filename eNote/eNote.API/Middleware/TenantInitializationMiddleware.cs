using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

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
                // The auth controller must stay reachable: a store-less StoreEmployee
                // still has to be able to revoke their own token via logout.
                if (httpContext.User.IsInRole(AppRoles.StoreEmployee) && !IsAuthControllerRequest(httpContext))
                {
                    throw new AuthorizationException(Messages.Forbidden);
                }
            }
        }

        await next(httpContext);
    }

    private static bool IsAuthControllerRequest(HttpContext httpContext) =>
        httpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { ControllerName: "Auth" };
}
