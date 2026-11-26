# 0003 · Blazor (C#) for the UI instead of React

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
The client brief requires .NET MAUI or Blazor Hybrid, with all business logic in C# so it can be shared with the drivers' mobile app. The POS must eventually work offline and talk to local hardware (printers, cash drawer, hardware ID for licensing). The lead developer's background is React/TypeScript.

## Decision
Build the POS screens in **Blazor** (`.razor` components, C#).

## Alternatives considered
- **React + TypeScript inside MAUI (HybridWebView):** keeps the familiar stack, but every data operation crosses a JavaScript-to-C# bridge. It also means two languages and two builds, and an upgrade to .NET 9.
- **Full TypeScript stack (Electron/Tauri + SQLite):** contradicts the client brief and loses code sharing with the MAUI mobile app.

## Consequences
- Screens call C# services and EF Core directly, with no API layer inside the POS.
- The same components run in the browser (Phase 1) and the desktop app (Phase 2).
- There is a learning curve for React developers, but the concepts map directly (components, parameters, events, state). Tailwind can still be used.
- The smaller ecosystem means fewer ready-made component libraries.
- The cloud Web Admin is a separate app and may still use React if that proves more productive.
