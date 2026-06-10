using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Users.Profiles;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
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

            if (roles.Count != 1)
                throw new BusinessException("Korisnik mora imati tačno jednu ulogu.");

            var role = roles[0];

            IUserProfile profile = role switch
            {
                AppRoles.Student => await BuildStudentProfile(userId, user),
                AppRoles.Instructor => await BuildInstructorProfile(userId, user),
                AppRoles.StoreEmployee => await BuildMusicStoreProfile(userId, user),
                _ => throw new BusinessException("Nepoznata uloga.")
            };

            return new UserProfileResponse(role, profile);
        }

        private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(_context, userId);

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
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(_context, userId);

            return new InstructorProfile(
                instructor.Id,
                user.FirstName,
                user.LastName
            );
        }

        private async Task<MusicStoreProfile> BuildMusicStoreProfile(int userId, UserIdentityDto user)
        {
            var employee = await UserProfileHelper.GetActiveEmployeeByUserIdAsync(_context, userId);

            var shop = await _context.Set<MusicStore>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employee.MusicStoreId)
                ?? throw new BusinessException("Radnja nije pronađena.");

            return new MusicStoreProfile(
                shop.Id,
                shop.StoreName,
                shop.BusinessHours,
                user.Address
            );
        }
    }
}
