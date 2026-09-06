# WarehousePOS

A **single-PC, fully offline Windows desktop** Point of Sale and Warehouse Management System built with .NET 10, WPF, MVVM, and EF Core + SQLite.

> **Status:** ~90% feature complete — core modules are built and functional. Active work focuses on bug fixes, validation polish, and UI refinements.

---

## 🌟 Features

- **Authentication & Role-Based Access Control** — BCrypt-hashed passwords with `Admin` and `Worker` roles.
- **Product & Category Management** — Dynamic pricing (Retail vs. Wholesale), SKU management, stock reorder tracking.
- **Supplier Management** — Supplier records with automated payable balance tracking.
- **Purchase Order System** — Full lifecycle state machine (Draft → Confirmed → Received | Cancelled) with atomic inventory stocking.
- **Inventory & Movement Tracking** — Immutable stock movement logging (`StockIn`, `StockOut`, `Adjustment`, `PurchaseReceive`, `ReturnIn`).
- **POS & Checkout** — Barcode scanning / fast product search, retail & wholesale pricing, cash payment processing, instant change calculation.
- **Receipt & PO Printing** — Epson LQ-310 dot-matrix hardware integration via Windows Printing Subsystem (`System.Drawing.Printing`).
- **Reports & Analytics** — Daily sales revenue, top 10 fast-moving products, inventory valuation, and supplier balance reports.
- **Expenses Tracking** — Record and categorise warehouse operational expenses.
- **Backup & Audit Trail** — Automatic daily SQLite backup and security action logging (`AuditLog`).

---

## 🛠️ Technology Stack

| Component        | Technology                             |
|------------------|----------------------------------------|
| Language         | C#                                     |
| Runtime          | .NET 10                                |
| Desktop UI       | WPF (Windows Presentation Foundation) |
| UI Pattern       | MVVM                                   |
| Architecture     | Clean Architecture — Modular Monolith  |
| ORM              | Entity Framework Core                  |
| Database         | SQLite                                 |
| Hardware Printer | Epson LQ-310 (Windows Printing)        |
| Security         | BCrypt Password Hashing                |
| Logging          | Serilog (structured, daily rolling)    |

---

## 🏛️ Architecture

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

### Clean Architecture Dependency Rules
```
Domain          → no dependencies (pure C#)
Application     → depends only on Domain
Infrastructure  → implements Application/Domain abstractions
Desktop         → depends on Application and Infrastructure through DI
```

> **Desktop does NOT directly reference Domain.** All domain access goes through Application interfaces.

---

## 📂 Project Structure

```
WarehousePOS/
├── src/
│   ├── WarehousePOS.Domain/          Pure business logic & aggregate roots
│   ├── WarehousePOS.Application/     Use-cases, services, and DTOs
│   ├── WarehousePOS.Infrastructure/  EF Core, SQLite, Printing, Backup, Logging
│   └── WarehousePOS.Desktop/         WPF UI with MVVM & DI Root
│
├── tests/
│   ├── WarehousePOS.Domain.Tests/         Domain entity unit tests
│   ├── WarehousePOS.Application.Tests/    Use-case unit tests with mocks
│   ├── WarehousePOS.Infrastructure.Tests/ EF Core integration tests
│   └── WarehousePOS.IntegrationTests/     End-to-end flow tests
│
├── docs/
│   ├── architecture/overview.md
│   ├── database/design.md
│   └── decisions/                    Architecture Decision Records (ADRs)
│
├── installer/WarehousePOS_Setup.iss  Inno Setup installer script
├── AGENTS.md                         Development rules & constraints
├── CONTRIBUTING.md                   How to contribute
├── Directory.Build.props             TreatWarningsAsErrors = true
└── WarehousePOS.sln
```

---

## 💻 Developer Setup (Windows)

### Prerequisites

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community or higher) with the **".NET desktop development"** workload
- Git

### Run in Development

```bash
# Clone the repository
git clone https://github.com/dineTH2003-dev/WarehousePOS.git
cd WarehousePOS

# Build the solution (zero warnings enforced)
dotnet build

# Run tests
dotnet test

# Run the application directly from source
dotnet run --project src/WarehousePOS.Desktop/WarehousePOS.Desktop.csproj
```

### Build a Release EXE

```bash
dotnet publish src/WarehousePOS.Desktop/WarehousePOS.Desktop.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o dist/win-x64
```

Output: `dist/win-x64/WarehousePOS.Desktop.exe` — a fully self-contained single file (no .NET required on client machine).

To build the Windows installer, open **Inno Setup Compiler** and compile `installer/WarehousePOS_Setup.iss`.

### Local Data Directories (auto-created on first launch)

```
C:\ProgramData\WarehousePOS\
├── Data\WarehousePOS.db       ← SQLite database
├── Backups\                   ← daily automatic backups
└── Logs\application.log       ← Serilog structured logs
```

---

## 🐛 Reporting Issues

Use [GitHub Issues](https://github.com/dineTH2003-dev/WarehousePOS/issues/new/choose):

- **Bug** → use the **Bug Report** template
- **Feature / Enhancement** → use the **Feature Request** template

Both templates include a **🔧 Technical Support / Fix Hints** section — fill it in if you know the likely cause or a suggested fix approach.

---

## 📄 Licence

MIT — see [LICENSE](LICENSE).
