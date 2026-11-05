# Tasks: Generic Work Center — Dunnage Catalogue Page

**Feature**: `018-generic-wc-dunnage-page` · **Plan**: [plan.md](./plan.md) · **Spec**: [spec.md](./spec.md)
**Size**: `oversized` — nothing trimmed.

## Scale note

The change reaches five assemblies and about eighteen files, six of them new. Two points carry the risk: the seam that lets the generic flow read the Setup catalogue, and the raise path that carries the chosen part with no schema change. The waves below are the dependency map; read a phase's `Files:` line before editing anything in it.

## Paths

Feature dir is `specs/018-generic-wc-dunnage-page`. Run every command from the repo root `C:\Users\jkoll\source\repos\MTM_Waitlist`, with `python`, never `python3`.

---

## Phase 1: Setup

**Files:** `specs/018-generic-wc-dunnage-page/wording.md`

**Wave 1 — independent:**

- [x] **T001** Record the page wording the spec's 2026-11-03 design note requires, adopting the unattended default below and marking it `STATUS: default, awaiting review`. Write: loading `Loading the dunnage catalogue…`; empty `No dunnage parts to offer.`; unavailable `The dunnage catalogue could not be read at {0}. Check the connection and retry.`; gone marker `No longer in the catalogue.`; second-part note `One dunnage part per request. Raise a second request for another part.` Record the resource key each string gets, the state each belongs to, and, for the gone marker, the surfaces that show it (confirmation and detail). Every wording task below reads this file; none invents its own words. · `specs/018-generic-wc-dunnage-page/wording.md` · **Check**: the file exists, holds five strings and five keys, and states the review flag.

**⟶ Wait for Wave 1 to finish, then Phase 2.**

---

## Phase 2: Foundational

**Files:** `MTM_Waitlist.Shared/Models/DunnageCataloguePart.cs`, `MTM_Waitlist.Shared/Services/IDunnageCatalogueReader.cs`, `MTM_Waitlist.Shared/ViewModels/DunnageCatalogueViewModel.cs`, `MTM_Waitlist.Setup/Services/DunnageCatalogueReader.cs`, `MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs`, `MTM_Waitlist.Core/Models/GenericRequestDunnageAnswer.cs`, `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestStepData.cs`, `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs`, `Module_Shared/Controls/DunnageCatalogueView.xaml(.cs)`, `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml(.cs)`, `Strings/en-us/Resources.resw`, `Services/DependencyInjection/ServiceRegistrationExtensions.DunnageCatalogue.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, and the five test files named below.

Blocks every story. Nothing in Phase 3 onward starts until Phase 2's last wave is green.

### Wave 1 — independent (different files)

- [x] **T002** [P] Add the shared catalogue row: `DunnageCataloguePart` with `Id`, `TypeId`, `PartNumber`, `DisplayName`, `DunnageTypeName`, `HomeLocation`, `ImagePath`, plus computed `HasImage`, `ShowNoImagePlaceholder`, `DisplayImagePath` (which falls back to `ImagePicturePolicy.NoImagePath`) and `ImageFallbackText`. Mirror `MTM_Waitlist.Setup.Models.SetupDunnagePart` exactly; do not carry `Metadata` or `IsSelectedForPair`. · `MTM_Waitlist.Shared/Models/DunnageCataloguePart.cs` · **Check**: `dotnet build MTM_Waitlist.Shared` clean.

- [x] **T003** [P] Add the new resource keys from `wording.md`: the catalogue control's own words (its filter placeholder, refresh, show-all toggle and no-image text moved out of the dialog markup) and the page's five strings. English text stays byte-identical to what the dialog shows today. · `Strings/en-us/Resources.resw` · **Check**: every key named in `wording.md` resolves; no existing key's value changes.

**⟶ Wait for Wave 1 to finish, then:**

### Wave 2 — independent (different files)

- [x] **T004** [P] Declare the read seam as one method, `Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken cancellationToken = default)`. One interface, no other member, no store named in its docs. · `MTM_Waitlist.Shared/Services/IDunnageCatalogueReader.cs` · **Check**: `dotnet build MTM_Waitlist.Shared` clean.

- [x] **T005** [P] Add the recorded answer for a raised request: const keys `dunnage.part`, `dunnage.type`, `dunnage.picture`, `dunnage.offered`; `ToAnswers()` returning a `Dictionary<string, string>` compared case-insensitively; static `TryRead`. Mirror `GenericRequestPartAnswer` in the same folder. · `MTM_Waitlist.Core/Models/GenericRequestDunnageAnswer.cs` · **Check**: `dotnet build MTM_Waitlist.Core` clean.

**⟶ Wait for Wave 2 to finish, then:**

### Wave 3 — independent (different files)

- [x] **T006** [P] Implement the seam over the existing read: `DunnageCatalogueReader` calls `IDunnageWorkflowService.GetAllDunnagePartsAsync()` and projects each `SetupDunnagePart` to a `DunnageCataloguePart`. Write no SQL and add no second read. · `MTM_Waitlist.Setup/Services/DunnageCatalogueReader.cs` · **Check**: `dotnet build MTM_Waitlist.Setup` clean.

- [x] **T007** [P] Add the shared catalogue view model: `FilterText`, `ShowPartsWithoutImages`, `DisplayedParts`, `SelectedPart`, `IsBusy`, `EmptyStateMessage`, `HasNoResults`, and `LoadAsync` returning `Ready` / `Empty` / `Unavailable` with `attemptedAtUtc` on `Unavailable`. Filter is one box matching `PartNumber`, `DunnageTypeName` or `HomeLocation`, case-insensitive, by containment. Sort by `PartNumber` with `StringComparer.OrdinalIgnoreCase`. Picture choice persists per person under `Setup.DunnageImageSearch.ShowPartsWithoutImages`, default `true`, and hides no-picture parts before the text filter. Nothing is pre-selected. It reports the outcome and chooses no surface's words. · `MTM_Waitlist.Shared/ViewModels/DunnageCatalogueViewModel.cs` · **Check**: `dotnet build MTM_Waitlist.Shared` clean.

**⟶ Wait for Wave 3 to finish, then:**

### Wave 4 — independent (different files)

- [x] **T008** [P] Test the shared view model: three outcomes; `Empty` never reported as `Unavailable`; a still-loading read never reported as empty; one-box filter across all three fields; sort by part number; the picture preference hiding no-picture parts before the filter; nothing pre-selected so `SelectedPart` starts null. Fail-first: these are the assertions the page's gates rely on. · `MTM_Waitlist.Tests/Module_Shared/ViewModels/DunnageCatalogueViewModelTests.cs` · **Check**: file present, each named behaviour has an assertion, `dotnet test --filter FullyQualifiedName~DunnageCatalogueViewModelTests` green after T007.

- [x] **T009** [P] Bind the seam and the view model. Add a new partial file defining `static partial void RegisterDunnageCatalogue(IServiceCollection services)`: bind `IDunnageCatalogueReader` to `MTM_Waitlist.Setup.Services.DunnageCatalogueReader`, register `DunnageCatalogueViewModel` transient. In the main file declare `RegisterDunnageCatalogue` and also `RegisterDunnageCataloguePage`, and call both from `AddAppServices`; this one forward declaration is what keeps this file single-owner across phases, and T024 supplies the second implementation. Leave no other scaffolding. · `Services/DependencyInjection/ServiceRegistrationExtensions.DunnageCatalogue.cs`, `Services/DependencyInjection/ServiceRegistrationExtensions.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean; resolving `IDunnageCatalogueReader` in a smoke assertion returns the Setup type.

- [x] **T010** [P] Test the recorded answer: all four keys round-trip through `ToAnswers` and `TryRead`, key comparison ignores case, `dunnage.offered` reads back `false` for a part gone from the catalogue, and a missing or malformed dictionary yields no answer rather than an exception. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDunnageAnswerTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~GenericRequestDunnageAnswerTests` green.

**⟶ Wait for Wave 4 to finish, then:**

### Wave 5 — independent (different files)

- [x] **T011** [P] Extract the catalogue markup and its state into one control: `DunnageCatalogueView` binds a `DunnageCatalogueViewModel`, draws the tiles through the existing `ClickToEnlargeImageView`, and keeps the four `AutomationId` values exactly as the dialog spells them (`SetupDunnageImageSearchDialog_FilterInput`, `_RefreshButton`, `_ShowAllToggle`, `_PartTiles`). Its words come from the T003 keys with the English text unchanged. Add no second filter, list or picture source. · `Module_Shared/Controls/DunnageCatalogueView.xaml`, `Module_Shared/Controls/DunnageCatalogueView.xaml.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean, with no `NETSDK1022` (rely on default item globbing; add no explicit `<Page>` item).

- [x] **T012** [P] Retire the generic flow's dunnage read and carry the chosen part onto the raise. Delete `ReadDunnageAsync` from the seam and its implementation, delete `GenericRequestDunnageOption`, and stop `ReadStepAvailabilityAsync` reporting the dunnage step unavailable. In `SubmitAsync` set `InputValue` to `draft.DunnagePart` when `draft.RequiresDunnage`, and merge `GenericRequestDunnageAnswer.ToAnswers()` into the answers dictionary, with `dunnage.offered` false when a fresh read through `IDunnageCatalogueReader` does not hold the chosen part. Keep the part rather than clearing it. · `MTM_Waitlist.GenericRequest/Contracts/IGenericRequestStepData.cs`, `MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs`, `MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs` · **Check**: `dotnet build MTM_Waitlist.GenericRequest` clean; no reference to `GenericRequestDunnageOption` or `ReadDunnageAsync` remains.

- [x] **T013** [P] Point the dialog's view model at the shared one: `SetupDunnageImageSearchDialogViewModel` delegates filter, picture choice, list, selection and load to a `DunnageCatalogueViewModel`, keeping its own public surface resolving to the same values so the dialog's markup and tests compile unchanged. Keep `Setup.DunnageImageSearch.ShowPartsWithoutImages` as the key. It keeps its flat failure sentence; it does not adopt the page's unavailable wording. · `MTM_Waitlist.Setup/ViewModels/SetupDunnageImageSearchDialogViewModel.cs` · **Check**: `dotnet build MTM_Waitlist.Setup` clean, and `dotnet test --filter FullyQualifiedName~SetupDunnageImageSearch` still green on the pre-existing suite.

**⟶ Wait for Wave 5 to finish, then:**

### Wave 6 — independent (different files)

- [x] **T014** [P] Rehost the dialog on the control: `SetupDunnageImageSearchDialog.xaml` hosts `DunnageCatalogueView` and keeps the four `AutomationId` values and the accept-only-when-a-part-is-chosen gate. The dialog's behaviour, wording, sort and one-box filter do not change. · `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml`, `Module_Setup/Views/SetupDunnageImageSearchDialog.xaml.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean; the four `AutomationId` strings still appear in this file.

- [x] **T015** [P] Test the reader: it projects every field the row declares, drops what the Setup type does not carry, keeps `HomeLocation`, and issues one call to `IDunnageWorkflowService.GetAllDunnagePartsAsync`. Fail-first against the T006 implementation. · `MTM_Waitlist.Tests/Module_Setup/DunnageCatalogueReaderTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~DunnageCatalogueReaderTests` green.

- [x] **T016** [P] Extend the generic-request test doubles with a fake `IDunnageCatalogueReader` (parts in hand, empty, throwing) and test the raise: `InputValue` carries the chosen part only when the item asks for dunnage; the four keys land in the answers dictionary; a part absent from a fresh read is kept with `dunnage.offered` false; an item that does not need dunnage records neither. Add every double the later dunnage tests need, so no later phase edits this file. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestTestDoubles.cs`, `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDunnageSubmissionTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~GenericRequestDunnageSubmissionTests` green.

**⟶ Wait for Wave 6 to finish, then:**

### Wave 7 — the extraction's own acceptance

- [x] **T017** Pin the dialog so the extraction cannot drift: assert the four `AutomationId` values, that the sort is by part number case-insensitively, that one filter matches part number, type and location, that the picture choice starts true and persists under the same key, that the accept gate is closed until a part is chosen, and that the dialog's failure sentence is the flat one and not the page's unavailable wording. · `MTM_Waitlist.Tests/Module_Shared/SetupDunnageImageSearchDialogRegressionTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~SetupDunnageImageSearchDialogRegressionTests` green against T011 and T014.

---

## Phase 3: User Story 1 — dunnage is chosen from the catalogue as a page (P1)

**Files:** `MTM_Waitlist.GenericRequest/Contracts/GenericRequestStep.cs`, `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml(.cs)`, `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDunnageViewModel.cs`, `Module_GenericRequest/Views/GenericRequestDunnagePage.xaml(.cs)`, `Services/DependencyInjection/ServiceRegistrationExtensions.DunnageCataloguePage.cs`

### Tests

**Wave 1 — independent (different files):** write these first and let them fail until the implementation waves land.

- [x] **T018** [P] [US1] Test the step: the catalogue's `Ready` maps to `Populated`, `Empty` to `Empty`, `Unavailable` to `Unavailable` with the attempt time carried; `CanAnswer` is false with nothing chosen and true once one part is chosen; choosing writes `Draft.DunnageType` and `Draft.DunnagePart`; revisiting shows the chosen part as `SelectedPart`; nothing pre-selects; a part gone from a fresh read keeps the draft value. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestDunnageViewModelTests.cs` · **Check**: fails before T022, green after.

- [x] **T019** [P] [US1] Test the frame's loading state: a `Loading` presentation draws the progress ring and hides the body, empty text and unavailable panel; `Populated` hides the ring; a still-loading frame never shows empty text. · `MTM_Waitlist.Tests/Module_GenericRequest/GenericRequestStepFrameLoadingTests.cs` · **Check**: fails before T021, green after.

### Implementation

**Wave 2 — the presentation the frame needs:**

- [x] **T020** [US1] Add `Loading` to `GenericRequestPresentation` ahead of the three settled values, so a step frame can distinguish a read in flight from a finished one. · `MTM_Waitlist.GenericRequest/Contracts/GenericRequestStep.cs` · **Check**: `dotnet build MTM_Waitlist.GenericRequest` clean.

**⟶ Wait for Wave 2 to finish, then:**

**Wave 3 — independent (different files):**

- [x] **T021** [P] [US1] Give the shared step frame a loading state: a `ProgressRing` shown only when `Presentation` is `Loading`, with body, empty text and unavailable panel collapsed in that state. Add no per-page progress control. · `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml`, `MTM_Waitlist.GenericRequest/Controls/GenericRequestStepFrame.xaml.cs` · **Check**: T019 green.

- [x] **T022** [P] [US1] Rewrite the dunnage step view model onto `DunnageCatalogueViewModel`: own the catalogue, load it on entry, map its three outcomes to `Populated` / `Empty` / `Unavailable` (carrying the attempt time and a retry command), gate `CanAnswer` on `SelectedPart is not null`, and commit `DunnageType` and `DunnagePart` to the draft. Leave the step without a chosen part recording nothing. Words come from the `wording.md` keys. · `MTM_Waitlist.GenericRequest/ViewModels/GenericRequestDunnageViewModel.cs` · **Check**: T018 green; `dotnet build MTM_Waitlist.GenericRequest` clean.

**⟶ Wait for Wave 3 to finish, then:**

**Wave 4 — the page:**

- [x] **T023** [US1] Create the step page in the app: `GenericRequestDunnagePage` hosted in `GenericRequestStepFrame`, with `DunnageCatalogueView` bound to the step view model's catalogue, the unavailable panel carrying the attempt time and retry, and the second-part note shown. Page namespace is `Module_GenericRequest.Views`. · `Module_GenericRequest/Views/GenericRequestDunnagePage.xaml`, `Module_GenericRequest/Views/GenericRequestDunnagePage.xaml.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean with no `NETSDK1022` and no masked `WMC9999`.

**⟶ Wait for Wave 4 to finish, then:**

**Wave 5 — route the page and retire the library's copy:**

- [x] **T024** [US1] Implement the forward-declared `RegisterDunnageCataloguePage` in its own partial file, calling `pageService.Configure<GenericRequestDunnageViewModel, GenericRequestDunnagePage>()` with `Module_GenericRequest.Views`, and delete the library's copy of the page. Touching the main registration file is not needed: the declaration and call already exist from T009. · `Services/DependencyInjection/ServiceRegistrationExtensions.DunnageCataloguePage.cs`, deleting `MTM_Waitlist.GenericRequest/Views/GenericRequestDunnagePage.xaml` and `.xaml.cs` · **Check**: `dotnet build MTM_Waitlist.csproj` clean; no file named `GenericRequestDunnagePage` remains under `MTM_Waitlist.GenericRequest/Views`.

**Checkpoint**: a generic dunnage request raises through the page. Choosing a part enables the forward button, the part reaches the draft and the request, and the substitute dialog still behaves as it did. US2 and US3 build on this.

---

## Phase 4: User Story 2 — both surfaces show the one catalogue (P2)

**Files:** `MTM_Waitlist.Tests/Module_Shared/DunnageCatalogueParityTests.cs`, `MTM_Waitlist.Tests/Module_Shared/DunnageCatalogueSourceAuditTests.cs`

This story is the proof that the shared control did its job, so its work is the tests that would fail if either surface drifted.

### Tests

**Wave 1 — independent (different files):**

- [x] **T025** [P] [US2] Prove both surfaces read one source: resolve `IDunnageCatalogueReader` and assert it is the Setup implementation over `IDunnageWorkflowService`; assert the page and the dialog view models both hold a `DunnageCatalogueViewModel`; assert a part added to the fake read appears in both and a part removed from it appears in neither. · `MTM_Waitlist.Tests/Module_Shared/DunnageCatalogueParityTests.cs` · **Check**: green after Phase 3; fails if the page is ever given its own list.

- [x] **T026** [P] [US2] Audit the tree for a second dunnage list: both host files reference `DunnageCatalogueView`, no XAML outside the control declares the four `AutomationId` values, `DunnageCatalogueViewModel` and `LoadAsync` are the only catalogue loaders, and no type other than `DunnageCataloguePart` is projected at the seam. · `MTM_Waitlist.Tests/Module_Shared/DunnageCatalogueSourceAuditTests.cs` · **Check**: green; the audit names the offending path when it fails.

**Checkpoint**: SC-003 is proven by test rather than by inspection.

---

## Phase 5: User Story 3 — the chosen dunnage is visible afterwards (P2)

**Files:** `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs`

The confirmation step already names the part from the draft, and the card's second line already resolves `{dunnage_part}` from `input_value`, which T012 fills. What is missing is the picture and the gone marker.

### Tests

**Wave 1:**

- [x] **T027** [US3] Extend the titles tests: `ResolveDunnagePartPicturePath` returns the request's own `dunnage.picture` when no job snapshot is in hand, still prefers the job's picture when one is, returns nothing when the request records no picture, and the gone marker renders for `dunnage.offered` false and not for true. Fail-first. · `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs` · **Check**: fails before T028.

### Implementation

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T028** [US3] Add the request-side picture fallback and the gone marker: read `GenericRequestDunnageAnswer.TryRead` from the request's answers and, when `jobAvailability` is null, resolve the picture from `dunnage.picture`; keep the job path first. Expose the gone marker from the `wording.md` key so the confirmation and the detail can show it for `dunnage.offered` false. A part with no picture still yields no placeholder implying a picture exists. · `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs` · **Check**: T027 green; `dotnet build MTM_Waitlist.Waitlist.View` clean.

**Checkpoint**: FR-008 and FR-009 hold on the card and the detail for a request with no job.

---

## Phase 6: Polish

**Files:** `CHANGELOG.md`, `docs/guides/guides-xaml-surface-inventory.md`

**Wave 1 — independent (different files):**

- [x] **T029** [P] Record the change for readers: the dunnage step is now the catalogue page, the catalogue is one shared control, and the raise path carries the part and its picture with no schema change. · `CHANGELOG.md` · **Check**: one entry naming the feature and the user-visible effect.

- [x] **T030** [P] Repair the surface inventory the audit enforces: move the `GenericRequestDunnagePage.xaml` row from `MTM_Waitlist.GenericRequest/Views/...` to `Module_GenericRequest/Views/...`, and add a row for `Module_Shared/Controls/DunnageCatalogueView.xaml`. · `docs/guides/guides-xaml-surface-inventory.md` · **Check**: `dotnet test --filter FullyQualifiedName~SurfaceInventoryAuditTests` green.

- [x] **T031** [P] Verify FR-011 rather than re-do it: the `Temp` preview source is already gone (commit `723cec0f`) and the audit that keeps it out is in place, so this task only runs it and records the result. Do not invent deletion or rename work. · read-only against `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs` · **Check**: `dotnet test --filter FullyQualifiedName~TempArtefactBoundaryAuditTests` green.

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T032** Validate against the Success Criteria. No post-implement hook owns validation (`.specify/companion.yml` is absent), so this task runs it. Clean solution build, then the full suite. Treat a masked `WMC9999` as a XAML error, verify no `[RelayCommand]` method lost its name to trailing-`Async` stripping, and confirm no `NETSDK1022`. Record against SC-001 one part chosen with no browsing tree, SC-002 one part per request, SC-003 the parity tests, SC-004 no card without its part and no false placeholder. · `MTM_Waitlist.sln` · **Check**: `dotnet build MTM_Waitlist.sln` zero errors and `dotnet test` green, both recorded in the report.

---

## Dependencies & Execution Order

**Phase order**: Setup → Foundational → US1 → US2 and US3 → Polish. US2 and US3 are independent of each other and both depend on US1.

- **Setup**: one wave. T001 blocks every task that produces words (T003, T022, T028).
- **Foundational**: Wave 1 (T002, T003) → Wave 2 (T004, T005) → Wave 3 (T006, T007) → Wave 4 (T008, T009, T010) → Wave 5 (T011, T012, T013) → Wave 6 (T014, T015, T016) → Wave 7 (T017). Wave 5 is the join: nothing hosts the control or reads the catalogue before it.
- **US1**: Wave 1 tests (T018, T019) → Wave 2 enum (T020) → Wave 3 (T021, T022) → Wave 4 page (T023) → Wave 5 route and deletion (T024).
- **US2**: one wave (T025, T026), after US1.
- **US3**: test (T027) → implementation (T028), after Foundational.
- **Polish**: Wave 1 (T029, T030, T031) → T032.

**Parallel markers**: T002, T003, T004, T005, T006, T007, T008, T009, T010, T011, T012, T013, T014, T015, T016, T018, T019, T021, T022, T025, T026, T029, T030, T031. Every `[P]` task touches a file no other task in its wave touches.
