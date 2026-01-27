using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Users.DTOs;
using eNote.Application.Features.Users.Profiles;
using eNote.Application.Features.Users.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Users.Services
{
    public class UserService(IAppDbContext context, IUserIdentityService identity) : IUserService
    {
        private readonly IAppDbContext _context = context;
        private readonly IUserIdentityService _identity = identity;

        public async Task<UserProfileResponse?> GetCurrentUserAsync(int userId)
        {
            var user = await _identity.GetUserAsync(userId);

            if (user == null || !user.IsActive)
                return null;

            var roles = await _identity.GetRolesAsync(userId);

            if (roles.Count == 0)
                throw new InvalidOperationException("Korisnik nema dodijeljenu ulogu.");

            var role = roles[0];

            IUserProfile profile = role switch
            {
                AppRoles.Student => await BuildStudentProfile(userId, user),
                AppRoles.Instructor => await BuildInstructorProfile(userId, user),
                AppRoles.MusicShop => await BuildMusicShopProfile(userId, user),
                _ => throw new InvalidOperationException("Nepoznata uloga.")
            };

            return new UserProfileResponse(role, profile);
        }

        private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user)
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId) 
                ?? throw new InvalidOperationException("Student profil nije pronađen.");

            return new StudentProfile(
                student.Id,
                student.EnrollmentDate,
                user.FirstName,
                user.LastName,
                user.DateOfBirth,
                user.Address
            );
        }

        private async Task<InstructorProfile> BuildInstructorProfile(int userId, UserIdentityDto user)
        {
            var instructor = await _context.Instructors
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId)
                ?? throw new InvalidOperationException("Instruktor profil nije pronađen.");

            return new InstructorProfile(
                instructor.Id,
                user.FirstName,
                user.LastName
            );
        }

        private async Task<MusicShopProfile> BuildMusicShopProfile(int userId, UserIdentityDto user)
        {
            var shop = await _context.MusicShops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId)
                ?? throw new InvalidOperationException("Music shop profil nije pronađen.");

            return new MusicShopProfile(
                shop.Id,
                shop.StoreName,
                shop.BusinessHours,
                user.Address
            );
        }
    }
}
