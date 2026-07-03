# Semantic File Schema — v1

This is the single source of truth for what a `.codeaudit/semantic/*.semantic.json`
file is. It was written **after** the fact, derived from the 15 real files
that already exist, not designed speculatively — the previous attempt at a
schema (`class-semantic-schema.json`) was written before any real output
existed and drifted from day one. This document reflects what's actually
there, including two known inconsistencies (§ "Known inconsistencies")
that exist across the current 15 files and should be reconciled before
this schema is treated as strictly enforced.

If someone asks "what is a semantic file," this document is the answer —
not a prompt, not an example file.

## File identity

- Path: `.codeaudit/semantic/{sanitizedSymbolId}.semantic.json`
- `sanitizedSymbolId` = `symbolId` with `.`, `<`, `>`, `, `, `,` replaced by `_`
  (matches `Program.cs`'s `SanitizeId`)
- One file per curated symbol. Not every symbol has one — absence of a file
  means "not curated, query tokensave or read the source directly," not
  "unknown."

## Top-level fields

| Name | Required | Type | Allowed values | Description |
|---|---|---|---|---|
| `schemaVersion` | Yes | integer | `1` | Format version. Bump when a breaking field change is made. |
| `symbolId` | Yes | string | — | Fully-qualified Roslyn display name. Must match the `symbolId` in the corresponding `.codeaudit/symbols/*.json` Layer 1 fact file. |
| `fingerprint` | Yes | string | 16-char uppercase hex | Copied from Layer 1 at the time this file was written/last confirmed. This is what sweep compares against to detect staleness — see `status`. |
| `generatedAt` | Yes | string | ISO 8601 UTC | When this entry was authored or last substantively edited. Not auto-updated by sweep (sweep only touches `status`/`staleReason`). |
| `status` | Yes | string | `proposed` \| `confirmed` \| `stale` | `proposed`: written but not yet human-reviewed. `confirmed`: a human has verified it against current source. `stale`: the indexer detected the source changed since this was written/confirmed — see `staleReason`. **As of this writing, all 15 files are `proposed`; none are `confirmed` yet, and none are currently `stale`.** |
| `staleReason` | Only when `status: "stale"` | string | `fingerprint_changed` \| `source_deleted` | Set exclusively by `Indexer/Program.cs`'s `FlagSemanticStaleAsync` during sweep. Never written by a human, never removed automatically — a human clears it by re-confirming or rewriting the entry. |
| `facts` | Yes | object | — | Deterministic, copied or directly derivable from the Layer 1 `SymbolInfo`. See below. |
| `interpretation` | Yes | object | — | Judgment. Not directly observable from Layer 1 facts alone — this is the actual value-add of the file. See below. |
| `evidence` | Yes | object | — | Concrete pointers (method/field names) that ground the interpretation. |
| `review` | Yes | object | — | Human review trail. See below. |

## `facts` (object)

| Name | Required | Type | Allowed values | Description |
|---|---|---|---|---|
| `kind` | Yes | string | `Class` \| `Interface` (only these two observed; `Struct`/`Enum`/`Record` are plausible given Layer 1 supports them but none curated yet) | Roslyn `TypeKind`. |
| `namespace` | Yes | string | — | Fully-qualified namespace. |
| `inherits` | Yes | string \| `null` | — | Base type's fully-qualified name, or `null` if there is none / it's implicitly `object`. |
| `implements` | Yes | array\<string\> | — | Fully-qualified interface names. May be empty. |
| `sourceFile` | Yes | string | — | Repo-relative path to the declaration file. **Caveat:** Layer 1's own `sourceFiles` field is populated from *referencing* locations (`FindReferencesAsync`), not necessarily the declaration file — for symbols with few/no in-solution callers this can be misleading or empty. The author of a semantic file is expected to locate and confirm the actual declaration path independently, not trust Layer 1's `sourceFiles[0]` blindly. |
| `dependencies` | Yes | array\<string\> | — | Curated subset of Layer 1's `references`: meaningful domain/application/infrastructure types, with primitives and framework/BCL noise (`System.*`, `Microsoft.*`, `int`, `string`, etc.) excluded. May be empty. |

## `interpretation` (object)

| Name | Required | Type | Allowed values | Description |
|---|---|---|---|---|
| `architecturalRole.value` | Yes | string | See enum below | The symbol's primary architectural role. |
| `architecturalRole.confidence` | Yes | number | `0.0`–`1.0` | Calibrated, not a fixed default — see "Confidence calibration" below. |
| `responsibility.value` | Yes | string | — | One clear sentence: what this symbol does and why it exists in the domain. |
| `responsibility.confidence` | Yes | number | `0.0`–`1.0` | — |
| `owns` | Yes | array\<string\> | — | Specific responsibilities this symbol actually has, grounded in its real behavior. May be empty. |
| `doesNotOwn` | Yes | array\<string\> | — | Things explicitly delegated elsewhere. May be empty. |
| `collaborators` | Yes | array\<object\> | — | See below. May be empty. |
| `contracts` | Yes | array\<string\> | — | Falsifiable guarantees — statements a test could check. May be empty. |
| `assumptions` | Yes | array\<string\> | — | **Flat strings**, not objects. Unenforced preconditions the symbol relies on. May be empty. |
| `invariants` | Yes | array\<string\> | — | **Flat strings**, not objects. Conditions the symbol actively maintains. May be empty. |
| `failureModes` | Yes | array\<string\> | — | Concrete ways this can fail. May be empty. |
| `architecturalNotes` | Yes | array\<string\> | — | Anything else worth flagging — fragility, risk, design tradeoffs. May be empty. |

**`architecturalRole` enum, as actually used across the 15 curated files:**
`StateMachine`, `ApplicationService`, `InfrastructureService`, `Middleware`,
`Helper`, `Configuration`, `Specification`, `BackgroundJob`.

**Additional values carried over from the original draft schema, legal but
not yet used by any curated file:** `Entity`, `ValueObject`, `AggregateRoot`,
`DomainService`, `DomainEvent`, `Repository`, `UnitOfWork`, `Command`,
`Query`, `CommandHandler`, `QueryHandler`, `Validator`, `DTO`, `Mapper`,
`Controller`, `IntegrationEventHandler`, `Extension`, `TestDouble`, `Unknown`.

Note: `InfrastructureService` is **not** in the original draft schema at
all — it was needed in practice (for `AuthService`, `TokenService`,
`TokenRevocationService`, `RentalNotificationDispatcher`) to distinguish
infrastructure-layer implementations from in-domain `ApplicationService`s,
and is now part of the frozen enum.

### `collaborators[]` (object)

| Name | Required | Type | Allowed values | Description |
|---|---|---|---|---|
| `symbol` | Yes | string | — | Fully-qualified name of the collaborating type. |
| `relationship` | Yes | string | See "Known inconsistencies" below | Nature of the relationship. |
| `detail` | No | string | — | Why/how, grounded in actual code. Present on most entries; absent entirely in the earliest file (`RentalStateMachine`), written before this field existed. |

## `evidence` (object)

| Name | Required | Type | Description |
|---|---|---|---|
| `primaryMethods` | Yes | array\<string\> | Real method names that define the symbol's behavior. May be empty. |
| `primaryFields` | Yes | array\<string\> | Real field/property names. May be empty. |

## `review` (object)

| Name | Required | Type | Allowed values | Description |
|---|---|---|---|---|
| `status` | Yes | string | `pending` (only value observed so far; `approved` or similar is presumably legal once a human review pass actually happens, but this hasn't been exercised yet) | Human review state, distinct from the top-level `status` (which tracks freshness, not review). |
| `reviewedBy` | Yes | string \| `null` | — | Who reviewed it. `null` until reviewed. |
| `reviewedAt` | Yes | string \| `null` | ISO 8601 UTC | When reviewed. `null` until reviewed. |

## Confidence calibration (applies to every `confidence` field)

Not a fixed default. Roughly:
- **0.9+**: obvious from naming/structure alone (e.g. a class named `*StateMachine` that implements a single-method `IStateMachine` interface).
- **0.7–0.89**: a reasonable inference from directly observed behavior, not just naming.
- **< 0.5**: a genuine guess filling a required field — should be rare, and the accompanying text should say so explicitly rather than hiding the uncertainty behind a confident-looking number.

A batch where every entry has the same confidence value is a sign calibration didn't actually happen, not a sign of consistent quality.

## Known inconsistencies (not yet reconciled — read before treating this as strictly enforced)

1. **`collaborators[].relationship` casing is inconsistent across the 15 files.** The 4 earliest hand-written files (`RentalStateMachine`, `RentalCommandService`, `RentalQueryService`, `RentalBilling`) and a few others use PascalCase values (`Accepts`, `Calls`, `Implements`, `Injects`, `Mutates`, `ReferencesStatically`, `Returns`). The remaining files (the DeepSeek-generated batch, and the Auth/Token/Tenant files) use lowercase-hyphenated values (`accepts`, `calls`, `creates`, `implements`, `injects`, `mutates`, `owns`, `references-statically`, `returns`). **This schema adopts lowercase-hyphenated as canonical** (it's what the majority of files use and matches the original draft schema's convention): `implements`, `extends`, `injects`, `calls`, `creates`, `publishes`, `subscribes`, `returns`, `accepts`, `owns`, `mutates`, `references-statically`. The PascalCase files have not yet been migrated to match — treat their `relationship` values as needing normalization before any tooling does exact-match filtering on this field.
2. **`class-semantic-schema.json`** (the previous, pre-existing schema file in this same directory) is stale and contradicts this document in several places (it expects a flat `facts.architecturalRole` string and a top-level `confidence` field; no file actually uses that shape). It should be deleted once this document is treated as authoritative, to avoid two contradictory schemas sitting side by side.

## What this schema deliberately does not cover

No prompt-generator, validator, or knowledge-graph builder exists against
this schema, and none is planned unless the curated set grows meaningfully
beyond its current 15 entries. This document exists so that anyone
adding or editing a semantic file by hand (or reviewing one written by an
external model) has one place to check the shape against — not to
support a mechanical pipeline that doesn't exist yet.
