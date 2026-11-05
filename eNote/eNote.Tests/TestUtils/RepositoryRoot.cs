namespace eNote.Tests.TestUtils;

public static class RepositoryRoot
{
    private const string MarkerFileName = ".env.docker.example";

    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, MarkerFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find {MarkerFileName} above {AppContext.BaseDirectory}.");
    }
}
