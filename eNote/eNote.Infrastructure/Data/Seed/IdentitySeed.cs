using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
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
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

            var context = serviceProvider.GetRequiredService<ENoteContext>();

            var userService = serviceProvider.GetRequiredService<IUserService>();

            await RoleSeed.SeedRoles(roleManager);

            var defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);

            var testUsers = new[]
            {
                ("admin", "admin@enote.com", AppRoles.Administrator, (int?)null),
                ("instructor", "instructor@enote.com", AppRoles.Instructor, (int?)null),
                ("student", "student@enote.com", AppRoles.Student, (int?)null),
                ("storeemployee", "storeEmployee@enote.com", AppRoles.StoreEmployee, (int?)defaultStoreId)
            };

            foreach (var (username, email, role, storeId) in testUsers)
            {
                var (_, error) = await userService.ProvisionUserAsync(new UserProvisionRequest
                {
                    Username = username,
                    Email = email,
                    Password = "Test1234!",
                    Role = role,
                    MusicStoreId = storeId
                });

                if (error is not null)
                    throw new BusinessException(error);
            }
        }
    }

    internal static class RoleSeed
    {
        public static async Task SeedRoles(RoleManager<AppRole> roleManager)
        {
            string[] roles = [AppRoles.Administrator, AppRoles.Instructor, AppRoles.Student, AppRoles.StoreEmployee];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new AppRole { Name = role });

                    if (!result.Succeeded)
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));

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
            var storeId = await context.Set<MusicStore>()
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (storeId.HasValue)
                return storeId.Value;

            var store = new MusicStore("Test Music Store", "09:00-17:00");

            context.Set<MusicStore>().Add(store);

            await context.SaveChangesAsync();

            return store.Id;
        }
    }
}
