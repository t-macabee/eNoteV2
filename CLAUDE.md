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