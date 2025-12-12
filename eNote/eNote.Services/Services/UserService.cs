using eNote.Application.Interfaces;
using eNote.Contracts.Constants;
using eNote.Contracts.DTOs.Auth;
using eNote.Contracts.DTOs.Common;
using eNote.Contracts.DTOs.Profiles;
using eNote.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Services
{
    public class UserService(ENoteContext context) : IUserService
    {
        private readonly ENoteContext _context = context;

        public async Task<UserProfileResult?> GetCurrentUserAsync(int userId)
        {
            var student = await GetStudent(userId);
            if (student != null)
                return new UserProfileResult(AppRoles.Student, student);

            var instructor = await GetInstructor(userId);
            if (instructor != null)
                return new UserProfileResult(AppRoles.Instructor, instructor);

            var shop = await GetMusicShop(userId);
            if (shop != null)
                return new UserProfileResult(AppRoles.MusicShop, shop);

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
               s.AppUser.Address == null
                   ? null
                   : new Address(
                       s.AppUser.Address.City,
                       s.AppUser.Address.Street,
                       s.AppUser.Address.Number
                   )
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
                    m.AppUser.Address == null
                        ? null
                        : new Address(
                            m.AppUser.Address.City,
                            m.AppUser.Address.Street,
                            m.AppUser.Address.Number
                        )
                ))
                .FirstOrDefaultAsync();
        }
    }
}
