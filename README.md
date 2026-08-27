# WarehousePOS

A **single-PC, fully offline Windows desktop** Point of Sale and Warehouse Management System built with .NET 10, WPF, MVVM, and EF Core + SQLite.

---

## 🌟 Key Features

- **Authentication & Role-Based Access Control**: BCrypt-hashed security with `Admin` and `Worker` roles.
- **Product & Category Management**: Dynamic pricing (Retail vs. Wholesale), SKU management, and stock reorder tracking.
- **Supplier Management**: Supplier records with automated payable balance tracking.
- **Purchase Order System**: Full lifecycle state machine (Draft → Confirmed → Received | Cancelled) with atomic inventory stocking.
- **Inventory & Movement Tracking**: Immutable stock movement logging (`StockIn`, `StockOut`, `Adjustment`, `PurchaseReceive`, `ReturnIn`).
- **POS & Checkout System**: Barcode scanning / fast search, retail & wholesale pricing support, cash payment processing, and instant change calculation.
- **Receipt & PO Printing**: Epson LQ-310 dot-matrix hardware integration via Windows Printing Subsystem (`System.Drawing.Printing`).
- **Reports & Executive Analytics**: Daily sales revenue, top 10 fast-moving products, inventory valuation, and supplier balance reports.
- **Backup & Audit Trail**: Automatic SQLite database backup to `C:\ProgramData\WarehousePOS\Backups\` and security action logging (`AuditLog`).

---

## 🛠️ Technology Stack

| Component         | Technology                        |
|-------------------|-----------------------------------|
| Language          | C#                                |
| Runtime           | .NET 10                           |
| Desktop UI        | WPF (Windows Presentation Foundation) |
| UI Pattern        | MVVM                              |
| Architecture      | Clean Architecture — Modular Monolith |
| ORM               | Entity Framework Core             |
| Database          | SQLite                            |
| Hardware Printer  | Epson LQ-310 (Windows Printing)   |
| Security          | BCrypt Password Hashing           |
| Logging           | Serilog structured logging        |

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
│   ├── WarehousePOS.Domain.Tests/    Domain entity unit tests
│   ├── WarehousePOS.Application.Tests/ Use-case unit tests with mocks
│   ├── WarehousePOS.Infrastructure.Tests/ EF Core integration tests
│   └── WarehousePOS.IntegrationTests/ End-to-end flow tests
│
├── AGENTS.md                         Development rules & constraints
├── Directory.Build.props             TreatWarningsAsErrors = true
└── WarehousePOS.sln
```

---

## 💻 Developer Setup (Windows)

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (Community or higher) with **".NET desktop development"** workload
- Git

### Building & Running

```bash
# Clone repository
git clone https://github.com/dineTH2003-dev/WarehousePOS.git
cd WarehousePOS

# Build solution (zero warnings allowed)
dotnet build

# Run unit tests
dotnet test
```

### Local Database Directory
On first launch, the application automatically initializes the local SQLite database at:
```
C:\ProgramData\WarehousePOS\Data\WarehousePOS.db
```
Backups are automatically saved to:
```
C:\ProgramData\WarehousePOS\Backups\
```
Logs are saved to:
```
C:\ProgramData\WarehousePOS\Logs\application.log
```
