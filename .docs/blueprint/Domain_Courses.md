# Bounded Context: Courses

**Generated**: 2026-06-28T16:06:01.476556+00:00  
**Commit**: latest  
**Total Files**: 17

---

## 🤖 Agent Briefing (Read First)

This file contains the complete source for the **Courses** bounded context.

**Your goals when reading this context:**
1. Build an accurate mental model of entities, behavior, and state transitions.
2. Identify cross-context interactions (see "Key Interactions" sections).
3. Note any architectural smells, duplicated logic, or unnecessary abstractions.
4. Track how this context communicates with others (especially via events).

**Focus areas for deep analysis:**
- Domain entities with rich behavior (not anemic)
- Service orchestration and access control
- State machines / workflow logic
- Cross-domain event contracts

---

## File: `eNote\eNote.API\Controllers\Courses\InstructorCourseController.cs`
**Hash**: `d32df44c864c` | **Size**: 2221 chars

**Classes**: InstructorCourseController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/courses")]
public sealed class InstructorCourseController(ICourseService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetMyCourses([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CourseRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CourseRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Courses\InstructorRankingController.cs`
**Hash**: `ca76545bcc23` | **Size**: 1300 chars

**Classes**: InstructorRankingController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/courses/{courseId:int}/ranking")]
public sealed class InstructorRankingController(IRankingService rankingService, IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRanking(int courseId, CancellationToken cancellationToken)
    {
        return Ok(await rankingService.GetForInstructorAsync(courseId, cancellationToken));
    }

    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRankingReport(int courseId, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateCourseRankingPdfAsync(courseId, cancellationToken);
        return File(pdf, "application/pdf", $"course-{courseId}-ranking.pdf");
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Courses\StudentCourseController.cs`
**Hash**: `9f4624a1ef6a` | **Size**: 1822 chars

**Classes**: StudentCourseController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/courses")]
public sealed class StudentCourseController(
    ICourseService service,
    ICourseEnrollmentService enrollmentService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetPublished([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/enroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Enroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.EnrollAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/unenroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unenroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.UnenrollAsync(id, cancellationToken);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Courses\StudentRankingController.cs`
**Hash**: `2ade1f768b0a` | **Size**: 836 chars

**Classes**: StudentRankingController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/courses/{courseId:int}/ranking")]
public sealed class StudentRankingController(IRankingService rankingService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRanking(int courseId, CancellationToken cancellationToken)
    {
        return Ok(await rankingService.GetForStudentAsync(courseId, cancellationToken));
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseDto.cs`
**Hash**: `6761b5a44b10` | **Size**: 464 chars

**Classes**: CourseDto
```cs
namespace eNote.Application.Features.Academic.Courses;

public class CourseDto
{
    public int Id { get; set; }
    public int InstructorId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal Price { get; set; }

    public int EnrolledCount { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseMappingConfig.cs`
**Hash**: `36fc8bc9736e` | **Size**: 450 chars

**Classes**: CourseMappingConfig
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Features.Academic.Courses;

public sealed class CourseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Course, CourseDto>()
            .Map(dest => dest.EnrolledCount, src => src.Enrollments == null ? 0 : src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active));
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseRankingEntryDto.cs`
**Hash**: `702ee7297ce5` | **Size**: 316 chars

**Classes**: CourseRankingEntryDto
```cs
namespace eNote.Application.Features.Academic.Courses;

public class CourseRankingEntryDto
{
    public int Rank { get; set; }
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public double? AverageGrade { get; set; }
    public int GradedSubmissions { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseRequest.cs`
**Hash**: `eaac52d7c0eb` | **Size**: 488 chars

**Classes**: CourseRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Courses;

public class CourseRequest
{
    [Required]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
    public decimal Price { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseSearchExtensions.cs`
**Hash**: `040041c366f2` | **Size**: 470 chars

**Classes**: CourseSearchExtensions
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Academic.Courses;

public static class CourseSearchExtensions
{
    public static IQueryable<Course> ApplySearch(this IQueryable<Course> query, CourseSearchObject search) =>
        query
            .WhereContainsIf(search.Name, c => c.Name.Contains(search.Name!))
            .WhereEqualsIf(search.IsPublished, c => c.IsPublished == search.IsPublished!.Value);
}
```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\CourseSearchObject.cs`
**Hash**: `6cb2121a59f0` | **Size**: 232 chars

**Classes**: CourseSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Courses;

public class CourseSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public bool? IsPublished { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\CourseEnrollmentService.cs`
**Hash**: `e2ccc043cbe0` | **Size**: 2772 chars

**Classes**: CourseEnrollmentService
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class CourseEnrollmentService(
    IAppDbContext context,
    IClock clock,
    ICurrentActor actor,
    ILogger<CourseEnrollmentService> logger) : ICourseEnrollmentService
{
    public async Task EnrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var student = await actor.GetCurrentStudentAsync();

        if (!student.HasActiveMembership(clock.UtcNow))
        {
            throw new BusinessException(Messages.MembershipInactive);
        }

        _ = await context.Set<Course>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId, cancellationToken);

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Active)
        {
            return;
        }

        if (enrollment?.EnrollmentStatus == EnrollmentStatus.Canceled)
        {
            enrollment.UpdateStatus(EnrollmentStatus.Active);
            enrollment.UpdatedById = actor.UserId;
        }
        else
        {
            context.Set<Enrollment>().Add(new Enrollment(student.Id, courseId, EnrollmentStatus.Active)
            {
                CreatedById = actor.UserId
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", actor.UserId, courseId);
    }

    public async Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var enrollment = await context.Set<Enrollment>()
            .FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.EnrollmentStatus == EnrollmentStatus.Active,
                cancellationToken)
            ?? throw new BusinessException(Messages.StudentNotEnrolled);

        enrollment.UpdateStatus(EnrollmentStatus.Canceled);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", actor.UserId, courseId);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\CourseService.cs`
**Hash**: `180b379775ec` | **Size**: 5606 chars

**Classes**: CourseService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
﻿using eNote.Application.Common.Exceptions;
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
    public async Task<CourseDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.CourseNotFound);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

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
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

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
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, actor.UserId);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task<CourseDto> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.UpdateDetails(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate);
        entity.SetPublishedStatus(request.IsPublished);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<CourseDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var entity = await instructorAccess.CoursesFor(instructorId).FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.CourseNotFound);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.Set<Lecture>()
            .Where(l => l.CourseId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false)
            .SetProperty(l => l.UpdatedById, actor.UserId), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, actor.UserId);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\ICourseEnrollmentService.cs`
**Hash**: `c0642ff26f53` | **Size**: 279 chars

**Classes**: 
**Interfaces**: ICourseEnrollmentService
```cs
namespace eNote.Application.Features.Academic.Courses.Services;

public interface ICourseEnrollmentService
{
    Task EnrollAsync(int courseId, CancellationToken cancellationToken = default);
    Task UnenrollAsync(int courseId, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\ICourseService.cs`
**Hash**: `236dfa6d97b3` | **Size**: 951 chars

**Classes**: 
**Interfaces**: ICourseService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Courses;

namespace eNote.Application.Features.Academic.Courses.Services;

public interface ICourseService
{
    Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search, CancellationToken cancellationToken = default);
    Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search, CancellationToken cancellationToken = default);
    Task<CourseDto> GetByIdForInstructorAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> GetByIdForStudentAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> CreateAsync(CourseRequest request, CancellationToken cancellationToken = default);
    Task<CourseDto> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\IRankingService.cs`
**Hash**: `daa1632b6cea` | **Size**: 413 chars

**Classes**: 
**Interfaces**: IRankingService
```cs
using eNote.Application.Features.Academic.Courses;

namespace eNote.Application.Features.Academic.Courses.Services;

public interface IRankingService
{
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\Services\RankingService.cs`
**Hash**: `cd38797f6c10` | **Size**: 3903 chars

**Classes**: RankingService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IStudentDisplayNameService** → Student display-name formatting
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Courses.Services;

public sealed class RankingService(IAppDbContext context, ICurrentActor actor, IStudentDisplayNameService displayNames, IInstructorAccessService instructorAccess) : IRankingService
{
    public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForInstructorAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId, cancellationToken))
        {
            throw new NotFoundException(Messages.CourseNotFound);
        }

        return await BuildRankingAsync(courseId, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseRankingEntryDto>> GetForStudentAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        if (!await context.IsEnrolledInCourseAsync(studentId, courseId, cancellationToken))
        {
            throw new AuthorizationException(Messages.StudentNotEnrolled);
        }

        return await BuildRankingAsync(courseId, cancellationToken);
    }

    private async Task<IReadOnlyList<CourseRankingEntryDto>> BuildRankingAsync(int courseId, CancellationToken cancellationToken)
    {
        var enrolledStudents = await context.Set<Enrollment>()
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Include(e => e.Student)
            .Select(e => e.Student)
            .ToListAsync(cancellationToken);

        if (enrolledStudents.Count == 0)
        {
            return [];
        }

        HashSet<int> studentIds = [.. enrolledStudents.Select(s => s.Id)];

        Dictionary<int, StudentGradeStats> gradeData = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Where(s => s.Grade != null && s.Assignment.Lecture.CourseId == courseId && studentIds.Contains(s.StudentId))
            .GroupBy(s => s.StudentId)
            .Select(g =>
                new StudentGradeStats(g.Key,
                    g.Average(x => (double?)x.Grade),
                    g.Count()))
                .ToDictionaryAsync(x => x.StudentId, cancellationToken);

        IReadOnlyDictionary<int, string> nameMap = await displayNames.GetStudentDisplayNamesAsync(enrolledStudents);

        List<CourseRankingEntryDto> ranked = [.. enrolledStudents
            .Where(s => gradeData.ContainsKey(s.Id))
            .Select(s =>
            {
                var gradeStats = gradeData[s.Id];

                return new RankedStudentEntry(s, gradeStats.Average, gradeStats.Count, nameMap.GetValueOrDefault(s.Id, $"Student {s.Id}"));
            })
            .OrderByDescending(x => x.Average)
            .ThenBy(x => x.Student.Id)
            .Select((x, i) => new CourseRankingEntryDto
            {
                Rank = i + 1,
                StudentId = x.Student.Id,
                StudentName = x.Name,
                AverageGrade = x.Average.HasValue ? Math.Round(x.Average.Value, 2) : null,
                GradedSubmissions = x.Count
            })];

        return ranked;
    }

    private sealed record StudentGradeStats(int StudentId, double? Average, int Count);

    private sealed record RankedStudentEntry(Student Student, double? Average, int Count, string Name);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Courses\StudentEnrollmentExtensions.cs`
**Hash**: `1755b8b6720f` | **Size**: 1566 chars

**Classes**: StudentEnrollmentExtensions
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Courses;

public static class StudentEnrollmentExtensions
{
    public static Task<bool> IsEnrolledInCourseAsync(this IAppDbContext context, int studentId, int courseId, CancellationToken cancellationToken = default) =>
        context.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);

    public static IQueryable<Lecture> ForEnrolledStudent(this IQueryable<Lecture> query, int studentId) =>
        query.Where(x => x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled && x.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));

    public static IQueryable<LectureNote> ForEnrolledStudent(this IQueryable<LectureNote> query, int studentId) =>
        query.Where(x => x.Lecture.Course.IsPublished && x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));

    public static IQueryable<Assignment> ForEnrolledStudent(this IQueryable<Assignment> query, int studentId) =>
        query.Where(x => x.Lecture.Course.IsPublished && x.Lecture.LectureStatus != LectureStatus.Cancelled && x.Lecture.Course.Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));
}

```

---

