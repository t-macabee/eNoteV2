using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Academic.Courses;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    IMapper mapper) : IAssignmentService
{
    public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.AssignmentsForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt), cancellationToken);
    }

    public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default) =>
        mapper.Map<AssignmentDto>(await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken: cancellationToken));

    public async Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId, cancellationToken);

        var entity = new Assignment(request.Title.Trim(), request.Description.Trim(), request.DueAt, lectureId)
        {
            CreatedById = actor.UserId
        };

        context.Set<Assignment>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true, cancellationToken: cancellationToken);

        entity.UpdateDetails(request.Title.Trim(), request.Description.Trim(), request.DueAt);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true, cancellationToken: cancellationToken);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Assignment>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt), cancellationToken);
    }

    public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<Assignment>()
            .ForEnrolledStudentById(studentId, assignmentId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        return mapper.Map<AssignmentDto>(entity);
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, bool track = false, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, track, cancellationToken);
    }
}
