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

public class ENoteContext(DbContextOptions<ENoteContext> options, IClock clock, ICurrentUserContext currentUser) : IdentityDbContext<AppUser, AppRole, int>(options), IAppDbContext, IStoreContext
{
    private int? _storeId;
    private bool _storeResolved;

    public int? ExplicitStoreId { get; set; }

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

        modelBuilder.Entity<InstrumentRental>().HasQueryFilter(r => GetStoreId() == null || r.MusicStoreId == GetStoreId());

        modelBuilder.Entity<Instrument>().HasQueryFilter(i => GetStoreId() == null || i.MusicStoreId == GetStoreId());

        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.IsActive && (GetStoreId() == null || a.MusicStoreId == null || a.MusicStoreId == GetStoreId()));

        ConfigureAuditUserForeignKeys(modelBuilder);

        ModelBuilderSeed.Seed(modelBuilder);
    }

    private static void ConfigureAuditUserForeignKeys(ModelBuilder modelBuilder)
    {
        var auditableEntityType = typeof(AuditableEntity);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!auditableEntityType.IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(ENoteContext)
                .GetMethod(nameof(ConfigureAuditUserForeignKeysForEntity), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder]);
        }
    }

    private static void ConfigureAuditUserForeignKeysForEntity<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TEntity>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private int? GetStoreId()
    {
        if (!_storeResolved && ExplicitStoreId.HasValue)
        {
            _storeId = ExplicitStoreId;
            _storeResolved = true;
        }

        return _storeId;
    }

    public async Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_storeResolved)
        {
            var storeId = await Set<MusicStoreEmployee>()
                .AsNoTracking()
                .Where(x => x.AppUserId == currentUser.UserId && x.IsActive)
                .Select(x => (int?)x.MusicStoreId)
                .SingleOrDefaultAsync(cancellationToken);

            _storeId = storeId ?? throw new StoreNotResolvedException(Messages.ActiveEmployeeStoreNotFound);
            _storeResolved = true;
        }

        return _storeId!.Value;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var isAuthenticated = currentUser.IsAuthenticated;
        var userId = isAuthenticated ? currentUser.UserId : (int?)null;

        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;

                if (userId.HasValue)
                {
                    entry.Entity.CreatedById = userId.Value;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;

                if (userId.HasValue)
                {
                    entry.Entity.UpdatedById = userId.Value;
                }
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
