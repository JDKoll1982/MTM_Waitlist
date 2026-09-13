# Tasks — Unified Waitlist Card and Category/Item Request Picker

**Feature**: `specs/004-unified-card-item-picker` | **Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)
**Size**: `oversized` — full phased list, grouped by user story, laid out as explicit waves
**Constitution**: v1.1.1 | **Generated**: 2026-09-13

Line format: `- [ ] **T###** [P] [US#] Description · exact/file/path`
`[P]` = independent of the other tasks **in its own wave** (different file, no unfinished dependency), so it may be
built in any order. `[US#]` maps the task to its user story for traceability.

**Gates** (from `contracts/verification-gates.md`; a box is ticked only against one of these, never speculatively):

- **Build** — `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → `0 Warning(s) 0 Error(s)`
- **Tests** — `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → `Failed: 0`
- **Live DB** — the paired `create.sql` / `rollback.sql` apply and reverse against a local `mtm_waitlist`
- **UI** — the scripted UI-automation recipe in `.github/instructions/winui3-ui-automation.instructions.md`

**Two traps that produce a red gate for a non-code reason** (restated because every XAML task below can hit them):

- `WMC9999` is a **masked real XAML error** on this machine, never an environment problem — the WindowsAppSDK
  `XamlCompiler` is missing its `ErrorMessages.resources` satellite. Surface the real cause with a deliberate C#
  error or by bisecting the changed XAML, then fix it.
- CommunityToolkit.Mvvm `[RelayCommand]` **strips a trailing `Async`**, so `private async Task FooAsync()` binds as
  `FooCommand`, never `FooAsyncCommand`. A wrong binding name is silent and appears only as the `WMC9999` above.
- `PRI175` / `PRI224` mean a running `MTM_Waitlist.exe` or stale `*.pri`, not a code error.

---

## Phase 1: Setup

Shared prerequisites for every phase. No user-visible behaviour, but every later gate depends on them.

**Wave 1 — independent (different files):**

- [x] **T001** [P] Record the baseline: build the solution and run the full suite at `ea2190a`, so every later gate
  result is attributable to a change rather than to the starting tree. Evidence: the two gate commands above
  produce `0 Warning(s) 0 Error(s)` and `Failed: 0` **before** T004 edits a file. · `MTM_Waitlist.sln`,
  `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [x] **T002** [P] Add the FR-027 document-is-not-an-input guard (§D19): a test that fails the build if any `.cs`,
  `.xaml`, `.csproj`, `.props`, `.targets` or `.json` file in the tree references
  `Request-Config-Template.csv` or `unified-item-picker-workflows.md`. **Write it to fail first** — point it at a
  fixture that does reference the CSV, record that failure, then assert the real tree is clean. · new
  `MTM_Waitlist.Tests/Module_Mock/DocumentInputAuditTests.cs`
- [x] **T003** [P] Establish the scripted-UI verification harness baseline that every UI gate in Phase 8 depends on:
  confirm the Debug exe reaches the **Sign in** window with **both** connection overrides set
  (`MTM_WAITLIST_DB_CONNECTION_STRING` *and* `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING`), and record the
  process-id window-discovery recipe. Evidence: a text dump showing the sign-in window, then the app closed with no
  orphan process. · `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`

---

## Phase 2: Foundational

Core infrastructure that **blocks every user story**: the new table, the re-keyed request, the three new procedures,
the seeds, the models, the providers and the availability snapshot. **No user-story work begins until this phase is
done.**

### Wave 1 — the new stored artifacts, independent (different files)

- [x] **T004** [P] Create the Item-configuration table: `waitlist_request_item_configs`, one row per Item, with the
  columns, types, nulls, defaults and unique keys fixed by `data-model.md` §3 (`public_id`, `item`, `category`,
  `control_flow`, `requires_answer`, `answer_value_type`, `prompt_text`, `min_length`, `max_length`, `options_json`,
  `detail_fields_json`, `allotted_minutes`, `created_utc`, `updated_utc`; `uq_waitlist_request_item_configs_item`
  gives the single-row-per-Item guarantee FR-014 needs). MySQL 5.7-compatible DDL, utf8mb4/utf8mb4_unicode_ci,
  lowercase snake_case identifiers. Ship the paired rollback in the same change. · new
  `Database/Tables/31_waitlist_request_item_configs/create.sql`,
  `Database/Tables/31_waitlist_request_item_configs/rollback.sql`
- [x] **T005** [P] Create `sp_waitlist_request_item_configs_get` — no parameters, one row per configured Item,
  returning every column except `id` (contract §2). Read-only. Paired rollback in the same change. · new
  `Database/StoredProcedures/sp_waitlist_request_item_configs_get/create.sql`, `…/rollback.sql`
- [x] **T006** [P] Create `sp_waitlist_request_item_allotted_minutes_update` — `p_item VARCHAR(64)`,
  `p_allotted_minutes INT`, plus the auditing columns the repo's other update procedures carry. It is the **only**
  writer of a configured allotment and must never be called with an observed average (contract §3). Paired rollback
  in the same change. · new `Database/StoredProcedures/sp_waitlist_request_item_allotted_minutes_update/create.sql`,
  `…/rollback.sql`
- [x] **T007** [P] Author the configuration seed: **one row for each of the twenty-three Item codes** (including the
  four out-of-scope Items — a hidden Item and a missing row are different states, FR-028), carrying each Item's flow,
  answer requirement, prompt, length limits, options and allotted minutes from
  `contracts/request-picker-flow.md` §4 and §6. Design-time sources only (FR-027): the spreadsheet's per-Item field
  definitions and the retiring catalogs' populated `category` / `item_id` columns. Paired rollback in the same
  change. · new `Database/Seeds/seed_waitlist_request_item_configs/create.sql`, `…/rollback.sql`
- [x] **T008** [P] Author the image seed: `config_images_locations` rows under the two new scopes `request_item`
  (keyed by Item code) and `request_category` (keyed by Category code), so the card's two-hop fallback of FR-009 has
  something to fall back to. Data rows only — the column is already free-form, so this is not DDL. Paired rollback
  in the same change. · new `Database/Seeds/seed_waitlist_request_item_images/create.sql`, `…/rollback.sql`
- [x] **T009** [P] Author the eight-configuration active-job seed (§D18): active jobs covering coil, flatstock, die,
  components, dunnage, everything at once, a job with **no** subordinate part, and a work centre with **no** active
  job. `subordinate_parts_json` is written in the exact shape `sp_setup_save_setup` stores, because the read path
  normalises `MMC→Coil`, `MMF→Flatstock`, `FGT→Die` from the part-number prefix — a hand-written `category` field
  alone would not exercise the real path. Paired rollback in the same change. · new
  `Database/Seeds/seed_setup_active_jobs_eight_configurations/create.sql`, `…/rollback.sql`

**⟶ Wait for Wave 1 to finish, then:**

### Wave 2 — the request table re-key (single task; nothing else may touch this file)

- [x] **T010** Re-key `waitlist_requests_queue`: **remove** `request_type` and `subtype`, **add**
  `category VARCHAR(16) NOT NULL` and `item VARCHAR(64) NOT NULL`. Keep `input_value`, `status`, `requested_utc`,
  `target_time_utc`, `accepted_utc`, `completed_utc`, `released_utc` and the requester/work-centre/assignment/
  cancellation columns untouched (FR-004, FR-032). No back-fill and no migration path — the database is reinstalled
  from seed. Paired rollback in the same change. · `Database/Tables/18_waitlist_requests_queue/create.sql`,
  `Database/Tables/18_waitlist_requests_queue/rollback.sql`

**⟶ Wait for Wave 2 to finish, then:**

### Wave 3 — the request procedures re-keyed and the observed average added, independent (different files)

- [x] **T011** [P] Re-key `sp_waitlist_request_get` to carry `category` + `item` and stop carrying the two legacy
  columns. Paired rollback in the same change. · `Database/StoredProcedures/sp_waitlist_request_get/create.sql`,
  `…/rollback.sql`
- [x] **T012** [P] Re-key `sp_waitlist_request_insert` to take `category` + `item` and stop taking the two legacy
  columns. Paired rollback in the same change. · `Database/StoredProcedures/sp_waitlist_request_insert/create.sql`,
  `…/rollback.sql`
- [x] **T013** [P] Re-key `sp_waitlist_request_list` to select and group on `category` + `item`. Paired rollback in
  the same change. · `Database/StoredProcedures/sp_waitlist_request_list/create.sql`, `…/rollback.sql`
- [x] **T014** [P] Create `sp_waitlist_request_item_observed_average_get` (§D6, FR-019): per Item, the request count
  and the average of `completed_utc - accepted_utc` over requests whose status is `Completed`; a request missing
  either endpoint is excluded; a released-then-completed request contributes **once** through its final
  accepted → completed pair; **no time window**. Read-only; must not be reusable to write anything. Paired rollback
  in the same change. · new `Database/StoredProcedures/sp_waitlist_request_item_observed_average_get/create.sql`,
  `…/rollback.sql`
- [x] **T015** [P] Rewrite `seed_waitlist_requests_default` in the new vocabulary: every seeded request carries a
  `category` and an `item` drawn from the twenty-three codes, and no request carries a type or subtype. Cover enough
  distinct Items — and enough completed requests — that the observed average has a non-empty population for at least
  two different Items (SC-008). Paired rollback in the same change. ·
  `Database/Seeds/seed_waitlist_requests_default/create.sql`,
  `Database/Seeds/seed_waitlist_requests_default/rollback.sql`

**⟶ Wait for Wave 3 to finish, then:**

### Wave 4 — the master lists in sync (single task; one owner for four files)

- [x] **T016** Bring the four master lists in sync with the files on disk in the same change (FR-025, constitution
  III): add the new table, the three new procedures and the four new seeds; add the new table's description to the
  mandatory maintenance file. The retired artifacts' removals land with their own retirement tasks in Phase 8 — do
  **not** remove them here, or their owners' rollbacks will have nothing to match. ·
  `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`,
  `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 4 to finish, then:**

### Wave 5 — models and contracts, independent (different files)

- [x] **T017** [P] Reshape `RequestItemDefinition`: **add** `CardLine2Template` (the `{token}` template for the
  card's second line, §D3) and `DisplayNameResourceKey` (replacing the literal display text, §D14/FR-022);
  **remove** `ValueType` and `NeedsUserEntry`, which move onto the configuration row. Keep the identity columns —
  `Id`, `Category`, `Order`, `NormalizedName`, `UmbrellaVerb`, `ProducedValue`. ·
  `MTM_Waitlist.Settings/Models/RequestItemDefinition.cs`
- [x] **T018** [P] Reshape `RequestItemCatalog` to the twenty-three rows of `data-model.md` §2: the Category split
  (11 / 8 / 3 / 1), the per-Category `Order`, the umbrella phrase (with `Wrong Coil Bring:` and
  `Wrong Flatstock Bring:` on the two wrong-material Items) and the `CardLine2Template` for each Item — including
  `pickup-die`'s conditional (the die's **location** when the captured destination is `Home Location`, otherwise its
  **number**), `pickup-riser-table`/`deliver-riser-table` → `Riser Table`, and
  `pickup-hopper`/`deliver-hopper` → `Hopper`. Display text becomes resource keys. Do **not** change the code set or
  the order. · `MTM_Waitlist.Settings/Models/RequestItemCatalog.cs`
- [x] **T019** [P] Add `RequestItemConfiguration` — one Item's stored behaviour, mapping the table's columns of
  `data-model.md` §3 (`ControlFlow`, `RequiresAnswer`, `AnswerValueType`, `PromptText`, `MinLength`, `MaxLength`,
  `OptionsJson`, `DetailFieldsJson`, `AllottedMinutes`) plus the parsed detail fields and options. · new
  `MTM_Waitlist.Settings/Models/RequestItemConfiguration.cs`
- [x] **T020** [P] Add `RequestItemFieldDefinition` — an element of `detail_fields_json` (`Label`, `ValueType`,
  `Source`, `Order`, `IsRequired`), the shape `data-model.md` §4 fixes. The JSON key stays `order`; no bare `order`
  column is ever introduced (database-schema rules). · new
  `MTM_Waitlist.Settings/Models/RequestItemFieldDefinition.cs`
- [x] **T021** [P] Add `RequestItemObservedTime` — the derived, never-stored pair a screen shows side by side: the
  Item, the completed-request count, the observed average, and **whether the configured value is a labelled default**
  (FR-017/FR-018/FR-019). · new `MTM_Waitlist.Settings/Models/RequestItemObservedTime.cs`
- [x] **T022** [P] Extend `ImageLocationScope` with `request_item` and `request_category`. Leave `request_subtype`
  and `request_type` in place — their removal is a retirement task with its own owner (Phase 8), so the scope enum
  and its consumers do not change twice in one pass. · `MTM_Waitlist.Settings/Models/ImageLocationScope.cs`
- [x] **T023** [P] Extend `RequestJobPartAvailability` so the snapshot can express FR-031 at all: the record
  currently carries `HasActiveJob`, `HasCoil`, `HasFlatstock`, `HasDie`, `HasComponent`, `HasDunnage` and derives
  `HasAnySubordinate` — **no** member can say whether the job has a real scrap decision, so `pickup-scrap`'s gate is
  unrepresentable as written. Add the flag (a real scrap type is set, is not `No Scrap`, and is not
  `Scrap Type Required`; see T024), update the `None`/`All` presets, and keep the record a pure value snapshot. ·
  `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs`

**⟶ Wait for Wave 5 to finish, then:**

### Wave 6 — the shared rules and the four new providers, independent (different files)

- [x] **T024** [P] Extract the canonical scrap-decision rule into `MTM_Waitlist.Core` — the **only** project both
  `MTM_Waitlist.Settings` and `MTM_Waitlist.Setup` reference — as a `static` predicate alongside the two constant
  values (`No Scrap`, `Scrap Type Required`). This is required, not cosmetic: `SetupDunnageTypeViewModel.HasScrapDecision`
  is an **instance** property, so the picker (in Settings) cannot call it, and it returns `true` for `No Scrap`,
  which is the opposite of FR-031. Then point `HasScrapDecision` and `SetupWorkflowService`'s constants at the
  extracted rule so Setup and New Request cannot diverge (§D12). · new
  `MTM_Waitlist.Core/Services/ScrapDecisionRules.cs`, `MTM_Waitlist.Setup/ViewModels/SetupDunnageTypeViewModel.cs`,
  `MTM_Waitlist.Setup/Services/SetupWorkflowService.cs`
- [x] **T025** [P] Add `RequestItemConfigurationService` — reads `sp_waitlist_request_item_configs_get` **once** as
  the Item step is entered (never per keystroke, never per row render), returns a lookup by Item code, drops rows
  whose `item` is not in the catalog, and reports a catalogued Item with no returned row as **unavailable** in plain
  language rather than half-configured (FR-014, FR-026). This task covers the row and the scalar behaviour only; the
  declared-fields payload is T085. · new `MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs`
- [x] **T026** [P] Add `RequestItemLine2Resolver` — resolves a `CardLine2Template` against the closed token set
  (job-derived `{part_number}`, `{part_description}`, `{die_number}`, `{die_location}`, `{dunnage_part}`,
  `{sequence_number}`, `{scrap_type}`; captured `{answer}`, `{destination}`, `{component}`, `{defect}`) plus the one
  conditional the die needs. An unresolvable token renders the Item's own display name and reports the
  configuration problem — never a blank, never a fabricated value (FR-026). · new
  `MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs`
- [x] **T027** [P] Add `RequestItemObservedTimeService` — reads `sp_waitlist_request_item_observed_average_get` and
  composes each Item's configured minutes with the observed average as **two distinct values**, marking the
  15-minute fallback as a **default** rather than as configured (FR-017, FR-018, SC-007). The observed value is
  display data and is never written back. Constant for the fallback: `UrgencySettingsService.DefaultMinutes` → 15
  (was 30). · new `MTM_Waitlist.Settings/Services/RequestItemObservedTimeService.cs`,
  `MTM_Waitlist.Core/Services/UrgencySettingsService.cs`

**⟶ Wait for Wave 6 to finish, then:**

### Wave 7 — the merged pickup Item's visibility rule (single task; same file as Wave 8)

- [x] **T028** Correct `RequestItemPickerRules.RequiredJobPart` (§D21): the merged pickup Item is visible when the
  requesting job has a coil **or** flatstock — it currently maps to `RequestJobPartKind.Coil` alone, so a
  flatstock-only job silently withholds a supported Item, which is the second half of FR-002 and is measured by
  SC-004. **Delete the dead `pickup-flatstock` arm**: it is not one of the twenty-three pinned codes, it is not in
  `RequestItemCatalog`, and it advertises an Item that does not exist. ·
  `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs`

**⟶ Wait for Wave 7 to finish, then:**

### Wave 8 — the scrap gate and the out-of-scope Items (single task; same file as Wave 7)

- [x] **T029** Gate `pickup-scrap` through the canonical predicate rather than letting it fall through to the
  always-visible default (FR-031, §D12): offered only when the snapshot says a real scrap decision exists — a value
  that is set, is not `No Scrap`, and is not `Scrap Type Required`. A job that chose `No Scrap` has nothing to
  collect; a stored placeholder means **no decision was made** and must never be presented as a scrap type. In the
  same file, exclude the four out-of-scope Items (`pickup-fg`, `pickup-ncm`, `pickup-wip`, `pickup-outside-service`)
  by rule rather than by deleting them from the catalog (FR-028). ·
  `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs`

**⟶ Wait for Wave 8 to finish, then:**

### Wave 9 — the filtered list, built after the check (single task)

- [x] **T030** Reshape `NewRequestPickerService` so the availability filter runs **inside** the build: add
  `GetVisibleItems(category, availability)` and `GetVisibleCategories(availability)`. An unsupported Item is
  therefore never constructed, never bound and never offered — the defect FR-002 was written to stop is
  "offered and then refused", so filtering a bound collection afterwards is not an acceptable implementation
  (§D11). `GetVisibleCategories` is what makes FR-002's "MUST never be empty" and SC-005 true of every Item step a
  person can reach: none of the five job-independent Items belongs to Assist, so an unfiltered Assist Category would
  open an **empty** Item step on a job with no parts (§D20). ·
  `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs`

**⟶ Wait for Wave 9 to finish, then:**

### Wave 10 — the availability snapshot at the composition root (single task)

- [x] **T031** Declare `IRequestJobPartAvailabilityProvider` in `MTM_Waitlist.Settings` and implement it in the app
  project — the **only** place permitted to see both `IActiveJobItemResolverService` (`MTM_Waitlist.Setup`) and
  `RequestJobPartAvailability` (`MTM_Waitlist.Settings`), which is why nothing has to move between projects. Register
  it together with the new services (`RequestItemConfigurationService`, `RequestItemLine2Resolver`,
  `RequestItemObservedTimeService`, `IWaitlistSortPreferenceService` in its own story). **Note the contract's
  wording carefully:** the snapshot is not computed at registration time — registration happens at startup, when no
  work centre is known. The composition root owns the *mapping*; the provider is *invoked* when the Item step is
  entered, which is what `contracts/request-picker-flow.md` §3 requires and what makes the check run before the list
  is built. · new `MTM_Waitlist.Settings/Services/IRequestJobPartAvailabilityProvider.cs`, new
  `Services/RequestJobAvailabilityProvider.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 10 to finish, then:**

### Wave 11 — the seed round-trip proof (single task)

- [x] **T032** Add the seed round-trip test (§D18): read the seeded `subordinate_parts_json` from
  `setup_active_jobs` back through `ActiveJobItemResolverService`'s **own** deserializer and assert it yields
  `SetupSubordinatePart` rows with the normalised Category (`MMC→Coil`, `MMF→Flatstock`, `FGT→Die`). This is what
  makes the eight-configuration matrix a proof about the real read path rather than about a hand-written seed shape.
  Environment-gated: `Assert.Inconclusive` when no live `mtm_waitlist` is reachable, never weakened. · new
  `MTM_Waitlist.Tests/Module_Setup/Services/ActiveJobSeedRoundTripTests.cs`

**Checkpoint — Foundational complete.** The store carries the new table, the re-keyed request, the three new
procedures and the four new seeds; the four master lists agree with the files on disk; the models, the shared scrap
rule and the four providers exist and compile. Every user story may now begin.

---

## Phase 3: User Story 1 — Raise a request by saying what it is (Priority: P1) [US1]

**Goal**: a person chooses a work centre, a Category and an Item — from the short list of things their job actually
has — answers at most the one thing the Item asks for, and the stored request carries that Category and that Item,
with no type or subtype anywhere.
**Independent Test**: open New Request, choose a work centre, choose a Category, choose an Item, finish the flow.
Confirm the stored request carries the chosen Category and Item, and that no screen anywhere asked for a type or a
subtype (SC-001, SC-002).

### Tests — write these first; they must fail before the implementation lands

- [x] **T033** [P] [US1] Extend `RequestItemPickerRulesTests` to the SC-004 matrix §D18/§D21 drove: for **each of the
  eight** job configurations, the exact set of visible Items — driven through the rules, not a snapshot of expected
  strings; the merged pickup Item offered for a job with **only** flatstock; the scrap three-way gate (a real scrap
  type → offered; `No Scrap` → not; `Scrap Type Required` → not; unset → not); and the four out-of-scope Items never
  offered while still present in the catalog. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemPickerRulesTests.cs`
- [x] **T034** [P] [US1] Extend `NewRequestPickerServiceTests`: `GetVisibleItems` is **never empty** for a job with no
  parts and for a work centre with no active job, and `GetVisibleCategories` does **not** offer Assist on such a job
  (the five job-independent Items are `pickup-riser-table`, `deliver-riser-table`, `pickup-hopper`,
  `deliver-hopper`, `other` — none of them Assist). Assert the filter runs before binding by asserting the unoffered
  Items are absent from the built collection, not merely hidden. · `MTM_Waitlist.Tests/Module_Settings/Services/NewRequestPickerServiceTests.cs`
- [x] **T035** [P] [US1] Add `NewRequestItemViewModelTests`: the Item step binds the filtered list in the catalog's
  `Order`; an Item with **no** configuration row stops the flow with a plain-language unavailable report rather than
  proceeding half-configured (FR-014); the flow cannot advance past the Item step without a choice. ·
  new `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestItemViewModelTests.cs`

### Implementation

**Wave 1 — the flow state and the Item tile, independent (different files):**

- [x] **T036** [P] [US1] Reshape `NewRequestFlowState`: `Category` (`RequestCategory`) and `Item`
  (`RequestItemDefinition`) replace `RequestType` and `Subtype`; keep the work-centre, job-snapshot, captured-answer
  and timestamp members the later steps read. · `MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestFlowState.cs`
- [x] **T037** [P] [US1] Add `NewRequestItemOption` — one Item tile built **from configuration**: the Item, its
  resolved display name, its Category, whether it captures an answer, and whether its configuration row was found.
  · new `MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestItemOption.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the step order and the flow service, independent (different files):**

- [x] **T038** [P] [US1] Reshape `NewRequestFlowRules` to
  Work Centre → Category → Item → Details → Preview → Summary → Result, with the availability snapshot resolved
  **between** Category and Item. Remove the type and subtype steps. ·
  `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs`
- [x] **T039** [P] [US1] Reshape `NewRequestFlowService`: remove the request-type catalog read, hand the Category step
  the **filtered** Category list and the Item step the filtered Item list, and resolve the availability snapshot
  through `IRequestJobPartAvailabilityProvider` (T031). ·
  `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowService.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the step view models, independent (different files):**

- [x] **T040** [P] [US1] Re-lay `NewRequestJobTypeViewModel` as the **Category** step: bind the filtered Category
  list from `GetVisibleCategories`, and keep only the Category's own selection state. ·
  `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestJobTypeViewModel.cs`
- [x] **T041** [P] [US1] Add `NewRequestItemViewModel` — the **new** Item step: the filtered Item list in `Order`,
  selection, continue/back, and the plain-language unavailable report when a chosen Item has no configuration row.
  · new `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestItemViewModel.cs`
- [x] **T042** [P] [US1] Update `NewRequestWorkCenterViewModel` to enter the re-laid flow (available Categories for
  the chosen work centre's job) and drop every request-type or subtype reference. ·
  `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestWorkCenterViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the step pages, independent (different files):**

- [x] **T043** [P] [US1] Re-lay `NewRequestJobTypePage` as the Category step: the four Categories, the same tile
  affordance, localized labels through `x:Uid`, every binding on an existing command. Remember the `[RelayCommand]`
  `Async`-stripping rule in T-prefixed bindings. ·
  `Module_Waitlist/Views/NewRequestJobTypePage.xaml`, `Module_Waitlist/Views/NewRequestJobTypePage.xaml.cs`
- [x] **T044** [P] [US1] Add `NewRequestItemPage` — the Item step: the filtered Item tiles, the empty state never
  reachable (T030 guarantees at least one Item), and the unavailable report surfaced in the page's own text. ·
  new `Module_Waitlist/Views/NewRequestItemPage.xaml`, `Module_Waitlist/Views/NewRequestItemPage.xaml.cs`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the later steps re-laid, independent (different files):**

- [x] **T045** [P] [US1] Re-lay `NewRequestDetailsViewModel` to render **from the configuration row and nothing
  else** (§D16): nothing for a pure flag or a job-derived value; one option pick where the configuration declares an
  enumerated answer; one text input honouring the configured prompt and the configured minimum/maximum length with a
  plain-language out-of-range message; a message where the Item carries its own text. **No branch may key on the
  Item's identity** — every branch keys on the row (FR-013). The `options_json` values are the pinned die options
  `Die Shop`, `Home Location`, `Other`. · `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDetailsViewModel.cs`
- [x] **T046** [P] [US1] Update `NewRequestPreviewViewModel` to show the Category and the Item in the new vocabulary
  instead of a type and a subtype. · `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestPreviewViewModel.cs`
- [x] **T047** [P] [US1] Update `NewRequestSummaryViewModel` to summarise Category + Item + the captured answer, and
  stop reading anything named for a type or subtype. · `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — the write path carries the new pair (single task):**

- [x] **T048** [US1] Update `WaitlistRequestService` so the draft → procedure mapping passes `category` + `item` to
  `sp_waitlist_request_insert` and reads them back from `sp_waitlist_request_get` / `_list` (FR-004). *Added by this
  task list: `plan.md`'s Project Structure enumerates this project's Models, ViewModels and the three procedures but
  not this service, and FR-004 can only hold if the insert path carries the pair.* ·
  `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the type and subtype steps are gone (FR-003), independent (different files):**

- [x] **T049** [P] [US1] Delete the subtype step: `NewRequestSubtypeViewModel.cs`,
  `Module_Waitlist/Views/NewRequestSubtypePage.xaml` and `.xaml.cs`, and their registrations' remaining references.
  · delete `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSubtypeViewModel.cs`, delete
  `Module_Waitlist/Views/NewRequestSubtypePage.xaml`, delete `Module_Waitlist/Views/NewRequestSubtypePage.xaml.cs`
- [x] **T050** [P] [US1] Delete the wizard's type and subtype model layer: `NewRequestTypeDefinition.cs`,
  `NewRequestSubtypeDefinition.cs`, `NewRequestCanonicalCategory.cs`, `NewRequestCanonicalItem.cs`. ·
  delete those four files under `MTM_Waitlist.Waitlist.NewRequest/Models/`
- [x] **T051** [P] [US1] Delete the tree-grouping indirection that exists only to read the retiring catalog:
  `NewRequestCanonicalPicker.cs`, `RequestTypeCatalogService.cs`, the `Assets/Config/waitlist-request-types.json`
  asset and its `MTM_Waitlist.csproj` `Content Update` entry — **inspect first**: retire the asset only once no
  remaining reader is found (`ImageLocationService` already reads the database catalog instead, but the
  `request_type` image scope is still live until Phase 8). · delete
  `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestCanonicalPicker.cs`, delete
  `MTM_Waitlist.Waitlist.NewRequest/Services/RequestTypeCatalogService.cs`,
  `Assets/Config/waitlist-request-types.json`, `MTM_Waitlist.csproj`
- [x] **T052** [P] [US1] Delete the tests whose subject no longer exists: `NewRequestTypeDefinitionTests.cs`,
  `NewRequestCanonicalPickerTests.cs`, `RequestTypeCatalogServiceTests.cs`,
  `RequestTypeCatalogServiceIntegrationTests.cs`. Do **not** weaken a surrounding gate to compensate — 19
  environment-gated skips are the recorded baseline. · delete those four files under `MTM_Waitlist.Tests/Module_Waitlist/`

**⟶ Wait for Wave 7 to finish, then:**

**Wave 8 — resources and registration, independent (different files):**

- [x] **T053** [P] [US1] Add the new user-visible strings through the existing resource mechanism (FR-022): the four
  Category names, the nineteen in-scope Item display names, the Category and Item step titles and prompts, and the
  "Item not available" and "no Item is offered" messages. Remove the strings keyed for the type and subtype steps —
  their screens are gone as of Wave 7. · `Strings/en-us/Resources.resw`
- [x] **T054** [P] [US1] Register the new page and view model and drop the retired registrations:
  `pageService.Configure<NewRequestItemViewModel, NewRequestItemPage>()`,
  `services.AddTransient<NewRequestItemViewModel>()`, `services.AddTransient<NewRequestItemPage>()`, and remove the
  `NewRequestSubtypeViewModel` / `NewRequestSubtypePage` pairs from both blocks. ·
  `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 8 to finish, then:**

**Wave 9 — the Category step's tests follow its subject (single task):**

- [x] **T055** [US1] Re-point `NewRequestJobTypeViewModelTests` at the Category step: the filtered Category list,
  selection, and the absence of any type or subtype concept. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestJobTypeViewModelTests.cs`

**Checkpoint — US1 is independently functional and testable.** A request can be raised by choosing a Category and an
Item in one pass, never asked for a type or a subtype; the stored request carries `category` + `item`; no reachable
Item step is empty. SC-001 and SC-005 are provable from this phase alone.

---

## Phase 4: User Story 2 — Every request reads the same way (Priority: P1) [US2]

**Goal**: every request on the list is one card with two lines — what kind of thing it is, then which thing it is —
the same shape for every Item, with the right picture and the four metadata rows intact.
**Independent Test**: get requests for several different Items onto the list and confirm each draws the same card
shape, umbrella word first, identifier second, right picture (SC-003).

### Tests — write these first; they must fail before the implementation lands

- [x] **T056** [P] [US2] Extend `WaitlistRequestTitlesTests`: Line 1 is the Item's umbrella phrase — the Category's
  word, or the Item's own phrase — and Line 2 is the Item's identifier; the die's Line 2 is its **location** when the
  captured destination is `Home Location` and its **number** otherwise; the two wrong-material Items read
  `Wrong Coil Bring:` / `Wrong Flatstock Bring:` with the **correct** material's identifier on Line 2, never the
  wrong one being collected (FR-005, FR-029); an unresolvable token renders the Item's display name and reports the
  problem (FR-026); and **no legacy fallback path exists**. ·
  `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs`
- [x] **T057** [P] [US2] Extend `SampleOrderTests`: the row exposes Line 1, Line 2, the picture, the four metadata
  values and the action affordances, and exposes **no** member that selects a layout by Item (FR-006). ·
  `MTM_Waitlist.Tests/Module_Waitlist/Models/SampleOrderTests.cs`
- [x] **T058** [P] [US2] Extend `RequestImagePathPolicyTests`: resolution order is Item → Category family → the
  existing placeholder, and a `"nothing configured"` answer **never** replaces an image the request already resolves
  (FR-009, FR-021). · `MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestImagePathPolicyTests.cs`
- [x] **T059** [P] [US2] Extend `ImageLocationServiceCascadeTests`: the two new scopes key an override by Item code
  and by Category code, and the two-hop inheritance resolves through `SupportsGrouping` / `SupportsInheritance` as
  `ImageOverrideDialogViewModel` already implements it (§D10). ·
  `MTM_Waitlist.Tests/Module_Settings/ImageLocationServiceCascadeTests.cs`
- [x] **T060** [US2] **Rewrite** `WaitlistLineCardMarkupTests` to the new anatomy — this is a recorded supersession,
  not a deletion (§D17): the constraint it guards is lifted in writing, and the test is rewritten to the new shape so
  an **unintended** card change still fails. Assert the single layout (no Item-selected span), Line 1 and Line 2 on
  the card, the four metadata rows, the picture and its fallback, and that the per-type detail grids are **gone**
  (FR-006, FR-007, FR-008). The current file's assertions on the six per-type line views and the
  `WaitlistLineTemplateSelector` are what the assertion set replaces. ·
  `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`

### Implementation

**Wave 1 — the two lines (single task):**

- [x] **T061** [US2] Reshape `WaitlistRequestTitles` to `ResolveLine1` / `ResolveLine2` with **no legacy fallback**:
  Line 1 from the Item's umbrella phrase, Line 2 through `RequestItemLine2Resolver` against the job snapshot and the
  captured answer, with the die's `Home Location` conditional and the two wrong-material Items' correct-material
  identifiers. · `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the row model (single task):**

- [x] **T062** [US2] Reshape `SampleOrder` into the one card row model of `data-model.md` §7: Line 1, Line 2, picture,
  requested-by, press, remaining time (`target_time_utc`, else `requested_utc + the Item's allotted minutes`), waiting
  (`requested_utc` → now), the status pill, the new-message marker and the action area. Overdue stays a property of
  the row so every sort can still mark it (FR-012). · `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — Item-picture resolution (single task):**

- [x] **T063** [US2] Add `ResolveRequestItemImagePathAsync` to `ImageLocationService`, implementing
  Item → Category family → existing placeholder and refusing to let a `"nothing configured"` answer overwrite a
  resolved image (FR-009, FR-021). Reuse the existing storage service, read/write services and storage root — this is
  a scope addition, not a second image subsystem (§D10). · `MTM_Waitlist.Settings/Services/ImageLocationService.cs`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the one card (single task):**

- [x] **T064** [US2] Reduce `WaitlistLineCardView` to **one** layout for every Item (FR-006): the Item's image as a
  fixed centred square, Line 1 on its own top row, Line 2 beneath it, the four metadata rows, the compact status
  pill, the new-message marker and the action area — keeping the existing `Auto` / `*` grid with `MinHeight` and
  star widths and adding **no** hardcoded pixel boundary to a layout-critical container (constitution V). Remove the
  per-type detail grids: an Item's own fields go to the request page and never appear on the card (FR-007). The
  'Other' full-width span is **not** on the card — it is a property of the page's field grid (§D9). ·
  `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml.cs`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the list builds one card per row (single task):**

- [x] **T065** [US2] Reshape `WaitlistViewViewModel` to populate one card row per request and bind the single
  template — no layout selection, no per-type branch, and the per-screen unavailable state
  (`Store` / `LastAttemptUtc` / `RetryCount` / `NextRetryUtc` + manual retry) preserved for a failed read, never an
  empty list and never a fabricated row (FR-026). ·
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — the per-type card controls retire, independent (different files):**

- [x] **T066** [P] [US2] Delete the six per-type line views and their code-behind: `Coil/CoilWaitlistLineView.xaml(.cs)`,
  `PickupFg/PickupFgWaitlistLineView.xaml(.cs)`, `PickupNcm/PickupNcmWaitlistLineView.xaml(.cs)`,
  `PickupOs/PickupOsWaitlistLineView.xaml(.cs)`, `PickupWip/PickupWipWaitlistLineView.xaml(.cs)`,
  `Scrap/ScrapWaitlistLineView.xaml(.cs)`. · delete those twelve files under `Module_Waitlist/Controls/`
- [x] **T067** [P] [US2] Delete the eight per-type request-type image views and their code-behind:
  `Coil/CoilRequestTypeImageView.xaml(.cs)`, `DieHandling/…`, `Flatstock/…`, `ForkliftAssist/…`, `Other/…`,
  `Pickup/…`, `Scrap/…`, `TableHandling/…`. *The plan calls these "seven per-type card views"; the tree actually
  holds **six** per-type line views (T066) plus these **eight** image views — fifteen per-type controls in all, which
  is the "fifteen cards" the specification names, with `WaitlistLineCardView` the one that survives.* · delete those
  sixteen files under `Module_Waitlist/Controls/`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the selector and the control model layer, independent (different files):**

- [x] **T068** [P] [US2] Delete the layout selector and the seven data templates it chooses between in
  `WaitlistViewPage.xaml` (`CoilTemplate`, `PickupFgTemplate`, `PickupNcmTemplate`, `PickupOsTemplate`,
  `PickupWipTemplate`, `ScrapTemplate`, `DefaultTemplate`) plus the `ItemTemplateSelector` binding on the list, so
  the page renders the one card template. · delete
  `MTM_Waitlist.Waitlist.View/Selectors/WaitlistLineTemplateSelector.cs`, `Module_Waitlist/Views/WaitlistViewPage.xaml`
- [x] **T069** [P] [US2] Delete the per-type control model/view-model layer in `MTM_Waitlist.Waitlist.Controls`
  (`Controls/*/…Model.cs`, `…ViewModel.cs` for Coil, DieHandling, Flatstock, ForkliftAssist, Other, Pickup, Scrap,
  TableHandling) and update that project's dependency-injection extension. ·
  delete `MTM_Waitlist.Waitlist.Controls/Controls/**`, `MTM_Waitlist.Waitlist.Controls/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 7 to finish, then:**

**Wave 8 — the field grid lands on the request page (single task):**

- [x] **T070** [US2] Move the declared-field grid off the card and onto the request detail page: lay the Item's fields
  out in declared order two per row, and let a field alone on its row take that row's **full width** — the span is
  derived from the **declared field count**, never from the Item's identity, which is how `other`'s single field gets
  the full width the design document asks for while FR-006 stays absolute (§D9). · `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`

**⟶ Wait for Wave 8 to finish, then:**

**Wave 9 — resources (single task):**

- [x] **T071** [US2] Add the card's user-visible strings through the resource mechanism (FR-022), including the two
  wrong-material first lines whose **resolved text is pinned verbatim** — `Wrong Coil Bring:` and
  `Wrong Flatstock Bring:` — and the four metadata row labels. ·
  `Strings/en-us/Resources.resw`

**Checkpoint — US2 is independently functional and testable.** Every request reachable in the list renders the same
card shape across all nineteen in-scope Items; the picture falls back Item → Category → placeholder and is never
overwritten by a placeholder; the four metadata rows survive. SC-003 is provable from this phase alone.

---

## Phase 5: User Story 3 — Sort the list the way I need it (Priority: P2) [US3]

**Goal**: a viewer picks what the list is sorted by from a control beside the ones already there; most urgent is what
it starts as, and their pick comes back next time.
**Independent Test**: pick each sort in turn and confirm the order follows; restart the application and confirm the
choice came back (SC-009).

### Tests — write these first; they must fail before the implementation lands

- [x] **T072** [P] [US3] Extend `UrgencyCalculatorTests`: the five order keys — most urgent (the default), longest
  waiting, press, requested by, status — each produce the expected order, and an overdue row stays visibly marked
  under every one of them (FR-010, FR-012). Include the case that makes "longest waiting" and "most urgent"
  demonstrably different orders: two requests raised at the same instant for Items with different allotted minutes
  have waited equally but are **not** equally urgent. ·
  `MTM_Waitlist.Tests/Core/Services/UrgencyCalculatorTests.cs`
- [x] **T073** [P] [US3] Add `WaitlistSortPreferenceServiceTests`: the default on first run is most urgent; each of
  the five keys round-trips through `ILocalSettingsService`; the value survives a service re-construction, which is
  the unit-level stand-in for surviving a restart (SC-009). *Test file placed under `Module_Core/` per the test
  conventions' `Module_<Area>` rule for new files; `plan.md` spelled this one under the older `Services/` folder.* ·
  new `MTM_Waitlist.Tests/Module_Core/Services/WaitlistSortPreferenceServiceTests.cs`
- [x] **T074** [P] [US3] Extend `WaitlistViewViewModelActionTests`: the list applies the **remembered** order on load,
  and switching the order does not disturb the action affordances or the overdue marking. ·
  `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs`

### Implementation

**Wave 1 — the per-viewer preference, independent (different files):**

- [x] **T075** [P] [US3] Add `IWaitlistSortPreferenceService` — read/write one of the five option keys for the
  current viewer, defaulting to most urgent. · new `MTM_Waitlist.Core/Contracts/Services/IWaitlistSortPreferenceService.cs`
- [x] **T076** [P] [US3] Add `WaitlistSortPreferenceService` over the existing `ILocalSettingsService` — **not** a new
  table and not the database (§D7), following the scalar-key style `UrgencySettingsService` already uses so it works
  in both the MSIX and unpackaged branches, and the small-store pattern `LocalWaitlistMessageSeenStore` establishes. ·
  new `MTM_Waitlist.Core/Services/WaitlistSortPreferenceService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the four new order keys (single task):**

- [x] **T077** [US3] Extend `UrgencyCalculator` with the four additional order keys beside `OrderMostUrgentFirst` and
  **keep that method as the default path** (§D8). Every order key reads a value the row already carries for the card
  — `UrgencyState`, requested-by, press, status — so no ordering rule adds a data read. ·
  `MTM_Waitlist.Core/Services/UrgencyCalculator.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the list applies the preference (single task):**

- [x] **T078** [US3] Update `WaitlistViewViewModel` to order the rows through the remembered sort key and to re-apply
  it after every refresh, so the order is identical when the list reloads. ·
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the shell control, independent (different files):**

- [x] **T079** [P] [US3] Add the sort control to the shell beside the existing My Requests and building controls
  (FR-011) as a `MenuFlyout` of `RadioMenuFlyoutItem` elements sharing a `GroupName` with `IsChecked` on the selected
  one — the documented WinUI 3 mechanism for mutually exclusive menu options. Every item's `Text` binds to a
  localized resource. · `Module_Core/Views/ShellPage.xaml`
- [x] **T080** [P] [US3] Add the sort control's state and its preference write to `ShellViewModel`: the five options
  in order (most urgent, longest waiting, press, requested by, status), the selected one, and the command that
  persists it for the viewer. Remember the `[RelayCommand]` stripping rule when the command is named. ·
  `ViewModels/ShellViewModel.cs`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — registration and resources, independent (different files):**

- [x] **T081** [P] [US3] Register `IWaitlistSortPreferenceService` → `WaitlistSortPreferenceService` in the
  composition root. · `Services/DependencyInjection/ServiceRegistrationExtensions.cs`
- [x] **T082** [P] [US3] Add the five sort option labels and the flyout's own label through the resource mechanism
  (FR-022). · `Strings/en-us/Resources.resw`

**Checkpoint — US3 is independently functional and testable.** The list defaults to most urgent, the shell control
changes the order, the choice returns after a restart, and overdue work stays visibly overdue under every sort.
SC-009 is provable from this phase alone.

---

## Phase 6: User Story 4 — An Item is configured, not coded (Priority: P2) [US4]

**Goal**: how far the flow goes, whether an answer is required, the prompt, the limits, the options and the fields an
Item's page shows all belong to the Item and can be changed afterwards without changing the program.
**Independent Test**: change an Item's configuration in the database — add a field, change a limit, add an option —
and confirm the flow reflects it with no code change and no rebuild (SC-006).

### Tests — write these first; they must fail before the implementation lands

- [x] **T083** [P] [US4] Add `RequestItemConfigurationServiceTests`: fields are read from configuration **in the
  declared order**, with the right value types and labels (FR-013) — the **mechanism**, never today's contents
  (FR-015). Include the three failure paths: a catalogued Item with no row is reported unavailable (FR-014); a row
  naming an Item that is not in the catalog is ignored and never offered; a malformed `detail_fields_json` or
  `options_json` is a plain-language configuration report, not an empty list and not an escaping exception (FR-026).
  **Swap the field set in the fake and assert the render follows** — that negative test is what stops the next
  spreadsheet change from becoming a build failure. · new
  `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigurationServiceTests.cs`
- [x] **T084** [P] [US4] Extend `WaitlistViewDetailFieldTests`: the detail view renders whatever fields the
  configuration declares, in order, with the declared value types and labels, and **no test in this class asserts a
  frozen field list**. · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailFieldTests.cs`

### Implementation

**Wave 1 — the declared-fields payload (single task; adds to the file T025 created):**

- [x] **T085** [US4] Add the declared-fields layer to `RequestItemConfigurationService`: deserialize
  `detail_fields_json` and `options_json` with `System.Text.Json` **in the application** (§D13 — the schema targets
  MySQL 5.7, where `JSON_TABLE` is unavailable, and this is the pattern `RequestTypeCatalogService` already used for
  its own JSON column), validate each field's declared order, value type and label, and report a malformed payload as
  a configuration problem in plain language (FR-026). Adding a field must stay a one-row data edit, never a
  three-artifact change. · `MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the detail view renders what the data declares, independent (different files):**

- [x] **T086** [P] [US4] Update `WaitlistViewDetailViewModel` and the detail page to render the declared fields in
  declared order with their declared types and labels, sized by the field count (T070 owns the grid's layout; this
  task owns the data binding). Any new converter goes into `App.xaml` in this same change (constitution V). ·
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`,
  `Module_Waitlist/Views/WaitlistViewDetailPage.xaml`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the changeability proof (single task):**

- [x] **T087** [US4] Demonstrate SC-006 and record it: change one Item's configuration **in the store only** — add a
  declared field, change a length limit, add an enumerated option — and confirm the flow reflects the change with
  **no code change and no rebuild**. Evidence: the row edit, the resulting flow, and an unchanged source tree. ·
  `Database/Seeds/seed_waitlist_request_item_configs/create.sql` (the row the demonstration edits, restored after)

**Checkpoint — US4 is independently functional and testable.** An Item's fields, options, prompts and limits are
data; the configuration is read once per wizard entry; both directions of disagreement (missing row, stray row) are
reported or ignored, never half-configured and never a crash.

---

## Phase 7: User Story 5 — Set an Item's minutes and picture, with real numbers in front of you (Priority: P3) [US5]

**Goal**: the people who set the floor's expectations set each Item's allotted minutes and its picture, and see what
that Item has actually been taking.
**Independent Test**: open both screens, set an Item's minutes and picture, and confirm a request for that Item is
then measured and pictured accordingly; in the minutes screen, confirm each Item shows both its configured minutes
and the average it has actually taken.

### Tests — write these first; they must fail before the implementation lands

- [ ] **T088** [P] [US5] Extend `UrgencyAllotmentEditorViewModelTests`: each row shows the configured minutes and the
  observed average as **two clearly separate values** (FR-018, SC-008), the observed value is never written back
  (FR-019), the write is clamped to a positive minimum and a twenty-four-hour maximum matching
  `UrgencySettingsService.SetMaxAllottedAsync`'s existing clamp, and the screen's gate is
  **`CanManageUrgencySettings`** — the gate that already governs it (FR-020). ·
  `MTM_Waitlist.Tests/Module_Settings/UrgencyAllotmentEditorViewModelTests.cs`
- [ ] **T089** [P] [US5] Add `RequestItemImagesDialogViewModelTests`: the picture screen is keyed by Item, its gate is
  **`CanManageImageLocationSettings`** — a **different** gate from the minutes editor's, neither collapsed into one
  nor replaced by a new one (FR-020) — and a `"nothing configured"` answer never overwrites a resolved image
  (FR-021). · new `MTM_Waitlist.Tests/Module_Settings/RequestItemImagesDialogViewModelTests.cs`
- [ ] **T090** [P] [US5] Extend `UrgencySettingsServiceTests`: the allotted minutes are read from the Item's stored
  configuration rather than a per-Windows-user local key, an Update goes through
  `sp_waitlist_request_item_allotted_minutes_update`, and an Item with no configured minutes uses the **15-minute**
  default, **labelled as a default**, and keeps its place in the urgency order (FR-016, FR-017, SC-007). ·
  `MTM_Waitlist.Tests/Core/Services/UrgencySettingsServiceTests.cs`
- [ ] **T091** [P] [US5] Add `RequestItemObservedTimeServiceTests`: only `Completed` requests count; the endpoints are
  `accepted_utc` → `completed_utc`; a released-then-completed request contributes **exactly once**; a request missing
  either endpoint is excluded; no time window is applied; and an Item with no completed request yields **no value**
  rather than a fabricated zero (FR-019, FR-026). *New file under `Module_Settings/Services/` per the
  `Module_<Area>` rule.* · new
  `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemObservedTimeServiceTests.cs`

### Implementation

**Wave 1 — the deadline source moves into the store, independent (different files):**

- [ ] **T092** [P] [US5] Re-key `IUrgencySettingsService` from subtype to Item and from local settings to the store
  (§D4): the configured value must be visible to everyone and must sit beside a value that comes from a stored
  procedure, which a per-Windows-user key cannot do. · `MTM_Waitlist.Core/Contracts/Services/IUrgencySettingsService.cs`
- [ ] **T093** [P] [US5] Re-key `UrgencySettingsService` to the Item's stored minutes: read through the
  configuration read, write through `sp_waitlist_request_item_allotted_minutes_update`, keep
  `DefaultMinutes` at the 15-minute fallback (T027), and stop writing `Urgency.MaxAllottedMinutes.<subtype>` keys. ·
  `MTM_Waitlist.Core/Services/UrgencySettingsService.cs`
- [ ] **T094** [P] [US5] Re-key `IUrgencyDeadlineService` / `UrgencyDeadlineService` to the Item's allotted minutes, so
  the deadline derives from the Item and never from a type or subtype (FR-016). ·
  `MTM_Waitlist.Core/Contracts/Services/IUrgencyDeadlineService.cs`,
  `MTM_Waitlist.Core/Services/UrgencyDeadlineService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the minutes screen (single task):**

- [ ] **T095** [US5] Re-key `UrgencyAllotmentEditorViewModel` from subtype to Item and add the second column:
  configured minutes and the observed average as visibly distinct values, with the default labelled as a default
  rather than as configured (FR-017, FR-018, SC-007, SC-008). Keep its **own** role gate
  `CanManageUrgencySettings` — do not collapse it with the picture screen's. Update that module's dependency
  injection to supply the new collaborators. ·
  `MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs`,
  `MTM_Waitlist.Settings/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the picture screen's view model is renamed and re-pointed (single task):**

- [ ] **T096** [US5] Rename `RequestSubtypeImagesDialogViewModel` to `RequestItemImagesDialogViewModel` and re-point
  it at the Item: the `request_item` scope per Item, the `request_category` scope as the family it inherits from,
  keeping `SupportsGrouping` / `SupportsInheritance`, the per-row batched commit path and the storage root exactly as
  they are (§D10). This is a **rename**, not a new image subsystem. ·
  `MTM_Waitlist.Settings/ViewModels/RequestItemImagesDialogViewModel.cs` (renamed from
  `MTM_Waitlist.Settings/ViewModels/RequestSubtypeImagesDialogViewModel.cs`)

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the picture screen's view (single task; its code-behind constructs the renamed view model):**

- [ ] **T097** [US5] Rename and re-point the dialog view: `Module_Settings/Views/RequestItemImagesDialog.xaml(.cs)`,
  with its code-behind constructing the renamed `RequestItemImagesDialogViewModel`. · new
  `Module_Settings/Views/RequestItemImagesDialog.xaml`, `Module_Settings/Views/RequestItemImagesDialog.xaml.cs`
  (renamed from the `RequestSubtypeImagesDialog` pair)

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the Settings page hosts the re-keyed screens (single task):**

- [ ] **T098** [US5] Update `SettingsViewModel` and `SettingsPage` for the re-keyed screens: the minutes panel's
  configured + observed columns, the Item-keyed picture entry point (the `RequestSubtypeImages_Click` handler becomes
  the Item handler and opens the renamed dialog), panel visibility, and the settings search reaching both panels.
  Keep **both** role gates separate — `CanManageUrgencySettings` for the minutes editor and
  `CanManageImageLocationSettings` for the picture screen (FR-020: "the role gate" is two gates today and must stay
  two). · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`,
  `Module_Settings/Views/SettingsPage.xaml.cs`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — resources (single task):**

- [ ] **T099** [US5] Add both screens' new user-visible strings through the resource mechanism (FR-022): the Item
  column headers, the configured/observed column labels, the "default" marker, and the picture screen's Item labels.
  · `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the settings tests follow their subjects (single task):**

- [ ] **T100** [US5] Update `SettingsViewModelTests` and `SettingsPageMarkupTests`: panel visibility, settings search
  reaching the re-keyed panels, and that neither screen exposes a control to a viewer without its own gate. ·
  `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests.cs`,
  `MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests.cs`

**Checkpoint — US5 is independently functional and testable.** Both screens show and save per-Item values, the
minutes screen shows configured beside observed, a rowless Item is measured by the labelled 15-minute default and
keeps its place in the urgency order, and neither gate was collapsed. SC-007 and SC-008 are provable from this phase
alone.

---

## Phase 8: Polish

Cross-cutting retirement, documentation, and validation against the Success Criteria.

### Wave 1 — the retired database artifacts, independent (different files)

- [ ] **T101** [P] Retire `Database/Tables/28_waitlist_request_types/{create,rollback}.sql` and
  `Database/Tables/29_waitlist_request_subtypes/{create,rollback}.sql` (FR-023), consuming the mapping at design time
  — by now the seed authored in T007 has already taken everything those two tables' populated `category` / `item_id`
  columns carried, so nothing at runtime reads them. ·
  `Database/Tables/AllTables.sql` (entries removed with T104)
- [ ] **T102** [P] Retire the two type/subtype read procedures
  `Database/StoredProcedures/sp_waitlist_request_types_get/{create,rollback}.sql` and
  `Database/StoredProcedures/sp_waitlist_request_subtypes_get/{create,rollback}.sql` (FR-023). ·
  `Database/StoredProcedures/AllSPs.sql` (entry removed with T104)
- [ ] **T103** [P] Retire `Database/Seeds/seed_waitlist_request_catalog/{create,rollback}.sql` once its `item_id`
  mapping has been consumed (FR-023). · `Database/Seeds/AllSeeds.sql` (entry removed with T104)

**⟶ Wait for Wave 1 to finish, then:**

### Wave 2 — the master lists and descriptions agree with the disk (single task; one owner for four files)

- [ ] **T104** Remove the retired artifacts from the master lists and from the mandatory description file, so
  `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql` and the descriptions agree with the files on disk (FR-025,
  constitution III, database-schema rules). Nothing may be added here — T016 already added this feature's artifacts. ·
  `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`,
  `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 2 to finish, then:**

### Wave 3 — the retired code symbols, independent (different files)

- [ ] **T105** [P] Retire `RequestSubtypeInventory.cs` and `RequestTypeInventory.cs` (FR-023). ·
  delete both files under `MTM_Waitlist.Settings/Models/`
- [ ] **T106** [P] Retire the subtype display-label service: `IRequestSubtypeDisplayLabelService.cs` and
  `RequestSubtypeDisplayLabelService.cs` (FR-023). · delete both files under `MTM_Waitlist.Settings/Services/`
- [ ] **T107** [P] **Inspect, then retire** the request-type display-label service —
  `IRequestTypeDisplayLabelService.cs`, `RequestTypeDisplayLabelService.cs` — and
  `RequestItemLegacyMapper.cs` with its test `MTM_Waitlist.Tests/Module_Settings/RequestItemLegacyMapperTests.cs`.
  §D15 requires the inspection: the mapper may have backed the seed rewrite and a display-label service may still
  have a non-Item consumer. Retire each only once no remaining reader is found, and record which were. ·
  `MTM_Waitlist.Settings/Services/IRequestTypeDisplayLabelService.cs`,
  `MTM_Waitlist.Settings/Services/RequestTypeDisplayLabelService.cs`,
  `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs`,
  `MTM_Waitlist.Tests/Module_Settings/RequestItemLegacyMapperTests.cs`
- [ ] **T108** [P] Retire the two obsolete image scopes: remove `request_subtype` and `request_type` from
  `ImageLocationScope` and from the `config_images_locations` rows that carry them, once the
  `RequestSubtypeImagesDialog` has already become the Item dialog (T096–T097) and the `work_center` scope — which is
  **kept** — is the only other live scope. ·
  `MTM_Waitlist.Settings/Models/ImageLocationScope.cs`, `Database/Seeds/seed_waitlist_request_item_images/create.sql`
- [ ] **T109** [P] **Inspect, then retire** `RequestTypeImagesDialog.xaml(.cs)` and
  `RequestTypeImagesDialogViewModel.cs` (with a `RequestItemImagesDialog` in their place) — the `request_type` scope
  they served is gone as of T108. *§D10 says the `request_type` / `request_subtype` scopes and their dialogs retire
  with the vocabulary, while `plan.md`'s Project Structure lists only the subtype dialog as retired; the inspection
  this task requires is what resolves the two statements.* · `Module_Settings/Views/RequestTypeImagesDialog.xaml`,
  `Module_Settings/Views/RequestTypeImagesDialog.xaml.cs`,
  `MTM_Waitlist.Settings/ViewModels/RequestTypeImagesDialogViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

### Wave 4 — the audit guard that makes the retirement stick (single task)

- [ ] **T110** Extend `RetiredSymbolAuditTests` (FR-023, SC-012): add every symbol, wording and scope value this
  feature retires — the two catalog tables, the two read procedures, the catalog seed, the retired models, services,
  dialogs, the per-type controls, `request_type` / `request_subtype`, and the retired user-visible wording — so a
  return of any of them fails the build. The existing pattern set is the extension point; do not invent a second
  mechanism. · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 4 to finish, then:**

### Wave 5 — documentation in the same change, independent (different files)

- [ ] **T111** [P] Record the supersession in `specs/003-waitlist-handler-fulfilment` (§D17): rewrite the
  **"VERIFIED CARD ANATOMY … DO NOT REGRESS"** constraint (line 321 and its quote block) to the new anatomy, rewrite
  its **matching test** rather than deleting it — the freeze is lifted **in writing** so an unintended change still
  fails — and amend its FR-015 from "MUST be ordered most-urgent-first" to "**defaults to** most-urgent-first". ·
  `specs/003-waitlist-handler-fulfilment/spec.md`
- [ ] **T112** [P] Sweep the project documentation for the retired vocabulary (SC-012): remove stale references to
  request types and subtypes and to the per-type cards from `README.md`, `CHANGELOG.md`, `FEATURES.md`,
  `OPEN-TASKS.md` and the `WeekendProject/` documents. **Do not touch the two documents of record**
  (`WeekendProject/Documents/Request-Config-Template.csv`,
  `WeekendProject/Documents/unified-item-picker-workflows.md`) — FR-027 keeps them as design documents, and T002's
  audit only forbids the *application* referencing them. ·
  `README.md`, `CHANGELOG.md`, `FEATURES.md`, `OPEN-TASKS.md`, `WeekendProject/**`

**⟶ Wait for Wave 5 to finish, then:**

### Wave 6 — the gates

- [ ] **T113** **Build gate.** With no `MTM_Waitlist.exe` running, run
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and record the result: it must read
  **`0 Warning(s) 0 Error(s)`**. Treat `WMC9999` as a masked **real XAML error** and surface it with the documented
  technique (a deliberate C# error to force the file/type report, or bisecting the changed XAML) — never as an
  environment problem. Treat `PRI175` / `PRI224` as a running app or stale `*.pri`, and clear the stale processes
  before re-running. · `MTM_Waitlist.sln`
- [ ] **T114** **Test gate.** Run
  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` and record the result: it must
  read **`Failed: 0`** *with* this feature's new tests in place, not instead of them. Environment-gated tests stay
  `Assert.Inconclusive` and are recorded as skipped, never as passing, and no gate is weakened to go green (no
  deletion, no `[Ignore]`, no new skip). · `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [ ] **T115** **Live database validation.** Validate the new and changed schema artifacts against a live local
  `mtm_waitlist` (constitution III): every paired `create.sql` / `rollback.sql` applies **and reverses** cleanly, the
  three new procedures return the shapes `contracts/item-configuration.md` §2–§4 document, and the four new seeds
  load. Confirm the four master lists and the description file agree with the files on disk. **The reinstall is the
  owner's action, never the agent's** — hand it over and wait; no migration is written, and that absence is the
  recorded assumption rather than an omission. · `Database/**`
- [ ] **T116** **Scripted UI verification.** Drive the running app with the PowerShell UI-Automation recipe and prove
  what only the running artifact can: a request can be raised by choosing a Category and an Item in one pass without
  ever being asked for a type or a subtype (SC-001); requests for several different Items each draw the **same** card
  shape (SC-003); the sort control changes the order and its choice returns after a restart (SC-009); and both
  Settings screens show and save per-Item values (US5). Carry the recorded traps: both connection overrides are
  needed, **never locate the window by title**, re-read a control's bounding rectangle in the same command that
  clicks it, and close the app when finished so no orphan process holds database connections. Then walk **one** work
  centre through the real Setup flow and confirm the picker filters from what Setup actually wrote — this is what
  makes the eight-configuration matrix a proof about the real path rather than about a seed shape. *Written against
  the **live** `mtm_waitlist`, resolved locally because the shared host `172.16.1.104` is unreachable from this
  workstation. `contracts/verification-gates.md` §4 says "against the mock mirror"; that is a wording error — the
  `mtm_mock` mirror serves **external Infor Visual reads only**, and the internal stores are always read and written
  live (constitution II). A Setup walk cannot be served by a cache that never holds it.* ·
  `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`
- [ ] **T117** **Validate against Success Criteria.** Walk SC-001 … SC-012 against the recorded gate evidence — SC-011
  closes on T113's and T114's outputs, so **do not run the suites a second time** — and for each one record the
  evidence that closes it. Any criterion without recorded evidence stays unticked and is reported, never asserted.
  Single-owner validation: `.specify/companion.yml` is empty, so there is **no** `owns: validation` hook and Polish
  owns this walk. · `specs/004-unified-card-item-picker/spec.md`,
  `specs/004-unified-card-item-picker/contracts/verification-gates.md`

---

## Dependencies & Execution Order

**Phase dependencies.**

```text
Phase 1 Setup ──▶ Phase 2 Foundational ──▶ ┌ Phase 3 US1 (P1) ──┐
                                           ├ Phase 4 US2 (P1) ──┤
                                           ├ Phase 5 US3 (P2) ──┼──▶ Phase 8 Polish
                                           ├ Phase 6 US4 (P2) ──┤
                                           └ Phase 7 US5 (P3) ──┘
```

Every user story waits for Foundational and nothing else. **Do not begin any story before Phase 2's Checkpoint** —
the request's two columns, the configuration table and the availability snapshot are what each story stands on.

**Cross-story boundaries that are real, not organisational.**

- **US2 depends on US1's model layer, not on its screens.** `WaitlistRequestTitles` / `SampleOrder` read
  `RequestItemDefinition` and `RequestItemLine2Resolver` (Phase 2), so US2 can start once Foundational is done. But
  the list is only *demoable* across several Items once a request can be raised — so if a single-threaded order is
  wanted, US1 then US2.
- **US5's observed average needs US1's stored pair.** `sp_waitlist_request_item_observed_average_get` reads the
  request's `item`, `accepted_utc` and `completed_utc`; the procedure exists from Phase 2, but it has a non-empty
  population only after requests carry Items — which US1's T048 completes.
- **US4 re-opens US1's Details step and US2's detail page.** T085 extends the file T025 created, and T086 binds the
  page T070 laid out. Both are deliberate sequential same-file edits across phases; neither may be merged into its
  earlier counterpart without making the earlier phase untestable on its own.
- **`Strings/en-us/Resources.resw` is one file touched by five phases** (T053, T071, T082, T099, plus the removals in
  T053). It can never appear twice in one wave. If the stories are run in parallel, the resource edits are the
  single serialised resource — sequence them.

**Wave summary, phase by phase.**

- **Phase 1 Setup** — one wave of three independent tasks (T001, T002, T003); nothing else depends on their order.
- **Phase 2 Foundational** — eleven waves: W1 six new artifacts (T004–T009) → W2 the request table alone (T010) →
  W3 five re-keyed/added procedures and seeds (T011–T015) → W4 the master lists (T016) → W5 seven models
  (T017–T023) → W6 the shared scrap rule and the four providers (T024–T027) → W7 the merged pickup rule (T028) →
  W8 the scrap gate and out-of-scope Items (T029) → W9 the filtered list (T030) → W10 the snapshot provider (T031) →
  W11 the seed round-trip (T032). W2 blocks W3 because the three request procedures and the observed average all read
  the new columns. W4 blocks every later phase because the master lists are what the live-DB gate checks.
- **Phase 3 US1** — tests first (T033–T035), then nine waves: the flow state (T036–T037) → the step order
  (T038–T039) → the step view models (T040–T042) → the step pages (T043–T044) → the later steps (T045–T047) → the
  write path (T048) → the type/subtype retirement (T049–T052) → resources and registration (T053–T054) → the
  Category step's tests (T055).
- **Phase 4 US2** — tests first (T056–T060), then nine waves: `WaitlistRequestTitles` (T061) → `SampleOrder` (T062) →
  the image scope (T063) → the card (T064) → the list (T065) → the per-type line and image views (T066–T067) → the
  selector and the control model layer (T068–T069) → the page's field grid (T070) → resources (T071). W6 must
  precede W7: `WaitlistViewPage.xaml` still references the templates W6 deletes until W7 removes them.
- **Phase 5 US3** — tests first (T072–T074), then five waves: the preference service (T075–T076) → the order keys
  (T077) → the list (T078) → the shell control (T079–T080) → registration and resources (T081–T082).
- **Phase 6 US4** — tests first (T083–T084), then three waves: the payload layer (T085) → the detail binding (T086) →
  the changeability proof (T087).
- **Phase 7 US5** — tests first (T088–T091), then seven waves: the deadline source (T092–T094) → the minutes screen
  (T095) → the picture view model rename (T096) → the dialog view rename (T097) → the Settings page (T098) →
  resources (T099) → the settings tests (T100). W3 must precede W4: the renamed dialog's code-behind constructs the
  renamed view model.
- **Phase 8 Polish** — W1 the retired DB artifacts (T101–T103) → W2 the master lists (T104) → W3 the retired code
  symbols (T105–T109) → W4 the audit guard (T110) → W5 documentation (T111–T112) → W6 the gates (T113–T117).
  W3 must precede W4 only in the sense that the guard should be extended once the deletions exist; W4 before W6 so
  the build gate proves the retirements, not the intent.

**Parallel opportunities.** Every wave of `[P]` tasks may be built concurrently — different files, no unfinished
dependency between them. The largest are Phase 2 W1 (six new stored artifacts), Phase 2 W5 (seven model files) and
Phase 2 W6 (four independent providers), the three of which are also the best candidates for fanning out to
subagents, because none of their members share a file. The three build traps to carry into any parallel run: the
XAML compiler races on its own intermediate files (**never build the solution in parallel** — `/m:1
/nodeReuse:false`), `WMC9999` is a real masked XAML error, and `[RelayCommand]` strips a trailing `Async`.

**Independent test points.** Each story's Checkpoint is a real stopping point: US1 closes SC-001/SC-005, US2 closes
SC-003, US3 closes SC-009, US4 closes SC-006, US5 closes SC-007/SC-008. Foundational alone closes nothing
user-visible — do not report it as shipped.
