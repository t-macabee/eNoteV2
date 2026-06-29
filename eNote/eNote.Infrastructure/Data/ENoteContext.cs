using eNote.Domain.Entities;
using eNote.Domain.Entities.Shared;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
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

        // Announcement: already has MusicStoreId column (nullable for course-scoped).
        // Store employees see their store's announcements + course-scoped ones; others see all.
        var storeId = GetStoreId();
        modelBuilder.Entity<Announcement>().HasQueryFilter(a =>
            a.IsActive && (storeId == null || a.MusicStoreId == null || a.MusicStoreId == storeId));

        ModelBuilderSeed.Seed(modelBuilder);
    }

    private int? GetStoreId()
    {
        if (_storeId is not null) return _storeId;
        try { _storeId = actor.GetCurrentStoreId(); }
        catch (BusinessException ex) when (ex.Message == Messages.ActiveEmployeeStoreNotFound) { /* Not a store employee; filter will match nothing — safe */ }
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
