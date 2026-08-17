# ADR-0002: SQL Server and EF Core

**Status:** Accepted · 2026-08-16

## Context

The application needs a relational store. The central requirement is not volume or query
complexity — it is **correctness under concurrent writes**: two clients must not be able to
book the same resource for overlapping times. That requirement, more than any other,
constrains the choice.

Alternatives considered:

- **PostgreSQL** — a strong option, common in new builds, and its exclusion constraints
  with a `tstzrange` type would express the overlap rule more directly than anything SQL
  Server offers. Declined because the deliverable is enforcement under concurrency, not the
  tersest DDL that states the rule — and against the more elegant constraint stand the
  first-party EF Core provider and the tooling built around it.
- **SQLite** — rejected outright. Its concurrency model is database-level locking, so it
  cannot demonstrate the isolation-level and row-locking behaviour that is the centrepiece
  of this project. Convenient for tests, useless for the thing being proven.
- **Dapper** instead of EF Core — declined because schema is a first-class concern here.
  The unique index that enforces the overlap rule belongs in the model and has to arrive
  through a migration; under Dapper it becomes hand-maintained SQL kept in step by
  discipline. Change tracking is a secondary benefit, not the reason.

## Decision

SQL Server 2022 (Developer edition) in Docker, with EF Core 10 and code-first migrations.
The database is the source of truth for the overlap rule; application-level checks are a
usability affordance, not the enforcement mechanism.

## Consequences

- Real isolation semantics are available, which ADR-0003 depends on.
- Integration tests need a running SQL Server rather than an in-process fake. This is a
  deliberate cost, revisited in ADR-0004.
- The container image is large (well over 1 GB) and slow to start, so CI runs longer than
  it would with a lighter engine. Acceptable at this size.
- Developer edition is licensed for development and test only. Any production deployment
  would need a different edition, or a move to PostgreSQL — which the EF Core provider
  abstraction makes a smaller change than it would otherwise be.
- Expressing the overlap rule will take a filtered unique index or a check-based approach
  rather than PostgreSQL's exclusion constraint. That work is ADR-0003.
