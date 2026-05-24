using eNote.Application.Constants;
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
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<ENoteContext>();

            await SeedRoles(roleManager);
            await SeedUsers(userManager, context);
        }

        private static async Task SeedRoles(RoleManager<AppRole> roleManager)
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
                        throw new Exception($"Greška pri kreiranju uloge {role}: {errors}");
                    }
                }
            }
        }

        private static async Task SeedUsers(UserManager<AppUser> userManager, ENoteContext context)
        {
            var defaultStoreId = await EnsureDefaultStoreAsync(context);

            var testUsers = new[]
            {
                //("administrator", "admin@enote.com", AppRoles.Administrator),
                ("instructor", "instructor@enote.com", AppRoles.Instructor),
                ("student", "student@enote.com", AppRoles.Student),
                ("storeemployee", "storeEmployee@enote.com", AppRoles.StoreEmployee)
            };

            foreach (var (username, email, role) in testUsers)
            {
                var user = await userManager.FindByNameAsync(username);

                if (user == null)
                {
                    user = new AppUser
                    {
                        UserName = username,
                        Email = email,
                        EmailConfirmed = true,
                        IsActive = true
                    };

                    var createResult = await userManager.CreateAsync(user, "test1234");

                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                        throw new Exception($"Error creating user {username}: {errors}");
                    }
                }
                else
                {
                    if (user.Email != email)
                    {
                        user.Email = email;
                        user.NormalizedEmail = userManager.NormalizeEmail(email);
                    }

                    if (!user.EmailConfirmed)
                        user.EmailConfirmed = true;

                    if (!user.IsActive)
                        user.IsActive = true;

                    var updateResult = await userManager.UpdateAsync(user);

                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new Exception($"Error updating user {username}: {errors}");
                    }
                }

                await EnsureOneRoleAsync(userManager, user, role);

                EnsureRoleProfile(context, user.Id, role, defaultStoreId);

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureOneRoleAsync(UserManager<AppUser> userManager, AppUser user, string intendedRole)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            
            var toRemove = currentRoles.Where(r => r != intendedRole).ToArray();

            if (toRemove.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

                if (!removeResult.Succeeded)
                {
                    var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    throw new Exception($"Error removing roles from {user.UserName}: {errors}");
                }
            }
            
            if (!currentRoles.Contains(intendedRole))
            {
                var addResult = await userManager.AddToRoleAsync(user, intendedRole);

                if (!addResult.Succeeded)
                {
                    var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    throw new Exception($"Error assigning role {intendedRole} to {user.UserName}: {errors}");
                }
            }
        }

        private static void EnsureRoleProfile(ENoteContext context, int userId, string role, int defaultStoreId)
        {
            switch (role)
            {
                case AppRoles.Student:
                    if (!context.Students.Any(x => x.AppUserId == userId))
                        context.Students.Add(new Student(userId, DateTime.UtcNow.AddMonths(-3)));
                    break;

                case AppRoles.Instructor:
                    if (!context.Instructors.Any(x => x.AppUserId == userId))
                        context.Instructors.Add(new Instructor(userId));
                    break;

                case AppRoles.StoreEmployee:
                    {
                        var employees = context.StoreEmployees
                            .Where(x => x.AppUserId == userId)
                            .ToList();

                        if (employees.Count == 0)
                        {
                            context.StoreEmployees.Add(
                                new MusicStoreEmployee(userId, defaultStoreId, true));
                            break;
                        }

                        // Keep exactly one active employee-store membership for predictable auth context.
                        var activeEmployees = employees.Where(x => x.IsActive).ToList();
                        var primary = activeEmployees.FirstOrDefault() ?? employees[0];
                        primary.IsActive = true;

                        foreach (var employee in employees.Where(x => x.Id != primary.Id))
                            employee.IsActive = false;

                        break;
                    }
            }
        }

        private static async Task<int> EnsureDefaultStoreAsync(ENoteContext context)
        {
            var storeId = await context.MusicStores
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (storeId.HasValue)
                return storeId.Value;

            var store = new MusicStore("Test Music Store", "09:00-17:00");
            context.MusicStores.Add(store);
            await context.SaveChangesAsync();
            return store.Id;
        }
    }
}
