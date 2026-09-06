# Architecture Overview

> **Project Status:** ~90% feature complete. Core modules are built and functional. Active development is focused on bug fixes, validation, and UI polish.

## Project Description

WarehousePOS is a **single-PC, fully offline Windows desktop application** for warehouse and point-of-sale management.

There is:
- **No ASP.NET Core API**
- **No web frontend**
- **No cloud dependency**
- **No internet requirement**

The application runs entirely on one Windows machine.

---

## Architecture Diagram

```
                    SINGLE WINDOWS PC
                           │
                           ▼
                    WarehousePOS.exe
                           │
                    ┌──────┴──────┐
                    │             │
                   WPF          MVVM
                    │             │
                    └──────┬──────┘
                           │
                           ▼
                    Application Layer
                           │
                           ▼
                       Domain Layer
                           ▲
                           │
                    Infrastructure Layer
                     │      │      │
                     ▼      ▼      ▼
                   SQLite Printer Backup
                             │
                             ▼
                        Epson LQ-310
```

---

## Project Structure

```
src/
├── WarehousePOS.Domain/          Pure business logic — no framework dependencies
├── WarehousePOS.Application/     Use-cases and service interfaces
├── WarehousePOS.Infrastructure/  EF Core, SQLite, Printer, Backup, Logging
└── WarehousePOS.Desktop/         WPF UI with MVVM

tests/
├── WarehousePOS.Domain.Tests/
├── WarehousePOS.Application.Tests/
├── WarehousePOS.Infrastructure.Tests/
└── WarehousePOS.IntegrationTests/
```

---

## Dependency Rules

```
Domain ← Application ← Infrastructure
                              ↑
Desktop ─────────────────────┘
```

| Project        | May depend on                    |
|----------------|----------------------------------|
| Domain         | Nothing (pure C#)                |
| Application    | Domain only                      |
| Infrastructure | Application + Domain             |
| Desktop        | Application + Infrastructure     |

**Desktop does NOT directly reference Domain.** It accesses domain concepts through Application interfaces only.

---

## Data Flow

```
User Action (WPF View)
      │
      ▼
ViewModel (MVVM)
      │
      ▼
Application Service (Use-Case)
      │
      ▼
Repository Interface (Domain abstraction)
      │
      ▼
Repository Implementation (Infrastructure)
      │
      ▼
AppDbContext (EF Core)
      │
      ▼
WarehousePOS.db (SQLite file)
```

---

## Database Location

The production database is stored **outside the application installation directory**:

```
C:\ProgramData\WarehousePOS\
├── Data\
│   └── WarehousePOS.db       ← production database
├── Backups\
│   ├── backup-2026-08-27.db
│   └── ...
└── Logs\
    └── application.log
```

This protects the database from being overwritten during application updates.

---

## Hardware Integration

The application integrates with the **Epson LQ-310** dot-matrix printer via the Windows printing subsystem.
Printing is handled in the Infrastructure layer (`InvoicePrinter.cs`, `PrinterService.cs`).

The printer integration covers both sale receipts and Purchase Order documents.
