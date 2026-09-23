using eNote.Application.Constants;
using eNote.Application.Features.Academic;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using MapsterMapper;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    ICurrentUserContext currentUser, IStudentContext students,
    IStudentDisplayNameService displayNames,
    InstructorAccessService instructorAccess,
    IFileStorageService fileStorage,
    ISubmissionNotificationDispatcher notificationDispatcher,
    IMapper mapper)
{
    public async Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var (student, assignment, existing) = await ValidateSubmissionAsync(assignmentId, ct);

        var path = await fileStorage.SaveAssignmentAsync(stream, fileName, contentType, ct);

        try
        {
            if (existing is null)
            {
                existing = new AssignmentSubmission(assignment.Id, student.Id)
                {
                    CreatedById = currentUser.UserId
                };
                assignment.AssignmentSubmissions.Add(existing);
            }

            existing.Submit(path?.Trim(), clock.UtcNow);
            existing.UpdatedById = currentUser.UserId;

            await SaveWithSubmissionConflictMessageAsync(ct);
        }
        catch
        {
            fileStorage.Delete(path);
            throw;
        }

        return MapSubmission(existing, await displayNames.GetStudentDisplayNameAsync(student, ct));
    }

    public async Task<AssignmentSubmissionDto> GetOwnSubmissionAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync(cancellationToken);

        var assignmentExists = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId, clock.UtcNow)
            .AnyAsync(cancellationToken);
        if (!assignmentExists)
        {
            throw new NotFoundException(Messages.AssignmentNotFound);
        }

        var submission = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId && x.StudentId == student.Id, cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        return MapSubmission(submission, await displayNames.GetStudentDisplayNameAsync(submission.Student, cancellationToken));
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetHistoryForStudentAsync(AssignmentSubmissionSearchObject search, CancellationToken cancellationToken = default)
    {
        var student = await students.GetCurrentStudentAsync(cancellationToken);

        var accessibleAssignments = context.Set<Assignment>()
            .ForEnrolledStudent(student.Id, clock.UtcNow);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.StudentId == student.Id && accessibleAssignments.Any(a => a.Id == x.AssignmentId));

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var submissions = await query
            .OrderByDescending(x => x.SubmittedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var names = await displayNames.GetStudentDisplayNamesAsync(submissions.Select(x => x.Student), cancellationToken);

        return new PagedResult<AssignmentSubmissionDto>
        {
            Items = [.. submissions.Select(x => MapSubmission(x, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search, CancellationToken cancellationToken = default)
    {
        _ = await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == assignmentId);

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var submissions = await query
            .OrderBy(x => x.StudentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var names = await displayNames.GetStudentDisplayNamesAsync(submissions.Select(x => x.Student), cancellationToken);

        return new PagedResult<AssignmentSubmissionDto>
        {
            Items = [.. submissions.Select(x => MapSubmission(x, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var assignment = await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var submission = await context.Set<AssignmentSubmission>()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId, cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        if (submission.SubmittedAt is null)
        {
            throw new BusinessException(Messages.AssignmentNotSubmitted);
        }

        submission.SetGrade(request.Grade, request.Feedback);
        submission.UpdatedById = currentUser.UserId;

        await notificationDispatcher.DispatchGradedAsync(submission.Id, submission.Student.AppUserId, assignment.Title, request.Grade);

        await context.SaveChangesAsync(cancellationToken);

        return MapSubmission(submission, await displayNames.GetStudentDisplayNameAsync(submission.Student, cancellationToken));
    }

    private async Task<(Student Student, Assignment Assignment, AssignmentSubmission? Existing)> ValidateSubmissionAsync(int assignmentId, CancellationToken cancellationToken)
    {
        var student = await students.GetCurrentStudentAsync(cancellationToken);

        var assignment = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId, clock.UtcNow)
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

        return (student, assignment, existing);
    }

    private async Task SaveWithSubmissionConflictMessageAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex))
        {
            throw new ConflictException(Messages.AssignmentAlreadySubmitted);
        }
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, cancellationToken: cancellationToken);
    }

    private AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, string studentName)
    {
        var dto = mapper.Map<AssignmentSubmissionDto>(submission);
        dto.StudentName = studentName;
        return dto;
    }
}
