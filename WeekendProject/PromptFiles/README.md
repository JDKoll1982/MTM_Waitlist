# WeekendProject/PromptFiles — archived workstream checklists

**Status: archive (as of 2026-09-11).** These files are the checklists that drove the Waitlist workstream phase by
phase, kept as written so the history of what was requested and completed stays readable. They are **not** a
description of the current code.

Three things in them are now out of date, and are marked here rather than rewritten in place:
1. **The demo/mock-data system is gone.** The `Feature.InforVisualMockData` / `Feature.RecvMockData` toggles, the
   Settings "Mock Data" switch, the mock routing stack, and the `Sample*Catalog` family (with `ISampleDataService`)
   were removed by the Module_Mock workstream. Any instruction below that routes a screen "via the mock toggle" or
   "when mock is ON" describes a mechanism that no longer exists. Internal stores are always read live; only Infor
   Visual reads fall back to the `mtm_mock` mirror, automatically and with no user action.
2. **Request-type and coil data now come from real sources.** `ImageLocationService` reads the request-type catalog
   through its stored procedures, and `CoilAvailabilityService` reads the coil on the saved Work Center Setup job.
3. **The request-type and subtype vocabulary is retired outright.** `specs/004-unified-card-item-picker` (FR-023)
   replaced it with a Category/Item model: one card shape for every item, one picture per item inheriting its
   category's, and one wizard step for the Item in place of the type and subtype steps. The two catalog tables, the
   catalog seed, the two read procedures, the display-label services, the legacy mapper and the per-type card
   controls are all deleted, and `RetiredSymbolAuditTests` fails the build if any of them returns — so item 2 above
   is itself historical: `ImageLocationService` no longer reads a request-type catalog at all.

Where a checklist references a stale mechanism, treat the reference as historical. What shipped is recorded in:

- `../Module_Mock/README.md` — the shipped architecture, the enforced rules, and the removal record.
- `../ChangeLog.md` — the user-facing entry for the failover and the removal.
- `specs/001-module-mock-visual-fallback/` — the authoritative requirements, design, and task list.

Keep these files as archives: do not "update" an old checklist to match today's code, because that erases the
record of what was asked for at the time. Write new work into the spec folder or a new dated checklist.
