# Database Design

## Overview

WarehousePOS uses a single SQLite database file.

Database path: `C:\ProgramData\WarehousePOS\Data\WarehousePOS.db`

---

## Entity Map

### Core Entities

| Entity              | Description                                    |
|---------------------|------------------------------------------------|
| `Users`             | System users with role-based access            |
| `Categories`        | Product categories                             |
| `Products`          | Product catalog with retail/wholesale pricing  |
| `Suppliers`         | Supplier master data                           |
| `Customers`         | Customer master data                           |
| `Purchases`         | Purchase orders from suppliers                 |
| `PurchaseItems`     | Line items within a purchase                   |
| `Sales`             | Sale transactions (retail or wholesale)        |
| `SaleItems`         | Line items within a sale                       |
| `InventoryMovements`| Full audit trail of all stock changes          |
| `Payments`          | Payment records tied to sales or purchases     |
| `Discounts`         | Discount records tied to sales                 |
| `Invoices`          | Generated invoice records                      |
| `Employees`         | Employee records                               |
| `SalaryRecords`     | Monthly salary records per employee            |
| `Expenses`          | Business expense records                       |
| `AuditLogs`         | System audit trail for sensitive actions       |
| `AppSettings`       | Key-value application configuration table      |

---

## Relationship Summary

```
Categories ──< Products
Suppliers  ──< Purchases ──< PurchaseItems >── Products
Customers  ──< Sales     ──< SaleItems     >── Products
Sales      ──< Payments
Sales      ──< Discounts
Sales      ──  Invoices
Products   ──< InventoryMovements
Users      ──< AuditLogs
Employees  ──< SalaryRecords
```

---

## Key Design Decisions

1. **All monetary values use `decimal(18,2)`** — never `float` or `double`.
2. **All timestamps use UTC** (`DateTime.UtcNow`). Display conversion to local time is done in the UI layer.
3. **Soft deletes** — entities use `IsActive` flag; records are never physically deleted.
4. **Inventory movements** are always recorded — stock quantity on the Product is derived/updated, but all changes are traceable via `InventoryMovements`.
5. **Audit log** records sensitive actions: price changes, discount overrides, cancellations, user logins.

---

## Migrations

EF Core migrations are stored at:
`src/WarehousePOS.Infrastructure/Persistence/Migrations/`

To add a migration (run on Windows with .NET SDK):
```bash
dotnet ef migrations add <MigrationName> \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop
```

To apply migrations:
```bash
dotnet ef database update \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop
```
