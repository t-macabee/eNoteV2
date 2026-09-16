using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace eNote.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy<string, AuthRateLimiterPolicy>(AuthPolicy);

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    public static RateLimitPartition<string> AuthPartition(string key) =>
        RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
}

public sealed class AuthRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public const string IdentityItemKey = "AuthRateLimitIdentity";

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";

        if (httpContext.Items.TryGetValue(IdentityItemKey, out var value) &&
            value is string identity && !string.IsNullOrWhiteSpace(identity))
        {
            key = $"user:{identity.Trim().ToLowerInvariant()}";
        }

        return RateLimitingExtensions.AuthPartition(key);
    }
}

public static class AuthRateLimitIdentity
{
    private const int MaxBodyBytes = 4096;

    // The rate limiter's partition callback is synchronous and Kestrel forbids synchronous
    // body reads, so AuthRateLimitIdentityMiddleware peeks the body asynchronously before
    // UseRateLimiter and stashes the identity for the policy. Body-keyed only for
    // login / forgot-password / reset-password; register keeps the IP bucket because a new
    // username has no stable identity yet.
    public static bool IsIdentityAction(HttpContext httpContext) =>
        httpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is
        { ControllerName: "Auth", ActionName: "Login" or "ForgotPassword" or "ResetPassword" };

    public static async Task<string?> ReadIdentityAsync(HttpRequest request)
    {
        if (request.ContentLength is not (> 0 and <= MaxBodyBytes))
        {
            return null;
        }

        request.EnableBuffering();

        var buffer = new byte[request.ContentLength.Value];
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await request.Body.ReadAsync(buffer.AsMemory(read), request.HttpContext.RequestAborted);
            if (count == 0)
            {
                break;
            }

            read += count;
        }

        request.Body.Position = 0;

        try
        {
            using var document = JsonDocument.Parse(buffer.AsMemory(0, read));
            string? email = null;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (property.Name.Equals("username", StringComparison.OrdinalIgnoreCase))
                {
                    var value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }
                else if (email is null && property.Name.Equals("email", StringComparison.OrdinalIgnoreCase))
                {
                    email = property.Value.GetString();
                }
            }

            return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
