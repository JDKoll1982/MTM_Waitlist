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

- [x] **T088** [P] [US5] Extend `UrgencyAllotmentEditorViewModelTests`: each row shows the configured minutes and the
  observed average as **two clearly separate values** (FR-018, SC-008), the observed value is never written back
  (FR-019), the write is clamped to a positive minimum and a twenty-four-hour maximum matching
  `UrgencySettingsService.SetMaxAllottedAsync`'s existing clamp, and the screen's gate is
  **`CanManageUrgencySettings`** — the gate that already governs it (FR-020). ·
  `MTM_Waitlist.Tests/Module_Settings/UrgencyAllotmentEditorViewModelTests.cs`
- [x] **T089** [P] [US5] Add `RequestItemImagesDialogViewModelTests`: the picture screen is keyed by Item, its gate is
  **`CanManageImageLocationSettings`** — a **different** gate from the minutes editor's, neither collapsed into one
  nor replaced by a new one (FR-020) — and a `"nothing configured"` answer never overwrites a resolved image
  (FR-021). · new `MTM_Waitlist.Tests/Module_Settings/RequestItemImagesDialogViewModelTests.cs`
- [x] **T090** [P] [US5] Extend `UrgencySettingsServiceTests`: the allotted minutes are read from the Item's stored
  configuration rather than a per-Windows-user local key, an Update goes through
  `sp_waitlist_request_item_allotted_minutes_update`, and an Item with no configured minutes uses the **15-minute**
  default, **labelled as a default**, and keeps its place in the urgency order (FR-016, FR-017, SC-007). ·
  `MTM_Waitlist.Tests/Core/Services/UrgencySettingsServiceTests.cs`
- [x] **T091** [P] [US5] Add `RequestItemObservedTimeServiceTests`: only `Completed` requests count; the endpoints are
  `accepted_utc` → `completed_utc`; a released-then-completed request contributes **exactly once**; a request missing
  either endpoint is excluded; no time window is applied; and an Item with no completed request yields **no value**
  rather than a fabricated zero (FR-019, FR-026). *New file under `Module_Settings/Services/` per the
  `Module_<Area>` rule.* · new
  `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemObservedTimeServiceTests.cs`

### Implementation

**Wave 1 — the deadline source moves into the store, independent (different files):**

- [x] **T092** [P] [US5] Re-key `IUrgencySettingsService` from subtype to Item and from local settings to the store
  (§D4): the configured value must be visible to everyone and must sit beside a value that comes from a stored
  procedure, which a per-Windows-user key cannot do. · `MTM_Waitlist.Core/Contracts/Services/IUrgencySettingsService.cs`
- [x] **T093** [P] [US5] Re-key `UrgencySettingsService` to the Item's stored minutes: read through the
  configuration read, write through `sp_waitlist_request_item_allotted_minutes_update`, keep
  `DefaultMinutes` at the 15-minute fallback (T027), and stop writing `Urgency.MaxAllottedMinutes.<subtype>` keys. ·
  `MTM_Waitlist.Core/Services/UrgencySettingsService.cs`
- [x] **T094** [P] [US5] Re-key `IUrgencyDeadlineService` / `UrgencyDeadlineService` to the Item's allotted minutes, so
  the deadline derives from the Item and never from a type or subtype (FR-016). ·
  `MTM_Waitlist.Core/Contracts/Services/IUrgencyDeadlineService.cs`,
  `MTM_Waitlist.Core/Services/UrgencyDeadlineService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the minutes screen (single task):**

- [x] **T095** [US5] Re-key `UrgencyAllotmentEditorViewModel` from subtype to Item and add the second column:
  configured minutes and the observed average as visibly distinct values, with the default labelled as a default
  rather than as configured (FR-017, FR-018, SC-007, SC-008). Keep its **own** role gate
  `CanManageUrgencySettings` — do not collapse it with the picture screen's. Update that module's dependency
  injection to supply the new collaborators. ·
  `MTM_Waitlist.Settings/ViewModels/UrgencyAllotmentEditorViewModel.cs`,
  `MTM_Waitlist.Settings/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the picture screen's view model is renamed and re-pointed (single task):**

- [x] **T096** [US5] Rename `RequestSubtypeImagesDialogViewModel` to `RequestItemImagesDialogViewModel` and re-point
  it at the Item: the `request_item` scope per Item, the `request_category` scope as the family it inherits from,
  keeping `SupportsGrouping` / `SupportsInheritance`, the per-row batched commit path and the storage root exactly as
  they are (§D10). This is a **rename**, not a new image subsystem. ·
  `MTM_Waitlist.Settings/ViewModels/RequestItemImagesDialogViewModel.cs` (renamed from
  `MTM_Waitlist.Settings/ViewModels/RequestSubtypeImagesDialogViewModel.cs`)

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the picture screen's view (single task; its code-behind constructs the renamed view model):**

- [x] **T097** [US5] Rename and re-point the dialog view: `Module_Settings/Views/RequestItemImagesDialog.xaml(.cs)`,
  with its code-behind constructing the renamed `RequestItemImagesDialogViewModel`. · new
  `Module_Settings/Views/RequestItemImagesDialog.xaml`, `Module_Settings/Views/RequestItemImagesDialog.xaml.cs`
  (renamed from the `RequestSubtypeImagesDialog` pair)

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the Settings page hosts the re-keyed screens (single task):**

- [x] **T098** [US5] Update `SettingsViewModel` and `SettingsPage` for the re-keyed screens: the minutes panel's
  configured + observed columns, the Item-keyed picture entry point (the `RequestSubtypeImages_Click` handler becomes
  the Item handler and opens the renamed dialog), panel visibility, and the settings search reaching both panels.
  Keep **both** role gates separate — `CanManageUrgencySettings` for the minutes editor and
  `CanManageImageLocationSettings` for the picture screen (FR-020: "the role gate" is two gates today and must stay
  two). · `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`,
  `Module_Settings/Views/SettingsPage.xaml.cs`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — resources (single task):**

- [x] **T099** [US5] Add both screens' new user-visible strings through the resource mechanism (FR-022): the Item
  column headers, the configured/observed column labels, the "default" marker, and the picture screen's Item labels.
  · `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the settings tests follow their subjects (single task):**

- [x] **T100** [US5] Update `SettingsViewModelTests` and `SettingsPageMarkupTests`: panel visibility, settings search
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

- [x] **T101** [P] Retire `Database/Tables/28_waitlist_request_types/{create,rollback}.sql` and
  `Database/Tables/29_waitlist_request_subtypes/{create,rollback}.sql` (FR-023), consuming the mapping at design time
  — by now the seed authored in T007 has already taken everything those two tables' populated `category` / `item_id`
  columns carried, so nothing at runtime reads them. ·
  `Database/Tables/AllTables.sql` (entries removed with T104)
- [x] **T102** [P] Retire the two type/subtype read procedures
  `Database/StoredProcedures/sp_waitlist_request_types_get/{create,rollback}.sql` and
  `Database/StoredProcedures/sp_waitlist_request_subtypes_get/{create,rollback}.sql` (FR-023). ·
  `Database/StoredProcedures/AllSPs.sql` (entry removed with T104)
- [x] **T103** [P] Retire `Database/Seeds/seed_waitlist_request_catalog/{create,rollback}.sql` once its `item_id`
  mapping has been consumed (FR-023). · `Database/Seeds/AllSeeds.sql` (entry removed with T104)

**⟶ Wait for Wave 1 to finish, then:**

### Wave 2 — the master lists and descriptions agree with the disk (single task; one owner for four files)

- [x] **T104** Remove the retired artifacts from the master lists and from the mandatory description file, so
  `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql` and the descriptions agree with the files on disk (FR-025,
  constitution III, database-schema rules). Nothing may be added here — T016 already added this feature's artifacts. ·
  `Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`,
  `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 2 to finish, then:**

### Wave 3 — the retired code symbols, independent (different files)

- [x] **T105** [P] Retire `RequestSubtypeInventory.cs` and `RequestTypeInventory.cs` (FR-023). ·
  delete both files under `MTM_Waitlist.Settings/Models/`
- [x] **T106** [P] Retire the subtype display-label service: `IRequestSubtypeDisplayLabelService.cs` and
  `RequestSubtypeDisplayLabelService.cs` (FR-023). · delete both files under `MTM_Waitlist.Settings/Services/`
- [x] **T107** [P] **Inspect, then retire** the request-type display-label service —
  `IRequestTypeDisplayLabelService.cs`, `RequestTypeDisplayLabelService.cs` — and
  `RequestItemLegacyMapper.cs` with its test `MTM_Waitlist.Tests/Module_Settings/RequestItemLegacyMapperTests.cs`.
  §D15 requires the inspection: the mapper may have backed the seed rewrite and a display-label service may still
  have a non-Item consumer. Retire each only once no remaining reader is found, and record which were. ·
  `MTM_Waitlist.Settings/Services/IRequestTypeDisplayLabelService.cs`,
  `MTM_Waitlist.Settings/Services/RequestTypeDisplayLabelService.cs`,
  `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs`,
  `MTM_Waitlist.Tests/Module_Settings/RequestItemLegacyMapperTests.cs`
- [x] **T108** [P] Retire the two obsolete image scopes: remove `request_subtype` and `request_type` from
  `ImageLocationScope` and from the `config_images_locations` rows that carry them, once the
  `RequestSubtypeImagesDialog` has already become the Item dialog (T096–T097) and the `work_center` scope — which is
  **kept** — is the only other live scope. ·
  `MTM_Waitlist.Settings/Models/ImageLocationScope.cs`, `Database/Seeds/seed_waitlist_request_item_images/create.sql`
- [x] **T109** [P] **Inspect, then retire** `RequestTypeImagesDialog.xaml(.cs)` and
  `RequestTypeImagesDialogViewModel.cs` (with a `RequestItemImagesDialog` in their place) — the `request_type` scope
  they served is gone as of T108. *§D10 says the `request_type` / `request_subtype` scopes and their dialogs retire
  with the vocabulary, while `plan.md`'s Project Structure lists only the subtype dialog as retired; the inspection
  this task requires is what resolves the two statements.* · `Module_Settings/Views/RequestTypeImagesDialog.xaml`,
  `Module_Settings/Views/RequestTypeImagesDialog.xaml.cs`,
  `MTM_Waitlist.Settings/ViewModels/RequestTypeImagesDialogViewModel.cs`

**⟶ Wait for Wave 3 to finish, then:**

### Wave 4 — the audit guard that makes the retirement stick (single task)

- [x] **T110** Extend `RetiredSymbolAuditTests` (FR-023, SC-012): add every symbol, wording and scope value this
  feature retires — the two catalog tables, the two read procedures, the catalog seed, the retired models, services,
  dialogs, the per-type controls, `request_type` / `request_subtype`, and the retired user-visible wording — so a
  return of any of them fails the build. The existing pattern set is the extension point; do not invent a second
  mechanism. · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`

**⟶ Wait for Wave 4 to finish, then:**

### Wave 5 — documentation in the same change, independent (different files)

- [x] **T111** [P] Record the supersession in `specs/003-waitlist-handler-fulfilment` (§D17): rewrite the
  **"VERIFIED CARD ANATOMY … DO NOT REGRESS"** constraint (line 321 and its quote block) to the new anatomy, rewrite
  its **matching test** rather than deleting it — the freeze is lifted **in writing** so an unintended change still
  fails — and amend its FR-015 from "MUST be ordered most-urgent-first" to "**defaults to** most-urgent-first". ·
  `specs/003-waitlist-handler-fulfilment/spec.md`
- [x] **T112** [P] Sweep the project documentation for the retired vocabulary (SC-012): remove stale references to
  request types and subtypes and to the per-type cards from `README.md`, `CHANGELOG.md`, `FEATURES.md`,
  `OPEN-TASKS.md` and the `WeekendProject/` documents. **Do not touch the two documents of record**
  (`WeekendProject/Documents/Request-Config-Template.csv`,
  `WeekendProject/Documents/unified-item-picker-workflows.md`) — FR-027 keeps them as design documents, and T002's
  audit only forbids the *application* referencing them. ·
  `README.md`, `CHANGELOG.md`, `FEATURES.md`, `OPEN-TASKS.md`, `WeekendProject/**`

**⟶ Wait for Wave 5 to finish, then:**

### Wave 6 — the gates

- [x] **T113** **Build gate.** With no `MTM_Waitlist.exe` running, run
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and record the result: it must read
  **`0 Warning(s) 0 Error(s)`**. Treat `WMC9999` as a masked **real XAML error** and surface it with the documented
  technique (a deliberate C# error to force the file/type report, or bisecting the changed XAML) — never as an
  environment problem. Treat `PRI175` / `PRI224` as a running app or stale `*.pri`, and clear the stale processes
  before re-running. · `MTM_Waitlist.sln`
- [x] **T114** **Test gate.** Run
  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` and record the result: it must
  read **`Failed: 0`** *with* this feature's new tests in place, not instead of them. Environment-gated tests stay
  `Assert.Inconclusive` and are recorded as skipped, never as passing, and no gate is weakened to go green (no
  deletion, no `[Ignore]`, no new skip). · `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [x] **T115** **Live database validation.** Validate the new and changed schema artifacts against a live local
  `mtm_waitlist` (constitution III): every paired `create.sql` / `rollback.sql` applies **and reverses** cleanly, the
  three new procedures return the shapes `contracts/item-configuration.md` §2–§4 document, and the four new seeds
  load. Confirm the four master lists and the description file agree with the files on disk. **The reinstall is the
  owner's action, never the agent's** — hand it over and wait; no migration is written, and that absence is the
  recorded assumption rather than an omission. · `Database/**`
- [x] **T116** **Scripted UI verification.** Drive the running app with the PowerShell UI-Automation recipe and prove
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
- [x] **T117** **Validate against Success Criteria.** Walk SC-001 … SC-012 against the recorded gate evidence — SC-011
  closes on T113's and T114's outputs, so **do not run the suites a second time** — and for each one record the
  evidence that closes it. Any criterion without recorded evidence stays unticked and is reported, never asserted.
  Single-owner validation: `.specify/companion.yml` is empty, so there is **no** `owns: validation` hook and Polish
  owns this walk. · `specs/004-unified-card-item-picker/spec.md`,
  `specs/004-unified-card-item-picker/contracts/verification-gates.md`

---

## Phase 9: Follow-Up Batch — Sign Out, the Request Population and the Fulfilment Pass

This phase follows the **shipped** build: the feature reached `status: completed`, and Phases 1–8 above are all
ticked. Everything here is new work against the appended requirements **FR-033 … FR-045** and criteria
**SC-013 … SC-020**; nothing above is reopened, and `.spec-context.json` is not this phase's to edit. The same four
gates apply, and the three build traps carry over.

The `[US#]` tags map the batch onto the stories it serves: **US6** is the new sign-out story, **US1** owns raising a
request, and **US2** owns the card's action surface. The handling outcomes are **verification**, not a new story —
`plan.md` §F5 is their plan of record.

### Tests — write these first; they must fail before the implementation lands

- [x] **T118** [P] [US6] Add `ShellViewModelTests` (new): the signed-in badge offers **Sign out**, and choosing it
  invokes the sign-out path through a recording stub rather than merely blanking `CurrentUserDisplayName` — the
  negative case is the point, because a view model that never calls the service must not look signed out
  (FR-033, FR-034). · new `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellViewModelTests.cs`
- [x] **T119** [P] [US6] Add `SignOutServiceTests` (new): the five keys the sign-in path reads
  (`Login.RememberPassword`, `Login.RememberedUsername`, `Login.RememberedPassword`, `Startup.Session.Token`,
  `Startup.Session.ExpiresUtc`) are reset **before** the relaunch is asked for, so the restarted instance cannot
  restore the session, and a refused relaunch is reported in plain language rather than swallowed
  (FR-034, FR-026). · new `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs`
- [x] **T120** [P] [US1] Re-point `ActiveJobSeedRoundTripTests` at the real work centres that now carry the seven
  prepared situations, driving the same `ActiveJobItemResolverService` deserializer, and assert the seven `900-*`
  names resolve to **no** active job — the fixtures must be gone from the data, not merely from the seed file.
  **Write it to fail first**, against today's seed. ·
  `MTM_Waitlist.Tests/Module_Setup/Services/ActiveJobSeedRoundTripTests.cs`
- [x] **T120a** [US1] (2026-09-14) `ActiveJobSeedRoundTripTests` now **writes its own seven fixture rows** under a
  per-run `IT-ROUNDTRIP-*` work centre and deletes them in teardown, so the matrix no longer reads the deployed
  seed. The re-point T120 landed was not survivable in practice: work centre `100-3` had already been overwritten
  by a live Setup save for `WO-060954`, which made `Seeded100_3_CoilOnly_NormalisesToCoil` red for a reason that
  was not a defect. The shapes asserted are unchanged (the same seven situations through the same deserializer),
  and `Retired900Fixtures_ResolveToNoActiveJob` still reads live rows because its subject is an absence. ·
  `MTM_Waitlist.Tests/Module_Setup/Services/ActiveJobSeedRoundTripTests.cs`
- [x] **T121** [P] [US1] Add the raisability check (FR-035): every in-scope Item with `requires_answer = 1` and
  `answer_value_type = 'enum'` yields at least one choice, resolved from the configuration row and the job snapshot
  — **never** from an Item's identity (FR-013). **Write it to fail first**; it must name `pickup-component` in the
  failure. · `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigurationServiceTests.cs`
- [x] **T122** [P] [US2] Add the transition-guard test (FR-043): an accept applied to a request that is no longer
  `Pending` reports zero affected rows and does **not** reassign the handler — the assertion the race outcome rests
  on, made at the service/procedure seam rather than in the UI. **Write it to fail first**; today
  `sp_waitlist_request_status_update` matches on `public_id` alone. ·
  `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs`

### Implementation

**Wave 1 — the sign-out service (single task; adds the service and its contract):**

- [x] **T123** [US6] Add `ISignOutService` and `SignOutService`: reset the remembered-credential and session keys the
  sign-in path reads, then relaunch the process through the path `StartupRecoveryService.CorruptAndRestartAsync`
  already uses (`Environment.ProcessPath`, the `dotnet`-hosted case included) and exit — or `AppInstance.Restart`,
  which Microsoft Learn documents for a packaged **or unpackaged** desktop app. A relaunch that cannot be performed
  is reported in plain language so the person is never left apparently signed in (FR-026, FR-034). · new
  `MTM_Waitlist.Core/Contracts/Services/ISignOutService.cs`, new
  `MTM_Waitlist.Startup/Services/SignOutService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the badge control (single task; four files, one owner):**

- [x] **T124** [US6] Put **Sign out** on the badge: give `ShellPage_CurrentUserButton` (`CurrentUserButton`) a
  `MenuFlyout` in `Module_Core/Views/ShellPage.xaml`, add the command and the `ISignOutService` injection to
  `ViewModels/ShellViewModel.cs`, register the service in
  `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, and add the localized label and tooltip through
  the resource mechanism (FR-022). Remember the `[RelayCommand]` `Async`-stripping rule when the command is named,
  and keep the tooltip's `AssociatedFiles` list true to the files the control actually touches. ·
  `Module_Core/Views/ShellPage.xaml`, `ViewModels/ShellViewModel.cs`,
  `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the prepared situations move to real work centres (single task; the seed's own owner):**

- [x] **T125** [US1] Re-point `seed_setup_active_jobs_eight_configurations`: five of the seven situations onto Expo
  Drive work centres and two onto Vits Drive work centres (`100-3`, `100-6` … `100-28`, `100-1806`, `100-1807`; and
  `V100-33`, `V100-34`, `V100-35`), keeping each situation's job shape and the exact `subordinate_parts_json` shape
  `sp_setup_save_setup` writes. Record which five and which two in the seed's header comment, as that seed already
  records its own matrix. The eighth case stays an absent job. Paired rollback in the same change. ·
  `Database/Seeds/seed_setup_active_jobs_eight_configurations/create.sql`,
  `Database/Seeds/seed_setup_active_jobs_eight_configurations/rollback.sql`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the fixture work centres retire (single task; one owner for the master list):**

- [x] **T126** [US1] Retire `Database/Seeds/seed_setup_work_centers_fixture_stations/{create,rollback}.sql` — with
  T125 landed, no job points at `900-1` … `900-7` any more — and remove its entry from `Database/Seeds/AllSeeds.sql`
  and its description from `Database/Bootstrap/update_table_descriptions.sql` if it names them, so the lists agree
  with the files on disk (FR-025, constitution III). · delete
  `Database/Seeds/seed_setup_work_centers_fixture_stations/create.sql`, delete
  `Database/Seeds/seed_setup_work_centers_fixture_stations/rollback.sql`, `Database/Seeds/AllSeeds.sql`,
  `Database/Bootstrap/update_table_descriptions.sql`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the `pickup-component` repair (single task; the details step's own owner):**

- [x] **T127** [US1] Make `pickup-component` raisable (FR-035) by the direction `plan.md` §F4 records: give the
  enumerated answer the choices it needs through the job-derived path the seed's own comment names
  (`subordinate_parts_json → category='Component'`), keyed on the configuration row's `answer_value_type` and the
  field's declared `source` — **never** on the Item's identity (FR-013). If the configuration row itself must change,
  ship its paired rollback and keep `AllSeeds.sql` in sync. ·
  `MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs`,
  `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDetailsViewModel.cs`,
  `Database/Seeds/seed_waitlist_request_item_configs/create.sql`, `Database/Seeds/AllSeeds.sql`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — the transition guard (single task; the procedure's own owner):**

- [x] **T128** [US2] Make the transition conditional on the state the handler saw (FR-043):
  `sp_waitlist_request_status_update` (or a dedicated accept procedure) updates only while the row is still in the
  state that was read, so a second handler's accept reports zero affected rows instead of overwriting the first
  assignment, and the count reaches the caller through the non-query seam so the losing handler is told
  (FR-026, constitution III). Paired rollback in the same change, and the caller updated in the same pass. ·
  `Database/StoredProcedures/sp_waitlist_request_status_update/create.sql`,
  `Database/StoredProcedures/sp_waitlist_request_status_update/rollback.sql`,
  `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the population is raised (single task; one person, nineteen requests):**

- [ ] **T129** [US1] Raise the nineteen requests through the application, one per in-scope Item, signed in as the ten
  accounts that already exist — **no new account** — with the owner's weighting (ordinary floor people plus a couple
  of leads) and every one of the eight roles raising at least one, so each kind of login opens onto a populated list
  (FR-036, FR-037). Spread them across more work centres than the seven that carry the prepared situations, and raise
  every material-dependent Item from a work centre whose job actually carries that material (FR-038). Leave all
  nineteen unhelped (FR-040). Evidence: the nineteen rows read back with their `category`, `item`, `status` and
  requester. · `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`

**⟶ Wait for Wave 7 to finish, then:**

**Wave 8 — the seven handling outcomes, independent where the artifact differs:**

- [ ] **T130** [P] [US2] Outcome 1 — accept, then finish; and outcome 2 — accept, hand back, accept again, finish.
  Record both, including that a hand-back leaves **one** request, back to `Pending`, with `released_utc` set and the
  handler cleared, and that the second accept completes it (FR-042). · the running app
- [ ] **T131** [P] [US2] Outcome 3 — the raiser cancels before anyone accepts, signed in as that request's own
  raiser, and confirm a non-raiser cannot. · the running app
- [x] **T132** [P] [US2] Outcome 4 — give one Item the **smallest allowance the minutes screen accepts** through the
  minutes screen's own write (`sp_waitlist_request_item_allotted_minutes_update`), raise that Item's request and let
  its deadline genuinely expire. **No back-dated `requested_utc` and no hand-edited `target_time_utc`** (FR-044) —
  record the allowance, the deadline and the moment the row read overdue. · the running app
- [ ] **T133** [P] [US2] Outcomes 5 and 6 — one handler hands back and a **different** handler takes it over; and a
  handler accepts a request **they raised themselves**, which must be **permitted** (FR-041). Prove the first
  handler's assignment is gone and the second handler's stands, then prove the self-accept completes. · the running app
- [x] **T134** [US2] Outcome 7 — the two-handler race: run **two copies of the application side by side**, both acting
  on the same request at the same moment (FR-043, SC-019), and record exactly one assignment, one `accepted_utc`, and
  the losing handler's plain-language report. Two ordinary instances of the same Debug build — no special harness. ·
  `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe` (two instances)

**⟶ Wait for Wave 8 to finish, then:**

**Wave 9 — the gates:**

- [x] **T135** **Build gate.** With no `MTM_Waitlist.exe` running, run
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and record it as
  **`0 Warning(s) 0 Error(s)`**. Carry the three traps: `WMC9999` is a masked real XAML error, `PRI175` / `PRI224`
  mean a running app or stale `*.pri`, and `[RelayCommand]` strips a trailing `Async`. · `MTM_Waitlist.sln`
- [x] **T136** **Test gate.** Run `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`
  and record **`Failed: 0`** *with* T118–T122 in place, not instead of them. Environment-gated tests stay
  `Assert.Inconclusive` and are recorded as skipped; no gate is weakened to go green. ·
  `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [ ] **T137** **Live database validation.** Apply and reverse every paired `create.sql` / `rollback.sql` the batch
  touches against a local `mtm_waitlist`, confirm `AllSeeds.sql` and `Bootstrap/update_table_descriptions.sql` agree
  with the files on disk, and confirm the seven prepared situations now resolve from real work centres. **The
  reinstall is the owner's action, never the agent's.** · `Database/**`
- [x] **T138** **Sign-out gate.** On a computer where "remember me" is set and the session had been restored, choose
  Sign out from the badge and prove the application **restarts to the sign-in screen** rather than restoring the
  session. The displayed name alone is not evidence (FR-034, SC-013). · the running app
- [ ] **T139** **Validate against Success Criteria.** Walk SC-013 … SC-020 against the recorded evidence — SC-013 on
  T138, the population on T129, the outcomes on T130–T134, and the build/test criteria on T135–T136, so **do not run
  the suites a second time**. Confirm SC-020 last: the nineteen requests and the ten accounts are still present and
  browsable (FR-045). Any criterion without recorded evidence stays unticked and is reported, never asserted. ·
  `specs/004-unified-card-item-picker/spec.md`

**Checkpoint — the follow-up batch is complete.** Sign out ends the session by restart rather than by hiding it; the
nineteen requests exist, raised by the ten existing people with every role represented, and are left waiting; the
seven fixture work centres are gone and their situations live on real work centres; and all seven handling outcomes
have been exercised, including the two-copy race, with the overdue case a real expiry. SC-013 … SC-020 are provable
from this phase alone.

---

## Phase 10: Second Follow-Up Batch — Attribution and Give Back

This phase follows the Phase 9 walk, which left six of its tasks unticked. The walk surfaced **two defects** that
stand between it and its own checkpoint, and both are fixed here rather than worked around: a request is not
attributed to the person who raised it, and a claimed request can never be given back. Everything here is new work
against the appended requirements **FR-046** and **FR-047** and criterion **SC-021**; nothing above is reopened, and
`.spec-context.json` is not this phase's to edit. The same four gates apply, and the three build traps carry over.

The `[US#]` tags map the batch onto the stories it serves: **US1** owns raising a request, **US2** owns the card's
action surface.

### Tests — write these first; they must fail before the implementation lands

- [x] **T140** [P] [US1] Re-point the identity rule's tests at the **directory row** rather than at a literal
  (FR-046): the rule accepts the signed-in employee from the record the lookup returned — proved with an account
  that is **not** `6229`, so a name that is not `John Koll` has to come out — refuses an employee number the
  directory holds no row for, refuses a row that is known but inactive, and refuses a blank number. **Write it to
  fail first.** · `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs`
- [x] **T141** [P] [US1] Add the attribution test at the wizard's first step (FR-046, SC-021): with the directory
  holding the signed-in `9004` / `Sam Reyes`, selecting an active work centre attributes the request to **that**
  person — the negative is the point, because a step that names anyone else is the defect — and a signed-in person
  the directory does not account for is blocked in plain language without navigating (FR-026). **Write it to fail
  first**; today the step writes `6229 John Koll` whoever is signed in. ·
  `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestWorkCenterViewModelTests.cs`
- [x] **T142** [P] [US2] Add the give-back surface tests (FR-047): the assignee is offered **Give back** carrying the
  view model's actual generated release command — the command **name** is read from the view model, never assumed —
  and hidden from every viewer the gate excludes, `OfferedActionCount` equals the buttons the row actually offers,
  and executing it reaches `ReleaseAsync` exactly once as the signed-in handler; plus the markup check that the card
  draws a Release button whose gate, command, accessible name and tooltip all come from the row (contract C7).
  **Write it to fail first.** · `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests.cs`,
  `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`

### Implementation

**Wave 1 — the employee lookup (single task; adds the artifact and its procedure):**

- [x] **T143** [US1] Add the real employee lookup the attribution stands on (FR-046, constitution III): a paired
  `Database/StoredProcedures/sp_core_employee_by_identifier_get/{create,rollback}.sql` matching `core_users_profiles`
  on **`employee_identifier`** — not on a name — returning the stored identifier, display name and active state, with
  its block appended to `Database/StoredProcedures/AllSPs.sql`; and the service that reads it
  (`IEmployeeDirectoryService` + its implementation) over the non-query seam's read half. An unreachable store is
  reported rather than treated as "no such employee" (FR-026). ·
  new `Database/StoredProcedures/sp_core_employee_by_identifier_get/create.sql`, new
  `.../rollback.sql`, `Database/StoredProcedures/AllSPs.sql`, new
  `MTM_Waitlist.Core/Contracts/Services/IEmployeeDirectoryService.cs`, new
  `MTM_Waitlist.Core/Models/EmployeeIdentity.cs`, new
  `MTM_Waitlist.Waitlist.View/Services/EmployeeDirectoryService.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the rule and its only caller (single task; one owner for the rule and the call site):**

- [x] **T144** [US1] Replace the literal with the signed-in person (FR-046): `NewRequestFlowRules.VerifyEmployeeIdentity`
  takes the signed-in number and the directory's row for it, and keeps the refusal — a number with no active record is
  still refused rather than accepted — while `NewRequestWorkCenterViewModel.SelectWorkCenter` resolves the signed-in
  identity from `StartupState`, looks it up, and attributes the request from the answer; the directory is registered in
  the DI host. · `MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs`,
  `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestWorkCenterViewModel.cs`,
  `Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the give-back surface on the row (single task; row, list view model and label):**

- [x] **T145** [US2] Give the row its Give back action (FR-047): `SampleOrder` carries the command and the localized
  label, and `OfferedActionCount` counts the buttons the row offers rather than the flags it holds, so the count and
  the drawn controls cannot disagree; `WaitlistViewViewModel.ApplyHandlerActionStateAsync` sets both from the gate
  that already governs Complete/Release; the label and its tooltip go through the resource mechanism (FR-022). ·
  `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`,
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the card control (single task; the card's own owner):**

- [x] **T146** [US2] Draw Give back on the card (FR-047, contract C7): a third 44×44 icon button in the action area,
  named and described by the row's own action text, distinguishable from Complete and Cancel by both colour and glyph
  (the glyph is checked against the Segoe Fluent Icons list, and the deprecated `E0`–`E5` range is not used). ·
  `Module_Waitlist/Controls/WaitlistLineCardView.xaml`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the supersession, written down (single task; the documents that currently contradict it):**

- [x] **T147** [US2] Record the supersession rather than leaving the contradiction (FR-047): amend
  `specs/003-waitlist-handler-fulfilment/contracts/action-contracts.md` C1 ("Release has no surface") and C7 ("Release
  — not drawn", and the two-button ceiling) in place, and amend the `ReleaseRequestAsync` remark in
  `WaitlistViewViewModel.cs` that says the same. The earlier decision is **superseded, not deleted**: the reason it
  was taken is kept and the new behaviour stated beside it. ·
  `specs/003-waitlist-handler-fulfilment/contracts/action-contracts.md`,
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — the specification stays current (single task; the two documents this batch was planned in):**

- [x] **T148** [US1] Record what this batch changed in the specification and the plan (FR-046, FR-047, SC-021): append
  the two requirements and the criterion to `spec.md` without renumbering anything, and add the design decisions to
  `plan.md` — the employee-lookup source (`core_users_profiles.employee_identifier`, compared as the stored
  identifier), why the literal had to go, and the supersession the give-back records — including that the eight test
  accounts take `1111` as their first-use password change. ·
  `specs/004-unified-card-item-picker/spec.md`, `specs/004-unified-card-item-picker/plan.md`

**⟶ Wait for Wave 6 to finish, then:**

**Wave 7 — the gates:**

- [x] **T149** **Build gate.** With no `MTM_Waitlist.exe` running, run
  `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and record it as
  **`0 Warning(s) 0 Error(s)`**. · `MTM_Waitlist.sln`
- [x] **T150** **Test gate.** Clear the ambient `MTM_*` environment variables, then run
  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` and record
  **`Failed: 0`** *with* T140–T142 in place, not instead of them. Environment-gated tests stay
  `Assert.Inconclusive` and are recorded as skipped; no gate is weakened to go green. ·
  `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [x] **T151** **Live database validation.** Apply and reverse the new procedure pair against a local
  `mtm_waitlist`, confirm the lookup resolves the ten existing identifiers and returns nothing for a number no
  account holds, and confirm `AllSPs.sql` agrees with the files on disk. **The reinstall is the owner's action,
  never the agent's.** · `Database/StoredProcedures/**`

**Checkpoint — the two defects are gone.** A request carries the name of the person who raised it, resolved from that
person's own stored employee record, and a number no active record accounts for is still refused; a handler can give a
claimed request back to the queue from its card, which supersedes the earlier decision that releasing keeps no screen
control. FR-046, FR-047 and SC-021 are provable from this phase alone, and Phase 9's six unticked tasks can be walked
again.

---

## Phase 11: Third Follow-Up Batch — The Operator Picks the Dunnage

This phase follows Phase 10. The dunnage Items were seeded as *read the part out of the job and ask nothing*, which is
what the CSV row spec said at the time — while `OPEN-WORK-NEXT-SPEC.md` §5.3 said the opposite, that the operator
always selects the part. The owner settled it: **the operator picks**, from the dunnage the job carries, with a control
that opens the receiving catalogue so a part the job does **not** carry can be used as a **substitute**. Everything
here is new work against the appended requirements **FR-048 … FR-051** and criteria **SC-022** and **SC-023**; nothing
above is reopened, and `.spec-context.json` is not this phase's to edit. The same four gates apply, and the three
build traps carry over.

The `[US#]` tags map the batch onto the stories it serves: **US1** owns raising a request, **US2** owns the card's
identifier.

### Tests — write these first; they must fail before the implementation lands

- [x] **T152** [P] [US1] Add the dunnage step's tests (FR-048, FR-049): the step offers every part the job carries,
nothing is pre-chosen, clicking a card captures that part as the request's answer and continues, a substitute
captures a part the job does not carry and continues exactly the same way, a **dismissed** picker captures nothing
and does not advance, a picker that **cannot open** is reported in plain language while the job's own cards stay
usable, a job with no dunnage reports why the step is empty, and returning to the step re-shows a substitute as the
chosen card without duplicating an assigned one. · new
`MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestDunnageViewModelTests.cs`
- [x] **T153** [P] [US1] Add the routing tests (FR-050, SC-023): a row whose answer field names the `dunnage` list
reaches the dunnage step, a row naming the `component` list still reaches Details, and a captured answer still goes
straight to Preview — with the fixture row deliberately **not** one of the two dunnage Items, so the step cannot be
chosen by an Item code. · `MTM_Waitlist.Tests/Module_Waitlist/Models/NewRequestFlowStateTests.cs`
- [x] **T154** [P] [US1] Add the named-job-list tests (FR-035, FR-050): a field naming `dunnage` takes the job's
assigned dunnage part numbers in the job's order, one naming `component` takes the components and not the dunnage,
one naming nothing keeps the component list the existing rows rely on, and a name nothing supplies yields **no**
choices rather than a plausible list. Include the source-side check that both shipped dunnage rows ask for an answer
and name the `dunnage` list. ·
`MTM_Waitlist.Tests/Module_Settings/Services/RequestItemConfigurationServiceTests.cs`
- [x] **T155** [P] [US2] Add the card tests (FR-051): a dunnage request carrying its part resolves that part as
Line 2 through the template's `{dunnage_part}` token; a request that stored nothing shows the Item's display name
with the configuration problem reported; and a captured answer is **not** handed to the dunnage token of an Item
whose template does not ask for it. · `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs`

### Implementation

**Wave 1 — the job's dunnage parts reach the wizard (single task; the Settings snapshot gains a member):**

- [x] **T156** [US1] Carry the job's assigned dunnage parts onto the availability snapshot (FR-048): a
`RequestDunnagePart` value in `MTM_Waitlist.Settings`, a `DunnageParts` member and `WithDunnageParts` on
`RequestJobPartAvailability` beside `ComponentPartNumbers`, and the mapping at the composition root —
`RequestJobAvailabilityProvider` — which is the only place that may see the Setup-side part and resolve each part's
picture to an absolute path. · `MTM_Waitlist.Settings/Models/RequestDunnagePart.cs`,
`MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs`, `Services/RequestJobAvailabilityProvider.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the named job list (single task; one file owns the resolver):**

- [x] **T157** [US1] Make the enumerated answer's choices name their job list (FR-035, FR-050): add the optional
`list` key to `RequestItemFieldDefinition` with its two names, read it in the configuration parser, resolve it in
`RequestItemAnswerOptionsResolver` — named list first, no name keeps today's component default, an unknown name
yields nothing — and expose `DeclaredJobListName` for the flow to read. ·
`MTM_Waitlist.Settings/Models/RequestItemFieldDefinition.cs`,
`MTM_Waitlist.Settings/Services/RequestItemConfigurationService.cs`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the step itself, independent (different files):**

- [x] **T158** [P] [US1] Build the dunnage step: `NewRequestDunnageViewModel` binds the job's parts as picture
cards, captures the chosen part as the request's answer and continues, refuses to continue without one, offers the
substitute through `IDunnageSubstitutePicker` — a dismissed picker changes nothing, a part with no number is refused,
a failure is reported in plain language — and remembers a substitute so returning to the step still shows it.
`NewRequestFlowState` carries that remembered substitute. ·
`MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestDunnageViewModel.cs`,
`MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestDunnageOption.cs`,
`MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestFlowState.cs`
- [x] **T159** [P] [US1] Draw the step: `NewRequestDunnagePage` reuses the wizard's existing page shells and its
image-card tile shape — `SetupPageGridStyle`, `SetupPageHeaderStyle`, `SetupActionBarStyle`, the 150-wide tile inside
`ItemsWrapGrid`, `ResolvedImagePathToSourceConverter`, the accent selection outline — and adds the `Use substitute…`
control. Every new user-visible string comes from `Strings/en-us/Resources.resw` (FR-022). · new
`Module_Waitlist/Views/NewRequestDunnagePage.xaml`, `…/NewRequestDunnagePage.xaml.cs`, `Strings/en-us/Resources.resw`
- [x] **T160** [US1] Route it and register it: `NewRequestFlowRules.GetNextStepType` sends an answer that names the
`dunnage` list to the new step, and the composition root gains the substitute bridge — `IDunnageSubstitutePicker`
with `DunnageSubstitutePicker`, which shows the **existing** `SetupDunnageImageSearchDialog` through
`ISetupDialogService` and maps the part it returns, so no second dunnage picker exists (FR-049). ·
`MTM_Waitlist.Waitlist.NewRequest/Services/NewRequestFlowRules.cs`,
`MTM_Waitlist.Settings/Services/IDunnageSubstitutePicker.cs`, `Services/DunnageSubstitutePicker.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the card reads it back (single task; the title resolver owns the rule):**

- [x] **T161** [US2] Resolve the dunnage identifier from what the request stored (FR-051): the captured answer
fills the `{dunnage_part}` token for the Items whose template asks for it, and nothing else changes — the rule moves
beside the resolver as `WaitlistRequestTitles.ResolveContext`, and the view model delegates to it. ·
`MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — the data and the requirement of record (single task; the seed and its master move together):**

- [x] **T162** [US1] Seed both dunnage Items as an operator choice (FR-048, FR-050):
`collect-input-then-confirm`, `requires_answer = 1`, `answer_value_type = 'enum'`, the prompt, and an
`enum`/`answer` field naming the `dunnage` list — with `options_json` left NULL because the values arrive with the
job snapshot, exactly as `pickup-component`'s do. The spreadsheet's own two rows and the contracts move in the same
change, so the row spec, the seed and the code cannot disagree. ·
`Database/Seeds/seed_waitlist_request_item_configs/create.sql`, `Database/Seeds/AllSeeds.sql`,
`WeekendProject/Documents/Request-Config-Template.csv`, `specs/004-unified-card-item-picker/spec.md`,
`specs/004-unified-card-item-picker/contracts/request-picker-flow.md`,
`specs/004-unified-card-item-picker/contracts/item-configuration.md`,
`specs/004-unified-card-item-picker/contracts/card-and-identifier.md`,
`specs/004-unified-card-item-picker/data-model.md`,
`WeekendProject/OPEN-WORK-NEXT-SPEC.md`

**⟶ Wait for Wave 5 to finish, then:**

**Wave 6 — the gates:**

- [x] **T163** **Build and tests.** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1
/nodeReuse:false` → `0 Error(s)`; `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug
-p:Platform=x64` → `Failed: 0`. Evidence recorded: build succeeded, `total: 1060, failed: 0, succeeded: 1033,
skipped: 27`. · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [ ] **T164** **UI gate.** In the running app, raise a request for a job that carries dunnage and prove, from a
text/geometry dump rather than from the code: the dunnage step appears between Item and Preview; the job's parts
are drawn as cards; Continue is reachable only after a part is chosen; `Use substitute…` opens the receiving
catalogue and a part chosen there becomes the request's value; and the raised request's card shows the chosen part
on its second line. Needs a signed-in session and a job carrying dunnage. ·
`bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`
- [ ] **T165** **Live database validation.** Re-run the configuration seed against a local `mtm_waitlist` and read
both dunnage rows back with their `list` key intact, confirming `AllSeeds.sql` agrees with the file on disk.
**The reinstall is the owner's action, never the agent's.** · `Database/Seeds/**`

**Checkpoint — the operator picks the dunnage, and the card says which.** A dunnage request cannot be raised without a
part being chosen; the parts offered are the ones the job carries; a substitute from the receiving catalogue is a
first-class answer; and the card shows whichever part the operator ended on. FR-048 … FR-051 and SC-022/SC-023 are
provable from this phase's tests alone, with the UI walk and the seed reinstall the two gates still open.

---

## Phase 12: Fourth Follow-Up Batch — The Die Card Says Which Part, Which Die, and Where

This phase follows Phase 11, and it started from the same discovery: **a die card was broken in exactly the way a
dunnage card was.** `pickup-die`'s identifier template resolved `{die_number}` / `{die_location}`, but nothing ever
filled either value — the card context carried only the captured answer — so a die card rendered the word `Die` and
the configuration-problem report, never a number and never a location. The owner settled what the two lines should
say: **Line 1** the Item's own phrase plus the requesting **job's part number**, **Line 2** the die's own number and
where the die is. Everything here is new work against **FR-052**, **FR-053**, the amended **FR-005** and criterion
**SC-024**; nothing above is reopened.

Three decisions were taken by the agent because the owner was unavailable, and each is the conservative one:

- **Line 1's part number is the *job's*, and it is a token of its own** (`{job_part_number}`). Reusing
  `{part_number}` would have been wrong: that token means the *subordinate's* number for the coil and flatstock
  Items, so a die's "which part is this die for" value must not borrow it. This is the FR-053 record of that call.
- **The die values are re-read from the job** rather than stored on the request. `pickup-die` already uses its one
  stored answer for the destination, and `OPEN-WORK-NEXT-SPEC.md` §5.4 asks for exactly this re-resolution.
- **The destination question stays.** It is in the row spec and it tells the handler where to take the die; it
  simply no longer shapes the card.

### Tests

- [x] **T166** [P] [US2] Re-point the die card tests at the new lines (FR-052, FR-053): both die Items resolve
`FGT0002000-DIE SHOP` from the job snapshot; the destination no longer changes the identifier, which is the
supersession written as an assertion; Line 1 reads `Pickup Die: PART-9003` / `Deliver Die: PART-9003`; a die request
with no job degrades to the phrase rather than a blank or a bare template; and the job values are handed over **only**
to the Items whose templates name their tokens. · `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs`
- [x] **T167** [P] [US2] Update the catalog's own assertions (FR-052, FR-053): the die Items carry
`Pickup Die:` / `Deliver Die:` as their phrase, `{umbrella} {job_part_number}` as their first line and
`{die_number}-{die_location}` as their identifier — replacing the assertion that pinned the retired conditional, and
moving the plain-Category-word check off `pickup-die` onto `pickup-coil`. ·
`MTM_Waitlist.Tests/Module_Settings/RequestItemCatalogTests.cs`

### Implementation

**Wave 1 — the first line becomes a template (single task; one file owns the resolver):**

- [x] **T168** [US2] Give an Item a first line that can name a job value (FR-005, FR-052): `CardLine1Template` on the
Item definition with `{umbrella}` as the phrase token, the resolver's walk extracted so both lines share one
implementation, `ResolveLine1` degrading to the phrase instead of reporting, and the `{job_part_number}` token added
to the closed set. · `MTM_Waitlist.Settings/Models/RequestItemDefinition.cs`,
`MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the job's identity reaches the snapshot, independent (different files):**

- [x] **T169** [P] [US2] Carry the job's own part number and its primary die's number and location on the wizard's
availability snapshot (FR-053), and map them at the composition root, where the Setup-side part is visible. ·
`MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs`, `Services/RequestJobAvailabilityProvider.cs`
- [x] **T170** [P] [US2] Give both die Items their two lines (FR-052, FR-053): the phrase, the Line 1 template and
the identifier, with the phrases resolved through the resource mechanism like the two wrong-material Items already
are. · `MTM_Waitlist.Settings/Models/RequestItemCatalog.cs`, `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — the card reads the job (single task; the list view model gains a dependency):**

- [x] **T171** [US2] Fill the job tokens a template names, from the requesting job (FR-053): the card's context is
built from the request **and** the job snapshot, the job is read **once per work centre per load** and cached for it,
a job that cannot be read is logged rather than failing the list, and the row builder takes the snapshot as an
optional argument so the existing one-argument call sites keep their behaviour. ·
`MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`,
`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the gates:**

- [x] **T172** **The requirement of record, in the same change** (FR-027's discipline, applied to the docs): the two
die rows in the spreadsheet, the card-and-identifier contract's Line 1 and Line 2 sections and its token set, the
picker-flow contract's two die rows plus §10 for where a card's job values come from, the failure table, the data
model's Item table and the spec's FR-005/FR-052/FR-053/SC-024 all move together, so the row spec, the contracts and
the code cannot disagree. · `WeekendProject/Documents/Request-Config-Template.csv`,
`specs/004-unified-card-item-picker/{spec.md,data-model.md}`,
`specs/004-unified-card-item-picker/contracts/{card-and-identifier.md,request-picker-flow.md}`
- [x] **T173** **Build and tests.** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1
/nodeReuse:false` → `0 Error(s)`; `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug
-p:Platform=x64` → `Failed: 0`. Evidence recorded: build succeeded, `total: 1064, failed: 0, succeeded: 1037,
skipped: 27`. · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [ ] **T174** **UI gate.** In the running app, raise a pickup-die and a deliver-die request from a work centre whose
job carries a die (the seed's `100-7`), and prove from a text dump rather than from the code: Line 1 reads
`Pickup Die: <the job's part number>` / `Deliver Die: <the job's part number>`, and Line 2 reads
`<FGT number>-<die location>`. Then move the die's location in the store and reload, to prove the card follows the job
rather than a stale copy. · `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`
- [ ] **T175** **Live database validation.** No schema change here, so this is the read path: confirm
`sp_setup_active_jobs_latest_by_work_center_get` returns the job's `part_number` and the die row's `PartNumber` /
`Location` for a real active job, so the two lines have something to resolve against outside the seed. · local
`mtm_waitlist`

**Checkpoint — a die card says which part, which die, and where it is.** Both die Items render a first line naming the
requesting job's part number and an identifier carrying the die's own number and location, read from the job; the
destination answer no longer changes either line; and an Item that names none of these tokens is provably untouched.
FR-052, FR-053, the amended FR-005 and SC-024 are provable from this phase's tests alone, with the UI walk and the
live read the two gates still open.

---

## Phase 13: Fifth Follow-Up Batch — What Counts as a Die, and One Request per Die

The batch that a live job forced. `100-3` carries a die row described `No Die` with an empty location — the query's
way of saying "this job has no die" — yet it was counted as a die, offered to the operator, and rendered as a
dangling separator. So this phase settles what a die **is** (FR-055), how a die is **written** (FR-056), and what a
request for a die shows on the **request page** as well as the card (FR-052, FR-053); then it builds the step that
lets an operator choose among several dies and raises one request for each (FR-054).

### Wave 1 — What a die is, and how it is written

- [x] **T176** [P] [US2] **Settle what a die is** (FR-055, SC-025): one definition, `DieDecisionRules` in
  `MTM_Waitlist.Core` beside `ScrapDecisionRules`, whose `HasRealDie(partNumber, description)` refuses a row carrying
  the `No Die` placeholder and refuses a row with no part number to identify it by. The snapshot exposes the job's
  `RealDies`, the availability's `HasDie` is derived from that list rather than from the row count, and the picker's
  existing `RequestJobPartKind.Die` gate then withholds the die Items from a job that has no die. The Setup workflow
  reads the same rule, so the picker and the setup screens cannot disagree. · `MTM_Waitlist.Core/Services/DieDecisionRules.cs`,
  `MTM_Waitlist.Setup/Models/SetupModels.cs`, `Services/RequestJobAvailabilityProvider.cs`,
  `MTM_Waitlist.Tests/Module_Core/Services/DieDecisionRulesTests.cs`,
  `MTM_Waitlist.Tests/Module_Settings/Services/RequestItemPickerRulesTests.cs`
- [x] **T177** [P] [US2] **Settle how a die is written** (FR-056, SC-026): the die's identifier is composed as its
  number and its location by one rule — `RequestDiePart.ComposeLabel`, which omits the separator when no location is
  known — and exposed to templates as the single `{die}` token, with `{die_number}` and `{die_location}` kept for a
  row that wants one half. Both die Items' Line 2 becomes `{die}`, and the card's context hands the die over when a
  template names the die **at all**, not only when it names the old two-token spelling. · `MTM_Waitlist.Settings/Models/RequestDiePart.cs`,
  `MTM_Waitlist.Settings/Models/RequestItemCatalog.cs`, `MTM_Waitlist.Settings/Services/RequestItemLine2Resolver.cs`,
  `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`, `MTM_Waitlist.Tests/Module_Settings/RequestItemCatalogTests.cs`,
  `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs`

### Wave 2 — The request page carries the job too

- [x] **T178** [US2] **Give the request page the requesting job** (FR-052, FR-053): the detail page built its row
  from the request alone, so the die and its location — which are **job** values the request never stored — could not
  appear anywhere on it. The page now reads the job once per work centre, hands it to `CreateSessionOrder` for both
  card lines, and resolves a declared field whose `source` is `job` through `RequestJobFieldValues`, which maps the
  declared label onto the job value and yields nothing for a label the job cannot supply — so no row is drawn empty
  and none is invented. · `MTM_Waitlist.Settings/Services/RequestJobFieldValues.cs`,
  `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`,
  `Services/DependencyInjection/ServiceRegistrationExtensions.cs`
- [x] **T179** **Build and tests.** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` →
  `0 Error(s)`; `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → `Failed: 0`.
  Evidence recorded: build succeeded, `total: 1089, failed: 0, succeeded: 1062, skipped: 27`. · `MTM_Waitlist.sln`,
  `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

### Wave 3 — One request per die (FR-054) — **BLOCKED, not built**

- [ ] **T180** [US1] **Decide where a chosen die is stored per request.** A die request stores exactly one free
  value: `waitlist_requests.input_value`, and for `pickup-die` that column already holds the operator's
  **destination** answer, which the request page renders as a declared `answer`-sourced field. A multi-die run must
  record, per entry, both **which die** and that destination, and one column cannot hold both. Verified against the
  live schema: the table has no die column and no second answer column. **Blocks T181–T183.** · local `mtm_waitlist`
- [ ] **T181** [US1] **Carry a per-entry die identifier** (FR-054): whichever store T180 chooses — the existing
  `input_value`, or a new column with the procedure pair and its `create.sql` / `rollback.sql` that a schema change
  requires — a raised die request must resolve *its own* die rather than the job's first one, which is what the card
  does today and what makes several entries from one job indistinguishable.
- [ ] **T182** [US1] **Build the die step** (FR-054): `NewRequestDieViewModel` + `NewRequestDiePage`, reached because
  the chosen Item's configuration names the `die` job list, offering the job's dies as selectable cards that may be
  selected **more than once**, refusing to continue with nothing selected.
- [ ] **T183** [US1] **Raise one request per selected die** (FR-054): the confirmation step submits one request for
  each die the operator selected — never one request carrying several dies — and reports how many were raised.
- [ ] **T184** **UI gate.** In the running app, raise a die request from the seed's `100-7` and prove from a text
  dump that the card and the request page both carry the die's number **and** its location, then do the same for a
  job carrying two dies and confirm one entry appears per die selected. · `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MTM_Waitlist.exe`
- [ ] **T185** **Live database validation.** Confirm the live `100-3` job offers **no** die Item — its only die row is
  the `No Die` placeholder — while `100-7` and `V100-33` do, and that the die values resolve from the live read
  rather than the seed. · local `mtm_waitlist`

**Checkpoint — half closed, and the open half named.** FR-055, FR-056, the amended FR-052/FR-053 and SC-025/SC-026
are provable from this phase's tests alone: a job whose only die row is the placeholder is offered no die Item, a die
with no known location renders as its number alone, and the request page carries the die and its location as the card
does. FR-054 is **not** built: it waits on T180's decision, because a die request has one value column and it is
already spent on the destination. Reporting this phase as complete would be false.

---

## Dependencies & Execution Order

**Phase dependencies.**

```text
Phase 1 Setup ──▶ Phase 2 Foundational ──▶ ┌ Phase 3 US1 (P1) ──┐
                                           ├ Phase 4 US2 (P1) ──┤
                                           ├ Phase 5 US3 (P2) ──┼──▶ Phase 8 Polish ──▶ Phase 9 Follow-Up Batch
                                           ├ Phase 6 US4 (P2) ──┤
                                           └ Phase 7 US5 (P3) ──┘                          │
                                                                                           ▼
                                                                    Phase 10 Attribution & Give Back
```

Every user story waits for Foundational and nothing else. **Do not begin any story before Phase 2's Checkpoint** —
the request's two columns, the configuration table and the availability snapshot are what each story stands on.

**Phase 9 follows the shipped build and waits on nothing above it.** Phases 1–8 are complete; Phase 9's work is new
and may start at once. Its own order is fixed only where a file demands it: sign out (T118–T124) → the prepared
situations re-pointed (T125) → the fixtures retired (T126) → `pickup-component` (T127) → the transition guard (T128)
→ the population (T129) → the seven outcomes (T130–T134) → the gates (T135–T139).

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
- **`Strings/en-us/Resources.resw` is one file touched by six phases** (T053, T071, T082, T099, T124, plus the
  removals in T053). It can never appear twice in one wave. If the stories are run in parallel, the resource edits
  are the single serialised resource — sequence them.

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
- **Phase 9 Follow-Up Batch** — tests first (T118–T122), then nine waves: the sign-out service (T123) → the badge
  control (T124) → the situations re-pointed (T125) → the fixtures retired (T126) → `pickup-component` (T127) → the
  transition guard (T128) → the population (T129) → the seven outcomes (T130–T134) → the gates (T135–T139).
  W3 must precede W4: the fixture stations cannot be deleted while jobs still point at them. W7 must precede W8:
  there is nothing to handle until the nineteen exist.
- **Phase 10 Attribution & Give Back** — tests first (T140–T142), then seven waves: the employee lookup (T143) → the
  rule and its call site (T144) → the give-back surface on the row (T145) → the card control (T146) → the
  supersession written down (T147) → the specification brought current (T148) → the gates (T149–T151). W1 must
  precede W2: the rule is given the directory's row, so the lookup has to exist first. W3 must precede W4: the card
  binds the row's command, so the row has to carry it first. Phase 10 waits on Phase 9 only in that its two defects
  are what Phase 9's own walk found.
- **Phase 11 Dunning Picker** — tests first (T152–T155), then five waves: the job's dunnage parts on the snapshot
  (T156) → the named job list (T157) → the step and its page (T158–T159) → routing and the substitute bridge (T160) →
  the card reading the answer back (T161) → the data and the row spec (T162) → the gates (T163–T165). W2 must precede
  W3: the flow cannot route on a list name the parser does not read — and that gap was real, found only by asserting
  on the shipped seed rather than on a fixture.
- **Phase 12 Die Lines** — tests first (T166–T167), then three waves: the first line as a template (T168) → the job's
  identity and the two die rows (T169–T170) → the card reading the job (T171) → the requirement of record and the
  gates (T172–T175). W1 must precede W3: the card cannot resolve a token the resolver does not know, and W2 must
  precede W3 for the same reason one level down. Phases 11 and 12 were prompted by the same class of defect — a
  job-derived token with no source — so **a card line that still renders a display name plus a problem report is the
  next thing to audit**, and the audit should name which tokens each Item's templates use against what the card is
  handed.
- **Phase 13 Die Identity** — the settled rules first (T176–T177, independent of each other), then the request page
  (T178) → the gates (T179) → the decision (T180) → the per-entry identifier (T181) → the step (T182) → one request
  per die (T183) → the gates (T184–T185). W1 precedes W2 because T178's row would otherwise resolve a die rule that
  is still counting placeholders. **W3 is blocked on W1's sibling decision, not on W2**: T180 asks which column holds
  a chosen die, and T181–T183 cannot be written until it is answered. Phase 13's first two waves came from the same
  audit Phase 12's closing note asked for, and they found what it predicted — a job offering a die it does not have,
  and a request page that never received the job its rows resolve against.

**Parallel opportunities.** Every wave of `[P]` tasks may be built concurrently — different files, no unfinished
dependency between them. The largest are Phase 2 W1 (six new stored artifacts), Phase 2 W5 (seven model files) and
Phase 2 W6 (four independent providers), the three of which are also the best candidates for fanning out to
subagents, because none of their members share a file. The three build traps to carry into any parallel run: the
XAML compiler races on its own intermediate files (**never build the solution in parallel** — `/m:1
/nodeReuse:false`), `WMC9999` is a real masked XAML error, and `[RelayCommand]` strips a trailing `Async`.

**Independent test points.** Each story's Checkpoint is a real stopping point: US1 closes SC-001/SC-005, US2 closes
SC-003, US3 closes SC-009, US4 closes SC-006, US5 closes SC-007/SC-008. Foundational alone closes nothing
user-visible — do not report it as shipped.
