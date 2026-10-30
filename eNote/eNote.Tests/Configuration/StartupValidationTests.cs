using System.Diagnostics;

namespace eNote.Tests.Configuration;

public sealed class StartupValidationTests
{
    [Fact]
    public async Task WorkerStartup_StopsBeforeConsuming_WhenSmtpHostIsEmpty()
    {
        var result = await RunAsync("eNote.Worker", WorkerSettings(new Dictionary<string, string>
        {
            ["Smtp__Host"] = string.Empty
        }));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Smtp__Host is required.", result.Output);
    }

    [Fact]
    public async Task WorkerStartup_StopsBeforeConsuming_WhenRabbitMqIsMissing()
    {
        var result = await RunAsync("eNote.Worker", WorkerSettings(new Dictionary<string, string>
        {
            ["RabbitMQ__Host"] = string.Empty
        }));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("RabbitMQ__Host is required.", result.Output);
        Assert.DoesNotContain("MassTransit.RabbitMqConnectionException", result.Output);
    }

    [Fact]
    public async Task ApiStartup_Stops_WhenJwtKeyIsEmpty()
    {
        var result = await RunAsync("eNote.API", ApiSettings(new Dictionary<string, string>
        {
            ["Jwt__Key"] = string.Empty
        }));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Jwt__Key is required.", result.Output);
    }

    [Fact]
    public async Task ApiStartup_Stops_WhenStripeSecretKeyIsEmpty()
    {
        var result = await RunAsync("eNote.API", ApiSettings(new Dictionary<string, string>
        {
            ["Stripe__SecretKey"] = string.Empty
        }));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Stripe__SecretKey is required.", result.Output);
    }

    [Fact]
    public async Task ApiStartup_ValidatesBeforeSeeding()
    {
        var result = await RunAsync("eNote.API", ApiSettings(new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["Seed__Enabled"] = "true",
            ["Jwt__Key"] = string.Empty,
            ["ConnectionStrings__DefaultConnection"] =
                "Server=127.0.0.1,1;Database=enote-order-test;User Id=sa;Password=Placeholder1!;TrustServerCertificate=True;Connect Timeout=1"
        }));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Jwt__Key is required.", result.Output);
        Assert.DoesNotContain("SqlException", result.Output);
    }

    private static async Task<ProcessResult> RunAsync(string projectName, Dictionary<string, string> settings)
    {
        var assemblyPath = ResolveAssemblyPath(projectName);
        var workingDirectory = Directory.CreateTempSubdirectory("enote-startup-");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(assemblyPath);

            foreach (var (key, value) in settings)
            {
                startInfo.Environment[key] = value;
            }

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {projectName}.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                Assert.Fail($"{projectName} did not stop at startup.");
            }

            return new ProcessResult(process.ExitCode, $"{await outputTask}{Environment.NewLine}{await errorTask}");
        }
        finally
        {
            Directory.Delete(workingDirectory.FullName, recursive: true);
        }
    }

    private static string ResolveAssemblyPath(string projectName)
    {
        var tfmDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var configuration = tfmDirectory.Parent?.Name;
        var eNoteDirectory = tfmDirectory.Parent?.Parent?.Parent?.Parent;

        var path = eNoteDirectory is null || configuration is null
            ? null
            : Path.Combine(eNoteDirectory.FullName, projectName, "bin", configuration, tfmDirectory.Name, $"{projectName}.dll");

        return path is not null && File.Exists(path)
            ? path
            : throw new FileNotFoundException($"Could not find {projectName}.dll next to the test build output ({path}).");
    }

    private static Dictionary<string, string> ApiSettings(Dictionary<string, string>? overrides = null)
    {
        var settings = BaseSettings();
        settings["ASPNETCORE_ENVIRONMENT"] = "Production";
        settings["Jwt__Key"] = "test-signing-key-that-is-at-least-32-chars";
        settings["Jwt__Issuer"] = "ENote.Api";
        settings["Jwt__Audience"] = "ENote.Client";
        settings["Jwt__ExpirationMinutes"] = "30";
        settings["Stripe__SecretKey"] = "sk_test_123";
        settings["Stripe__WebhookSecret"] = "whsec_123";
        settings["Stripe__Currency"] = "bam";
        settings["Stripe__StatementDescriptor"] = "ENOTE";
        settings["Storage__Root"] = "storage";
        settings["Cors__AllowedOrigins__0"] = "https://localhost";
        settings["Seed__Enabled"] = "false";
        settings["Seed__DefaultPassword"] = "test";

        Apply(settings, overrides);

        return settings;
    }

    private static Dictionary<string, string> WorkerSettings(Dictionary<string, string>? overrides = null)
    {
        var settings = BaseSettings();
        settings["DOTNET_ENVIRONMENT"] = "Production";

        Apply(settings, overrides);

        return settings;
    }

    private static Dictionary<string, string> BaseSettings() => new()
    {
        ["ConnectionStrings__DefaultConnection"] = "Server=localhost;Database=enote-startup-test;User Id=sa;Password=Placeholder1!;TrustServerCertificate=True",
        ["RabbitMQ__Host"] = "localhost",
        ["RabbitMQ__VirtualHost"] = "/",
        ["RabbitMQ__User"] = "guest",
        ["RabbitMQ__Password"] = "guest",
        ["Smtp__Host"] = "localhost",
        ["Smtp__Port"] = "1025",
        ["Smtp__EnableSsl"] = "false",
        ["Smtp__From"] = "noreply@example.com",
        ["Smtp__PasswordResetUrl"] = "https://localhost/reset-password"
    };

    private static void Apply(
        Dictionary<string, string> settings,
        Dictionary<string, string>? overrides)
    {
        if (overrides is null)
        {
            return;
        }

        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
