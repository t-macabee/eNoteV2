using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace eNote.Tests.TestUtils;

public sealed class StubWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "eNote.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
