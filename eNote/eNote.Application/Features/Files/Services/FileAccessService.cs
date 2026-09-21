using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Files.Services;

public sealed class FileAccessService(
    IAppDbContext context,
    IUserProfileLookup lookup,
    InstructorAccessService instructorAccess,
    IUserIdentityService identity) : IFileAccessService
{
    private const string AssignmentApiPath = "/api/v1/uploads/assignments/";
    private const string AssignmentLegacyPath = "/api/uploads/assignments/";
    private const string AssignmentLegacyPathV0 = "/uploads/assignments/";

    public async Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default)
    {
        var apiPath = AssignmentApiPath + fileName;
        var legacyPath = AssignmentLegacyPath + fileName;
        var legacyPathV0 = AssignmentLegacyPathV0 + fileName;

        var authData = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Where(x => x.FilePath == apiPath || x.FilePath == legacyPath || x.FilePath == legacyPathV0)
            .Select(x => new
            {
                x.StudentId,
                InstructorId = x.Assignment.Lecture.Course.InstructorId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (authData is null)
        {
            return false;
        }

        var roles = await identity.GetRolesAsync(userId, cancellationToken);

        if (roles.Contains(AppRoles.Administrator))
        {
            return true;
        }

        if (roles.Contains(AppRoles.Student))
        {
            var student = await lookup.GetStudentAsync(userId, cancellationToken);
            return authData.StudentId == student.Id;
        }

        if (roles.Contains(AppRoles.Instructor))
        {
            var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(userId, cancellationToken);
            return authData.InstructorId == instructorId;
        }

        return false;
    }
}