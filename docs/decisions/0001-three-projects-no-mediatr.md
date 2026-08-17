# ADR-0001: Three projects; no MediatR, CQRS or DDD

**Status:** Accepted · 2026-08-16

## Context

SlotBook is a small booking API — resources, reservations, users — built to a three-week
budget. The conventional .NET reference architecture for a project like this is four
projects (Domain, Application, Infrastructure, Api), MediatR for an in-process request
pipeline, and a repository interface wrapping `DbContext`.

Applied here, that structure would add four indirections between an HTTP request and a SQL
statement while the application has, at most, a dozen operations. The cost is not just
typing: every read of the code pays it, and so does every reviewer.

Two specific points weighed against the conventional layout:

- `DbContext` is already a unit of work and `DbSet<T>` is already a repository. Wrapping
  them in hand-written interfaces re-implements what EF Core provides, and usually leaks
  anyway the moment a query needs `Include`, projection, or `AsNoTracking`.
- DDD earns its place where a domain has invariants worth defending across an aggregate
  boundary. Booking has exactly one rule of that kind — no overlapping reservations on a
  resource — and it is enforced by a database constraint, not by an aggregate root.

## Decision

Three projects:

| Project | Contains |
| --- | --- |
| `SlotBook.Core` | Entities and booking rules. No framework dependencies. |
| `SlotBook.Infrastructure` | `DbContext`, EF Core configuration, migrations. |
| `SlotBook.Api` | Minimal API endpoints, DI, hosting. |

Endpoints call into `Core` and `Infrastructure` directly. No mediator, no command/query
segregation, no repository layer over `DbContext`.

## Consequences

- Less code and fewer files. A request path can be read end to end without navigating
  through a dispatcher.
- Cross-cutting concerns (logging, validation, error handling) go in ASP.NET Core
  middleware and endpoint filters rather than pipeline behaviours. This is the platform's
  own mechanism and is one fewer abstraction to learn.
- Growth is not blocked. If the operation count grew substantially, splitting an
  `Application` project out of `Core` is a mechanical refactor, and introducing a mediator
  later is additive.
- Reviewers who expect Clean Architecture will find this layout thinner than usual. That is
  the reason this record exists: the alternative was considered and declined on grounds of
  size.
