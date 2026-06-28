# Bounded Context: Assignments

**Generated**: 2026-06-28T21:48:14.412851+00:00  
**Commit**: latest  
**Total Files**: 19

---

## 🤖 Agent Briefing (Read First)

This file contains the complete source for the **Assignments** bounded context.

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

## File: `eNote\eNote.API\Controllers\Assignments\InstructorAssignmentController.cs`
**Hash**: `8e22ad1933dc` | **Size**: 2560 chars

**Classes**: InstructorAssignmentController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/assignments")]
public sealed class InstructorAssignmentController(IAssignmentService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForLectureAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(lectureId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            assignmentId = dto.Id
        }, dto);
    }

    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(lectureId, assignmentId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(lectureId, assignmentId, cancellationToken);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Assignments\InstructorAssignmentSubmissionController.cs`
**Hash**: `edd8dc26408e` | **Size**: 1567 chars

**Classes**: InstructorAssignmentSubmissionController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}/submissions")]
public sealed class InstructorAssignmentSubmissionController(IAssignmentSubmissionService submissionService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, [FromQuery] SubmissionSearchObject search, CancellationToken cancellationToken)
    {
        var result = await submissionService.GetSubmissionsAsync(lectureId, assignmentId, search, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{submissionId:int}/grade")]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.GradeAsync(lectureId, assignmentId, submissionId, request, cancellationToken);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Assignments\StudentAssignmentController.cs`
**Hash**: `20b3af4921f4` | **Size**: 1224 chars

**Classes**: StudentAssignmentController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/assignments")]
public sealed class StudentAssignmentController(IAssignmentService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments([FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Assignments\StudentAssignmentSubmissionController.cs`
**Hash**: `8d7a9425fab9` | **Size**: 1194 chars

**Classes**: StudentAssignmentSubmissionController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/assignments/{id:int}/submit")]
public sealed class StudentAssignmentSubmissionController(IAssignmentSubmissionService submissionService) : CoreController
{
    [HttpPost]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Submit(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var dto = await submissionService.SubmitWithFileAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentDto.cs`
**Hash**: `b0354943e0d0` | **Size**: 304 chars

**Classes**: AssignmentDto
```cs
namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentDto
{
    public int Id { get; set; }
    public int LectureId { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public DateTime DueAt { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentRequest.cs`
**Hash**: `c674e9eedf88` | **Size**: 326 chars

**Classes**: AssignmentRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentRequest
{
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Description { get; set; } = null!;
    [Required]
    public DateTime DueAt { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentSearchExtensions.cs`
**Hash**: `94a502808331` | **Size**: 832 chars

**Classes**: AssignmentSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;
using eNote.Application.Features.Academic.Courses;

namespace eNote.Application.Features.Academic.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search) =>
        query
            .WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!))
            .WhereEqualsIf(search.DueAfter, x => x.DueAt >= search.DueAfter!.Value)
            .WhereEqualsIf(search.DueBefore, x => x.DueAt <= search.DueBefore!.Value);

    public static IQueryable<Assignment> ForEnrolledStudentById(this IQueryable<Assignment> query, int studentId, int assignmentId) =>
        query.ForEnrolledStudent(studentId).Where(x => x.Id == assignmentId);
}
```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentSearchObject.cs`
**Hash**: `b2031a4915eb` | **Size**: 288 chars

**Classes**: AssignmentSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentSearchObject : BaseSearchObject
{
    public string? Title { get; set; }

    public DateTime? DueAfter { get; set; }
    public DateTime? DueBefore { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentSubmissionDto.cs`
**Hash**: `a794733cc8f1` | **Size**: 387 chars

**Classes**: AssignmentSubmissionDto
```cs
namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentSubmissionDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }

    public string? StudentName { get; set; }
    public string? FilePath { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? Grade { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\AssignmentSubmitRequest.cs`
**Hash**: `8c938f40812a` | **Size**: 143 chars

**Classes**: AssignmentSubmitRequest
```cs
namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentSubmitRequest
{
    public string? FilePath { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\GradeAssignmentRequest.cs`
**Hash**: `50d9babd5c5e` | **Size**: 252 chars

**Classes**: GradeAssignmentRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Assignments;

public class GradeAssignmentRequest
{
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
    public int Grade { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\AssignmentService.cs`
**Hash**: `2250cfd6f32c` | **Size**: 4568 chars

**Classes**: AssignmentService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
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

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\AssignmentSubmissionService.cs`
**Hash**: `25e0556da4f6` | **Size**: 5021 chars

**Classes**: AssignmentSubmissionService
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
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Academic.Courses;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    ICurrentActor actor,
    IStudentDisplayNameService displayNames,
    IInstructorAccessService instructorAccess,
    IFileStorageService fileStorage,
    IMapper mapper) : IAssignmentSubmissionService
{
    public async Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var path = await fileStorage.SaveAssignmentAsync(stream, fileName, contentType, ct);
        return await SubmitAsync(assignmentId, new AssignmentSubmitRequest { FilePath = path }, ct);
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search, CancellationToken cancellationToken = default)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == assignmentId);

        return await query.ToPagedResultAsync(
            search,
            items => displayNames.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
            (x, names) => MapSubmission(x, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")),
            q => q.OrderBy(x => x.StudentId),
            cancellationToken);
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId, cancellationToken);

        var submission = await context.Set<AssignmentSubmission>()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId, cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        submission.SetGrade(request.Grade);
        submission.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return MapSubmission(submission, await displayNames.GetStudentDisplayNameAsync(submission.Student));
    }

    private async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, AssignmentSubmitRequest request, CancellationToken cancellationToken)
    {
        var student = await actor.GetCurrentStudentAsync();

        var assignment = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId)
            .Include(x => x.AssignmentSubmissions)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        var existing = assignment.AssignmentSubmissions.FirstOrDefault(x => x.StudentId == student.Id);

        if (existing?.SubmittedAt is not null)
        {
            throw new ConflictException(Messages.AssignmentAlreadySubmitted);
        }

        if (clock.UtcNow > assignment.DueAt)
        {
            throw new BusinessException(Messages.AssignmentPastDue);
        }

        if (existing is null)
        {
            existing = new AssignmentSubmission(assignment.Id, student.Id)
            {
                CreatedById = actor.UserId
            };
            assignment.AssignmentSubmissions.Add(existing);
        }

        existing.Submit(request.FilePath?.Trim(), clock.UtcNow);
        existing.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return MapSubmission(existing, await displayNames.GetStudentDisplayNameAsync(student));
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, cancellationToken: cancellationToken);
    }

    private AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, string studentName)
    {
        var dto = mapper.Map<AssignmentSubmissionDto>(submission);
        dto.StudentName = studentName;
        return dto;
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\IAssignmentService.cs`
**Hash**: `c293b72d37e2` | **Size**: 1105 chars

**Classes**: 
**Interfaces**: IAssignmentService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default);
    Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int lectureId, int assignmentId, CancellationToken cancellationToken = default);
    Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\IAssignmentSubmissionService.cs`
**Hash**: `5c4ad23bdea5` | **Size**: 726 chars

**Classes**: 
**Interfaces**: IAssignmentSubmissionService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search, CancellationToken cancellationToken = default);
    Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\SubmissionSearchObject.cs`
**Hash**: `88413a1a617a` | **Size**: 166 chars

**Classes**: SubmissionSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public sealed class SubmissionSearchObject : BaseSearchObject
{
}

```

---

## File: `eNote\eNote.Domain\Entities\Assignments\Assignment.cs`
**Hash**: `6977181f4887` | **Size**: 1080 chars

**Classes**: Assignment
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Assignment : AuditableEntity
{
    public int LectureId { get; private set; }
    public Lecture Lecture { get; private set; } = null!;

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTime DueAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; private set; } = new List<AssignmentSubmission>();

    protected Assignment()
    {
    }

    public Assignment(string title, string description, DateTime dueAt, int lectureId)
    {
        Title = title;
        Description = description;
        DueAt = dueAt;
        LectureId = lectureId;
        IsActive = true;
    }

    public void UpdateDetails(string title, string description, DateTime dueAt)
    {
        Title = title;
        Description = description;
        DueAt = dueAt;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Assignments\AssignmentSubmission.cs`
**Hash**: `180e85e3c97a` | **Size**: 897 chars

**Classes**: AssignmentSubmission
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class AssignmentSubmission : AuditableEntity
{
    public int AssignmentId { get; private set; }
    public Assignment Assignment { get; private set; } = null!;
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public string? FilePath { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public int? Grade { get; private set; }

    protected AssignmentSubmission()
    {
    }

    public AssignmentSubmission(int assignmentId, int studentId)
    {
        AssignmentId = assignmentId;
        StudentId = studentId;
    }

    public void Submit(string? filePath, DateTime submittedAt)
    {
        FilePath = filePath;
        SubmittedAt = submittedAt;
    }

    public void SetGrade(int grade)
    {
        Grade = grade;
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AssignmentSubmissionConfig.cs`
**Hash**: `ae42f6bac64e` | **Size**: 939 chars

**Classes**: AssignmentSubmissionConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AssignmentSubmissionConfig : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.HasQueryFilter(s => s.Assignment.IsActive);

        builder.HasOne(s => s.Assignment)
               .WithMany(a => a.AssignmentSubmissions)
               .HasForeignKey(s => s.AssignmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Student)
               .WithMany(s => s.AssignmentSubmissions)
               .HasForeignKey(s => s.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Grade).HasDefaultValue(null);
        builder.Property(s => s.FilePath).HasMaxLength(500);
    }
}

```

---

