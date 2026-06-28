# eNoteV2 — Phase 6: Full Codebase Style & Syntax Audit

**Role:** Static code auditor. Read, analyze, produce a ranked file-by-file worklist. No execution — findings only.

**Stack:** .NET 10 / C# 13, ASP.NET Core, EF Core 10, MassTransit, Mapster, FluentValidation, QuestPDF.

---

## Context

Phases 1–5 completed:
- Phase 3: Actor/identity redesign, bs-BA culture fix, read-only collections (partial)
- Phase 5: Entity namespace flatten, ghost folder removal, ICurrentActor naming standardized, caching tightened, announcement builder extracted

NDepend post-Phase-5 open items:
- **ND1203** — unsealed leaf classes (new Phase 3 services not sealed)
- **ND2300** — 2 collection properties still not read-only
- **ND1212** — 1 empty interface in Rentals area
- **ND1004** — 2 methods still with 8 parameters (known, deliberate)

---

## Audit Rules

Apply all rules from the checklist below to every file you read.

### 1. Syntax & Correctness
- Nullable reference types — flag any `#nullable` gaps or unguarded nulls
- Unused `using` directives, unused variables, dead code
- Modern C# syntax: expression-bodied members, pattern matching (`is not null`, `is { }`, switch expressions), `init` properties, `required` properties, record types for DTOs

### 2. Naming Conventions
- Classes/Interfaces/Methods/Properties: PascalCase
- Local variables/parameters: camelCase
- Private fields: `_camelCase`
- Constants: PascalCase or UPPER_CASE (consistent within file)
- Interfaces: `I` prefix
- No abbreviations unless industry-standard (`Id`, `Dto`, `Url`)

### 3. Unnecessary Complexity
- Passthrough `async` methods that `await` a single call — remove `async`/`await`, return the Task directly
- Trivial private helpers that wrap a single expression with no added logic
- Wrapper classes/methods with no behaviour
- YAGNI: no speculative abstractions

### 4. Readability & Structure
- Methods over 40 lines — flag for extraction
- Nesting deeper than 3–4 levels — flag for guard clause / early return refactor
- Magic strings/numbers — flag for constant or enum
- Deep if-else chains — prefer early returns

### 5. C# Backend Best Practices
- `async/await` correctness: no `.Result`, `.Wait()`, `async void` (except event handlers)
- `CancellationToken` — EF Core async methods accept it; flag any service/repository method that queries the DB without threading a `CancellationToken` parameter through
- `AsNoTracking()` — flag any read-only EF query (no entity mutation in same scope) that is missing it
- Constructor injection only — no service locator, no `HttpContext.RequestServices`
- `sealed` keyword — flag concrete leaf classes (no descendants, not abstract) that are missing `sealed`
- Primary constructors (C# 12+) — flag inconsistency: if a class uses old-style constructor + field declarations where a primary constructor would be cleaner, note it
- `private readonly` — flag private fields assigned only in constructor that are missing `readonly`

### 6. Consistency
- `var` vs explicit type — flag inconsistency within a file (pick one style per file)
- Brace style, spacing — note only if it deviates from the file's own established pattern
- Mapster vs manual mapping — flag any manual property-by-property mapping where Mapster is already used in the project

---

## Scope

**Read and audit these layers:**

### eNote.Application
- `Features/**/Services/*.cs` — all service implementations
- `Features/**/Services/I*.cs` — all service interfaces
- `Common/**/*.cs` — shared interfaces, exceptions, extensions, paging

### eNote.API
- `Controllers/**/*.cs` — all controllers
- `Services/*.cs` — API-layer services
- `Extensions/*.cs` — service registration extensions
- `Consumers/*.cs` — MassTransit consumers

### eNote.Worker
- All `.cs` files

### eNote.Infrastructure (non-generated only)
- `Data/Seed/*.cs`
- `Data/ENoteContext.cs`
- `Data/ENoteContextFactory.cs`
- `Services/*.cs` (if any)
- `Extensions/*.cs` (if any)
- **Skip:** `Data/Migrations/` (auto-generated), `Data/Configurations/` (spot-check 3–4 only)

### eNote.Domain
- `Entities/**/*.cs` — light pass: entity methods, nav collections, value objects
- **Skip:** navigation property declarations (correct by design)

---

## Skip Explicitly
- `eNote.Infrastructure/Data/Migrations/` — auto-generated, do not read
- `eNote.Infrastructure/Data/Configurations/` — spot-check 3 files max, note if pattern is consistent, move on
- `eNote.Tests/` — test files are out of scope
- `graphify-out/` — tooling output

---

## Output Format

Produce a **file-by-file worklist**, sorted by estimated fix effort (highest ROI first within each layer).

```
## [Layer] — [File path]

- [Rule category] | [Finding] → [Recommended fix]
- [Rule category] | [Finding] → [Recommended fix]
...
```

**Example:**
```
## Application — Features/Rentals/Services/RentalCommandService.cs

- Async | GetActiveRentalsAsync passes no CancellationToken to EF queries → add CancellationToken parameter, thread to all .ToListAsync()/.FirstOrDefaultAsync() calls
- Sealed | RentalCommandService is a leaf class → add sealed
- Readonly | private RentalStateMachine _stateMachine not marked readonly → add readonly
```

After the worklist, append:

```
## Cross-cutting findings
[Patterns that appear in 3+ files — call out once here, not in every file]

## Skip log
[Files skipped and why]

## Summary
Total files audited: X
Total findings: Y
Estimated execution effort: [low/medium/high]
Top 3 highest-ROI fixes: [list]
```

---

## Guardrails

- **Do not execute.** Findings only — no diffs, no edits.
- **Do not re-report Phase 3/5 completed work.** The actor redesign, bs-BA fix, announcement builder, namespace flatten, ICurrentActor renaming are done.
- **Do not flag NDepend-tracked items** (ND1004 8-param constructors — known and deliberate).
- **One finding per issue per file.** Do not repeat cross-cutting findings in every file — call them out once in the cross-cutting section.
- **Skip auto-generated files.** If you open a file and see `<auto-generated>` or EF migration scaffolding, close it immediately.
- **Flag but do not decide on `CancellationToken`.** It's a broad change; list every gap but note it as a batch operation for the executor.

---

## Execution handoff

After producing this report, it will be handed to **qwen3-coder:4b via Aider** for file-by-file mechanical execution. Write findings precisely enough that a small model can act on them without re-reading your analysis. Each finding should be self-contained: file, location, what to change, what to change it to.

**Write the report to:** `.docs/blueprint/Phase6_Audit_Report.md`

Then stop. No follow-up questions — you have everything you need.
