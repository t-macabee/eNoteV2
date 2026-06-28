# 🗺️ eNote System Architecture Overview

**Generated**: 2026-06-28T06:49:19.455015+00:00  
**Commit**: latest

## Architectural Layers (Clean Architecture)

```
eNote.API (Presentation)
        │
        ▼
eNote.Application  ──►  eNote.Domain
        │
        ▼
eNote.Infrastructure (Persistence, Messaging, External Services)
```

## Asynchronous Communication
- **Event Bus**: RabbitMQ + Outbox pattern for cross-domain integration events.

## Bounded Contexts

Use these files when building or refreshing your mental model:

- **`Domain_Auth.md`** — 13 files (core logic, entities, features)
- **`Domain_InstrumentRentals.md`** — 23 files (core logic, entities, features)
- **`Domain_Assignments.md`** — 19 files (core logic, entities, features)
- **`Domain_Courses.md`** — 17 files (core logic, entities, features)
- **`Domain_Payments.md`** — 0 files (core logic, entities, features)
- **`Domain_Shared_Infrastructure.md`** — 249 files (foundational + cross-cutting)

---

## How to Use These Files with AI Agents (Recommended Workflow)

1. Start with `00_System_Overview.md`
2. Load the specific domain files you need into persistent context
3. Ask the agent to "explain the {domain} bounded context back to me"
4. Run architecture audit prompts on the loaded files
5. Re-run this script in incremental mode after code changes
6. Compare outputs over time for drift detection

This structure is optimized for long-running agent sessions rather than one-shot prompts.
