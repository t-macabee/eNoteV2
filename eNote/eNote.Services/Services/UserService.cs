using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Model.Auth;
using eNote.Model.Profiles;
using eNote.Model.Shared;
using eNote.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace eNote.Service.Services
{
    public class UserService(ENoteContext context, UserManager<AppUser> userManager) : IUserService
    {
        private readonly ENoteContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<UserProfileResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.Status)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains(AppRoles.Student))
            {
                var profile = await GetStudent(userId)
                    ?? throw new InvalidOperationException("Student uloga nije vezana za studenta.");

                    return new UserProfileResponse(AppRoles.Student, profile);
            }

            if (roles.Contains(AppRoles.Instructor))
            { 
                var profile = await GetInstructor(userId) 
                    ?? throw new InvalidOperationException("Instruktor uloga nije vezana za instruktora.");

                return new UserProfileResponse(AppRoles.Instructor, profile);
            }

            if (roles.Contains(AppRoles.MusicShop))
            {
                var profile = await GetMusicShop(userId)
                    ?? throw new InvalidOperationException("Music Shop uloga nije vezana za shop.");

                return new UserProfileResponse(AppRoles.MusicShop, profile);
            }                

            return null;
        }

        private async Task<IUserProfile?> GetStudent(int userId)
        {
            return await _context.Students
           .AsNoTracking()
           .Where(s => s.AppUserId == userId)
           .Select(s => new StudentProfile(
               s.Id,
               s.EnrollmentDate,
               s.AppUser.FirstName,
               s.AppUser.LastName,
               s.AppUser.DateOfBirth,
               MapAddress(s.AppUser)
           ))        
           .FirstOrDefaultAsync();
        }

        private async Task<IUserProfile?> GetInstructor(int userId)
        {
            return await _context.Instructors
                .AsNoTracking()
                .Where(i => i.AppUserId == userId)
                .Select(i => new InstructorProfile(
                    i.Id,
                    i.AppUser.FirstName,
                    i.AppUser.LastName
                ))
                .FirstOrDefaultAsync();
        }

        private async Task<IUserProfile?> GetMusicShop(int userId)
        {
            return await _context.MusicShops
                .AsNoTracking()
                .Where(m => m.AppUserId == userId)
                .Select(m => new MusicShopProfile(
                    m.Id,
                    m.StoreName,
                    m.BusinessHours,
                    MapAddress(m.AppUser)
                ))
                .FirstOrDefaultAsync();
        }

        private static AddressDto? MapAddress(AppUser user)
        {
            var a = user.Address;
            return a == null ? null : new AddressDto(a.City, a.Street, a.Number);
        }
    }    
}
