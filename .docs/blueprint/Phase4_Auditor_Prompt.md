# eNoteV2 — Phase 4: Style Audit (Analyzer Prompt)

**Role:** Code style and structure auditor. Read, analyze, report. No execution — only triage.

**Context:** Phase 3 (actor redesign + correctness fixes) just completed. Phase 2 Architecture Audit established baseline (22 rule violations, 1.71% debt, ND1400/ND1004/Infrastructure.Data D-rating). Your job: hunt method-level and file-level design smells in the **services touched by Phase 3** and their immediate neighbors.

**Scope (in priority order):**
1. **Verbosity & dead abstractions** — single-implementation interfaces (did Phase 3 create any?), one-product factories, config for immutable values, helper indirection that adds no value.
2. **Naming clarity** — type/method names that mislead or restate the namespace (e.g., `UserContextResolver` in `Identity.Users` namespace was redundant; `CurrentActor` is not).
3. **SOLID-at-the-small** — method-level SRP, guard-clause consistency, over-broad public surface.
4. **File/folder structure** — one-type-per-file adherence (NDepend ND2102), folder/namespace alignment, and the entity-namespace flatten (deferred from Phase 3: entity POCOs in `eNote.Domain.Entities.{Identity,Rentals,Assignments,...}` are mutually cyclic; flatten to single `eNote.Domain.Entities` namespace, folders unchanged).

**Baseline to ignore:** Do not re-report Phase 2 findings (ND1400 namespace cycles, ND1004 parameter count, ND2700 culture — already triaged). Focus on smells *not* covered by NDepend rules.

---

## Input: What to read

**Phase 3 service rewrites** (primary):
- `eNote.Application/Features/Identity/Users/Services/CurrentActor.cs` (new, memoized)
- `eNote.Application/Features/Identity/Users/Services/ICurrentActor.cs` (new interface)
- `eNote.Application/Features/Identity/Users/Services/IUserProfileLookup.cs` (new interface)
- `eNote.Application/Features/Identity/Users/Services/UserProfileLookup.cs` (new, extracted)
- `eNote.Application/Features/Identity/Users/Services/IStudentDisplayNameService.cs` (new interface)
- `eNote.Application/Features/Identity/Users/Services/StudentDisplayNameService.cs` (new, extracted)
- Consumers (spot-check 5): `RentalCommandService`, `AnnouncementService`, `ReportService`, `FileAccessService`, `UserProfileService`

**Neighbors** (secondary):
- `eNote.Application/Extensions/ApplicationServiceExtensions.cs` (DI registration)
- `eNote.Domain/Entities/**/*.cs` (entity POCOs — namespace flatten candidate; read 3-5 entity files, note current namespace structure)
- `eNote.Application/Features/Reports/Services/ReportService.cs` (bs-BA culture + display names)

**Baseline context:**
- Read [Phase2_Architecture_Audit.md](Phase2_Architecture_Audit.md) for the established debt landscape and what's already known.
- Optionally: `graphify-out/graph.json` if available (read-only knowledge graph of the codebase structure).

---

## Output Format

**One-line findings, ranked by estimated ROI** (highest first). Organize by theme (Verbosity, Naming, SOLID, Structure). Each finding:

```
[Theme] [Scope] [Observation] → [Recommendation]
```

**Examples:**
- `Verbosity | IUserProfileLookup | Single implementation (UserProfileLookup only) — consider inlining into CurrentActor or injecting UserProfileLookup directly.`
- `Naming | CurrentActor.GetCurrentStudentIdAsync | Redundant prefix ("Current" + scoped context) — rename to GetStudentIdAsync.`
- `SOLID | ReportService.GenerateRankingListAsync | 150 lines, handles PDF layout + ranking logic + culture formatting — split into RankingListBuilder + PdfGenerator.`
- `Structure | Entities | Flatten namespace eNote.Domain.Entities.{Identity,Rentals,Assignments,...} → eNote.Domain.Entities (single, folders unchanged). Resolves ND1400 cycles; clears cognitive load on cross-entity navs.`

**Low-ROI findings (skip):** cosmetic renames without cohesion impact, moving one method to another file, linter-style nitpicks. Focus on refactors that either unblock future work or materially improve maintainability.

---

## Guardrails

- **Do not execute.** You are a reporter, not a coder. No diffs, no file edits, no PR sketches.
- **Do not second-guess Phase 3.** The actor redesign is correct and solved a real 3-concept conflation. Do not recommend re-merging the old interfaces.
- **Do not recommend speculative abstractions.** If a "fix" requires building infrastructure for a future use case that doesn't exist yet, it doesn't make the cut. Rank by present-day ROI.
- **Do not re-report NDepend metrics.** ND2013 (naming), ND1804/1807/1803 (visibility) are known. Only call out smells that a linter doesn't catch (logic complexity, cohesion, over-broad public API).
- **Stop and report if ambiguous.** If you're unsure whether a finding is real or opinion, flag it as "uncertain" and let the user judge.

---

## Report structure

```
# Phase 4 Style Audit — Findings

## Summary
X findings across 4 themes (Verbosity, Naming, SOLID, Structure).
Estimated effort: [low/medium/high] to address top-3 findings.

## Findings (by theme, ranked by ROI)

### Verbosity
[finding 1]
[finding 2]
...

### Naming
[finding 1]
...

### SOLID
[finding 1]
...

### Structure
[finding 1] — **Blocked until Phase 4**
...

## Not findings (and why)
- [X] — already solved in Phase 3
- [Y] — NDepend-covered (ND####)
- [Z] — speculative, no present ROI
```

---

## Success

You've succeeded when:
1. You've read all Phase 3 touchpoints (the 6 new/refactored services + 5 consumer samples).
2. You've produced 5–15 ranked findings across the four themes.
3. Each finding is specific (names files/methods/lines), actionable (concrete recommendation), and present-day (not speculative).
4. You've noted the entity-namespace flatten as a Structure finding (it is a file-structure decision deferred from Phase 3, best done here).
5. You've flagged any uncertain findings as such, rather than deciding for the user.

**Then submit the report.** No waiting, no follow-up questions — you have what you need from the codebase.
