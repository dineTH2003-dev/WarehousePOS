## Summary

<!-- Link the issue this PR resolves -->
Closes #

---

## Changes

<!-- Describe what was changed and why -->

---

## Architecture Checklist

- [ ] Domain does not reference Application, Infrastructure, or Desktop
- [ ] Application references Domain only
- [ ] Infrastructure implements Domain/Application abstractions
- [ ] Desktop references Application and Infrastructure only (not Domain directly)
- [ ] No business logic placed in XAML code-behind
- [ ] No plaintext passwords stored or logged

## Code Quality Checklist

- [ ] Follows naming conventions from `.editorconfig`
- [ ] No commented-out code left in
- [ ] No hardcoded connection strings or file paths
- [ ] All monetary values use `decimal` (not `float` or `double`)
- [ ] All timestamps stored as UTC

## Testing Checklist

- [ ] Unit tests added/updated for Domain changes
- [ ] Unit tests added/updated for Application changes
- [ ] Integration tests added/updated if Infrastructure changed
- [ ] `dotnet build` passes with zero warnings
- [ ] `dotnet test` passes with zero failures

## Database Checklist (if applicable)

- [ ] EF Core migration added for schema changes
- [ ] Migration is reversible (down migration works)
- [ ] No raw SQL queries bypassing EF Core (unless justified)

## Printer / Hardware (if applicable)

- [ ] Tested on physical Epson LQ-310
- [ ] Invoice layout verified with test print
