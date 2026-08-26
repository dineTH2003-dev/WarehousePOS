# Seed Data

This directory contains seed SQL scripts for initial data population.

## Usage

Seed scripts are applied after the EF Core migration in the first deployment.

```bash
# Apply schema
dotnet ef database update

# Apply seed data
sqlite3 "C:\ProgramData\WarehousePOS\Data\WarehousePOS.db" < database/seed/initial_seed.sql
```

## What gets seeded

- Default admin user
- Basic product categories
- Application settings defaults
