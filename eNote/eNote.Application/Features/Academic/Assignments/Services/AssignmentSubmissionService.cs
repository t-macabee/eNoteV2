using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using MapsterMapper;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    ICurrentActor actor,
    IStudentDisplayNameService displayNames,
    InstructorAccessService instructorAccess,
    IFileStorageService fileStorage,
    ISubmissionNotificationDispatcher notificationDispatcher,
    IMapper mapper)
{
    public async Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var path = await fileStorage.SaveAssignmentAsync(stream, fileName, contentType, ct);
        return await SubmitAsync(assignmentId, path, ct);
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == assignmentId);

        return await query.ToPagedResultAsync(
            search,
            items => displayNames.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
            (x, names) => MapSubmission(x, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")),
            q => q.OrderBy(x => x.StudentId),
            cancellationToken);
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var assignment = await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var submission = await context.Set<AssignmentSubmission>()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId, cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        submission.SetGrade(request.Grade);
        submission.UpdatedById = actor.UserId;

        await notificationDispatcher.DispatchGradedAsync(submission.Id, submission.Student.AppUserId, assignment.Title, request.Grade);

        await context.SaveChangesAsync(cancellationToken);

        return MapSubmission(submission, await displayNames.GetStudentDisplayNameAsync(submission.Student));
    }

    private async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, string filePath, CancellationToken cancellationToken)
    {
        var student = await actor.GetCurrentStudentAsync();

        var assignment = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId)
            .Include(x => x.AssignmentSubmissions)
            .FirstOrDefaultAsync(cancellationToken)
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
                CreatedById = actor.UserId
            };
            assignment.AssignmentSubmissions.Add(existing);
        }

        existing.Submit(filePath?.Trim(), clock.UtcNow);
        existing.UpdatedById = actor.UserId;

        await SaveWithSubmissionConflictMessageAsync(cancellationToken);

        return MapSubmission(existing, await displayNames.GetStudentDisplayNameAsync(student));
    }

    private async Task SaveWithSubmissionConflictMessageAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains(DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex) == true)
        {
            throw new ConflictException(Messages.AssignmentAlreadySubmitted);
        }
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, cancellationToken: cancellationToken);
    }

    private AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, string studentName)
    {
        var dto = mapper.Map<AssignmentSubmissionDto>(submission);
        dto.StudentName = studentName;
        return dto;
    }
}
