using eNote.Tests.TestUtils;

namespace eNote.Tests.Configuration;

public sealed class ProgramPipelineOrderTests
{
    [Fact]
    public void TenantInitializationMiddleware_RunsAfterAuthentication_AndBeforeAuthorization()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "eNote", "eNote.API", "Program.cs"));

        var authentication = program.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        var tenant = program.IndexOf("app.UseMiddleware<eNote.API.Middleware.TenantInitializationMiddleware>();", StringComparison.Ordinal);
        var authorization = program.IndexOf("app.UseAuthorization();", StringComparison.Ordinal);

        Assert.True(authentication >= 0, "Program.cs does not call app.UseAuthentication().");
        Assert.True(tenant >= 0, "Program.cs does not register TenantInitializationMiddleware.");
        Assert.True(authorization >= 0, "Program.cs does not call app.UseAuthorization().");
        Assert.True(authentication < tenant, "TenantInitializationMiddleware must run after app.UseAuthentication().");
        Assert.True(tenant < authorization, "TenantInitializationMiddleware must run before app.UseAuthorization().");
    }
}
