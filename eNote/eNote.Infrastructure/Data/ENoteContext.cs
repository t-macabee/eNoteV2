using eNote.Domain.Entities;
using eNote.Domain.Entities.Shared;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Infrastructure.Data;

public class ENoteContext(DbContextOptions<ENoteContext> options, IClock clock, ICurrentActor actor) : IdentityDbContext<AppUser, AppRole, int>(options), IAppDbContext
{
    private int? _storeId;

    static ENoteContext()
    {
        // App is uniformly UTC (DateTime.UtcNow everywhere) and was built on SQL Server datetime2.
        // Maps DateTime -> 'timestamp without time zone' and accepts any DateTimeKind, so client-supplied
        // dates (Kind=Unspecified from JSON/query strings) don't trip Npgsql's Kind=Utc enforcement.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);

        // Apply global tenant filter for ITenantScoped entities (Instrument, InstrumentRental)
        var tenantEntityType = typeof(ITenantScoped);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (tenantEntityType.IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ENoteContext)
                    .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder, this });
            }
        }

        ModelBuilderSeed.Seed(modelBuilder);
    }

    private int? GetStoreId()
    {
        if (_storeId is not null) return _storeId;
        try { _storeId = actor.GetCurrentStoreId(); }
        catch (InvalidOperationException) { /* Not a store employee; filter will match nothing — safe */ }
        return _storeId;
    }

    private static void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, ENoteContext context) where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.MusicStoreId == context.GetStoreId());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }
}
