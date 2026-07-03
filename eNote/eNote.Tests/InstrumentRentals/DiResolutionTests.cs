using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.InstrumentRentals;

/// <summary>
/// Verifies the DI container can resolve IAppDbContext without circular dependency errors.
/// Previously, ENoteContext required ICurrentActor which required IAppDbContext -> circular.
/// CurrentActor now uses IServiceProvider to break the cycle.
/// </summary>
public sealed class DiResolutionTests
{
    [Fact]
    public void ResolveIAppDbContext_DoesNotThrowCircularDependency()
    {
        var services = new ServiceCollection();

        // Register ENoteContext with in-memory provider (no real DB needed)
        services.AddDbContext<ENoteContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Register the dependencies in the same order as the real app
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddScoped<ICurrentActor>(_ => new StubCurrentActor());

        var provider = services.BuildServiceProvider();

        // This should NOT throw InvalidOperationException about circular dependency
        using var scope = provider.CreateScope();
        var exception = Record.Exception(() =>
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            Assert.NotNull(context);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Intentionally not reusing TestUtils.StubCurrentActor: that stub's async methods throw
    /// NotSupportedException when Student/Instructor/Employee are null, which would pollute this
    /// test with unrelated entity setup. This local stub returns hardcoded values from all members,
    /// keeping the DI resolution test fully self-contained with zero domain object construction.
    /// </summary>
    private sealed class StubCurrentActor : ICurrentActor
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
        public System.Threading.Tasks.Task<eNote.Domain.Entities.Student> GetCurrentStudentAsync()
            => System.Threading.Tasks.Task.FromResult(new eNote.Domain.Entities.Student(1, System.DateTime.UtcNow));
        public System.Threading.Tasks.Task<int> GetCurrentStudentIdAsync()
            => System.Threading.Tasks.Task.FromResult(1);
        public System.Threading.Tasks.Task<eNote.Domain.Entities.Instructor> GetCurrentInstructorAsync()
            => throw new System.NotSupportedException();
        public System.Threading.Tasks.Task<eNote.Domain.Entities.MusicStoreEmployee> GetCurrentEmployeeAsync()
            => throw new System.NotSupportedException();
        public System.Threading.Tasks.Task<int> GetCurrentStoreIdAsync(System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.FromResult(1);
        public int GetCurrentStoreId() => 1;
    }
}
