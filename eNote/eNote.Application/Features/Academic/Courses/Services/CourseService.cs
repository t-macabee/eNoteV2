using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IInstructorAccessService instructorAccess, ILogger<CourseService> logger) : ICourseService
{
    public async Task<CourseDto> GetByIdForInstructorAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> GetByIdForStudentAsync(int id)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<Course>()
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && (c.IsPublished || c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active))) ?? throw new NotFoundException(Messages.CourseNotFound);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate));
    }

    public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search)
    {
        var query = context.Set<Course>()
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .Where(c => c.IsPublished)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate));
    }

    public async Task<CourseDto> CreateAsync(CourseRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, actor.UserId);

        var entity = new Course(
            request.Name.Trim(),
            request.Description?.Trim(),
            request.Price,
            request.StartDate,
            request.EndDate,
            instructorId)
        {
            CreatedById = actor.UserId
        };
        entity.SetPublishedStatus(request.IsPublished);

        context.Set<Course>().Add(entity);
        await context.SaveChangesAsync();

        logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, actor.UserId);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> UpdateAsync(int id, CourseRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.UpdateDetails(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate);
        entity.SetPublishedStatus(request.IsPublished);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<CourseDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId).FirstOrDefaultAsync(c => c.Id == id) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.Set<Lecture>()
            .Where(l => l.CourseId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false)
            .SetProperty(l => l.UpdatedById, actor.UserId));

        await context.SaveChangesAsync();

        logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, actor.UserId);
    }
}
