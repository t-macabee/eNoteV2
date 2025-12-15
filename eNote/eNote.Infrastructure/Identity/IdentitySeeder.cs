using eNote.Application.DTOs.Auth;
using eNote.Domain.Entities;
using eNote.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<ENoteContext>();

            await SeedRoles(roleManager);
            await SeedUsers(userManager, context);
        }

        private static async Task SeedRoles(RoleManager<AppRole> roleManager)
        {
            string[] roles = [ AppRoles.Administrator, AppRoles.Instructor, AppRoles.Student, AppRoles.MusicShop ];

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
            var testUsers = new[] { //{ ("administrator", "admin@enote.com", AppRoles.Administrator), 
                ("instructor", "instructor@enote.com",AppRoles.Instructor),
                ("student", "student@enote.com", AppRoles.Student), 
                ("musicshop", "shop@enote.com", AppRoles.MusicShop)
            };

            foreach (var (username, email, role) in testUsers)
            {
                if (await userManager.FindByNameAsync(username) != null)
                    continue;                

                var user = new AppUser
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

                var roleResult = await userManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Error assigning role {role} to {username}: {errors}");
                }

                AddRoleProfile(context, user.Id, role);

                await context.SaveChangesAsync();                
            }
        }

        private static void AddRoleProfile(ENoteContext context, int userId, string role)
        {
            switch (role)
            {
                case AppRoles.Student: context.Students.Add(new Student{ AppUserId = userId, EnrollmentDate = DateTime.UtcNow.AddMonths(-3)});
                    break;

                case AppRoles.Instructor: context.Instructors.Add(new Instructor { AppUserId = userId }); 
                    break;

                case AppRoles.MusicShop: context.MusicShops.Add(new MusicShop { AppUserId = userId, StoreName = "Test Music Shop", BusinessHours = "09:00–17:00"});
                    break;
            }
        }
    }
}

