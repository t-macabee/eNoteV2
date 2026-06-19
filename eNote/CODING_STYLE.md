# eNote C# coding style

This document defines formatting and readability rules for the eNote solution.  
**Machine-enforced rules** live in [`.editorconfig`](.editorconfig). **Judgment rules** (spacing, grouping, `var`) are defined here and should be applied consistently across all `.cs` files.

When refactoring or adding code, match existing files that already follow this guide (`InstrumentRentalDto`, `CourseService`, `MapsterConfig`).

---

## 1. Tooling

| Tool | Role |
|------|------|
| `.editorconfig` | Indent (4 spaces), Allman braces, 180-char lines, `var` preferences, spaces in control flow |
| IDE / Rider / VS | Applies `.editorconfig` on format |
| This document | Blank lines, property grouping, when to wrap, logical code chunks |
| Cursor rule | `.cursor/rules/enote-csharp-style.mdc` — agent applies these rules when editing C# |

Run from the `eNote/` directory:

```bash
dotnet format eNote.sln
```

`dotnet format` fixes brace/indent issues. It does **not** fully apply grouping or `var` rules — those need manual or agent review using this document.

---

## 2. `var` vs explicit type

### Use explicit type when the type is not obvious from the right-hand side

```csharp
// Good
Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);
Course entity = await context.Set<Course>()...
IQueryable<Course> query = context.Set<Course>()...
PagedResult<CourseDto> result = await query.ToPagedResultAsync(...);
Enrollment? existing = course.Enrollments.FirstOrDefault(...);

foreach (Lecture lecture in entity.Lectures)
{
    lecture.SoftDelete();
}
```

### `var` is allowed when the type is apparent

```csharp
// Good — type is obvious from constructor
var entity = new Course(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate, instructor.Id);
var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), ...);
```

### Do not use `var` when it hides the type

```csharp
// Bad
var data = GetData();
var result = await query.ToPagedResultAsync(...);

// Good
List<CourseDto> courses = await GetCoursesAsync();
PagedResult<CourseDto> result = await query.ToPagedResultAsync(...);
```

### Summary

| Right-hand side | Prefer |
|-----------------|--------|
| `new ConcreteType(...)` | `var` or explicit (both OK) |
| `await` method call | **Explicit** |
| LINQ / `IQueryable` | **Explicit** |
| Interface-typed result | **Explicit** |
| Anonymous type | `var` (required) |

`.editorconfig` encodes: `var` when apparent, explicit elsewhere.

---

## 3. Vertical spacing (blank lines)

Blank lines separate **logical steps**, not every statement.

### Inside a method

```csharp
public async Task<CourseDto> CreateAsync(CourseRequest request)
{
    Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

    logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, currentUserService.UserId);

    var entity = new Course(...);
    entity.SetPublishedStatus(request.IsPublished);
    entity.CreatedById = currentUserService.UserId;

    context.Set<Course>().Add(entity);
    await context.SaveChangesAsync();

    logger.LogInformation("Course {CourseId} created", entity.Id);

    return mapper.Map<CourseDto>(entity);
}
```

| Do | Don't |
|----|-------|
| Blank line between fetch → log → mutate → save → return | Blank line between every statement |
| Blank line before `return` when the method body is long | Blank line between `entity.Foo = x` and `entity.Bar = y` on the same object |
| Blank line after variable declarations block when next block is unrelated | Blank lines inside a short fluent chain |

### Between members

- One blank line between methods, properties, and nested types.
- No blank line between `{` and first statement, or between last statement and `}`.

### Between usings and namespace

```csharp
using eNote.Application.Common.Paging;
using MapsterMapper;

namespace eNote.Application.Features.Courses.Services
{
```

One blank line after the last `using`, before `namespace`.

---

## 4. Grouping related lines

### Fluent queries and chains — keep as one block

```csharp
Course entity = await context.Set<Course>()
    .Include(c => c.Enrollments)
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
    ?? throw new NotFoundException(Messages.CourseNotFound);
```

Break after each `.Method()` when the chain spans multiple lines. Do not insert blank lines inside the chain.

### Entity mutation — group without blank lines

```csharp
entity.UpdateDetails(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate);
entity.SetPublishedStatus(request.IsPublished);
entity.UpdatedById = currentUserService.UserId;
```

### Complex conditions — break by clause

```csharp
.FirstOrDefaultAsync(c =>
    c.Id == id &&
    (c.IsPublished ||
     c.Enrollments.Any(e =>
         e.StudentId == student.Id &&
         e.EnrollmentStatus == EnrollmentStatus.Active)))
```

### DTO and entity properties — logical groups with one blank line between groups

**No section comments** (`// --- IDs ---`). Grouping alone should be enough.

Reference order for `InstrumentRentalDto`:

1. Identifiers (`Id`, foreign keys, `StudentUserId`)
2. Display / status strings (`InstrumentModel`, `RentalStatus`, notes)
3. Timestamps (`RequestedAt`, `ApprovedAt`, …)
4. Audit IDs (`ApprovedById`, `RejectedById`)
5. Money / computed fee fields

```csharp
public int Id { get; set; }
public int InstrumentId { get; set; }
public int StudentUserId { get; set; }

public string InstrumentModel { get; set; } = null!;
public InstrumentRentalStatus RentalStatus { get; set; }

public DateTime RequestedAt { get; set; }
public DateTime? ApprovedAt { get; set; }

public decimal Fee { get; set; }
```

Properties stay **single-line**: `public int Id { get; set; }` — not split across rows.

---

## 5. Line length and wrapping

From `.editorconfig`:

- **Max line length: 180 characters**
- **Do not wrap** method parameters or arguments by default

### Keep on one line when under 180 characters

```csharp
var message = new RentalStatusChanged(rental.Id, studentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);

return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate));
```

### Wrap when

1. Line exceeds **180 characters**
2. A fluent chain is clearer with one call per line
3. A boolean expression has multiple `&&` / `||` clauses
4. A method signature exceeds 180 characters (rare)

### Wrapping style

- Continuation lines indent **4 spaces** from the start of the statement
- For method arguments, align under the opening `(` or use consistent +4 indent — match the surrounding file

---

## 6. Braces, indentation, and syntax

| Rule | Value |
|------|-------|
| Indent | 4 spaces, no tabs |
| Braces | Allman — opening `{` on its own line |
| Control flow | Space after keyword: `if (x)`, `for (…)`, `foreach (…)` |
| Method declarations | No space before `(`: `void Foo()` not `void Foo ()` |
| File ending | Single trailing newline |
| Primary constructors | OK — keep parameter list on one line unless >180 chars |

---

## 7. File layout order

```csharp
// 1. usings (System first, then third-party, then project — optional grouping)
// 2. namespace
// 3. type declaration
//    3a. constants / static fields
//    3b. fields
//    3c. constructor(s)
//    3d. public methods
//    3e. private methods
```

Prefer **file-scoped namespace** for new files when the rest of the folder already uses it:

```csharp
namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher { ... }
```

Use block namespace when matching an existing file in the same folder.

---

## 8. Checklist — reviewing a file

- [ ] No line over 180 characters (unless unavoidable string literal)
- [ ] Allman braces, 4-space indent
- [ ] Properties single-line; DTOs grouped with blank lines between groups
- [ ] Explicit types for `await`, LINQ, interfaces; `var` only when apparent
- [ ] Blank lines between logical steps in methods, not within tight blocks
- [ ] Constructors and simple calls not broken unnecessarily
- [ ] No redundant section comments in DTOs
- [ ] `dotnet format eNote.sln` produces no unexpected diffs for that file

---

## 9. Applying this document to the whole codebase

This guide does not auto-run. To apply it repository-wide:

1. Run `dotnet format eNote.sln` for mechanical fixes.
2. Walk project-by-project (Domain → Application → Infrastructure → API → Worker):
   - DTOs and entities: property order and grouping
   - Services: spacing, `var`, line wraps
   - Extensions / config: file-scoped namespaces where consistent
3. Use the checklist in §8 per file.
4. In Cursor, the rule `.cursor/rules/enote-csharp-style.mdc` instructs the agent to follow this document on every C# edit.

Suggested commit message when doing a full pass:

```
style: apply CODING_STYLE formatting across solution
```
