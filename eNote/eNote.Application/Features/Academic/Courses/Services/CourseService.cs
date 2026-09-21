using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser, IStudentContext students, InstructorAccessService instructorAccess, ILogger<CourseService> logger, IUserIdentityService identityService)
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

    public async Task<PagedResult<CourseDto>> GetPagedCatalogForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var query = WhereCatalogVisible(
                context.Set<Course>()
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .Include(c => c.Instructor),
                instructorId)
            .ApplySearch(search);

        return await ToPagedWithInstructorNamesAsync(query, search, cancellationToken);
    }

    public async Task<CourseDto> GetCatalogByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var entity = await LoadCourseDetailAsync(
            WhereCatalogVisible(context.Set<Course>(), instructorId),
            id,
            cancellationToken);

        var dto = mapper.Map<CourseDto>(entity);
        dto.InstructorName = await ResolveInstructorNameAsync(entity.Instructor.AppUserId, cancellationToken);
        return dto;
    }

    public async Task<List<CourseCatalogInstructorDto>> GetCatalogInstructorsAsync(CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var instructors = await WhereCatalogVisible(context.Set<Course>().AsNoTracking().Include(c => c.Instructor), instructorId)
            .Select(c => new { c.Instructor.Id, c.Instructor.AppUserId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var appUserIds = instructors.Select(i => i.AppUserId).Distinct();
        var users = await identityService.GetUsersBulkAsync(appUserIds, cancellationToken);

        return instructors
            .Select(i => new CourseCatalogInstructorDto
            {
                Id = i.Id,
                Name = UserNameHelper.FormatName(users.GetValueOrDefault(i.AppUserId))
            })
            .OrderBy(i => i.Name)
            .ToList();
    }

    public async Task<CourseCatalogSummaryDto> GetCatalogSummaryAsync(CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

        var visibleCourses = WhereCatalogVisible(context.Set<Course>().AsNoTracking(), instructorId);

        var totalCourses = await visibleCourses.CountAsync(cancellationToken);

        var totalStudents = await visibleCourses
            .SelectMany(c => c.Enrollments)
            .Where(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new CourseCatalogSummaryDto
        {
            TotalCourses = totalCourses,
            TotalStudents = totalStudents
        };
    }

    private static IQueryable<Course> WhereCatalogVisible(IQueryable<Course> query, int instructorId) =>
        query.Where(c => c.IsPublished || c.InstructorId == instructorId);

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

        // The catalog/detail path is deliberately not gated on PaidUntil: an enrolled
        // student must still find the course in order to pay for the next period.
        var dto = mapper.Map<CourseDto>(entity);
        ApplyEnrollment(dto, entity, studentId);
        return dto;
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
        var studentId = await students.GetCurrentStudentIdAsync();

        var query = context.Set<Course>()
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .ApplySearch(search);

        // Unpublishing never revokes an enrolled student's access (only catalog
        // visibility), so the IsPublished hard-filter applies to the catalog only.
        if (search.EnrolledOnly == true)
        {
            query = query.Where(c => c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));
        }
        else
        {
            query = query.Where(c => c.IsPublished);
        }

        return await query.ToPagedResultAsync(search, entity =>
        {
            // The my-courses feed is deliberately not gated on PaidUntil: an enrolled
            // student must still find the course in order to pay for the next period.
            var dto = mapper.Map<CourseDto>(entity);
            ApplyEnrollment(dto, entity, studentId);
            return dto;
        }, q => q.OrderByDescending(x => x.StartDate), cancellationToken);
    }

    private static void ApplyEnrollment(CourseDto dto, Course entity, int studentId)
    {
        var enrollment = entity.Enrollments.FirstOrDefault(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active);
        dto.IsEnrolled = enrollment != null;
        dto.EnrollmentId = enrollment?.Id;
        dto.PaidUntil = enrollment?.PaidUntil;
    }

    public async Task<PagedResult<CourseDto>> GetPagedForAdminAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Course>()
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .Include(c => c.Instructor)
            .ApplySearch(search);

        return await ToPagedWithInstructorNamesAsync(query, search, cancellationToken);
    }

    private async Task<PagedResult<CourseDto>> ToPagedWithInstructorNamesAsync(
        IQueryable<Course> query,
        CourseSearchObject search,
        CancellationToken cancellationToken)
    {
        int? total = null;
        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        (var page, var pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var entities = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var appUserIds = entities
            .Select(c => c.Instructor.AppUserId)
            .Distinct();

        var users = await identityService.GetUsersBulkAsync(appUserIds, cancellationToken);

        return new PagedResult<CourseDto>
        {
            Items = [.. entities.Select(entity => MapAdmin(entity, users))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CourseDto> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadCourseDetailAsync(context.Set<Course>(), id, cancellationToken);

        var dto = mapper.Map<CourseDto>(entity);
        dto.InstructorName = await ResolveInstructorNameAsync(entity.Instructor.AppUserId, cancellationToken);
        return dto;
    }

    private static async Task<Course> LoadCourseDetailAsync(IQueryable<Course> query, int id, CancellationToken cancellationToken)
    {
        return await query
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);
    }

    private CourseDto MapAdmin(Course course, IReadOnlyDictionary<int, UserIdentityDto> users)
    {
        var dto = mapper.Map<CourseDto>(course);
        dto.InstructorName = UserNameHelper.FormatName(users.GetValueOrDefault(course.Instructor.AppUserId));
        return dto;
    }

    private async Task<string?> ResolveInstructorNameAsync(int appUserId, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserAsync(appUserId, cancellationToken);
        return UserNameHelper.FormatName(user);
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

        await SoftDeleteCourseAsync(entity, cancellationToken);

        logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }

    private async Task SoftDeleteCourseAsync(Course entity, CancellationToken cancellationToken)
    {
        entity.SoftDelete();
        entity.UpdatedById = currentUser.UserId;

        var lectures = await context.Set<Lecture>()
            .Where(l => l.CourseId == entity.Id)
            .ToListAsync(cancellationToken);

        foreach (var lecture in lectures)
        {
            lecture.SoftDelete();
            lecture.UpdatedById = currentUser.UserId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
