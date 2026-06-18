namespace eNote.API.Extensions
{
    public static class CorsExtensions
    {
        public const string PolicyName = "ENoteCors";

        public static IServiceCollection AddApplicationCors(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    if (origins.Length == 0)
                    {
                        if (environment.IsDevelopment())
                        {
                            policy.AllowAnyOrigin()
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                            return;
                        }

                        throw new InvalidOperationException("Cors:AllowedOrigins must be configured for non-development environments.");
                    }

                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
