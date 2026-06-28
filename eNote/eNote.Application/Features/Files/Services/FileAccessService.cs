using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Files.Services;

public sealed class FileAccessService(
    IAppDbContext context,
    IUserProfileLookup lookup,
    IInstructorAccessService instructorAccess,
    IUserIdentityService identity) : IFileAccessService
{
    public async Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default)
    {
        var apiPath = $"/api/uploads/assignments/{fileName}";
        var legacyPath = $"/uploads/assignments/{fileName}";

        var submission = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Assignment)
                .ThenInclude(x => x.Lecture)
                .ThenInclude(x => x.Course)
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.FilePath == apiPath || x.FilePath == legacyPath, cancellationToken);

        if (submission is null)
        {
            return false;
        }

        var roles = await identity.GetRolesAsync(userId);

        if (roles.Contains(AppRoles.Administrator))
        {
            return true;
        }

        if (roles.Contains(AppRoles.Student))
        {
            var student = await lookup.GetStudentAsync(userId);
            return submission.StudentId == student.Id;
        }

        if (roles.Contains(AppRoles.Instructor))
        {
            var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(userId);
            return submission.Assignment.Lecture.Course.InstructorId == instructorId;
        }

        return false;
    }
}