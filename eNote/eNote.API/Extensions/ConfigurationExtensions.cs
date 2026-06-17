using DotNetEnv;

namespace eNote.API.Extensions
{
    public static class ConfigurationExtensions
    {
        public static void LoadDotEnv()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                string envFile = Path.Combine(directory.FullName, ".env");

                if (File.Exists(envFile))
                {
                    Env.Load(envFile);
                    return;
                }

                directory = directory.Parent;
            }
        }

        public static void ValidateRequiredSettings(this IConfiguration configuration)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
            {
                errors.Add("ConnectionStrings__DefaultConnection");
            }

            string? jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                errors.Add("JWT__Key");
            }
            else if (jwtKey.Length < 32)
            {
                errors.Add("JWT__Key (minimum 32 characters)");
            }

            if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
            {
                errors.Add("JWT__Issuer");
            }

            if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            {
                errors.Add("JWT__Audience");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Missing or invalid required configuration values: " + string.Join(", ", errors));
            }
        }
    }
}
