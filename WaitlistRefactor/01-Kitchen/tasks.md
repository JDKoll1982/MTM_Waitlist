# Step 01 — Kitchen (Shared Infrastructure) — Tasks

> Format: phases (Setup -> Foundational -> Story -> Polish) with `[P]` on tasks that can run in parallel
> relative to other `[P]` tasks in the same phase. Every task keeps a persona tag for the
> checklist-execution skill. Tick a box only after it's build-verified and (for testing boxes) full
> suite green — no coverage regression at any point.

## Phase: Setup

- [ ] **Read `plan.md` in full and confirm the `RequestTypeKey` enum's 12 members match every request
  type currently in `Module_Waitlist/Controls/` before writing any code.** (Ref: plan.md §1) |
  **Persona: Tech Lead**
- [ ] **Run `codegraphy dependents` on both `ModuleDependencyInjectionExtensions.cs` stubs to confirm
  zero real usages before scheduling their deletion.** (Ref: plan.md §8) | **Persona: Tech Lead**

## Phase: Foundational (blocks every Story task below)

- [ ] **Database Engineer: confirm no schema change is needed for `RequestTypeKey`** (it's a parse/format
  layer over the existing string column) — if a schema change does turn out to be warranted, get
  explicit sign-off before proceeding (dev DB may reset freely, but scope creep still needs confirmation).
  (Ref: plan.md §1) | **Persona: Database Engineer**
- [ ] **Backend Engineer: add the `RequestTypeKey` enum and `RequestTypeDefinition` record.**
  (Ref: plan.md §1) | **Persona: Backend Engineer**
- [ ] **Backend Engineer: add `IRequestTypeRegistry` + in-code dictionary implementation, DI-registered.**
  (Ref: plan.md §1) *Depends on: RequestTypeKey/RequestTypeDefinition* | **Persona: Backend Engineer**
- [ ] **Backend Engineer: add unit tests for `IRequestTypeRegistry`** (one test per request type
  resolves a definition; unknown key returns a typed not-found result, never null/throws). | **Persona: Backend Engineer**

## Phase: Story — Views/Controls (depends on Foundational)

- [ ] **Frontend Engineer: build the generic `RequestCardView` control** wrapping the existing
  `WaitlistLineCardView`, bound to `RequestTypeDefinition`. (Ref: plan.md §2) | **Persona: Frontend Engineer**
- [ ] **Frontend Engineer: build the generic `RequestTypeImageView` control.** (Ref: plan.md §3) |
  **Persona: Frontend Engineer**
- [ ] **Frontend Engineer: replace `WaitlistLineTemplateSelector`'s string-match routing with a
  type-key-based lookup against the registry-keyed template dictionary.** (Ref: plan.md §4)
  *Depends on: RequestCardView, RequestTypeImageView* | **Persona: Frontend Engineer**
- [ ] **QA Engineer: add a test proving the selector returns an explicit Unknown/Default template for an
  unrecognized key (no silent mis-route).** (Ref: plan.md §4) | **Persona: QA Engineer**

## Phase: Story — Services `[P]` (independent of the Views/Controls story above)

- [ ] [P] **Backend Engineer: generalize `CoilAvailabilityService` into `RequestAvailabilityService`,
  dispatching per-type via the registry.** (Ref: plan.md §5) | **Persona: Backend Engineer**
- [ ] [P] **Backend Engineer: generalize `AverageCoilWeightService` into a type-agnostic quantity
  service.** (Ref: plan.md §5) | **Persona: Backend Engineer**
- [ ] [P] **QA Engineer: update `CoilAvailabilityServiceTests`/`AverageCoilWeightServiceTests` in place**
  to exercise the generalized services via the Coil registry entry — same assertions, like-for-like
  replacement, no coverage drop. (Ref: plan.md §5) | **Persona: QA Engineer**

## Phase: Story — Request lifecycle `[P]` (independent of the two stories above)

- [ ] [P] **Backend Engineer: rebuild `WaitlistRequestService` end-to-end** (submit/cancel/audit/status
  lifecycle) against the new registry types. (Ref: plan.md §6) | **Persona: Backend Engineer**
- [ ] [P] **QA Engineer: replace `WaitlistRequestServiceTests.cs` coverage test-by-test** (not as one big
  swap) so the suite stays green throughout the rebuild. (Ref: plan.md §6) | **Persona: QA Engineer**

## Phase: Story — Role helper `[P]` (independent of every story above)

- [ ] [P] **Backend Engineer: build the shared `RoleAuthorization` helper** (rank table + `IsAtLeast` +
  roles-at-or-above query) per the locked User Management spec. (Ref: plan.md §7) | **Persona: Backend Engineer**
- [ ] [P] **QA Engineer: unit tests for `RoleAuthorization`** (rank ordering, `IsAtLeast` both directions,
  boundary/own-rank case). (Ref: plan.md §7) | **Persona: QA Engineer**

## Phase: Polish

- [ ] **Full Stack Engineer: delete the 2 dead `ModuleDependencyInjectionExtensions.cs` stubs.**
  (Ref: plan.md §8) *Depends on: Setup's `codegraphy dependents` confirmation* | **Persona: Full Stack Engineer**
- [ ] **Tech Lead: run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1` clean.** |
  **Persona: Tech Lead**
- [ ] **Tech Lead: run the full test suite, confirm zero coverage regression vs. the pre-Step-01
  baseline.** | **Persona: Tech Lead**
- [ ] **Tech Lead: confirm no end-user-visible behavior changed yet** (foundation-only step, per
  spec.md's Definition of Done) before marking Step 01 complete in `MasterTaskList.md`. | **Persona: Tech Lead**

**GATE: All boxes above ticked + build/tests green before Step 02 (Coil migration) starts.**
