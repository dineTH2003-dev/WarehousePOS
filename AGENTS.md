# WarehousePOS Development Rules

## Project

WarehousePOS is a **single-PC, fully offline Windows desktop** POS and warehouse management system.

## Technology

- C# (.NET 10)
- WPF + XAML
- MVVM pattern
- Entity Framework Core
- SQLite

## Architecture

Clean Architecture principles with a modular monolith.

Projects:

| Project                         | Purpose                                        |
|---------------------------------|------------------------------------------------|
| `WarehousePOS.Domain`           | Pure business logic — entities, value objects, domain exceptions, repository interfaces |
| `WarehousePOS.Application`      | Use-cases and application service interfaces   |
| `WarehousePOS.Infrastructure`   | EF Core + SQLite, repositories, printer, backup, logging |
| `WarehousePOS.Desktop`          | WPF UI with MVVM, DI composition root          |

**There is NO ASP.NET Core API.**
**There is NO web frontend.**
**There is NO cloud dependency.**
**The application must work completely offline.**

## Dependency Rules

```
Domain          → no dependencies (pure C#)
Application     → depends only on Domain
Infrastructure  → implements Application/Domain abstractions
Desktop         → depends on Application and Infrastructure through DI
```

**Desktop must NOT directly reference Domain.** All domain access goes through Application interfaces.

## Database

- Use **SQLite** through **Entity Framework Core**.
- The database file is stored at: `C:\ProgramData\WarehousePOS\Data\WarehousePOS.db`
- **Never** place the database inside `C:\Program Files\WarehousePOS\`.
- Use **EF Core migrations** for all schema changes.
- All monetary values must use `decimal` (18,2) — never `float` or `double`.
- All timestamps must be stored as **UTC**. Convert to local time only in the UI layer.
- Entities use an `IsActive` flag for soft deletes — **never physically delete records**.

## UI (WPF + MVVM)

- All UI logic belongs in **ViewModels**, not XAML code-behind.
- Code-behind files (`*.xaml.cs`) must contain only `InitializeComponent()` unless absolutely necessary.
- Use `INotifyPropertyChanged` via `ViewModelBase`.
- Use `RelayCommand` for button bindings.
- Use `IValueConverter` implementations in `Converters/` for data transformations.

## Business Logic

- Business rules belong in the **Domain** or **Application** layers.
- Infrastructure contains only data access and hardware integration — no business rules.
- Desktop contains only UI and navigation — no business rules.

## Security

- Passwords must **never be stored in plaintext**.
- Use **BCrypt** for password hashing.
- Use **role-based authorization**: `Admin` and `Worker` roles.
- Sensitive actions must be recorded in the **AuditLog**.

## Transactions

- Sales, payments, and inventory updates must be **atomic** (wrapped in a single EF Core transaction).
- Never update inventory without recording a corresponding `InventoryMovement`.

## Hardware

- Support the **Epson LQ-310** dot-matrix printer via the Windows printing subsystem.
- Printer integration lives in `WarehousePOS.Infrastructure/Printing/`.
- Test printer integration **early** — do not defer to the end of the project.

## Backup

- The backup strategy is critical because all data lives on one local machine.
- Automatic daily backups must be implemented.
- Backups are stored at: `C:\ProgramData\WarehousePOS\Backups\`
- Advise the client to periodically copy backups to a USB drive.

## Logging

- Use **Serilog** for structured logging.
- Log files stored at: `C:\ProgramData\WarehousePOS\Logs\application.log`
- Roll logs daily, retain 30 days.

## Testing

- Business-critical logic (sales calculations, discount application, stock movements) must have automated unit tests.
- Test projects:
  - `WarehousePOS.Domain.Tests` — entity and value object unit tests
  - `WarehousePOS.Application.Tests` — use-case tests with mocked infrastructure
  - `WarehousePOS.Infrastructure.Tests` — EF Core integration tests (in-memory SQLite)
  - `WarehousePOS.IntegrationTests` — end-to-end flow tests

## Code Quality

- Do not introduce unnecessary NuGet dependencies.
- Do not create duplicate business logic across layers.
- Do not bypass the architecture (no direct DB calls from ViewModels).
- The build must pass with **zero warnings** (`TreatWarningsAsErrors = true`).
- Follow the naming conventions in `.editorconfig`.

## Commit Convention

Use Conventional Commits:

```
feat(domain): add Product entity with stock management
fix(pos): correct discount calculation for wholesale sales
test(domain): add unit tests for Product.DeductStock
docs(arch): update database design document
```
