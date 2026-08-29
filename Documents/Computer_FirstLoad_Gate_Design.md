# Computer First-Load Gate + Computer Rename — Design

> Source of truth for the `Computer_FirstLoad_Gate_Implementation_Checklist.md`.
> Decisions locked via Q&A on 2026-08-24. Dev-only database (recreate freely; no production migration).
>
> **Project structure (post-refactor):** The remaining implementation phases run after the per-module project split. Each module now builds as its own WinUI class library — `MTM_Waitlist.Core` (← `Module_Core`), `MTM_Waitlist.Shared` (← `Module_Shared`), `MTM_Waitlist.Startup` (← `Module_Startup`), `MTM_Waitlist.Setup` (← `Module_Setup`), `MTM_Waitlist.Settings` (← `Module_Settings`), `MTM_Waitlist.Waitlist` (← `Module_Waitlist`), `MTM_Waitlist.Reporting` (← `Module_Reporting`). The `MTM_Waitlist` app project is the **composition root** and owns all `Views`, `Controls`, XAML, resources (`.resw`), and DI wiring. Namespaces (`MTM_Waitlist.Module_*`) are **unchanged**, so every code identifier referenced in this design resolves within its owning project; any `Module_*` path below resolves under that module's project root.

## 1. Terminology (critical distinction)

This design separates two concepts that both currently use the word "workstation", and **eliminates "Workstation" entirely**:

| Concept | Meaning | DB home | Final naming |
| --- | --- | --- | --- |
| **Computer** | A physical machine (hostname e.g. `johnspc`, display name e.g. "John's Computer"). Has nothing to do with presses/work centers. | `core_workstations_registry` → `core_computers_registry` | **"Computer"** |
| **Work Center** | A press/work station (e.g. `100-3`, `100-6`). Selected in Module_Setup. | `setup_workstations_catalog` → `setup_work_centers_catalog` | **"Work Center"** |

**Definition of Done (rename):** After the rename, the term "Workstation" / "workstation" / "Work Station" must **not exist anywhere** in the codebase — UI, code identifiers, DB schema, docs, or tests. A VS Code workspace search for `Workstation`, `workstation`, or `Work Station` must return **zero results**. Every occurrence is categorized as either "Computer" or "Work Center" and renamed accordingly.

Display format anywhere a computer is shown: `{DisplayName} - {ComputerName}` → e.g. `John's Computer - johnspc`.

## 2. Data Model — `core_workstations_registry` → `core_computers_registry`

Rename the table and `workstation_name` column; add two columns:

- `computer_name VARCHAR(128) NOT NULL` (renamed from `workstation_name`)
- `mac_address_normalized VARCHAR(64) NOT NULL` (unchanged)
- `display_name VARCHAR(128) NOT NULL` (NEW — required; user-facing label, e.g. "John's Computer")
- `description VARCHAR(255) NULL` (NEW — optional)
- `is_registered TINYINT(1) NOT NULL DEFAULT 1` (unchanged)
- Keep composite unique `(computer_name, mac_address_normalized)` (strict composite identity).
- Add unique on `display_name` (Display Name is unique + enforced).

## 3. Dependent foreign keys (computer-side only)

Rename FK columns that reference the registry (these are the **computer** concept):

- `auth_sessions_tokens.workstation_id` → `computer_id`
- `config_settings_values.workstation_id` → `computer_id`
- `config_settings_history.workstation_id` → `computer_id`
- `config_workstation_hot_workcenters.core_workstation_id` → `computer_id` (computer side of the link table)

Rename matching FK constraint names and index names.

## 4. Work Center naming (previously "out of scope")

The work-center concept is **renamed from "Workstation" to "Work Center"** (never "Computer"):

- `setup_workstations_catalog` → `setup_work_centers_catalog`
- `sp_setup_workstations_*` → `sp_setup_work_centers_*`
- `fn_setup_workstation_name_normalized` → `fn_setup_work_center_name_normalized`
- `vw_setup_workstations_active` → `vw_setup_work_centers_active`
- `SetupWorkstation*` classes/pages/view models/services → `SetupWorkCenter*`
- `config_workstation_hot_workcenters.setup_workstation_id` → `work_center_id`
- `waitlist_requests_queue.workstation_name` → `work_center_name` (requests happen at a work center)

Work-center naming uses "work_center" / "Work Center" — never "workstation" and never "computer".

## 5. First-load gate (after login)

- Runs **after** login/auth, before the shell loads.
- Check the current physical computer is saved: match `core_computers_registry` by `computer_name` (hostname) + `mac_address_normalized` (composite).
- If hostname + MAC are **not** found → show the **Add Computer** modal.
- **Cancel blocks the app** until a computer is saved (hard gate).

### Identity matching rules

- **Renamed machine** (same MAC, new hostname): update the existing row (UPSERT), do not insert. Show the modal on startup to confirm.
- **Reimaged/replaced machine** (same hostname, new MAC): strict composite → insert a **second** registry row (accept duplicate hostnames; only the latest is linked). *Decision: accept duplicate hostname rows.*
- **No stable MAC** (VM / no NIC): fall back to hostname-only; if still not authoritative, **skip the dialog** (reuse `IsComputerRegistrationAuthoritative`). Never infinite-loop.

## 6. DB unavailable handling

- If the DB cannot be reached at startup: **block the app** with an end-user-facing error message and a **Retry** button with a **5-second lockout** between retries.
- Distinguish "DB down" (block + retry) from "can't verify due to no MAC" (skip dialog). A DB outage must NOT be swallowed into the can't-verify skip path.

## 7. Add Computer modal

- Captures **Display Name (required)** and **Description (optional)**.
- Auto-detects and persists hostname (`computer_name`) and normalized MAC.
- Saves a single registry row (transactionally trivial — one table).
- Surfaces a duplicate Display Name error (enforced unique).

## 8. Display format across UI

- Everywhere a computer name is shown to users across **all modules** (Setup, Waitlist, Reporting, Settings, history/logs) use `{DisplayName} - {ComputerName}`.
- Does NOT change stored data (e.g. `waitlist_requests_queue.work_center_name` remains raw); the format is display-only.

## 9. Settings panel (Module_Settings)

- New **collapsible panel to manage computers**: full CRUD (list, add, edit, deactivate/delete).
- Fields: computer name, display name, description, MAC, active.
- Restricted to Admin / Developer roles.

## 10. Rename scope (ui-codebase-db)

Rename **every "workstation" occurrence** into either "Computer" or "Work Center":

- **Computer concept → "Computer":** registry table + `computer_name`, computer-side FK columns/constraints/indexes, computer-scope SPs, startup repo/coordinator/models/services/DI, UI labels + `.resw`.
- **Work Center concept → "Work Center":** catalog table, work-center SPs, function, view, `SetupWorkCenter*` module code, work-center FK columns, UI labels + `.resw`.
- **Final sweep:** VS Code search for `Workstation`, `workstation`, and `Work Station` returns **zero results**.

## 11. Testing

- Unit tests: gate fires when computer missing; gate passes when present; renamed machine upserts; reimage inserts second row; no-MAC skips; DB-down blocks with retry; display format applied; duplicate display name rejected.
- Update existing tests that reference the old registry/computer naming.

## 12. Build & validation

- `dotnet build` clean (watch for masked `WMC9999`; use deliberate C# error to surface real XAML issues).
- Validate DB scripts against dev instance (recreate freely).

## 13. Rename scan tool

- `tools/scan_workstation_rename.ps1` scans every text file in the repo (excluding generated/binary/SCM dirs: `bin`, `obj`, `.git`, `.vs`, `node_modules`, `TestResults`, `packages`, `.serena`, `pri_dump`; log files `*.log`, `Log.md`, `testout.txt`, `testerr.txt`; PRI dump `pri_dump.xml`; and the tool's own files `scan_workstation_rename.ps1` / `Rename_Scan_Results.md`) for `workstation` / `Work Station` and emits a per-file, per-line edit map with a **Computer / Work Center / Review** heuristic. Post-refactor, the scan covers every per-module project root plus the app and test projects.
- Also reports **files and folders whose names contain the pattern** (a `Files / folders to rename` table) with a suggested new name (PascalCase → `WorkCenter`/`Computer`, snake_case → `work_center`/`computer`).
- Output: console table + `Documents/Rename_Scan_Results.md`. Regenerate with:
  `pwsh -NoProfile -File tools/scan_workstation_rename.ps1`
- Run **before Phase 1** to capture the baseline match count, and **after Phase 7** as final-sweep evidence for the Definition of Done (zero results).
- Items marked **Review** are ambiguous (computer vs work center) and need a human decision during execution (e.g. `14_config_workstation_hot_workcenters` is the computer↔work-center link; design target is `config_computer_hot_work_centers`).
