# AGENTS.md

Guidance for agents working on this Project.

## C# Standards

Use standard C# — code must compile cleanly under `dotnet build` with the default
analyzers and no suppressed warnings.

- Keep C# 12; libraries target **.NET 8** and the desktop app targets **.NET 10** (required by Avalonia 12). Only use APIs available on the target framework.
- Keep `<Nullable>enable</Nullable>`; do not silence warnings with `!` unless truly unavoidable.
- Use file-scoped namespaces (`namespace Launcher.Core;`).
- Use `var` only when the type is obvious from the right-hand side.
- Prefer expression-bodied members for single-line members.
- Use `async`/`await` for I/O; never block with `.Result` or `.Wait()`.
- Prefer `record` / `record struct` for immutable data models.
- Expose `IReadOnlyList<T>` / `IEnumerable<T>` in public signatures when mutation isn't needed.
- Use `using` declarations for disposables (`using var stream = ...;`).
- Catch exceptions only at boundaries; never swallow them silently.
- Naming: `PascalCase` for types, methods, properties, events; `camelCase` for locals and
  parameters; `_camelCase` for private fields; `I` prefix for interfaces; `Async` suffix for
  async methods.
- Follow `.editorconfig` if present.

## Git Commits

Follow [Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <subject>`

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`.

Rules:
- Imperative mood, lowercase subject, no trailing period, ≤ 72 characters.
- Scope is optional and names the affected area.
- Separate the body with a blank line; explain *why*, not *what*.
- One logical change per commit.

Examples:
- `docs: update README.md`
- `feat(auth): add Microsoft login flow`
- `fix(launcher): handle missing Java path`
- `refactor(download): extract retry logic into helper`

## Comments

Comment what the code achieves, briefly, in one line. Do not comment on what the code is.

- One line per comment; no multi-line blocks.
- Skip comments for self-explanatory code.
- Never restate the method/type name in the comment.
- No commented-out code.
- Use XML doc comments only on public APIs whose behavior isn't obvious.
- Write comments in English.

Good:
```csharp
// Retry download up to 3 times before failing.
// Skip files that already match the expected SHA-1.
```

Bad:
```csharp
// This method downloads a file.
// Loop over the entries.
// The list of mods.
```

## Others

Don't directly modify code that the developer has already changed.
If a modification is indeed necessary, please inquire first.
