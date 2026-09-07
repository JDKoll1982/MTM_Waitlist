# Waitlist Recode — Master Task List

> Aggregator for the whole recode effort. Each Step below is its own folder under `WaitlistRefactor/`
> with its own `spec.md` (what/why), `plan.md` (architecture/how), and `tasks.md` (phase-grouped,
> persona-tagged checklist — see format note). Companion research: `WaitlistRefactor/Research.md`.

## Task format (decided)
Combines two conventions rather than picking one over the other:
- **Phase grouping + parallel markers** (GitHub Spec Kit style): tasks are grouped into
  Setup -> Foundational -> per-story -> Polish, and independent tasks are tagged `[P]` (can run in
  parallel / in any order relative to each other).
- **Persona tags** (this repo's existing convention, `.github/skills/checklist-creation`): every task
  line still ends with `| **Persona: <Name>**` so the checklist-execution skill can drive it directly.

Example task line: `- [ ] [P] **Backend Engineer: build the request-type registry.** (Ref: 01-Kitchen/plan.md §2) | **Persona: Backend Engineer**`

## Delivery order (locked, 2-phase)
- **Phase A — "the kitchen":** build shared infrastructure once. Must be 100% done + build/tests green
  before any Phase B step starts.
- **Phase B — "the dishes":** migrate each request type onto the kitchen, one at a time, Coil first.

## Steps

- [ ] **Step 01 — Kitchen (shared infrastructure).** Type registry, generic card control, generic image
  view, type-key-based selector, generalized availability/weight services, `WaitlistRequestService`
  rebuild, `RoleAuthorization` helper (shared with the future User Management + Admin Settings work).
  Folder: `01-Kitchen/`. **BLOCKS all of Phase B.**
- [ ] **Step 02 — Coil migration (template type).** First request type moved onto the new kitchen;
  establishes the pattern the rest of Phase B repeats. Folder: `02-Coil/`.
- [ ] **Step 03 — Pickup variants migration** (Pickup, PickupFg, PickupNcm, PickupOs, PickupWip).
  Folder: `03-PickupVariants/`.
- [ ] **Step 04 — Remaining types migration** (Scrap, DieHandling, Flatstock, ForkliftAssist, Other,
  TableHandling). Folder: `04-RemainingTypes/`.
- [ ] **Step 05 — New Request wizard recode** (Work Center -> Job Type -> Subtype -> Details -> Preview
  -> Confirm pages), built against the Step 01 registry. Folder: `05-NewRequestWizard/`.
- [ ] **Step 06 — Handler workflow** (Accept/Complete/Release, assigned-handler-only rule, handler-facing
  card/detail data, note persistence) — folds in WeekendProject file 07. Folder: `06-HandlerWorkflow/`.
- [ ] **Step 07 — List behavior rules**: ignored-locations filtering (file 05), most-urgent-first sort
  (file 08), 90-day cancelled-request retention/hide (file 11), stock-on-hand snapshot at request
  creation (file 10, foundation only). Folder: `07-ListBehaviorRules/`.
- [ ] **Step 08 — Mock/data infrastructure**: mock/sample data reads from the database instead of
  hardcoded C# catalogs (file 12, service only), cross-computer mock-toggle mutex + full central
  mock-config rebuild (file 13). Folder: `08-MockDataInfrastructure/`.
- [ ] **Step 09 — Naming consistency pass**: standardize request-type vocabulary (Coil/Pickup/Scrap/etc.)
  across DB identifiers, code identifiers, and UI text. Run after Steps 01-04 so there's one final
  identifier set to rename, not a moving target. Folder: `09-NamingPass/`.
- [ ] **Step 10 — DB cleanup**: convert any hardcoded/inline MySQL query strings found anywhere in the
  recoded path into proper stored procedures under `Database/StoredProcedures/`. Folder:
  `10-DatabaseCleanup/`.
- [ ] **Step 11 — Full regression pass**: full test suite green, manual smoke test of every request
  type's card + New Request flow + handler workflow, `dotnet build` clean. Folder: `11-Regression/`.

## Explicitly out of scope (tracked, not forgotten)
- Plant Manager analytics dashboard screen/metrics module.
- Developer Settings catalog-editor UI (master list/table CRUD grid, request-type/subtype editor).
- Splash-screen startup messaging polish.
- **Developer Settings nav button** — needs a standalone spec file under `Documents/Development/`
  (not yet created — outstanding side-task).

## Adjacent, separately-scoped effort (not part of this recode, tracked for handoff)
- **Future Admin Settings module**: log retention (14 days), cancelled-request retention (90 days,
  overlaps Step 07), default fallback urgency minutes (30), and a consolidated role/permission list
  (overlaps the `RoleAuthorization` helper built in Step 01) — see `Research.md` if regenerated with this
  detail, and `/memories/session/plan.md` history for the full discovery record.
- **User Management panel** (`Documents/Development/UserManagement/*.md`) — not built; its
  `RoleAuthorization` helper should be designed generically in Step 01 to serve both efforts.

## Status
Phase A (Step 01) design not yet started. Next action: write `01-Kitchen/spec.md`,
`01-Kitchen/plan.md`, `01-Kitchen/tasks.md`.
