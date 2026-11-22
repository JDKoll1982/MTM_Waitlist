---
name: winui3-adaptive-table
description: 'Build the shared WinUI 3 data table control: horizontal scroll only when needed, full-width when it fits, and per-user column show/hide. Use when a page shows tabular data, or when a DataGrid clips columns on a narrow window.'
---

# Adaptive data table

One shared table for every page that shows rows. Requirements, from the scaling master:

- Scrolls sideways **only** when it doesn't fit; spans the full width when it does.
- Lets the user show/hide columns (persisted per view).
- Is the single sanctioned exception to the "no horizontal scrollbar" rule.

## Parts

- [ColumnState.cs](./references/ColumnState.cs) — pure C# column-visibility state (show/hide/toggle).
- [AdaptiveTable.md](./references/AdaptiveTable.md) — the DataGrid wiring: scroll policy, column
  flyout, and how to bind column visibility.

## Rules

- Wrap the toolkit `DataGrid`; do not build a grid from scratch.
- `HorizontalScrollBarVisibility = Auto` on the host so it scrolls only when needed.
- Provide a column-picker flyout (checkable menu items) bound to `ColumnState`.
- Never add a dependency to get column show/hide — the toolkit already ships the grid.
