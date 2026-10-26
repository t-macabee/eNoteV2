using eNote.Infrastructure.Configuration;

namespace eNote.Tests.Configuration;

public sealed class DotEnvConfigurationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"enote-dotenv-{Guid.NewGuid():N}");

    public DotEnvConfigurationTests() => Directory.CreateDirectory(root);

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void FindEnvFile_WalksUpFromStartDirectory()
    {
        var envFile = WriteEnv(root);
        var nested = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;

        Assert.Equal(envFile, DotEnvConfiguration.FindEnvFile(nested));
    }

    [Fact]
    public void FindEnvFile_FallsBackToLaterStartDirectory_WhenFirstHasNoEnv()
    {
        var withoutEnv = Directory.CreateDirectory(Path.Combine(root, "unrelated")).FullName;
        var repo = Directory.CreateDirectory(Path.Combine(root, "repo")).FullName;
        var envFile = WriteEnv(repo);
        var binDirectory = Directory.CreateDirectory(Path.Combine(repo, "bin", "Debug")).FullName;

        Assert.Equal(envFile, DotEnvConfiguration.FindEnvFile(withoutEnv, binDirectory));
    }

    [Fact]
    public void FindEnvFile_PrefersTheFirstStartDirectory()
    {
        var first = Directory.CreateDirectory(Path.Combine(root, "first")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(root, "second")).FullName;
        var firstEnv = WriteEnv(first);
        WriteEnv(second);

        Assert.Equal(firstEnv, DotEnvConfiguration.FindEnvFile(first, second));
    }

    [Fact]
    public void FindEnvFile_ReturnsNull_WhenNoDirectoryHasEnv()
    {
        var isolated = Directory.CreateDirectory(Path.Combine(root, "isolated")).FullName;

        var found = DotEnvConfiguration.FindEnvFile(isolated);

        Assert.True(found is null || !found.StartsWith(root, StringComparison.Ordinal));
    }

    private static string WriteEnv(string directory)
    {
        var path = Path.Combine(directory, ".env");
        File.WriteAllText(path, "A=1");
        return path;
    }
}
