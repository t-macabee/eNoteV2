using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Users.Services
{
    public class UserContextResolver(IAppDbContext context, IUserIdentityService identity) : IUserContextResolver
    {
        public async Task<Student> GetStudentAsync(int userId) => 
            await context.Set<Student>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId)
            ?? throw new Common.Exceptions.BusinessException(Messages.StudentProfileNotFound);

        public async Task<Instructor> GetInstructorAsync(int userId) =>
            await context.Set<Instructor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId)
            ?? throw new Common.Exceptions.BusinessException(Messages.InstructorProfileNotFound);

        public async Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
            await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
            ?? throw new Common.Exceptions.BusinessException(Messages.EmployeeProfileNotFound);

        public async Task<string> GetStudentDisplayNameAsync(Student student)
        {
            var user = await identity.GetUserAsync(student.AppUserId);

            if (user is null)
                return $"Student {student.Id}";

            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
        }
    }
}
