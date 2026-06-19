using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Data.Seed
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            RoleManager<AppRole> roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

            ENoteContext context = serviceProvider.GetRequiredService<ENoteContext>();

            IUserService userService = serviceProvider.GetRequiredService<IUserService>();

            await RoleSeed.SeedRoles(roleManager);

            int defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);

            (string, string, string, int?)[] testUsers = new[]
            {
                ("admin", "admin@enote.com", AppRoles.Administrator, (int?)null),
                ("instructor", "instructor@enote.com", AppRoles.Instructor, (int?)null),
                ("student", "student@enote.com", AppRoles.Student, (int?)null),
                ("storeemployee", "storeEmployee@enote.com", AppRoles.StoreEmployee, (int?)defaultStoreId)
            };

            foreach ((string? username, string? email, string? role, int? storeId) in testUsers)
            {
                (int _, string? error) = await userService.ProvisionUserAsync(new UserProvisionRequest
                {
                    Username = username,
                    Email = email,
                    Password = "Test1234!",
                    Role = role,
                    MusicStoreId = storeId
                });

                if (error is not null)
                {
                    throw new BusinessException(error);
                }
            }
        }
    }

    internal static class RoleSeed
    {
        public static async Task SeedRoles(RoleManager<AppRole> roleManager)
        {
            string[] roles = [AppRoles.Administrator, AppRoles.Instructor, AppRoles.Student, AppRoles.StoreEmployee];

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    IdentityResult result = await roleManager.CreateAsync(new AppRole { Name = role });

                    if (!result.Succeeded)
                    {
                        string errors = string.Join("; ", result.Errors.Select(e => e.Description));

                        throw new BusinessException(Messages.RoleCreateFailed(role, errors));
                    }
                }
            }
        }
    }

    internal static class StoreSeed
    {
        public static async Task<int> EnsureDefaultStoreAsync(ENoteContext context)
        {
            int? storeId = await context.Set<MusicStore>()
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (storeId.HasValue)
            {
                return storeId.Value;
            }

            MusicStore store = new MusicStore("Test Music Store", "09:00-17:00");

            context.Set<MusicStore>().Add(store);

            await context.SaveChangesAsync();

            return store.Id;
        }
    }
}
