You are doing a structural reorganization pass on the eNoteV2 .NET solution at
C:\Users\Tarik\Desktop\eNoteV2\eNote (solution file: eNote\eNote.sln). This is
NOT a logic rewrite — you are moving files/folders to where the project's own
conventions say they belong, fixing namespaces to match, and leaving behavior
untouched. The codebase went through an earlier AI-assisted change that left
structure inconsistent; your job is to restore consistency, not redesign it.

## Ground truth: the convention already in use

The solution is layered: eNote.Domain -> eNote.Application -> eNote.Infrastructure,
with eNote.Contracts (cross-cutting messages), eNote.API and eNote.Worker as
composition roots, and eNote.Tests mirroring source.

eNote.Application already establishes a feature-based convention under
Features/<Domain>/<Subfeature>/, e.g.:
  Features/Academic/Lectures/{LectureDto.cs, LectureCreateRequest.cs, Services/}
  Features/Identity/Users/Services/
  Features/Rentals/InstrumentRentals/{StateMachine/, Services/, Billing/}
Use this as the reference pattern for what "correct placement" means in
Application, Infrastructure/Data/Configurations, and Validation/.

eNote.Tests is *supposed* to mirror the source tree it exercises — a developer
should be able to guess a test's location from the service's Features/ path.
Treat any place where Tests/ doesn't mirror Application/Features/ as a
candidate defect, not a style choice.

## Layer boundary rules (never violate these while moving files)

- eNote.Domain must not reference Application, Infrastructure, API, Worker, or Contracts.
- eNote.Application may reference Domain and Contracts only.
- eNote.Infrastructure may reference Application and Domain.
- eNote.API and eNote.Worker may reference Application, Infrastructure, Contracts.
- A file move that would require crossing these boundaries backward means the
  file is in the wrong PROJECT, not just the wrong folder — flag it in your
  report rather than silently restructuring project references unless the fix
  is obviously a pure move with no new cross-boundary dependency introduced.

## Known inconsistencies to specifically evaluate (starting points — this list is not exhaustive; do a full pass)

1. eNote.Tests/Courses/CourseEnrollmentServiceTests.cs sits in a top-level
   `Courses/` folder, while every other Courses-related test
   (CourseServiceTests.cs, RankingServiceTests.cs) lives under
   eNote.Tests/Academic/, matching Application's Features/Academic/Courses/.
   Decide whether Courses/ should be merged into Academic/.

2. eNote.Tests splits identity-related tests across Identity/ and Users/
   (CurrentActorTests.cs, UserProfileServiceTests.cs, UserSelfServiceTests.cs
   are in Users/; UserAccountServiceTests.cs, UserIdentityServiceTests.cs,
   UserProvisioningServiceTests.cs are in Identity/) even though ALL of these
   services live together under
   eNote.Application/Features/Identity/Users/Services/ with no such split.
   Determine the correct single location.

3. eNote.Application/Features/Mapping/*MappingConfig.cs is a flat folder of
   Mapster profiles for five unrelated domains (Announcement, Course,
   Instrument, InstrumentRental, Lecture), while every other cross-cutting
   concern in this codebase is co-located inside its owning Features/<Domain>/
   folder. Decide whether each MappingConfig belongs beside its domain
   instead of in a shared bucket — and if you conclude the flat bucket is
   intentional (e.g. because Program.cs registers them as a group), say so
   and leave it, with your reasoning in the report.

4. eNote.Tests has no dedicated test folder for
   Features/Rentals/Instruments/ or Features/Rentals/ReferenceData/
   (Addresses, InstrumentTypes, MusicStores) even though InstrumentRentals
   has one. Confirm whether tests for those services exist elsewhere
   (misplaced) or are genuinely missing (out of scope to create — just note it).

5. Check eNote.API/Controllers/**, eNote.API/Extensions/**, and
   eNote.Infrastructure/** for any file whose contents belong to a different
   layer than the one it's currently sitting in (e.g. business logic that
   crept into a Controller or an Extensions class instead of an Application
   service) — flag these even if you don't move them, since fixing them is
   a code change, not a file move, and out of scope for this pass.

## What "rearrange" means here

- File/folder moves only. Do not change class logic, public signatures, or
  behavior.
- After every move, update the namespace declaration in the moved file and
  every `using` referencing its old namespace, so the solution still compiles.
- Never touch eNote.Infrastructure/Data/Migrations/** or
  ENoteContextModelSnapshot.cs — these are EF Core generated artifacts tied
  to applied database migrations; moving or renaming them breaks migration
  history.
- Use `git mv` (not delete+recreate) so history is preserved.
- Leave anything alone that you don't have a concrete reason to move. "It
  could arguably go elsewhere" is not a reason — only move something if it
  actively violates the mirrored-structure convention above or a layer
  boundary rule.

## Process

1. First, produce a written plan: a table of `current path -> new path ->
   one-sentence reason`, grouped by project. Include a separate section
   listing files you inspected and deliberately left in place, with why.
2. Execute the moves in small batches (a handful of related files at a time),
   fixing namespaces/usings as you go.
3. After each batch, run `dotnet build eNote/eNote.sln` and
   `dotnet test eNote/eNote.sln` from the repo root and confirm both succeed
   before moving to the next batch. Do not proceed past a batch that breaks
   the build.
4. If a fix requires touching Program.cs registrations, .csproj globs, or
   other config wired to the old paths, make that update as part of the same
   batch, not a separate pass.
5. When done, produce a final report: what moved and why, what you left alone
   and why, any layer-boundary violations you found but did NOT fix (because
   fixing them requires a logic change, not a move), and confirmation that
   `dotnet build` and `dotnet test` both pass on the final state.

Do not invent new top-level folders or a different organizing principle than
the Features/<Domain>/<Subfeature> convention already established in
eNote.Application — the goal is consistency with what's already there, not a
redesign.
