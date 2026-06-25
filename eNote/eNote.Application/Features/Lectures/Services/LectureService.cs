using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.Students;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ILogger<LectureService> logger,
    ICurrentUserService currentUserService,
    IMapper mapper) : ILectureService
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, includeAttendances: true);
        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var entity = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .AsNoTracking()
            .ForEnrolledStudent(student.Id)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);

        var query = instructorAccess.LecturesFor(instructorId)
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ForEnrolledStudent(student.Id)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
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
            CreatedById = currentUserService.UserId
        };

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
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
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, currentUserService.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.Cancel();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }
}
