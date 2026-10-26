using DotNetEnv;

namespace eNote.Infrastructure.Configuration;

public static class DotEnvConfiguration
{
    public static void Load()
    {
        var envFile = FindEnvFile(Directory.GetCurrentDirectory(), AppContext.BaseDirectory);

        if (envFile is not null)
        {
            Env.Load(envFile);
        }
    }

    public static string? FindEnvFile(params string[] startDirectories)
    {
        foreach (var start in startDirectories)
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var envFile = Path.Combine(directory.FullName, ".env");

                if (File.Exists(envFile))
                {
                    return envFile;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
