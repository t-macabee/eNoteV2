using eNote.Application.Features.Identity.Instructors;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser, IStudentContext students, InstructorAccessService instructorAccess, ILogger<CourseService> logger)
{
    public async Task<CourseDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync();

        var entity = await context.Set<Course>()
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (c.IsPublished || c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)),
                cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var query = instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate), cancellationToken);
    }

    public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Course>()
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .Where(c => c.IsPublished)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate), cancellationToken);
    }

    public async Task<CourseDto> CreateAsync(CourseRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, currentUser.UserId);

        var entity = new Course(
            request.Name.Trim(),
            request.Description?.Trim(),
            request.Price,
            request.StartDate,
            request.EndDate,
            instructorId)
        {
            CreatedById = currentUser.UserId
        };
        entity.SetPublishedStatus(request.IsPublished);

        context.Set<Course>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, currentUser.UserId);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.UpdateDetails(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate);
        entity.SetPublishedStatus(request.IsPublished);
        entity.UpdatedById = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId).FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        await context.Set<Lecture>()
            .Where(l => l.CourseId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false)
            .SetProperty(l => l.UpdatedById, currentUser.UserId), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }
}
