using eNote.API.Extensions;

namespace eNote.API.Middleware;

public sealed class AuthRateLimitIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (AuthRateLimitIdentity.IsIdentityAction(httpContext))
        {
            var identity = await AuthRateLimitIdentity.ReadIdentityAsync(httpContext.Request);
            if (!string.IsNullOrWhiteSpace(identity))
            {
                httpContext.Items[AuthRateLimiterPolicy.IdentityItemKey] = identity;
            }
        }

        await next(httpContext);
    }
}
