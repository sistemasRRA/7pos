# 0004 · Shared UI library with multiple hosts

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
Phase 1 is a web app. Phase 2 needs a Windows desktop app for things a browser cannot do: full offline operation, reading hardware IDs for license binding, and RAW printing. Windows MAUI apps must be built on Windows, but day-to-day development happens in WSL.

## Decision
- Screens live in a Razor class library, `SevenPos.UI`. Logic and data live in `SevenPos.Core`.
- Thin "host" projects run them: `SevenPos.Web` now (browser, works in WSL), and `SevenPos.App` (MAUI Blazor Hybrid) in Phase 2.

## Alternatives considered
- **Pages inside the web project:** they would have to be moved or copied when the desktop app arrives.
- **Start with MAUI:** it cannot be built or run in WSL, which slows development and does not match the Phase 1 goal.

## Consequences
- The same screens run in both shells unchanged.
- Hardware-specific features need an abstraction in Core (for example `IPrinter`), with an implementation per host.
- Hosts must register the UI assembly in their router (`AdditionalAssemblies`, via `SevenPos.UI.UiAssembly`).
