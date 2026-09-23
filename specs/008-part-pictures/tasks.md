# Tasks: A Picture for Every Part

**Feature**: `008-part-pictures` · **Spec**: `spec.md` (38 functional requirements, 7 user stories, 10 success criteria)
**Plan**: `plan.md` · **Created**: 2026-09-23 · **Size**: oversized (45 files projected, 5 areas)

**Scale note.** This change spans the picture share, the store and its procedures, a part-picture layer in
`MTM_Waitlist.Shared` and `MTM_Waitlist.Settings`, eight surfaces and two Settings screens. Three things need watching
while it is built: the recorded-path move is a rewrite of every picture already stored and not a rename, the picture
entitlement widens while the storage controls must not widen with it, and the local copy rule changes shape for parts
only.

## Legend

- `- [ ] **T###** [P] [US#] Description · the exact file or files this task creates or edits`
- `[P]` marks a task independent of the others in its wave: a different file, no incomplete dependency. Same-file tasks
  and dependent tasks are never in one wave, so a wave's `[P]` tasks can be built in any order.
- `[US#]` maps a task to a user story in `spec.md`. Setup, Foundational and Polish tasks carry no story tag: they serve
  every story or none.
- A `**⟶ Wait for …**` line is a join. Everything below it needs the wave above it.
- Every file in this list has exactly one owning phase.

---

## Phase 1: Setup

Tooling and evidence prerequisites shared by everything. Nothing here changes product behaviour.

**Wave 1 — independent (different files):**

- [x] **T001** [P] Record the pre-change evidence baseline: the solution builds with zero warnings and zero errors, and
  the full suite's counts are known, so every later gate has something to compare against · `MTM_Waitlist.sln`,
  `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`
- [x] **T002** [P] Confirm the export's save picker against Microsoft Learn for the Windows App SDK version this project
  references, and record the verified call shape the export task will use (constitution IV) ·
  `specs/008-part-pictures/research.md`
- [x] **T003** [P] Fix the shape the new validation script will follow, by reading the existing one: what it asserts,
  how it reports, and what a reader checks it against · `Database/Validation/settings_schema/validate.sql`

---

## Phase 2: Foundational

The spine every story stands on: the store's two new scopes and their change record, the dictated layout with a reader
that understands both layouts, the part-picture reader, the shared card, and the Settings surfaces three stories need.
No story work begins until this phase is done.

Two notes on ownership, because two paths look shared and are not. `SettingsPage.xaml` and `SettingsViewModel.cs` carry
the part-picture entry point (US3), the storage folders and the retention period (US6) and the two dunnage lists (US5),
so per the one-owner rule they are built here, before any story starts; each story then owns the behaviour behind them.
`Styles/PartPictureCardTemplate.xaml` is here for the same reason: four lists consume it and one of them is in this
phase.

**Wave 1 — independent (different files):**

- [x] **T004** [P] Widen the `scope` column's comment to name the five values it now holds, leaving every column and
  `uq_config_images_locations_scope_item` untouched · `Database/Tables/17_config_images_locations/create.sql`,
  `Database/Tables/17_config_images_locations/rollback.sql`
- [x] **T005** [P] Create `config_images_locations_history`: nine columns, `uq_config_images_locations_history_public_id`,
  `idx_config_images_locations_history_scope_item`, and both `ON DELETE SET NULL` foreign keys · 
  `Database/Tables/33_config_images_locations_history/create.sql`,
  `Database/Tables/33_config_images_locations_history/rollback.sql`
- [x] **T006** [P] Widen the picture entitlement: `permission.settings.part_pictures` becomes 1 for `role:setup_lead` and
  `role:plant_manager`, `permission.settings.storage_paths` is left alone, and the rollback restores exactly those two
  rows · `Database/Seeds/seed_permission_role_baselines/create.sql`,
  `Database/Seeds/seed_permission_role_baselines/rollback.sql`
- [x] **T007** [P] Spell the three collections, the four family folders, the catch-all and the file-name rule once, and
  nothing else · `MTM_Waitlist.Shared/Helpers/PartPictureLayout.cs`
- [x] **T008** [P] Teach the storage paths both layouts: the three collection folders, the three kind folders inside the
  application's own collection, and a resolve that answers for a pre-move value and for a post-move one ·
  `MTM_Waitlist.Shared/Helpers/AppStoragePaths.cs`
- [x] **T009** [P] Model the two part systems, so a part picture is never the part number alone ·
  `MTM_Waitlist.Settings/Models/PartPictureSystem.cs`
- [x] **T010** [P] Declare the reader every surface calls, so the surfaces depend on the contract rather than on the
  resolver · `MTM_Waitlist.Core/Contracts/Services/IPartPictureResolver.cs`
- [x] **T011** [P] Write every new label, action, heading and refusal the feature needs, under the feature's own prefixes
  · `Strings/en-us/Resources.resw`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T012** [P] Read one part's change record newest first, with the actor's display name ·
  `Database/StoredProcedures/sp_config_images_locations_history_get/create.sql`,
  `Database/StoredProcedures/sp_config_images_locations_history_get/rollback.sql`
- [x] **T013** [P] Return every active picture path of one scope, for the collision check and for the missing-picture
  subtraction · `Database/StoredProcedures/sp_config_images_locations_scope_paths_get/create.sql`,
  `Database/StoredProcedures/sp_config_images_locations_scope_paths_get/rollback.sql`
- [x] **T014** [P] Write one history row in the same transaction as a first picture, with a null predecessor, signature
  and return value unchanged · `Database/StoredProcedures/sp_config_images_locations_insert/create.sql`,
  `Database/StoredProcedures/sp_config_images_locations_insert/rollback.sql`
- [x] **T015** [P] Write one history row in the same transaction as a replacement, carrying the path it replaced,
  signature unchanged · `Database/StoredProcedures/sp_config_images_locations_update/create.sql`,
  `Database/StoredProcedures/sp_config_images_locations_update/rollback.sql`
- [x] **T016** [P] Resolve system, part number and prefix to a family folder, a relative path, and the file a control
  draws: the machine's copy when there is one, otherwise the source, otherwise nothing ·
  `MTM_Waitlist.Settings/Services/PartPictureResolver.cs`
- [x] **T017** [P] Delegate the prefix-to-family mapping so one copy of it exists ·
  `MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T018** [P] The shared card: fluid bounds rather than a fixed tile, one picture rule, a selection outline, and the
  shared placeholder for an entry with no picture, registered so a converted list can use it ·
  `Styles/PartPictureCardTemplate.xaml`, `App.xaml`
- [x] **T019** [P] Register the resolver in the feature's own registration partial, so no phase edits the factory the
  other phases also edit · `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictures.cs`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T020** [P] The part-picture entry point on Settings, offered to an account holding the picture entitlement and
  absent for one without it · `Module_Settings/Views/SettingsPage.xaml`,
  `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`
- [x] **T021** [P] Rewrite every recorded picture path from the pre-move shape to the new one through the non-query
  seam, report the rows affected, stay safe to run twice, and ship its reverse; then have the paired seed rewrite each
  value from the same table of pairs and report, rather than silently skip, a value it does not recognise ·
  `Database/StoredProcedures/sp_config_images_locations_paths_move/create.sql`,
  `Database/StoredProcedures/sp_config_images_locations_paths_move/rollback.sql`,
  `Database/Seeds/seed_picture_layout_move/create.sql`, `Database/Seeds/seed_picture_layout_move/rollback.sql`

**⟶ Wait for Wave 4 to finish, then:**

- [x] **T022** [P] The picture folder and the key folder as one panel, with the retention period beside them and the
  whole panel on the storage entitlement · `Module_Settings/Views/SettingsPage.xaml`,
  `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`
- [x] **T023** [P] The run-once file move: takes the configured root, reports what it will move and what it moved,
  refuses to overwrite an existing file, leaves what it does not recognise where it is, and is never run from
  application startup · `tools/Move-PartPictureLayout.ps1`

**⟶ Wait for Wave 5 to finish, then:**

- [x] **T024** [P] Move the picture-cache panel onto the storage entitlement, so widening the picture entitlement cannot
  hand a Setup Lead the folder this machine copies pictures from · `Module_Settings/Views/SettingsPage.xaml`,
  `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`
- [x] **T025** [P] Apply the new table and the widened comment to a store that already exists, by a guarded
  `information_schema` check and a prepared `ALTER TABLE`, and to the fresh-store path ·
  `Database/Bootstrap/update_table_descriptions.sql`, `Database/Bootstrap/create_database.sql`

**⟶ Wait for Wave 6 to finish, then:**

- [x] **T026** [P] The two dunnage type lists become cards, each still showing and hiding its type ·
  `Module_Settings/Views/SettingsPage.xaml`, `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`
- [x] **T027** [P] Regenerate the three master lists from the tree · `Database/Tables/AllTables.sql`,
  `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`

**⟶ Wait for Wave 7 to finish, then:**

- [x] **T028** [P] Prove the five scope values, the new table's columns, keys and index, and that each new and changed
  procedure exists and answers · `Database/Validation/part_pictures_schema/validate.sql`

**Checkpoint.** The store holds the two part scopes and their change record, the layout and its both-layout reader
exist, the recorded paths have a rewrite and its reverse, and every story below can resolve a part's picture path.

---

## Phase 3: US1 — The part is in front of me (P1)

**Files:** `Module_Setup/Views/SetupReviewPage.xaml`, `Module_Setup/Views/SetupPartSelectionPage.xaml`,
`Module_Waitlist/Views/NewRequestDiePage.xaml`, `Module_Waitlist/Views/NewRequestSummaryPage.xaml`,
`Module_Waitlist/Views/NewRequestPreviewPage.xaml`, `Module_Waitlist/Views/NewRequestComponentPage.xaml`,
`Module_Waitlist/Controls/WaitlistLineCardView.xaml`,
`MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`,
`MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestComponentOption.cs`

**Goal:** every surface that names a part draws its picture, and a part with no picture draws the one shared
placeholder rather than a blank space.

**Independent test:** photograph one part, then open each surface that names it and confirm the picture is drawn, and
that a part with no picture draws the shared placeholder and never family artwork (spec US1, SC-001, SC-002).

### Tests

**Wave T1 — independent (different files):**

- [x] **T029** [P] [US1] Pin the waitlist card: the picture resolves through the request's material part, and a material
  that cannot be pictured draws the shared placeholder and never the family's artwork ·
  `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests.cs`
- [x] **T030** [P] [US1] Pin the card model: the effective picture is the material part's picture when there is one, and
  the placeholder otherwise · `MTM_Waitlist.Tests/Module_Waitlist/Models/SampleOrderTests.cs`
- [x] **T031** [P] [US1] Pin the Setup review rows and the three wizard surfaces: each names the part's picture, and a
  surface with no placeholder path fails · `MTM_Waitlist.Tests/Module_Setup/Views/SetupReviewPageMarkupTests.cs`,
  `MTM_Waitlist.Tests/Module_Waitlist/Views/NewRequestPartPictureMarkupTests.cs`
- [x] **T032** [P] [US1] Pin the Setup part list: the entries carry each part's picture and choosing one still selects
  the part · `MTM_Waitlist.Tests/Module_Setup/Views/SetupPartSelectionPageMarkupTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T033** [P] [US1] The Setup review's subordinate-part rows carry the part's picture and stay grouped by family ·
  `Module_Setup/Views/SetupReviewPage.xaml`
- [x] **T034** [P] [US1] The die card draws the part's picture and is no longer text only ·
  `Module_Waitlist/Views/NewRequestDiePage.xaml`
- [x] **T035** [P] [US1] The confirmation step draws the part being asked for with its picture ·
  `Module_Waitlist/Views/NewRequestSummaryPage.xaml`
- [x] **T036** [P] [US1] The preview draws the same list the same way ·
  `Module_Waitlist/Views/NewRequestPreviewPage.xaml`
- [x] **T037** [P] [US1] The Setup part list's entries carry each part's picture and choosing one still selects the part
  · `Module_Setup/Views/SetupPartSelectionPage.xaml`
- [x] **T038** [P] [US1] The component card's picture becomes the part's picture, and the comment claiming the
  placeholder stands in "until part pictures exist" goes ·
  `MTM_Waitlist.Waitlist.NewRequest/Models/NewRequestComponentOption.cs`,
  `Module_Waitlist/Views/NewRequestComponentPage.xaml`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T039** [US1] The card model answers with the material part's picture, and with the placeholder when the material
  cannot be pictured · `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T040** [US1] The waitlist card draws the part's picture rather than the picture for the kind of request ·
  `Module_Waitlist/Controls/WaitlistLineCardView.xaml`

**Checkpoint.** US1 is independently functional: one photographed part is drawn on every surface that names it, and an
unpictured part draws the shared placeholder everywhere.

---

## Phase 4: US2 — A Visual part and a WIP part are pictured apart (P1)

**Files:** `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipInventoryPartNumbers.sql`,
`MTM_Waitlist.Shared/Helpers/ImageCachePaths.cs`, `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs`

**Goal:** the Visual part and the WIP part are two things with their own pictures and their own copies on this machine,
even where they share a number.

**Independent test:** give the same number a picture under Visual and a different picture under WIP, then open a surface
that shows each and confirm neither picture appears where the other belongs (spec US2, SC-003).

### Tests

**Wave T1 — independent (different files):**

- [x] **T041** [P] [US2] Pin the separation: with two rows sharing one number, each system's picture resolves for its
  own part only and the other system's part draws the placeholder ·
  `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureResolverSystemTests.cs`
- [x] **T042** [P] [US2] Pin the two part cache folders, so the same file name under Visual and under WIP cannot be one
  copy · `MTM_Waitlist.Tests/Module_Shared/Helpers/ImageCachePathsTests.cs`
- [x] **T043** [P] [US2] Pin the WIP read: the part numbers come from the new script through the existing seam and
  script-name convention · `MTM_Waitlist.Tests/Core/Services/WipFloorInventoryServiceTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T044** [P] [US2] The WIP floor's distinct part numbers as a script beside the existing floor-quantity script ·
  `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipInventoryPartNumbers.sql`
- [x] **T045** [P] [US2] One cache folder per part collection, beside the two folders mirrored today ·
  `MTM_Waitlist.Shared/Helpers/ImageCachePaths.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T046** [US2] Read the WIP part numbers through the existing seam, script-name pattern and store unchanged ·
  `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs`

**Checkpoint.** US2 is independently functional: a number shared by both systems is two parts to the application, and
their local copies can never be one file.

---

## Phase 5: US3 — A person who may set pictures sets them (P2)

**Files:** `MTM_Waitlist.Settings/Services/IPartPictureWriteService.cs`,
`MTM_Waitlist.Settings/Services/PartPictureWriteService.cs`,
`MTM_Waitlist.Settings/Services/PartPictureCoverageService.cs`,
`MTM_Waitlist.Settings/Models/PartPictureRow.cs`, `MTM_Waitlist.Settings/Models/PartPictureMissRow.cs`,
`MTM_Waitlist.Settings/Models/PartPictureSetResult.cs`,
`MTM_Waitlist.Settings/ViewModels/PartPictureManagerViewModel.cs`,
`Module_Settings/Views/PartPictureManagerPage.xaml`, `Module_Settings/Views/PartPictureManagerPage.xaml.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureScreen.cs`

**Goal:** an entitled account finds a part, sets or replaces its picture, sees the parts that still have none, exports
that list, and reads back who changed what and when; an unentitled account is not offered the section.

**Independent test:** as an entitled account, set a part's picture and see it appear on US1's surfaces; as an
unentitled account, confirm the section is not offered (spec US3, SC-008).

### Tests

**Wave T1 — independent (different files):**

- [x] **T047** [P] [US3] Pin the write path: a first save writes one history row with a null predecessor, a replacement
  writes one carrying the replaced path, and a refusal writes nothing ·
  `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureWriteServiceTests.cs`
- [x] **T048** [P] [US3] Pin the refusals: a number past the storage's limit is refused with the limit stated and never
  stored truncated, a name another part already holds is refused naming that part, and two numbers differing only by
  case are refused · `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureCollisionTests.cs`
- [x] **T049** [P] [US3] Pin the missing-picture list: the parts the application can name minus the pictured rows, and
  the shape of the exported rows · `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureCoverageServiceTests.cs`
- [x] **T050** [P] [US3] Pin the screen: finding a part, setting or replacing its picture, reading the change record
  back, and each refusal stated in the person's words ·
  `MTM_Waitlist.Tests/Module_Settings/ViewModels/PartPictureManagerViewModelTests.cs`
- [x] **T051** [P] [US3] The opt-in live checks: one history row on a first save, one on a replacement carrying what it
  replaced, the record read newest first, and the move reporting the rows it rewrote and reversing exactly ·
  `MTM_Waitlist.Tests/Module_Settings/ConfigImagesLocationsHistoryIntegrationTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T052** [P] [US3] The write contract: set, replace, refuse, and record ·
  `MTM_Waitlist.Settings/Services/IPartPictureWriteService.cs`
- [x] **T053** [P] [US3] One row of the picture screen · `MTM_Waitlist.Settings/Models/PartPictureRow.cs`
- [x] **T054** [P] [US3] One row of the missing-picture list, and the typed answer that lets a refusal name the part it
  collides with · `MTM_Waitlist.Settings/Models/PartPictureMissRow.cs`,
  `MTM_Waitlist.Settings/Models/PartPictureSetResult.cs`
- [x] **T055** [P] [US3] The screen's view model, on the existing picture-editor base ·
  `MTM_Waitlist.Settings/ViewModels/PartPictureManagerViewModel.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T056** [P] [US3] The write path: the storage limit's refusal, the collision refusal that names the part holding
  the name, the archive of the picture being replaced, and the history row ·
  `MTM_Waitlist.Settings/Services/PartPictureWriteService.cs`
- [x] **T057** [P] [US3] The parts with no picture and the rows the export writes ·
  `MTM_Waitlist.Settings/Services/PartPictureCoverageService.cs`
- [x] **T058** [P] [US3] The screen's markup and code-behind, reusing the existing editor control unchanged ·
  `Module_Settings/Views/PartPictureManagerPage.xaml`, `Module_Settings/Views/PartPictureManagerPage.xaml.cs`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T059** [P] [US3] The export through the platform save picker, writing the part number, the system, the family
  folder and the path the picture would be stored at · `Module_Settings/Views/PartPictureManagerPage.xaml.cs`
- [x] **T060** [P] [US3] Register the write service, the coverage service, the view model and the page route in the
  feature's own partial · `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureScreen.cs`

**Checkpoint.** US3 is independently functional: an entitled account sets and replaces pictures on the screen, reads
the change record back, and exports the missing-picture list; an unentitled account never sees the section.

---

## Phase 6: US4 — Pictures can be added without opening the application (P2)

**Files:** `FEATURES.md`

**Goal:** files named after their part are dropped into the right folder and are used the next time that part is drawn,
without the application having been used to add them.

**Independent test:** drop a correctly named file into the folder, then open a surface that names that part and confirm
its picture is drawn (spec US4).

The reader that makes this work is built in Phase 2 (T016) and the acceptance rule is the existing one, so this story's
own work is the operator-facing convention and its proof.

### Tests

**Wave T1 — independent (different files):**

- [x] **T061** [P] [US4] Pin the drop path: a file named exactly after a part resolves for that part with no recorded
  row, and a file the acceptance rule rejects is ignored rather than reported as a fault ·
  `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureFileDropTests.cs`
- [x] **T062** [P] [US4] Pin the drop isolation: the same file name placed in the other system's collection resolves for
  its own parts only · `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureFileDropIsolationTests.cs`

### Implementation

**Wave 1:**

- [x] **T063** [US4] Write the file-drop convention where an operator will find it: the three collections, the family
  folders, the name-is-the-part-number rule, and the extensions the application draws · `FEATURES.md`

**Checkpoint.** US4 is independently functional: a correctly named file placed by hand is drawn on the next render, and
a file the application cannot draw is ignored.

---

## Phase 7: US5 — The lists that ask by name show the part (P2)

**Files:** `Module_Waitlist/Views/NewRequestDetailsPage.xaml`, `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml`,
`Module_Core/Views/ShellPage.xaml`, `ViewModels/ShellViewModel.cs`

**Goal:** the lists that ask a person to choose by name offer their entries as selectable cards, and the building choice
becomes a flyout that leaves out the building currently in effect.

**Independent test:** open each converted list and choose an entry; confirm the choice still takes effect and that the
entries are cards where the entry has a picture (spec US5, SC-009).

The shared card (T018) and the dunnage lists (T026) are in Phase 2 and the Setup part list (T037) in Phase 3, because
each shares a file with another phase. This story's own lists are the remaining three.

### Tests

**Wave T1 — independent (different files):**

- [x] **T064** [P] [US5] Pin the remaining-answer list as cards that still choose the answer ·
  `MTM_Waitlist.Tests/Module_Waitlist/Views/NewRequestDetailsCardMarkupTests.cs`
- [x] **T065** [P] [US5] Pin the building flyout: every building is a selectable card and the building in effect is not
  among them, on the shell and in the wizard · `MTM_Waitlist.Tests/Module_Core/ViewModels/ShellBuildingFlyoutTests.cs`,
  `MTM_Waitlist.Tests/Module_Waitlist/Views/NewRequestWorkCenterBuildingFlyoutTests.cs`
- [x] **T066** [P] [US5] Extend the Settings markup rules so the two dunnage lists are cards carrying each type's
  picture, still showing and hiding their type · `MTM_Waitlist.Tests/Module_Settings/SettingsPageMarkupTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T067** [P] [US5] The building flyout's entries, with the building in effect left out ·
  `ViewModels/ShellViewModel.cs`
- [x] **T068** [P] [US5] The remaining-answer list becomes cards carrying each answer's picture, still choosing the
  answer · `Module_Waitlist/Views/NewRequestDetailsPage.xaml`
- [x] **T069** [P] [US5] The wizard's building choice becomes a flyout of cards without the building in effect ·
  `Module_Waitlist/Views/NewRequestWorkCenterPage.xaml`
- [x] **T070** [P] [US5] The shell's building choice becomes the same flyout, without the building in effect ·
  `Module_Core/Views/ShellPage.xaml`

**Checkpoint.** US5 is independently functional: three more lists are cards that still perform their choice, and the
building flyout never offers the building already in effect.

---

## Phase 8: US6 — Where the pictures live is a setting, from a screen (P3)

**Files:** `appsettings.json`, `MTM_Waitlist.Settings/Models/ConfigSettingValue.cs`,
`MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs`

**Goal:** an entitled account changes the picture folder, the key folder and the retention period from Settings, and a
recorded picture resolves under whichever folder a machine is configured with.

**Independent test:** change the picture folder from Settings, then confirm a picture stored afterwards resolves for a
second computer under that computer's own configured folder (spec US6).

The panel itself is in Phase 2 (T022), because it shares the Settings page with two other stories. This story's work is
what the panel writes and how the disagreement between the store and a machine's own file is reported.

### Tests

**Wave T1 — independent (different files):**

- [x] **T071** [P] [US6] Pin the store-wins cascade and the disagreement it must report between the stored folder and
  this machine's own configuration · `MTM_Waitlist.Tests/Module_Settings/ImageStorageConfigurationResolverTests.cs`
- [x] **T072** [P] [US6] Pin portability: a value recorded under one configured root resolves under another machine's
  own configured root, and never names the folder it was recorded under ·
  `MTM_Waitlist.Tests/Module_Settings/Services/PartPictureStoragePathPortabilityTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T073** [P] [US6] The shipped default retention period becomes ninety days · `appsettings.json`
- [x] **T074** [P] [US6] The retention period is owned by the storage screen rather than compiled ·
  `MTM_Waitlist.Settings/Models/ConfigSettingValue.cs`
- [x] **T075** [P] [US6] Report which folder is the truth, and say in one line when the store and this machine disagree
  · `MTM_Waitlist.Settings/Services/ImageStorageConfigurationResolver.cs`

**Checkpoint.** US6 is independently functional: both folders and the retention period are changeable by an entitled
account, and a recorded picture resolves on a machine configured with a different folder.

---

## Phase 9: US7 — A picture appears fast and survives being replaced (P3)

**Files:** `MTM_Waitlist.Shared/Services/PartPictureCacheStore.cs`,
`MTM_Waitlist.Shared/Services/ImageCacheSyncService.cs`,
`MTM_Waitlist.Startup/Services/StartupCoordinator.cs`,
`Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureCache.cs`

**Goal:** a part's picture is copied to the computer the first time that part is drawn, later draws use the copy, and a
replaced picture is kept until its retention period has passed.

**Independent test:** draw a part on a machine that has never drawn it and confirm the copy appears; replace the picture
at the source and confirm the previous file is kept and then removed once its period has passed (spec US7, SC-006,
SC-007).

### Tests

**Wave T1 — independent (different files):**

- [x] **T076** [P] [US7] Pin the copy rule: copied on first draw, nothing copied while the copy is unchanged, an
  unreachable source recorded with the existing copy kept, and an empty source removing nothing ·
  `MTM_Waitlist.Tests/Module_Shared/Services/PartPictureCacheStoreTests.cs`
- [x] **T077** [P] [US7] Extend the mirror's rules so the two part collections are left to the new store ·
  `MTM_Waitlist.Tests/Module_Shared/Services/ImageCacheSyncServiceTests.cs`
- [x] **T078** [P] [US7] Pin the startup step: the archive cleanup runs with the cache step and removes only what is
  past the configured period · `MTM_Waitlist.Tests/Module_Startup/Services/StartupArchiveCleanupTests.cs`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T079** [P] [US7] Copy one part's picture on first draw, and never remove one ·
  `MTM_Waitlist.Shared/Services/PartPictureCacheStore.cs`
- [x] **T080** [P] [US7] Leave the two part collections to that store and keep mirroring the two folders mirrored today
  · `MTM_Waitlist.Shared/Services/ImageCacheSyncService.cs`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T081** [P] [US7] Join the archive cleanup to the cache step ·
  `MTM_Waitlist.Startup/Services/StartupCoordinator.cs`
- [x] **T082** [P] [US7] Register the cache store in the feature's own partial ·
  `Services/DependencyInjection/ServiceRegistrationExtensions.PartPictureCache.cs`

**Checkpoint.** US7 is independently functional: the first draw of a part copies its picture, an unchanged picture is
never copied twice, and a replaced picture outlives its replacement until its period has passed.

---

## Phase 10: Polish

Cross-cutting cleanup, documentation, and validation against the specification's success criteria.

**Wave 1 — independent (different files):**

- [x] **T083** [P] Extend the retired-symbol and retired-wording audit, so the component card's "until part pictures
  exist" claim cannot return · `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`
- [x] **T084** [P] Extend the picture policy and markup scans, so a surface that draws a part without a placeholder path
  fails · `MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestImagePathPolicyTests.cs`
- [x] **T085** [P] Extend the permission gate-site audit to the moved picture-cache gate ·
  `MTM_Waitlist.Tests/Module_Core/Permissions/PermissionGateSiteAuditTests.cs`
- [x] **T086** [P] Document the five scopes, the new history table, the layout and the run-once move in the database
  rule set · `Database/Database-Ruleset.md`
- [x] **T087** [P] Record the feature in the changelog · `CHANGELOG.md`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T088** Drive the built application with the PowerShell UI-Automation recipe and confirm each converted surface
  draws a part's picture and the shared placeholder, with no blank space anywhere a part's picture belongs ·
  `tools/ui-drive.ps1`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T089** Validate against the Success Criteria: the solution builds with zero warnings and zero errors, and the
  full suite reports no failures · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj`

---

## Dependencies & Execution Order

**Phase dependencies.** Setup → Foundational → the seven story phases → Polish. Foundational blocks every story: no
story work begins until T004–T028 are done. Polish waits for all seven stories.

**Waves, phase by phase.**

| Phase | Waves | What blocks what |
| --- | --- | --- |
| 1 Setup | W1: T001–T003 (all `[P]`) | Nothing; every later gate reads T001's baseline |
| 2 Foundational | W1: T004–T011 · W2: T012–T017 · W3: T018–T019 · W4: T020–T021 · W5: T022–T023 · W6: T024–T025 · W7: T026–T027 · W8: T028 | Each wave needs the one above it. W3's registration partial needs the resolver (T016); W4–W7 are four sequential edits to `SettingsPage.xaml` and `SettingsViewModel.cs`, one deliverable each |
| 3 US1 | T1: T029–T032 · W1: T033–T038 · W2: T039 · W3: T040 | T039 needs T033–T038; T040 needs T039 |
| 4 US2 | T1: T041–T043 · W1: T044–T045 · W2: T046 | T046 needs T044 |
| 5 US3 | T1: T047–T051 · W1: T052–T055 · W2: T056–T058 · W3: T059–T060 | T056/T057/T058 need W1; T059 needs T058; T060 needs T055 and T058 |
| 6 US4 | T1: T061–T062 · W1: T063 | T063 needs T016's reader, which is Foundational |
| 7 US5 | T1: T064–T066 · W1: T067–T070 | Every task needs the shared card (T018) and the dunnage lists (T026) |
| 8 US6 | T1: T071–T072 · W1: T073–T075 | Needs the Settings panel (T022) |
| 9 US7 | T1: T076–T078 · W1: T079–T080 · W2: T081–T082 | T081 needs T079 and T080; T082 needs T079 |
| 10 Polish | W1: T083–T087 · W2: T088 · W3: T089 | Needs every story phase done |

**Story order.** US1 and US2 are both P1 and US1's surfaces are what US2's separation is seen on, so US2 follows US1.
US3, US4 and US5 follow; US4 depends only on the Foundational reader, so it can run any time after Phase 2. US6 and US7
are P3 and follow in that order, US7's cleanup reading the period US6 stores.

**Files with one owner.** `SettingsPage.xaml` and `SettingsViewModel.cs` are owned by Foundational (T020, T022, T024,
T026) because three stories need them. `Styles/PartPictureCardTemplate.xaml` and `App.xaml` are owned by Foundational
(T018) because four lists consume the card. Every other path appears in exactly one phase.
