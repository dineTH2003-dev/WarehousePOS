# Contributing to WarehousePOS

Thank you for contributing. Please follow these guidelines to keep the codebase consistent.

---

## Team

| Developer | Responsibility                                      |
|-----------|-----------------------------------------------------|
| Dev 1     | Products, Categories, Suppliers, Purchasing, Inventory |
| Dev 2     | POS, Sales, Customers, Payments, Discounts, Invoices, Printing |
| Dev 3     | Authentication, Users, Employees, Payroll, Expenses, Reports, Backup, Audit |

All three collaborate on: Domain model, Database design, Architecture, Code reviews.

---

## Branch Strategy

```
main       ← production-ready releases only
dev        ← integration branch — all features merge here first
feature/*  ← individual feature branches
bugfix/*   ← bug fix branches
```

### Branch naming

```
feature/EPIC-03-product-entity
feature/EPIC-07-pos-screen
bugfix/incorrect-discount-calculation
```

---

## Commit Messages

Use **Conventional Commits**:

```
<type>(<scope>): <short description>

Types: feat | fix | test | docs | refactor | chore | style

Examples:
feat(domain): add Supplier entity
fix(pos): apply discount before tax calculation
test(domain): add unit tests for Category
docs(db): update entity relationship diagram
refactor(infra): extract repository base class
```

---

## Pull Request Process

1. Create a PR from your `feature/*` branch into `dev`.
2. Fill in the PR template completely.
3. All CI checks must pass before merge.
4. At least **one other developer** must review and approve the PR.
5. Squash merge into `dev`.

---

## Architecture Rules

These rules are enforced through the project reference graph and code reviews:

| Rule | Why |
|------|-----|
| Domain has no NuGet dependencies | Keeps it portable and testable |
| Application references Domain only | Prevents infrastructure leaking upward |
| No business logic in code-behind | Keeps UI layer thin |
| No plaintext passwords | Security |
| All money values as `decimal` | Precision |
| All timestamps as UTC | Consistency |

---

## Code Style

- Follow `.editorconfig` for formatting.
- Private fields: `_camelCase`
- Public properties: `PascalCase`
- Interfaces: `IPrefix`
- Async methods: `AsyncSuffix`
- Use `var` when the type is obvious from the right-hand side.

---

## Testing

Every PR touching Domain or Application **must** include unit tests.
Every PR touching Infrastructure with schema changes **must** include a migration test.

Run locally:
```bash
dotnet build WarehousePOS.sln
dotnet test WarehousePOS.sln
```

---

## Database Changes

When making schema changes:

```bash
# Add migration
dotnet ef migrations add <DescriptiveName> \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop

# Apply migration
dotnet ef database update \
  --project src/WarehousePOS.Infrastructure \
  --startup-project src/WarehousePOS.Desktop
```

Include migration files in your PR. **Never hand-edit migration files** unless you understand exactly what you are doing.
