using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data
{
    public class ENoteContext(DbContextOptions<ENoteContext> options) : IdentityDbContext<AppUser, AppRole, int>(options), IAppDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);

            ModelBuilderSeed.Seed(modelBuilder);
        }        
    }
}
