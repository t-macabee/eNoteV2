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

    /// <summary>
    /// Bypasses database store-id resolution when set (tests and tooling).
    /// Production requests resolve the store through <see cref="GetCurrentStoreIdAsync"/> instead.
    /// </summary>
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

        modelBuilder.Entity<InstrumentRental>().HasQueryFilter(r => r.Instrument.IsActive && (GetStoreId() == null || r.MusicStoreId == GetStoreId()));

        modelBuilder.Entity<Instrument>().HasQueryFilter(i => i.IsActive && (GetStoreId() == null || i.MusicStoreId == GetStoreId()));

        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.IsActive && (GetStoreId() == null || a.MusicStoreId == null || a.MusicStoreId == GetStoreId()));

        ModelBuilderSeed.Seed(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<Lecture>()
                .Property(l => l.Version)
                .HasValueGenerator<InMemoryRowVersionGenerator>();
        }
    }

    private sealed class InMemoryRowVersionGenerator : Microsoft.EntityFrameworkCore.ValueGeneration.ValueGenerator<byte[]>
    {
        public override byte[] Next(EntityEntry entry) => Guid.NewGuid().ToByteArray()[..8];

        public override bool GeneratesTemporaryValues => false;
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
