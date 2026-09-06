# Contributing to WarehousePOS

Thank you for contributing. WarehousePOS is a mature, near-complete product (~90% feature complete).
The focus is now on **bug fixes, polish, validation improvements, and minor enhancements** — not new epics.
Please follow these guidelines to keep the codebase consistent and stable.

---

## Project Status

The core feature set is built and functional:

| Module | Status |
|---|---|
| Authentication & RBAC | ✅ Complete |
| Product & Category Management | ✅ Complete |
| Supplier Management | ✅ Complete |
| Purchase Order System | ✅ Complete |
| Inventory & Stock Movements | ✅ Complete |
| POS & Sales Checkout | ✅ Complete |
| Payments & Discounts | ✅ Complete |
| Receipt & PO Printing | ✅ Complete |
| Customers Directory | ✅ Complete |
| Employees & Payroll | ✅ Complete |
| Expenses Tracking | ✅ Complete |
| Reports & Analytics | ✅ Complete |
| Backup & Audit Trail | ✅ Complete |

Active work is now tracked through **GitHub Issues** for bugs and improvements.

---

## Branch Strategy

```
main       ← production-ready releases only
dev        ← integration branch — all fixes and features merge here first
feature/*  ← new feature branches
bugfix/*   ← bug fix branches
```

### Branch naming

```
bugfix/product-form-not-resetting
bugfix/supplier-phone-max-length
feature/logo-on-login-screen
feature/stock-field-in-product-form
```

---

## Commit Messages

Use **Conventional Commits**:

```
<type>(<scope>): <short description>

Types: feat | fix | test | docs | refactor | chore | style

Examples:
fix(pos): reset product form ViewModel before opening Add dialog
fix(suppliers): cap phone number at 10 digits and validate email format
fix(expenses): populate category dropdown on ViewModel load
feat(desktop): add stock quantity field to Add/Edit Product form
test(application): add unit tests for expense category service
docs(contributing): update project status and branch naming
```

---

## Pull Request Process

1. Create a PR from your `bugfix/*` or `feature/*` branch into `dev`.
2. Link the related GitHub Issue in the PR description (e.g. `Closes #12`).
3. Fill in the PR template completely.
4. All CI checks must pass before merge.
5. At least **one other developer** must review and approve the PR.
6. Squash merge into `dev`.

---

## Architecture Rules

These rules are enforced through the project reference graph and code reviews.
They apply to every change — no exceptions:

| Rule | Why |
|---|---|
| Domain has no NuGet dependencies | Keeps it portable and testable |
| Application references Domain only | Prevents infrastructure leaking upward |
| Desktop does NOT reference Domain directly | All domain access goes through Application interfaces |
| No business logic in XAML code-behind | Keeps the UI layer thin |
| No plaintext passwords stored or logged | Security |
| All monetary values use `decimal` | Precision — never `float` or `double` |
| All timestamps stored as UTC | Consistency — convert to local time in UI only |
| Never physically delete records | Use the `IsActive` soft-delete flag |
| Every inventory change must produce an `InventoryMovement` | Traceability |

---

## Code Style

- Follow `.editorconfig` for formatting.
- Private fields: `_camelCase`
- Public properties: `PascalCase`
- Interfaces: `IPrefix`
- Async methods: `MethodNameAsync`
- Use `var` when the type is obvious from the right-hand side.
- No commented-out code left in PRs.

---

## Testing

Every PR touching **Domain** or **Application** must include unit tests.
Every PR touching **Infrastructure** with schema changes must include a migration test.

Run locally before pushing:
```bash
dotnet build WarehousePOS.sln
dotnet test WarehousePOS.sln
```

The build must pass with **zero warnings** (`TreatWarningsAsErrors = true`).

---

## Database Changes

When making schema changes:

```bash
# Add migration
dotnet ef migrations add <DescriptiveName> \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop

# Apply migration locally
dotnet ef database update \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop
```

Include migration files in your PR. **Never hand-edit migration files.**

---

## Reporting Issues

Use GitHub Issues for all bugs and feature requests:

- **Bug**: use the **Bug Report** template — include steps to reproduce and the Affected Layer.
- **Feature / Enhancement**: use the **Feature Request** template — include acceptance criteria.
- Both templates have a **🔧 Technical Support / Fix Hints** section — fill it in if you know the cause or a suggested approach. This greatly speeds up the fix.
