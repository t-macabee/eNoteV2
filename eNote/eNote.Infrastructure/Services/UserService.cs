using eNote.Application.DTOs.Auth;
using eNote.Application.DTOs.Profiles;
using eNote.Application.DTOs.Shared;
using eNote.Application.Interfaces;
using eNote.Infrastructure.Data.Context;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Services
{
    public class UserService(ENoteContext context, UserManager<AppUser> userManager) : IUserService
    {
        private readonly ENoteContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<UserProfileResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
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
            return await (
                from s in _context.Students.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on s.AppUserId equals u.Id
                where s.AppUserId == userId
                select new StudentProfile(
                    s.Id,
                    s.EnrollmentDate,
                    u.FirstName,
                    u.LastName,
                    u.DateOfBirth,
                    MapAddress(u)
                )
            ).FirstOrDefaultAsync();
        }

        private async Task<IUserProfile?> GetInstructor(int userId)
        {
            return await (
                from i in _context.Instructors.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on i.AppUserId equals u.Id
                where i.AppUserId == userId
                select new InstructorProfile(
                    i.Id,
                    u.FirstName,
                    u.LastName
                )
            ).FirstOrDefaultAsync();
        }

        private async Task<IUserProfile?> GetMusicShop(int userId)
        {
            return await (
                from m in _context.MusicShops.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on m.AppUserId equals u.Id
                where m.AppUserId == userId
                select new MusicShopProfile(
                    m.Id,
                    m.StoreName,
                    m.BusinessHours,
                    MapAddress(u)
                )
            ).FirstOrDefaultAsync();
        }

        private static AddressDto? MapAddress(AppUser user)
        {
            var a = user.Address;
            return a == null ? null : new AddressDto(a.City, a.Street, a.Number);
        }
    }    
}
