# DECISIONS

This file records project decisions that affect implementation, scope, or demo readiness.

## Decision Format

Each decision should include:

- date
- decision
- owner
- affected docs/code
- reason

## Accepted Decisions

### 2026-05-13 - Use Documentation-First Startup

- Owner: Member 1
- Affected docs/code: `DOCS/`, `Code/`
- Decision: start by freezing scope, ownership, API baseline, and source layout
- Reason: reduce overlap and contract drift before implementation begins

### 2026-05-13 - Use .NET 8, WinForms, TCP, SQLite, and System.Text.Json

- Owner: Member 1
- Affected docs/code: all runtime projects
- Decision: use `.NET 8`, C#, Windows Forms, TCP socket transport, SQLite, and `System.Text.Json`
- Reason: practical stack for an 8-week Windows LAN demo project

### 2026-05-13 - Use One JSON Object Per Line

- Owner: Member 2
- Affected docs/code: `DOCS/API.md`, `Code/Shared/`, networking modules
- Decision: each TCP packet is framed as one UTF-8 JSON object per line
- Reason: simple framing that is easy to debug and implement

### 2026-05-13 - Bind Each Client Account to One MachineId

- Owner: Member 5
- Affected docs/code: `DOCS/API.md`, auth/database modules, client login
- Decision: client login validates `username`, `password`, and `machineId`
- Reason: avoid demo confusion and prevent one account from representing multiple machines

### 2026-05-16 - Make Week 1 a Runnable Skeleton Week

- Owner: Member 1
- Affected docs/code: `DOCS/TASKS.md`, `DOCS/LEADER_FLOW.md`, `Code/`
- Decision: Week 1 must produce a buildable solution and a basic TCP packet round trip, not only docs
- Reason: the project cannot safely reach an 8-week demo if runnable integration starts too late

## Pending Decisions

These must be closed before implementation goes deep:

- exact SQLite package and database initialization path
- exact logging implementation
- exact reconnect behavior
- duplicate login behavior
- heartbeat interval
- final UI service interface names
