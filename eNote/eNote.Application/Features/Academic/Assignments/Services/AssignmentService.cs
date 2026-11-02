using eNote.Application.Features.Identity.Instructors;
using MapsterMapper;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentService(
    IAppDbContext context,
    ICurrentUserContext currentUser, IStudentContext students,
    InstructorAccessService instructorAccess,
    IMapper mapper,
    IClock clock)
{
    public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var query = instructorAccess.AssignmentsForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt), cancellationToken);
    }

    public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default) =>
        mapper.Map<AssignmentDto>(await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken: cancellationToken));

    public async Task<AssignmentDto> CreateAsync(int lectureId, AssignmentCreateRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId, cancellationToken);

        var entity = new Assignment(request.Title.Trim(), request.Description.Trim(), request.DueAt, lectureId);

        context.Set<Assignment>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true, cancellationToken: cancellationToken);

        entity.UpdateDetails(request.Title.Trim(), request.Description.Trim(), request.DueAt);

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var query = context.Set<Assignment>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId, clock.UtcNow)
            .ApplySearch(search);

        if (search.IsSubmitted is { } isSubmitted)
        {
            query = query.Where(x => x.AssignmentSubmissions.Any(s => s.StudentId == studentId && s.SubmittedAt != null) == isSubmitted);
        }

        return await query.ToPagedResultAsync<Assignment, AssignmentSearchObject, AssignmentDto>(
            search,
            async (assignments, ct) =>
            {
                var dtos = assignments.Select(mapper.Map<AssignmentDto>).ToList();
                await ApplyOwnSubmissionAsync(studentId, dtos, ct);
                return dtos;
            },
            q => q.OrderBy(x => x.DueAt),
            cancellationToken);
    }

    public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);

        var entity = await context.Set<Assignment>()
            .ForEnrolledStudentById(studentId, assignmentId, clock.UtcNow)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        var dto = mapper.Map<AssignmentDto>(entity);
        await ApplyOwnSubmissionAsync(studentId, [dto], cancellationToken);

        return dto;
    }

    private async Task ApplyOwnSubmissionAsync(int studentId, IReadOnlyCollection<AssignmentDto> dtos, CancellationToken cancellationToken)
    {
        var assignmentIds = dtos.Select(x => x.Id).ToList();

        var submissions = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Where(x => x.StudentId == studentId && assignmentIds.Contains(x.AssignmentId))
            .ToListAsync(cancellationToken);

        var byAssignment = submissions.ToDictionary(x => x.AssignmentId);

        foreach (var dto in dtos)
        {
            if (byAssignment.TryGetValue(dto.Id, out var submission))
            {
                dto.MySubmittedAt = submission.SubmittedAt;
                dto.MyGrade = submission.Grade;
            }
        }
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, bool track = false, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, track, cancellationToken);
    }
}
