using DotNetEnv;

namespace eNote.Infrastructure.Configuration;

public static class DotEnvConfiguration
{
    public static void Load()
    {
        DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());

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
}
