# WarehousePOS

A **single-PC, fully offline Windows desktop** Point of Sale and Warehouse Management System.

---

## Technology Stack

| Component         | Technology                        |
|-------------------|-----------------------------------|
| Language          | C#                                |
| Runtime           | .NET 10                           |
| Desktop UI        | WPF (Windows Presentation Foundation) |
| UI Pattern        | MVVM                              |
| Architecture      | Clean Architecture — Modular Monolith |
| ORM               | Entity Framework Core             |
| Database          | SQLite                            |
| Printer           | Epson LQ-310 (Windows printing)   |
| Barcode           | USB barcode scanner               |
| Authentication    | Local, role-based                 |
| Internet          | ❌ Not required                   |
| ASP.NET Core      | ❌ Not used                       |
| Cloud             | ❌ Not used                       |

---

## Architecture

```
                    SINGLE WINDOWS PC
                           │
                           ▼
                    WarehousePOS.exe
                           │
                    ┌──────┴──────┐
                    │             │
                   WPF          MVVM
                    └──────┬──────┘
                           │
                    Application Layer
                           │
                       Domain Layer
                           ▲
                    Infrastructure
                     │      │      │
                   SQLite Printer Backup
```

---

## Project Structure

```
WarehousePOS/
├── src/
│   ├── WarehousePOS.Domain/          Pure business logic
│   ├── WarehousePOS.Application/     Use-cases and service interfaces
│   ├── WarehousePOS.Infrastructure/  EF Core, SQLite, Printer, Backup, Logging
│   └── WarehousePOS.Desktop/         WPF UI with MVVM
│
├── tests/
│   ├── WarehousePOS.Domain.Tests/
│   ├── WarehousePOS.Application.Tests/
│   ├── WarehousePOS.Infrastructure.Tests/
│   └── WarehousePOS.IntegrationTests/
│
├── docs/
│   ├── architecture/
│   ├── database/
│   └── decisions/        Architecture Decision Records (ADRs)
│
├── database/
│   └── seed/
│
├── AGENTS.md             AI assistant instruction file
├── CONTRIBUTING.md
├── Directory.Build.props
├── Directory.Packages.props
└── WarehousePOS.sln
```

---

## Developer Setup (Windows)

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (Community or higher) with **".NET desktop development"** workload
- Git

### Clone and build

```bash
git clone https://github.com/your-org/WarehousePOS.git
cd WarehousePOS
dotnet restore
dotnet build
```

### Run tests

```bash
dotnet test
```

### Run the application

Open `WarehousePOS.sln` in Visual Studio 2022, set `WarehousePOS.Desktop` as the startup project, and press F5.

### First-time database setup

On first launch, the application will automatically:
1. Create the data directory at `C:\ProgramData\WarehousePOS\Data\`
2. Apply EF Core migrations to create `WarehousePOS.db`

---

## Database Location (Production)

```
C:\ProgramData\WarehousePOS\
├── Data\
│   └── WarehousePOS.db
├── Backups\
└── Logs\
    └── application.log
```

---

## Team

| Developer | Area                                                |
|-----------|-----------------------------------------------------|
| Dev 1     | Products, Categories, Suppliers, Purchasing, Inventory |
| Dev 2     | POS, Sales, Customers, Payments, Invoices, Printing |
| Dev 3     | Auth, Users, Employees, Reports, Backup, Audit      |

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Epics

| Epic    | Description                 |
|---------|-----------------------------|
| EPIC-01 | Project Foundation          |
| EPIC-02 | Authentication              |
| EPIC-03 | Product & Catalog           |
| EPIC-04 | Suppliers                   |
| EPIC-05 | Purchasing                  |
| EPIC-06 | Inventory                   |
| EPIC-07 | POS & Sales                 |
| EPIC-08 | Payments & Discounts        |
| EPIC-09 | Billing & Printing          |
| EPIC-10 | Employees & Payroll         |
| EPIC-11 | Expenses                    |
| EPIC-12 | Reports & Analytics         |
| EPIC-13 | Audit & Backup              |
| EPIC-14 | Testing & Deployment        |
