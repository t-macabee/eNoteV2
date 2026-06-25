# 🗺️ System Architecture Overview & Index

This master index serves as Claude's top-level mental map of the system architecture.

## 🏗️ Architectural Layers Hierarchy (Clean Architecture)
The system strictly follows dependency injection patterns flowing inwards:
```text
  eNote.API (Presentation) ──> eNote.Infrastructure ──┐
             │                                        ▼
             └─────────> eNote.Application ──> eNote.Domain
```

## 🔀 Asynchronous Communication Pattern
* **Event Bus:** Cross-domain integration events are decoupled via **RabbitMQ**.
* **Flow Example:** When `Domain_InstrumentRentals` fires an execution state change event, a background consumer in the isolated Worker container processes it asynchronously to trigger notifications[cite: 3].

## 📦 Identified Bounded Contexts & File Distribution
Use these specific domain files when pinning context in Claude or Cursor:

* **`@Domain_Auth.md`**: Contains the core logic, features, DTOs, and contracts for the **Auth** context (13 source files).
* **`@Domain_InstrumentRentals.md`**: Contains the core logic, features, DTOs, and contracts for the **InstrumentRentals** context (23 source files).
* **`@Domain_Assignments.md`**: Contains the core logic, features, DTOs, and contracts for the **Assignments** context (18 source files).
* **`@Domain_Courses.md`**: Contains the core logic, features, DTOs, and contracts for the **Courses** context (16 source files).
* **`@Domain_Payments.md`**: Contains the core logic, features, DTOs, and contracts for the **Payments** context (0 source files).
* **`@Domain_Shared_Infrastructure.md`**: Fallback directory for shared configurations, generic middleware, and foundational cross-cutting components (238 files)[cite: 3].