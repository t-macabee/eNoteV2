using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace eNote.API.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string AuthPolicy = "auth";

        public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(AuthPolicy, opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}
