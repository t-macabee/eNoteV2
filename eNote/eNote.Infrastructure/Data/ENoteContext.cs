using eNote.Application.Common.Exceptions;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);

        var tenantEntityType = typeof(ITenantScoped);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (tenantEntityType.IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ENoteContext)
                    .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [modelBuilder, this]);
            }
        }

        modelBuilder.Entity<InstrumentRental>().HasQueryFilter(r => r.Instrument.IsActive && (GetStoreId() == null || r.MusicStoreId == GetStoreId()));

        modelBuilder.Entity<Instrument>().HasQueryFilter(i => i.IsActive && (GetStoreId() == null || i.MusicStoreId == GetStoreId()));

        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.IsActive && (GetStoreId() == null || a.MusicStoreId == null || a.MusicStoreId == GetStoreId()));

        ModelBuilderSeed.Seed(modelBuilder);
    }

    private int? GetStoreId()
    {
        if (_storeId is not null) return _storeId;
        try { _storeId = actor.GetCurrentStoreId(); }
        catch (StoreNotResolvedException) { }
        return _storeId;
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

    private static void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, ENoteContext context) where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => context.GetStoreId() == null || e.MusicStoreId == context.GetStoreId());
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }
}
