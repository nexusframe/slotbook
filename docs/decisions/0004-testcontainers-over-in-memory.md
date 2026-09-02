# ADR-0004: Testcontainers rather than the EF Core in-memory provider

**Status:** Accepted · 2026-08-27

## Context

Integration tests need a database, and which one is settled by asking what these tests have to
be able to fail on. The rule this project exists to prove is enforced by a database constraint;
a test asserting that the second write is rejected means nothing against a store that cannot
reject it.

- **EF Core in-memory provider.** Ignores unique indexes and runs no real transactions, so
  conflict tests pass without anything being enforced — the worst kind of green. The existing
  409 on a duplicate resource name is the example: it comes from catching SQL Server errors
  2601 and 2627, which this provider never raises.
- **SQLite in memory.** Real constraints and real transactions, so a genuine improvement, but
  its concurrency model is database-level locking. It cannot exhibit the row locking and
  isolation-level behaviour ADR-0003 depends on.
- **A SQL Server outside the test run**, installed locally or supplied as a CI service. Correct
  semantics, but its schema and rows outlive the run and become shared mutable state.

## Decision

Integration tests run against SQL Server 2022 in a container started by Testcontainers: one
container per xUnit collection, created by the fixture, with migrations applied through
`MigrateAsync()` before the first test. The in-memory provider is used nowhere in this
repository.

## Consequences

- The suite requires a Docker daemon. Without one the tests fail to run rather than running in
  a degraded mode.
- Cheaper than the approach's reputation suggests: the CI workflow takes about a minute, of
  which `dotnet test` is roughly 27 seconds including container start and migrations.
- Applying migrations rather than `EnsureCreated` means every run also exercises the path from
  an empty database to the current schema, against exactly the schema the migrations produce.
- Tests in a collection share one database, so each creates the state it needs rather than
  assuming rows are there.
- The concurrent booking test in ADR-0003 becomes possible. Against an in-process fake it would
  be theatre.
