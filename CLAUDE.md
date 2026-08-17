# CLAUDE.md

**SlotBook** — a resource booking API (meeting rooms, desks). The domain is deliberately
ordinary. The engineering interest is correctness under concurrent writes.

## Stack — decided, do not re-litigate

- **.NET 10 LTS / C# 14.** 14 is the ceiling — SDK 10.0.110 rejects `LangVersion=15.0`.
- **ASP.NET Core**, Minimal APIs with endpoint groups.
- **EF Core 10**, **SQL Server 2022** in Docker (host is Ubuntu 24.04).
- **xUnit with plain `Assert.*`.** NO FluentAssertions, NO Shouldly. Do not suggest either.
- **Testcontainers** for integration tests against real SQL Server.
- **Scalar** for the OpenAPI UI.
- Central Package Management (`Directory.Packages.props`), `global.json` SDK pin.

## Deliberately NOT used

**No DDD, no CQRS, no MediatR, no repository abstraction over `DbContext`.** The app does
not warrant them, and the indirection would obscure the platform's own mechanisms. This
is a decision, not an oversight — see `docs/decisions/0001`. Do not propose adding them.

Three projects, not four: `SlotBook.Api`, `SlotBook.Core`, `SlotBook.Infrastructure`.

## The point of the project

The core rule is **no two reservations may overlap on the same resource**. The flagship
deliverable is an integration test that fires concurrent booking requests for one slot and
proves exactly one wins — enforced by a unique constraint, not by application-level checks.
Everything else in the scope yields to that test.

## Scope — fixed

In: resources, reservations, users. Overlap prevention, business hours, max duration,
cancel, reschedule. JWT bearer auth with seeded users.

Out: frontend, notifications, payments, recurring bookings, calendar sync, full
ASP.NET Core Identity.

## How we work

- **TDD, split by layer.** Strict test-first for `Core`. For EF Core and ASP.NET Core APIs
  that are new ground here: spike in a throwaway branch, delete the spike, then test-first
  the real implementation. Never write a test for an API whose shape is still unknown.
- **Never `git commit`.** Propose the message; Leto commits. Same for push, PR, branch creation.
- Trunk-based on `main`. Short-lived branches, one PR per slice, conventional commits.
  The history should show red→green→refactor.

## ADRs

Written as the decisions are made — never up front:

1. `0001` Three projects; no MediatR, CQRS or DDD — considered and declined for this size
2. `0002` SQL Server + EF Core
3. `0003` Preventing double-booking — constraint, transaction, isolation level (flagship; put the Mermaid sequence diagram here)
4. `0004` Testcontainers over the EF Core in-memory provider
5. `0005` JWT bearer instead of full ASP.NET Core Identity

## Docs

- `README.md` is the primary artifact: what it is, `docker compose up`, Scalar screenshot,
  one inline Mermaid diagram, the concurrency test called out.
- `docs/decisions/` — Nygard ADRs (Title, Status, Context, Decision, Consequences).
  **Half a page each, hard limit.** Write them as decisions are made, never up front.
- No C4 hierarchy, no hand-written API reference. Scalar generates the latter from OpenAPI.

## Known traps

From auditing a previous project; all easy to repeat in C#:

- Hash high-entropy tokens (refresh tokens, API keys) with **SHA-256, never bcrypt** —
  bcrypt silently ignores input past 72 bytes.
- If JWTs are hand-minted, include a `type` claim, or a refresh token authenticates as an access token.
- In-process rate limiting is per-instance. Fine here; know why.
- Never let a catch-all exception handler swallow framework `HttpException` equivalents.
- The EF Core in-memory provider enforces no constraints and no real transactions. It
  cannot test the concurrency rule. Use Testcontainers.

## Response style

Tokens are expensive. Optimize for information per token.

- Lead with the answer. No preamble, no restating the question, no closing summary.
- Report what changed, not what you did to change it. The diff is visible.
- Prose over tables unless genuinely comparing across columns. Never use a table for a list.
- Cite `path:line` instead of pasting code. When showing code, show only changed lines.
- Omit caveats that do not change a decision.
- Read with `offset`/`limit` on large files; use subagents for broad exploration.
- Ask when genuinely blocked — do not enumerate options you will not pursue.
