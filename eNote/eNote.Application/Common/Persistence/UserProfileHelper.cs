using eNote.Application.Common.Exceptions;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Persistence
{
    public static class UserProfileHelper
    {
        public static async Task<Student> GetStudentByUserIdAsync(IAppDbContext context, int userId)
        {
            return await context.Set<Student>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId)
                ?? throw new BusinessException("Student profil nije pronađen.");
        }

        public static async Task<Instructor> GetInstructorByUserIdAsync(IAppDbContext context, int userId)
        {
            return await context.Set<Instructor>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId)
                ?? throw new BusinessException("Instruktor profil nije pronađen.");
        }

        public static async Task<MusicStoreEmployee> GetActiveEmployeeByUserIdAsync(IAppDbContext context, int userId)
        {
            return await context.Set<MusicStoreEmployee>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
                ?? throw new BusinessException("Profil uposlenika radnje nije pronađen.");
        }
    }
}
