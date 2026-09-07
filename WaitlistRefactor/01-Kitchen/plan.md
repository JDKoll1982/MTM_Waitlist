# Step 01 — Kitchen (Shared Infrastructure) — Plan

## 1. Request-type registry
Replace `MTM_Waitlist.Waitlist.Controls`'s 16 per-type Model+ViewModel files and the implicit
image-filename-as-type-key convention with one static/DI-registered registry:

- A `RequestTypeKey` enum (Coil, Pickup, PickupFg, PickupNcm, PickupOs, PickupWip, Scrap, DieHandling,
  Flatstock, ForkliftAssist, Other, TableHandling) — the single source of truth for "what type is this",
  replacing the filename-string convention everywhere (persisted data keeps its existing string column;
  the enum is a parse/format layer, not a schema change, per the "dev DB may reset freely" rule if a
  schema change does end up being warranted).
- A `RequestTypeDefinition` record: `Key`, `DisplayName`, `BadgeText`, `AccentColor` (theme-resource key,
  not a hardcoded hex), `ImagePath`, `DetailFieldDefinitions` (ordered list of label/value bindings).
- An `IRequestTypeRegistry` service (DI-registered) exposing `Get(RequestTypeKey)` and `All`, backed by a
  single static dictionary or a small DB-seeded table if the naming pass (Step 09) ends up wanting the
  metadata centrally editable later — start with an in-code dictionary; do not over-build a DB-backed
  registry unless a later step needs it.

## 2. Generic card control
Replace the 7 per-type card wrapper XAML files (`CoilWaitlistLineView`, etc.) with one
`RequestCardView` control that:
- Binds `AccentBrush`/`BadgeText`/labels from the `RequestTypeDefinition` looked up via the item's
  `RequestTypeKey`, instead of each wrapper hardcoding its own hex colors and field labels.
- Wraps the existing `WaitlistLineCardView` shell unchanged (it already has the right dependency
  properties; the wrappers were the redundant layer).
- Renders `DetailFieldDefinitions` via an `ItemsControl` (same auto-hide-empty-row pattern already used
  for Coil, per `/memories/repo/mtm-architecture-and-patterns.md` §8 card extraction notes) so it works
  for every type without a per-type XAML file.

## 3. Generic image-view control
Replace the 8 per-type `XxxRequestTypeImageView` controls with one `RequestTypeImageView` control that
takes a `RequestTypeKey` and resolves its image from the registry (same resolution pattern as
`ResolvedImagePathToSourceConverter` used elsewhere in the repo).

## 4. Type-key-based template selector
Replace `WaitlistLineTemplateSelector`'s lowercase-filename string switch with a selector that reads the
item's `RequestTypeKey` directly (added to `SampleOrder`/`WaitlistRequest` as needed) and looks up the
`DataTemplate` from a registry-keyed resource dictionary. No silent fallback-by-string-match; an unknown
key should be an explicit, loud "Default/Unknown" state, not a guess.

## 5. Generalized availability/weight services
- Rename/generalize `CoilAvailabilityService` -> a type-agnostic `RequestAvailabilityService` (interface
  stays `IRequestAvailabilityService`) that dispatches per-type logic via the registry instead of being
  hardcoded to Coil.
- Same treatment for `AverageCoilWeightService` -> `RequestQuantityService` (or similarly generalized
  name) — every request type gets an "available / on hand" answer, even if most types return a simple
  pass-through until real per-type logic is needed later.
- Existing Coil tests (`CoilAvailabilityServiceTests`, `AverageCoilWeightServiceTests`) get updated
  in-place to exercise the generalized service via the Coil registry entry — same assertions, new shape
  (like-for-like test replacement rule).

## 6. `WaitlistRequestService` rebuild
Rebuild submit/cancel/audit/status-lifecycle end-to-end against the new registry types, preserving every
behavior exercised by `WaitlistRequestServiceTests.cs` (the heaviest-coupled test file per
`Research.md` §5) — replace test-by-test, not in one big swap, to keep the suite green throughout.

## 7. `RoleAuthorization` helper
Build per the already-locked User Management spec (A1/E2 in
`Documents/Development/UserManagement/User-Management-Clarifying-Questions.md`): rank table
`Production < Material Handler < Setup < Production Lead < Setup Lead < Plant Manager < IT Department <
Developer`, `IsAtLeast(role, required)`, and a way to enumerate "roles >= X". Do NOT wire it into every
existing hardcoded `AllowedXRoles` array as part of this step (that consolidation is the separately
future-scoped Admin Settings work) — just build the helper itself here since Step 06 (handler workflow)
and the Admin Settings work both need it to exist.

## 8. Dead scaffolding cleanup
Delete the 2 empty `ModuleDependencyInjectionExtensions.cs` stubs (Controls + NewRequest projects) once
their (currently nonexistent) registrations are confirmed truly unused via `codegraphy dependents`.

## Sequencing within this step
1. Registry + `RequestTypeKey` enum (everything else depends on this).
2. Generic card + image-view controls (depend on the registry).
3. Selector rewire (depends on the registry + generic templates existing, but not yet wired to real
   data — that's Step 02).
4. Generalized services (independent of 2/3, can run in parallel — mark `[P]` in tasks.md).
5. `WaitlistRequestService` rebuild (independent of 2/3/4, can run in parallel — mark `[P]`).
6. `RoleAuthorization` helper (independent of everything above, can run in parallel — mark `[P]`).
7. Dead-stub cleanup last (low-risk, do after confirming nothing new depends on the stubs).
