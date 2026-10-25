using eNote.Application.Common.Search;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser, IStudentContext students, InstructorAccessService instructorAccess, ILogger<CourseService> logger, IUserIdentityService identityService, IPaymentGateway paymentGateway, IClock clock)
{
    public async Task<CourseDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        return await instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                InstructorId = c.InstructorId,
                Name = c.Name,
                Description = c.Description,
                IsPublished = c.IsPublished,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Price = c.Price,
                EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);
    }

    public async Task<PagedResult<CourseDto>> GetPagedCatalogForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var query = WhereCatalogVisible(context.Set<Course>().AsNoTracking(), instructorId)
            .ApplySearch(search);

        return await ToPagedWithInstructorNamesAsync(query, search, cancellationToken);
    }

    public async Task<CourseDto> GetCatalogByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        return await LoadCourseDetailDtoAsync(
            WhereCatalogVisible(context.Set<Course>(), instructorId),
            id,
            cancellationToken);
    }

    public async Task<PagedResult<CourseCatalogInstructorDto>> GetPagedCatalogInstructorsAsync(BaseSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        IQueryable<Instructor> query = WhereCatalogVisible(context.Set<Course>().AsNoTracking(), instructorId)
            .Select(c => c.Instructor)
            .Distinct();

        return await query.ToPagedResultAsync(
            search,
            async (instructors, ct) =>
            {
                var users = await identityService.GetUsersBulkAsync(instructors.Select(x => x.AppUserId), ct);
                return instructors.Select(x => new CourseCatalogInstructorDto
                {
                    Id = x.Id,
                    Name = UserNameHelper.FormatName(users.GetValueOrDefault(x.AppUserId))
                });
            },
            q => q.OrderBy(x => x.Id),
            cancellationToken);
    }

    public async Task<CourseCatalogSummaryDto> GetCatalogSummaryAsync(CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

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
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);
        var now = clock.UtcNow;

        var dto = await context.Set<Course>()
            .AsNoTracking()
            .Where(c => c.Id == id &&
                (c.IsPublished || c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)))
            .Select(c => new CourseDto
            {
                Id = c.Id,
                InstructorId = c.InstructorId,
                Name = c.Name,
                Description = c.Description,
                IsPublished = c.IsPublished,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Price = c.Price,
                EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active),
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active),
                HasContentAccess = c.Enrollments.AsQueryable().Any(StudentEnrollmentExtensions.HasContentAccess(studentId, now)),
                LectureCount = c.Lectures.Count(l => l.LectureStatus != LectureStatus.Cancelled
                    && (l.StudentId == null || l.StudentId == studentId)),
                EnrollmentId = c.Enrollments
                    .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
                    .Select(e => (int?)e.Id)
                    .FirstOrDefault(),
                PaidUntil = c.Enrollments
                    .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
                    .Select(e => e.PaidUntil)
                    .FirstOrDefault(),
                EnrollmentStatus = c.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => (EnrollmentStatus?)e.EnrollmentStatus)
                    .FirstOrDefault(),
                EnrollmentDecisionNote = c.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.DecisionNote)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        dto.HasEnded = Course.HasEndedOn(dto.EndDate, now);

        return dto;
    }

    public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var query = instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        int? total = null;
        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var items = await query
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                InstructorId = c.InstructorId,
                Name = c.Name,
                Description = c.Description,
                IsPublished = c.IsPublished,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Price = c.Price,
                EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CourseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var studentId = await students.GetCurrentStudentIdAsync(cancellationToken);
        var now = clock.UtcNow;

        var query = context.Set<Course>()
            .AsNoTracking()
            .ApplySearch(search);

        if (search.EnrolledOnly == true)
        {
            query = query.Where(c => c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));
        }
        else
        {
            query = query.Where(c => c.IsPublished);
        }

        int? total = null;
        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(cancellationToken);
        }

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var items = await query
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                InstructorId = c.InstructorId,
                Name = c.Name,
                Description = c.Description,
                IsPublished = c.IsPublished,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Price = c.Price,
                EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active),
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active),
                HasContentAccess = c.Enrollments.AsQueryable().Any(StudentEnrollmentExtensions.HasContentAccess(studentId, now)),
                LectureCount = c.Lectures.Count(l => l.LectureStatus != LectureStatus.Cancelled
                    && (l.StudentId == null || l.StudentId == studentId)),
                EnrollmentId = c.Enrollments
                    .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
                    .Select(e => (int?)e.Id)
                    .FirstOrDefault(),
                PaidUntil = c.Enrollments
                    .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
                    .Select(e => e.PaidUntil)
                    .FirstOrDefault(),
                EnrollmentStatus = c.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => (EnrollmentStatus?)e.EnrollmentStatus)
                    .FirstOrDefault(),
                EnrollmentDecisionNote = c.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.DecisionNote)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.HasEnded = Course.HasEndedOn(item.EndDate, now);
        }

        return new PagedResult<CourseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<CourseDto>> GetPagedForAdminAsync(CourseSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Course>()
            .AsNoTracking()
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

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        var items = await query
            .OrderByDescending(c => c.StartDate)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                Dto = new CourseDto
                {
                    Id = c.Id,
                    InstructorId = c.InstructorId,
                    Name = c.Name,
                    Description = c.Description,
                    IsPublished = c.IsPublished,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Price = c.Price,
                    EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                },
                InstructorAppUserId = c.Instructor.AppUserId
            })
            .ToListAsync(cancellationToken);

        var appUserIds = items
            .Select(x => x.InstructorAppUserId)
            .Distinct();

        var users = await identityService.GetUsersBulkAsync(appUserIds, cancellationToken);

        foreach (var item in items)
        {
            item.Dto.InstructorName = UserNameHelper.FormatName(users.GetValueOrDefault(item.InstructorAppUserId));
        }

        return new PagedResult<CourseDto>
        {
            Items = items.Select(x => x.Dto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CourseDto> GetByIdForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        return await LoadCourseDetailDtoAsync(context.Set<Course>(), id, cancellationToken);
    }

    private async Task<CourseDto> LoadCourseDetailDtoAsync(IQueryable<Course> query, int id, CancellationToken cancellationToken)
    {
        var result = await query
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                Dto = new CourseDto
                {
                    Id = c.Id,
                    InstructorId = c.InstructorId,
                    Name = c.Name,
                    Description = c.Description,
                    IsPublished = c.IsPublished,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Price = c.Price,
                    EnrolledCount = c.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
                },
                InstructorAppUserId = c.Instructor.AppUserId
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        result.Dto.InstructorName = await ResolveInstructorNameAsync(result.InstructorAppUserId, cancellationToken);
        return result.Dto;
    }

    private async Task<string?> ResolveInstructorNameAsync(int appUserId, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserAsync(appUserId, cancellationToken);
        return UserNameHelper.FormatName(user);
    }


    public async Task<CourseDto> CreateAsync(CourseRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, currentUser.UserId);

        var entity = new Course(
            request.Name.Trim(),
            request.Description?.Trim(),
            request.Price,
            request.StartDate,
            request.EndDate,
            instructorId);
        entity.SetPublishedStatus(request.IsPublished);

        context.Set<Course>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, currentUser.UserId);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.UpdateDetails(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate);
        entity.SetPublishedStatus(request.IsPublished);

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<CourseDto>(entity);
        dto.EnrolledCount = await context.Set<Enrollment>()
            .CountAsync(e => e.CourseId == id && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

        var entity = await instructorAccess.CoursesFor(instructorId).FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        await SoftDeleteCourseAsync(entity, cancellationToken);

        logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, currentUser.UserId);
    }

    private async Task SoftDeleteCourseAsync(Course entity, CancellationToken cancellationToken)
    {
        await EnsureNoActivePaidAccessAsync(entity, cancellationToken);
        await CancelOpenPaymentAttemptsAsync(entity, cancellationToken);

        entity.SoftDelete();

        var lectures = await context.Set<Lecture>()
            .Where(l => l.CourseId == entity.Id)
            .ToListAsync(cancellationToken);

        foreach (var lecture in lectures)
        {
            lecture.SoftDelete();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNoActivePaidAccessAsync(Course entity, CancellationToken cancellationToken)
    {
        if (entity.Price <= 0)
        {
            return;
        }

        var latestPaidUntil = await context.Set<Enrollment>()
            .Where(e => e.CourseId == entity.Id
                && e.EnrollmentStatus == EnrollmentStatus.Active
                && e.PaidUntil >= clock.UtcNow)
            .MaxAsync(e => e.PaidUntil, cancellationToken);

        if (latestPaidUntil.HasValue)
        {
            throw new BusinessException(Messages.CourseDeleteBlockedByPaidAccess(latestPaidUntil.Value));
        }
    }

    private async Task CancelOpenPaymentAttemptsAsync(Course entity, CancellationToken cancellationToken)
    {
        var openAttempts = await context.Set<CoursePayment>()
            .Where(p => p.Enrollment.CourseId == entity.Id
                && (p.Status == PaymentStatus.RequiresAction || p.Status == PaymentStatus.Failed))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        await PaymentGatewayHelpers.ResolveOpenAttemptsAsync(
            logger,
            context,
            paymentGateway,
            openAttempts,
            expected: null,
            Messages.PaymentProcessing,
            cancellationToken);
    }
}
