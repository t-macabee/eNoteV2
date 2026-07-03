# 🐴 The Ponytail Protocol & System Rules

This file defines the strict architectural constraints, code style guidelines, and execution rules for eNoteV2.

## 🛠️ Build & Test Commands
- **Backend (.NET Core API):** `dotnet build eNote/eNote.sln`
- **Frontend (Flutter Multi-platform):** `flutter build` (run from the frontend directory)

## 🎯 Code Style & The Ponytail Philosophy (YAGNI)
- **Be a Lazy, Elite Senior Developer:** Write the absolute bare minimum code required to satisfy the blueprint specification. Do not build speculative wrappers, interfaces, or helper functions for "future use cases."
- **Surgical Edits:** Use precise Search/Replace diff blocks. Do not rewrite whole files to change isolated logic lines.
- **Native-First:** Prioritize core platform features and native C# / Dart libraries over pulling in external third-party packages unless explicitly required.
- **Async Execution:** Ensure all Entity Framework Core and database calls use explicit async/await chains all the way up the application stack.
- **Data Isolation:** Never expose raw database entities directly to controllers or presentation layers. Map incoming requests to input models and outgoing payloads to clean DTO records.

## ⚠️ Critical Syllabus & Technical Constraints
These rules are non-negotiable. Code that violates these constraints will fail project evaluation:

### 1. Date and Time Standardization
- Use `DateTime.UtcNow` exclusively across the entire application stack. 
- Never mix `DateTime.Now` and `DateTime.UtcNow`, as it causes breaking data discrepancies inside Docker containers.

### 2. Cryptography and Password Security
- **Random Values:** Use `System.Security.Cryptography.RandomNumberGenerator` for tokens, confirmation codes, referral links, or any security-sensitive values. Never use `System.Random`.
- **Password Hashing:** Use `bcrypt`, `Argon2`, or `PBKDF2` exclusively. Using un-salted SHA256, plain HMAC-SHA512, or custom obfuscation algorithms is strictly prohibited.
- **Token Handling:** Password reset tokens must have an explicit expiration timestamp (`ExpiryTime`). Reset codes must never be stored in plain text inside the database.

### 3. Error Handling and Presentation
- **Error Response Transparency:** The frontend api translation utility (`_handleResponse`) must never swallow, mask, or genericize validation failures. It must transparently bubble specific backend validation exceptions and pass them cleanly to the user interface.
- **Production Naming:** Ensure files do not contain "mock", "test", or temporary prefixes if they contain operational production source code.

## 🗺️ Code Inspection Protocol (Agent Navigation Rules)

This codebase has a live structural map (`tokensave`, synced automatically on every commit via `.git/hooks/pre-commit`) plus a small set of hand-curated `.codeaudit/semantic/*.semantic.json` files for the highest-risk symbols. Use them — do not rely on an impression accumulated earlier in the conversation.

- **Never judge a specific location from memory of "the codebase in general."** An earlier pass forms an *averaged* impression, and averages erase exactly the local exceptions that matter. Before making any claim about a specific file or symbol, fetch its current connections fresh (`tokensave` callers/callees, one hop further) and read its actual current source — every time, even if you already looked at something similar this session.
- **Structural similarity is a candidate list, never a decision.** If N classes look alike (e.g. repeated CRUD methods), that only tells you where to look — it does not tell you they should be treated uniformly. Read each candidate individually for a reason it might legitimately diverge before proposing any shared fix. A single unverified generalization across several classes is how a small cleanup becomes a multi-thousand-line, hard-to-review diff.
- **Standard inspection prompt** — use this shape whenever investigating a specific symbol or file:

  ```
  Target: {symbol or file}

  Context (fetched fresh this turn):
  - What it is / its role: {tokensave lookup or the curated .semantic.json, if one exists}
  - Direct connections (1 hop): callers = {...}, callees = {...}
  - One hop further: what do those callers/callees themselves connect to = {...}

  Question: given what {target} is for and what it's connected to above, does anything in its
  current implementation — logic, syntax, or structure — deviate from what its stated role and
  those specific connections require? Do not compare against generic best practice or an
  impression of "how the rest of the codebase looks." If the answer implies a change reaching
  beyond this one location, stop — each additional location gets its own fresh pass through this
  same template before any shared fix is proposed.
  ```

- Curated semantic files are drafts (`status: "proposed"`), not verified truth, until a human review flips them to `"confirmed"`. A `"stale"` status means the indexer detected the source changed since the entry was written — re-examine before trusting it.