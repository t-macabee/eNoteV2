# Bounded Context: Assignments
Total Files Contained: 18
---

## File: eNote\eNote.Domain\Entities\AssignmentSubmission.cs
```cs
using eNote.Domain.Entities.Base;

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

## File: eNote\eNote.Application\Features\Assignments\AssignmentDto.cs
```cs
namespace eNote.Application.Features.Assignments;

public class AssignmentDto
{
    public int Id { get; set; }
    public int LectureId { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public DateTime DueAt { get; set; }
}

```

## File: eNote\eNote.Application\Features\Assignments\AssignmentRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Assignments;

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

## File: eNote\eNote.Application\Features\Assignments\AssignmentSearchExtensions.cs
```cs
using eNote.Application.Common.Search;
using eNote.Application.Features.Students;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Assignments;

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

## File: eNote\eNote.Application\Features\Assignments\AssignmentSearchObject.cs
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Assignments;

public class AssignmentSearchObject : BaseSearchObject
{
    public string? Title { get; set; }

    public DateTime? DueAfter { get; set; }
    public DateTime? DueBefore { get; set; }
}

```

## File: eNote\eNote.Application\Features\Assignments\AssignmentSubmissionDto.cs
```cs
namespace eNote.Application.Features.Assignments;

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

## File: eNote\eNote.Application\Features\Assignments\AssignmentSubmitRequest.cs
```cs
namespace eNote.Application.Features.Assignments;

public class AssignmentSubmitRequest
{
    public string? FilePath { get; set; }
}

```

## File: eNote\eNote.Application\Features\Assignments\GradeAssignmentRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Assignments;

public class GradeAssignmentRequest
{
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
    public int Grade { get; set; }
}

```

## File: eNote\eNote.Application\Features\Assignments\SubmissionSearchObject.cs
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Assignments;

public sealed class SubmissionSearchObject : BaseSearchObject
{
}

```

## File: eNote\eNote.Application\Features\Assignments\Services\AssignmentService.cs
```cs
﻿using eNote.Application.Common.Exceptions;
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

namespace eNote.Application.Features.Assignments.Services;

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
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var query = context.Set<Assignment>()
            .AsNoTracking()
            .ForEnrolledStudent(student.Id)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<AssignmentDto>, q => q.OrderBy(x => x.DueAt));
    }

    public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

        var entity = await context.Set<Assignment>()
            .ForEnrolledStudentById(student.Id, assignmentId)
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

```

## File: eNote\eNote.Application\Features\Assignments\Services\AssignmentSubmissionService.cs
```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.Students;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Assignments.Services;

public sealed class AssignmentSubmissionService(
    IAppDbContext context,
    IClock clock,
    IUserContextResolver resolver,
    IInstructorAccessService instructorAccess,
    ICurrentUserService currentUserService,
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
            items => resolver.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
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
        submission.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return MapSubmission(submission, submission.Student, await resolver.GetStudentDisplayNameAsync(submission.Student));
    }

    private async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, AssignmentSubmitRequest request)
    {
        var student = await resolver.GetStudentAsync(currentUserService.UserId);

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
                CreatedById = currentUserService.UserId
            };
            assignment.AssignmentSubmissions.Add(existing);
        }

        existing.Submit(request.FilePath?.Trim(), clock.UtcNow);
        existing.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return MapSubmission(existing, student, await resolver.GetStudentDisplayNameAsync(student));
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);
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

## File: eNote\eNote.Application\Features\Assignments\Services\IAssignmentService.cs
```cs
using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Assignments.Services;

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

## File: eNote\eNote.Application\Features\Assignments\Services\IAssignmentSubmissionService.cs
```cs
using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Assignments.Services;

public interface IAssignmentSubmissionService
{
    Task<AssignmentSubmissionDto> SubmitWithFileAsync(int assignmentId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, SubmissionSearchObject search);
    Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request);
}

```

## File: eNote\eNote.Infrastructure\Data\Configurations\AssignmentSubmissionConfig.cs
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

## File: eNote\eNote.API\Controllers\Assignments\InstructorAssignmentController.cs
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
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

## File: eNote\eNote.API\Controllers\Assignments\InstructorAssignmentSubmissionController.cs
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
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

## File: eNote\eNote.API\Controllers\Assignments\StudentAssignmentController.cs
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
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

## File: eNote\eNote.API\Controllers\Assignments\StudentAssignmentSubmissionController.cs
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
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

