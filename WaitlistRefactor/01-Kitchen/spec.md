# Step 01 — Kitchen (Shared Infrastructure) — Spec

## Why
Every Waitlist request type (Coil, Pickup + 4 variants, Scrap, DieHandling, Flatstock, ForkliftAssist,
Other, TableHandling) currently has its own hand-built card control, image-view control, Model/ViewModel
pair, and string-based type routing. Adding a 9th type means copying 2+ files and hoping nothing was
mistyped. This step builds the one-time shared foundation every request type will plug into during
Phase B, so future changes (a new type, a new field, a new color) are a data change, not a code copy.

## In scope
- A single request-type registry (metadata: display name, badge text, accent color, image reference,
  which detail fields to show) replacing the 16 per-type Model/ViewModel pass-through files.
- One generic card control replacing the 7 per-type card wrapper XAML files.
- One generic image-view control replacing the 8 per-type image-view XAML files.
- A type-key-based template selector replacing `WaitlistLineTemplateSelector`'s string/filename
  matching.
- Generalizing the Coil-only availability/weight services so every request type gets the same
  "is this available / how much is on hand" capability.
- Rebuilding `WaitlistRequestService` end-to-end (submit/cancel/audit/status lifecycle).
- A shared `RoleAuthorization` rank helper (also needed by the separately-tracked User Management panel
  and the future Admin Settings role-permission consolidation — design it generically enough for both).
- Deleting the 2 dead/unused `ModuleDependencyInjectionExtensions.cs` stubs.

## Out of scope (belongs to a later Step)
- Migrating any individual request type onto this new infrastructure (Step 02+).
- New Request wizard page recode (Step 05).
- Handler workflow, list-behavior rules, mock-data infra, naming pass, DB stored-procedure cleanup
  (Steps 06-10).

## Success criteria (Definition of Done for this step)
- `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1` succeeds with 0 errors.
- Full test suite green, with every deleted/replaced test having an equivalent new test (no coverage
  regression at any point — not just at the end).
- The new registry/card/image-view/selector exist and compile, but no existing request type has been
  switched over yet (that's Phase B) — OR, if a smoke-test switch-over is done for one type to prove the
  design, it is fully reverted or explicitly promoted into Step 02 with matching plan/tasks updates.
- No behavior change visible to end users yet (this step is foundation-only).

## Ambiguous-item convention
Anything discovered during this step that isn't clearly covered by this spec must be raised as a
question (see `MasterTaskList.md` process rules) rather than decided silently.
