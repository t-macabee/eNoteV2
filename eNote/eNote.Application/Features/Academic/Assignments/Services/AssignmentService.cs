using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Students;
using eNote.Domain.Entities.Assignments;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentService(
    IAppDbContext context,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ICurrentUserService currentUserService,
    IMapper mapper) : IAssignmentService
{
    public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);

        var query = instructorAccess.AssignmentsForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt));
    }

    public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId) =>
        mapper.Map<AssignmentDto>(await GetOwnedAssignmentAsync(lectureId, assignmentId));

    public async Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var entity = new Assignment(request.Title.Trim(), request.Description.Trim(), request.DueAt, lectureId)
        {
            CreatedById = currentUserService.UserId
        };

        context.Set<Assignment>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true);

        entity.UpdateDetails(request.Title.Trim(), request.Description.Trim(), request.DueAt);
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int assignmentId)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search)
    {
        var studentId = await resolver.GetCurrentStudentIdAsync(currentUserService.UserId);

        var query = context.Set<Assignment>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt));
    }

    public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId)
    {
        var studentId = await resolver.GetCurrentStudentIdAsync(currentUserService.UserId);

        var entity = await context.Set<Assignment>()
            .ForEnrolledStudentById(studentId, assignmentId)
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        return mapper.Map<AssignmentDto>(entity);
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, track);
    }
}
