# ADR-001: Use SQLite Instead of PostgreSQL

**Date:** 2026-08-27
**Status:** Accepted

---

## Context

The WarehousePOS application runs on a **single Windows PC**.
There is one installation, one instance, and no network access requirement.

We needed to choose a database for the application.

## Decision

We use **SQLite** via Entity Framework Core.

## Reasons

1. **Single machine**: There is no need for a database server. SQLite is an embedded database — the database is a single file on disk.

2. **Zero administration**: No PostgreSQL service, user, password, or installation to maintain. The client does not need to manage a database server.

3. **Reliability**: SQLite is used by billions of devices. It is well-tested and handles a single-user POS workload without issue.

4. **EF Core support**: EF Core has first-class SQLite support with full migration support.

5. **Simplicity**: The client's IT support capability is limited. SQLite requires no IT knowledge to operate.

## Consequences

- **Backup responsibility**: The database is a single file (`WarehousePOS.db`). Backup strategy must be clearly defined and implemented (see EPIC-13).
- **Single writer**: SQLite has limited write concurrency, but this is not a concern for a single-PC application.
- **Not a limitation**: For this scope, SQLite is not a constraint — it is the correct tool.

## Rejected alternative

**PostgreSQL** was considered but rejected because:
- Requires a running server process
- Requires installation, user account, and connection configuration
- Adds unnecessary complexity for a single-PC deployment
- Introduces operational overhead the client cannot manage independently
