# eNoteV2 — Phase 4: Style Audit

**Status**: Not started — backlog seeded
**Lens**: best-for-the-code — judged on cohesion, clarity, and honest structure, not diff size (see [Phase3_Refactor_Procedure.md](Phase3_Refactor_Procedure.md))

> Phase 4 is the method- and file-level pass: verbosity, dead abstractions, naming, SOLID-at-the-small, and **file/folder structure**. Run it *after* Phase 3's actor redesign, over the same files while they're fresh.

---

## Scope (to be expanded when Phase 4 begins)

- **Verbosity & dead abstractions** — single-implementation interfaces, one-product factories, config for values that never change, helper indirection that earns nothing.
- **Naming** — type/method names that mislead or restate the namespace (NDepend ND2013 flagged a few).
- **SOLID-at-the-small** — method-level SRP, guard-clause consistency, over-broad public surface (NDepend visibility rules: ND1804/ND1807/ND1803).
- **File & folder structure** — one-type-per-file adherence (ND2102), folder/namespace alignment, and the item below.

---

## File & folder structure — backlog

### B1. Flatten the entity sub-namespaces (carried over from Phase 3 §4)

**What:** entity POCOs currently live in per-feature sub-namespaces (`Entities.Identity`, `Entities.Rentals`, `Entities.Assignments`, …). Because the domain navigations are bidirectional and interlocking (`Student ↔ InstrumentRental`, `Student ↔ Enrollment ↔ Course ↔ Instructor`), these sub-namespaces are mutually dependent — NDepend ND1400.

**Why it belongs to file structure, not logic:** the navigations are correct and stay untouched. The only question is namespace granularity. C# namespaces are independent of folders, so **keep every file where it is** and change only the `namespace` declaration to a single `eNote.Domain.Entities`.

**Why do it (best-for-code):** the entity graph is one cohesive aggregate; per-feature sub-namespaces *imply* separable modules that the navs prove don't exist. One namespace is the more honest model, and ND1400 clears as a byproduct.

**Cost / risk:** mechanical — change `namespace` lines on the entity files + fix `using`s across the solution (EF configs, Mapster registers, services). Low risk, wide touch. Verify with an empty `dotnet ef migrations add _verify_noop` diff (schema unchanged) and a green `dotnet test`.

**Priority:** low. Bundle it with the rest of the folder-tree pass so the churn happens once.
