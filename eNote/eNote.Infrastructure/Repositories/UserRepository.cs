using eNote.Application.Interfaces;
using eNote.Application.Models.Auth;
using eNote.Application.Models.Profile;
using eNote.Application.Models.Shared;
using eNote.Infrastructure.Data.Context;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Repositories
{
    public class UserRepository(ENoteContext context, UserManager<AppUser> userManager) : IUserRepository
    {
        private readonly ENoteContext _context = context;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<UserProfileResponse?> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Address)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            IUserProfile? profile = roles.Contains(AppRoles.Student)
                ? await GetStudentProfile(userId, user)
                : roles.Contains(AppRoles.Instructor)
                    ? await GetInstructorProfile(userId, user)
                    : roles.Contains(AppRoles.MusicShop)
                        ? await GetMusicShopProfile(userId, user)
                        : null;

            if (profile == null)
                throw new InvalidOperationException("Uloga nije vezana za odgovarajući profil.");

            return new UserProfileResponse(roles.First(), profile);
        }

        private async Task<StudentProfile?> GetStudentProfile(int userId, AppUser user)
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AppUserId == userId);

            return student == null
                ? null
                : new StudentProfile(
                    student.Id,
                    student.EnrollmentDate,
                    user.FirstName ?? string.Empty,
                    user.LastName ?? string.Empty,
                    user.DateOfBirth,
                    user.Address == null ? null : new AddressDto(user.Address.City, user.Address.Street, user.Address.Number));
        }

        private async Task<InstructorProfile?> GetInstructorProfile(int userId, AppUser user)
        {
            var instructor = await _context.Instructors
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.AppUserId == userId);

            return instructor == null
                ? null
                : new InstructorProfile(
                    instructor.Id,
                    user.FirstName ?? string.Empty,
                    user.LastName ?? string.Empty);
        }

        private async Task<MusicShopProfile?> GetMusicShopProfile(int userId, AppUser user)
        {
            var shop = await _context.MusicShops
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.AppUserId == userId);

            return shop == null
                ? null
                : new MusicShopProfile(
                    shop.Id,
                    shop.StoreName,
                    shop.BusinessHours,
                    user.Address == null ? null : new AddressDto(user.Address.City, user.Address.Street, user.Address.Number));
        }
    }
}
