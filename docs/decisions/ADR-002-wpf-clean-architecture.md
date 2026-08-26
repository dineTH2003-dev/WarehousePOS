# ADR-002: WPF + Clean Architecture

**Date:** 2026-08-27
**Status:** Accepted

---

## Context

We needed a UI framework and architectural pattern for a single-PC Windows offline desktop POS application.

## Decision

We use **WPF (Windows Presentation Foundation)** as the UI framework with **Clean Architecture** principles organized as a **Modular Monolith**.

## Reasons

**WPF:**
- Mature, well-documented Windows UI framework
- Excellent XAML data binding for MVVM
- Full access to Windows printing APIs
- .NET 10 supported
- No internet or browser required

**Clean Architecture:**
- Separates business logic from UI and infrastructure concerns
- Makes business logic independently testable
- Protects domain rules from being scattered across the codebase
- Easy to understand layer boundaries for a 3-developer team

**MVVM Pattern:**
- Enables data binding in WPF
- Keeps business logic out of XAML code-behind
- ViewModels are independently testable

## Layer Boundaries

```
Domain    → knows nothing about WPF, EF Core, or SQLite
Application → knows about Domain only
Infrastructure → implements Domain/Application abstractions
Desktop     → wires everything together via DI
```

## Consequences

- All database code is confined to Infrastructure
- All business rules are in Domain/Application
- ViewModels in Desktop may call Application services only
- No business logic in XAML code-behind files

## Rejected alternatives

- **WinForms**: Less XAML binding capability, harder to maintain large UIs cleanly
- **ASP.NET Core + React**: Not appropriate for a single-PC offline desktop requirement
