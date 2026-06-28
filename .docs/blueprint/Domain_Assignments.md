# Bounded Context: Assignments

**Generated**: 2026-06-28T05:17:02.565509+00:00  
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
**Hash**: `338ebd21921f` | **Size**: 2280 chars

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
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search)
    {
        var result = await service.GetForLectureAsync(lectureId, search);
        return Ok(result);
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request)
    {
        var dto = await service.CreateAsync(lectureId, request);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            assignmentId = dto.Id
        }, dto);
    }

    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request)
    {
        var dto = await service.UpdateAsync(lectureId, assignmentId, request);
        return Ok(dto);
    }

    [HttpDelete("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int assignmentId)
    {
        await service.DeleteAsync(lectureId, assignmentId);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Assignments\InstructorAssignmentSubmissionController.cs`
**Hash**: `c766557b0cd7` | **Size**: 1455 chars

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
    public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, [FromQuery] SubmissionSearchObject search)
    {
        var result = await submissionService.GetSubmissionsAsync(lectureId, assignmentId, search);
        return Ok(result);
    }

    [HttpPut("{submissionId:int}/grade")]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request)
    {
        var dto = await submissionService.GradeAsync(lectureId, assignmentId, submissionId, request);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Assignments\StudentAssignmentController.cs`
**Hash**: `23f99ac37e52` | **Size**: 1112 chars

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
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments([FromQuery] AssignmentSearchObject search)
    {
        var result = await service.GetForStudentAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int id)
    {
        var dto = await service.GetByIdForStudentAsync(id);
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
**Hash**: `2a2151b8bd08` | **Size**: 836 chars

**Classes**: AssignmentSearchExtensions
```cs
using eNote.Application.Common.Search;
using eNote.Application.Features.Students;
using eNote.Domain.Entities.Assignments;

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
**Hash**: `030b1006cbb6` | **Size**: 3939 chars

**Classes**: AssignmentService
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
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Students;
using eNote.Domain.Entities.Assignments;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    IMapper mapper) : IAssignmentService
{
    public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.AssignmentsForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt));
    }

    public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId) =>
        mapper.Map<AssignmentDto>(await GetOwnedAssignmentAsync(lectureId, assignmentId));

    public async Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var entity = new Assignment(request.Title.Trim(), request.Description.Trim(), request.DueAt, lectureId)
        {
            CreatedById = actor.UserId
        };

        context.Set<Assignment>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true);

        entity.UpdateDetails(request.Title.Trim(), request.Description.Trim(), request.DueAt);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AssignmentDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int assignmentId)
    {
        var entity = await GetOwnedAssignmentAsync(lectureId, assignmentId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Assignment>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt));
    }

    public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<Assignment>()
            .ForEnrolledStudentById(studentId, assignmentId)
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.AssignmentNotFound);

        return mapper.Map<AssignmentDto>(entity);
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId, track);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\AssignmentSubmissionService.cs`
**Hash**: `3424344fc2bf` | **Size**: 4875 chars

**Classes**: AssignmentSubmissionService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IStudentDisplayNameService** → Student display-name formatting
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Students;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    ICurrentActor actor,
    IStudentDisplayNameService displayNames,
    IInstructorAccessService instructorAccess,
    IFileStorageService fileStorage) : IAssignmentSubmissionService
{
    public async Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var path = await fileStorage.SaveAssignmentAsync(stream, fileName, contentType, ct);
        return await SubmitAsync(assignmentId, new AssignmentSubmitRequest { FilePath = path });
    }

    public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId);

        var query = context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == assignmentId);

        return await query.ToPagedResultAsync(
            search,
            items => displayNames.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
            (x, names) => MapSubmission(x, x.Student, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")),
            q => q.OrderBy(x => x.StudentId));
    }

    public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request)
    {
        await GetOwnedAssignmentAsync(lectureId, assignmentId);

        var submission = await context.Set<AssignmentSubmission>()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId)
            ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

        submission.SetGrade(request.Grade);
        submission.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return MapSubmission(submission, submission.Student, await displayNames.GetStudentDisplayNameAsync(submission.Student));
    }

    private async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, AssignmentSubmitRequest request)
    {
        var student = await actor.GetStudentAsync();

        var assignment = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId)
            .Include(x => x.AssignmentSubmissions)
            .FirstOrDefaultAsync()
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

        await context.SaveChangesAsync();

        return MapSubmission(existing, student, await displayNames.GetStudentDisplayNameAsync(student));
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedAssignmentAsync(lectureId, assignmentId, instructorId);
    }
    private static AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, Student student, string studentName) => new()
    {
        Id = submission.Id,
        AssignmentId = submission.AssignmentId,
        StudentId = submission.StudentId,
        StudentName = studentName,
        SubmittedAt = submission.SubmittedAt,
        FilePath = submission.FilePath,
        Grade = submission.Grade
    };
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\IAssignmentService.cs`
**Hash**: `8e3e9e42d094` | **Size**: 776 chars

**Classes**: 
**Interfaces**: IAssignmentService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search);
    Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId);
    Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request);
    Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request);
    Task DeleteAsync(int lectureId, int assignmentId);
    Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search);
    Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Assignments\Services\IAssignmentSubmissionService.cs`
**Hash**: `e4d8262ef736` | **Size**: 632 chars

**Classes**: 
**Interfaces**: IAssignmentSubmissionService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Assignments;

namespace eNote.Application.Features.Academic.Assignments.Services;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search);
    Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request);
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
**Hash**: `55ad7487e8dc` | **Size**: 1104 chars

**Classes**: Assignment
```cs
using eNote.Domain.Entities.Shared.Base;

namespace eNote.Domain.Entities.Assignments;

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
**Hash**: `e1d54dc91a80` | **Size**: 959 chars

**Classes**: AssignmentSubmission
```cs
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Shared.Base;

namespace eNote.Domain.Entities.Assignments;

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
**Hash**: `9fcbc87d75f1` | **Size**: 951 chars

**Classes**: AssignmentSubmissionConfig
```cs
using eNote.Domain.Entities.Assignments;
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

