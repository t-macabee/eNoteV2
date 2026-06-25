using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.Students;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorage) : IAssignmentSubmissionService
{
    public async Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var path = await fileStorage.SaveAssignmentAsync(stream, fileName, contentType, ct);
        return await SubmitAsync(assignmentId, new AssignmentSubmitRequest { FilePath = path });
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == assignmentId);

        return await query.ToPagedResultAsync(
            search,
            items => resolver.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
            (x, names) => MapSubmission(x, x.Student, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")),
            q => q.OrderBy(x => x.StudentId));
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId);

        var submission = await context.Set<AssignmentSubmission>()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        submission.SetGrade(request.Grade);
        submission.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return MapSubmission(submission, submission.Student, await resolver.GetStudentDisplayNameAsync(submission.Student));
    }

    private async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, AssignmentSubmitRequest request)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var assignment = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId)
            .Include(x => x.AssignmentSubmissions)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        var existing = assignment.AssignmentSubmissions.FirstOrDefault(x => x.StudentId == student.Id);

        if (existing?.SubmittedAt is not null)
        {
            throw new ConflictException(Messages.AssignmentAlreadySubmitted);
        }

        if (clock.UtcNow > assignment.DueAt)
        {
            throw new BusinessException(Messages.AssignmentPastDue);
        }

        if (existing is null)
        {
            existing = new AssignmentSubmission(assignment.Id, student.Id)
            {
                CreatedById = currentUserService.UserId
            };
            assignment.AssignmentSubmissions.Add(existing);
        }

        existing.Submit(request.FilePath?.Trim(), clock.UtcNow);
        existing.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return MapSubmission(existing, student, await resolver.GetStudentDisplayNameAsync(student));
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId);
    }
    private static AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, Student student, string studentName) => new()
    {
        Id = submission.Id,
        AssignmentId = submission.AssignmentId,
        StudentId = submission.StudentId,
        StudentName = studentName,
        SubmittedAt = submission.SubmittedAt,
        FilePath = submission.FilePath,
        Grade = submission.Grade
    };
}
