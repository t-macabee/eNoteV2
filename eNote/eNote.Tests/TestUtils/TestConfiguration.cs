using Microsoft.Extensions.Configuration;

namespace eNote.Tests.TestUtils;

public static class TestConfiguration
{
    public static IConfiguration Create() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=x;",
            })
            .Build();
}
