using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Students;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    ILogger<LectureService> logger,
    IMapper mapper) : ILectureService
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, includeAttendances: true);
        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.LecturesFor(instructorId)
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsCourseAsync(request.CourseId, instructorId);

        var entity = new Lecture(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.LectureType,
            request.Capacity,
            request.CourseId)
        {
            CreatedById = actor.UserId
        };

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.UpdateDetails(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.Capacity);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, actor.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.Cancel();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }
}
