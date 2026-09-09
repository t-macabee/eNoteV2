using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Data.Seed;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

        var context = serviceProvider.GetRequiredService<ENoteContext>();

        var provisioningService = serviceProvider.GetRequiredService<IUserProvisioningService>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Password handed to reviewers (RS2 Upute section 5 credentials table).
        // It is deliberately short and deliberately NOT run through Identity's
        // password policy: the policy in DependencyInjection stays strict for
        // real registrations, and these seeded demo accounts get their hash
        // applied directly in ApplyReviewPasswordAsync below.
        var reviewPassword = configuration["Seed:DefaultPassword"] ?? "test";

        await RoleSeed.SeedRoles(roleManager);

        var defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);

        (string, string, string, int?)[] testUsers = new[]
        {
            ("desktop", "desktop@enote.com", AppRoles.Administrator, default(int?)),
            ("mobile", "mobile@enote.com", AppRoles.Student, default(int?)),
            ("admin", "admin@enote.com", AppRoles.Administrator, default(int?)),
            ("instructor", "instructor@enote.com", AppRoles.Instructor, default(int?)),
            ("student", "student@enote.com", AppRoles.Student, default(int?)),
            ("storeemployee", "storeEmployee@enote.com", AppRoles.StoreEmployee, defaultStoreId)
        };

        foreach ((var username, var email, var role, var storeId) in testUsers)
        {
            (var _, var error) = await provisioningService.ProvisionUserAsync(new UserProvisionRequest
            {
                Username = username,
                Email = email,
                Password = BootstrapPassword,
                Role = role,
                MusicStoreId = storeId
            });

            if (error is not null)
            {
                throw new BusinessException(error);
            }

            await ApplyReviewPasswordAsync(userManager, username, reviewPassword);
        }
    }

    // Satisfies the Identity policy so ProvisionUserAsync can create the account;
    // never used to log in, because ApplyReviewPasswordAsync overwrites the hash.
    private const string BootstrapPassword = "Seed1234!";

    private static async Task ApplyReviewPasswordAsync(UserManager<AppUser> userManager, string username, string password)
    {
        var user = await userManager.FindByNameAsync(username)
            ?? throw new BusinessException(Messages.UserUpdateFailed(username, "korisnik nije pronađen nakon kreiranja."));

        var verification = userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);

        if (verification != PasswordVerificationResult.Failed)
        {
            return;
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new BusinessException(Messages.UserUpdateFailed(username, string.Join("; ", result.Errors.Select(e => e.Description))));
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
        {
            return storeId.Value;
        }

        var store = new MusicStore("Test Music Store", "09:00-17:00", addressId: 1);

        context.Set<MusicStore>().Add(store);

        await context.SaveChangesAsync();

        return store.Id;
    }
}
