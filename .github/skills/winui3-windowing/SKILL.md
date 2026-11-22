---
name: winui3-windowing
description: Apply correct window sizing, minimum size, fullscreen, per-window persistence, snap-layout width, and title-bar choices in a WinUI 3 app. Use when implementing or reviewing window setup, AppWindow code, or deciding between a standard and custom title bar.
---

# WinUI 3 windowing rules

Apply when writing or reviewing `Window`/`AppWindow` setup. The full rationale and code-level
details are in [windowing-rules.md](./references/windowing-rules.md).

## Non-negotiables

- Minimum usable width **500 epx** (supports Windows 11 snap layouts).
- Per-monitor DPI aware, always — never hard-code physical pixel sizes.
- Remember each window's size and position between sessions.
- Desktop/landscape only; no special portrait handling.

## Native vs. interop

WinUI 3 has **no** native minimum/maximum window size. Enforce it by handling
`WM_GETMINMAXINFO` (see the reference for the pattern). Fullscreen is native:
`AppWindow.SetPresenter(FullScreenPresenter)`.

## Title bar

Document both styles and state when to use each:
- **Standard system title bar** — main and settings windows.
- **Custom edge-to-edge** (`ExtendsContentIntoTitleBar = true` + `SetTitleBar`) — modal or
  focused workflow windows, keeping interactive content clear of the caption buttons.

Load [windowing-rules.md](./references/windowing-rules.md) for the details and gotchas.
