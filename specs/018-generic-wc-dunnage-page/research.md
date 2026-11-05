# Phase 0 Research: Generic Work Center — Dunnage Catalogue Page

Each entry below is a genuine unknown this plan had to settle. Repository facts came from three mapped reads of the code: the generic-request flow, the Setup image-search dialog, and the substitute-picker data path.

## D1. Where the reusable catalogue lives

**Decision.** The catalogue's state and its row shape go into `MTM_Waitlist.Shared`. The catalogue's markup and the dunnage step page go into the app project.

**Rationale.** Two libraries have to agree on one catalogue: `MTM_Waitlist.Setup` (the dialog) and `MTM_Waitlist.GenericRequest` (the step). Their only common reference is `MTM_Waitlist.Shared`, which references `MTM_Waitlist.Core` and nothing else. Shared already owns `DunnageImagePathResolver`, `ImagePicturePolicy`, `PartPictureLayout` and `PartPictureCacheStore`, so the dunnage-picture rules already live there. The markup goes to the app because that is where this repository's reusable controls already are (`Module_Shared/Controls/ClickToEnlargeImageView`, `RotatingTextView`) and where the composition root and the page routes live.

**Alternatives considered.** Putting the whole catalogue into `MTM_Waitlist.Setup` and adding `MTM_Waitlist.Setup` as a reference of `MTM_Waitlist.GenericRequest`. Rejected: it would make one feature module depend on another for a screen, and the generic flow only ever needs the read. Putting the control in `MTM_Waitlist.Shared` as well. Rejected: the control draws its pictures with `Module_Shared/Controls/ClickToEnlargeImageView`, whose code-behind calls `App.GetService<IPictureEnlargePreference>()` and therefore cannot compile outside the app without rewriting a control that nineteen XAML files already use.

## D2. How the generic flow reaches the catalogue without a new dependency

**Decision.** `MTM_Waitlist.Shared` declares `IDunnageCatalogueReader` with one method, `Task<IReadOnlyList<DunnageCataloguePart>> ReadAllAsync(CancellationToken)`. `MTM_Waitlist.Setup` implements it in `DunnageCatalogueReader` over the existing `IDunnageWorkflowService.GetAllDunnagePartsAsync()`. The composition root binds them.

**Rationale.** The generic-request library gets the catalogue's shape and nothing else, and the read keeps the one implementation the substitute dialog already uses, which is what FR-002 asks for. No inline SQL is written and no second read of the receiving store is introduced.

**Alternatives considered.** Reading `sp_Dunnage_Parts_GetAll` directly from Shared through `MySqlHelperServer`, as `DunnageTypeVisibilityCatalogService` already does. Rejected: it would put a second implementation of the same read in the tree, which is the drift FR-002 forbids.

## D3. Two part shapes, with one projection

**Decision.** `DunnageCataloguePart` in Shared is the row that crosses the boundary. `SetupDunnagePart` stays where it is, and `DunnageCatalogueReader` projects one to the other.

**Rationale.** `SetupDunnagePart` has about seventy references across the Setup library, four app pages, the Setup dialog and the tests, and several XAML files name it in `x:DataType`. Moving it would drag all of them into a chunk about the dunnage page. The projection sits at one seam and the Setup side keeps its own type.

**Alternatives considered.** Moving `SetupDunnagePart` into Shared as `DunnageCataloguePart` and updating every reference. Rejected on churn, not on principle: it is the better long-term shape but it is not this chunk's work, and an `x:DataType` cannot follow a `using` alias. Reusing `RequestDunnagePart` from `MTM_Waitlist.Settings` as the shared row. Rejected: it is the New Request wizard's snapshot value, it carries no type name, and `MTM_Waitlist.Settings` is not a reference of `MTM_Waitlist.Shared`.

## D4. The dunnage step stops asking `IGenericRequestStepData`

**Decision.** `IGenericRequestStepData.ReadDunnageAsync` and `GenericRequestDunnageOption` are deleted. The step's view model owns a `DunnageCatalogueViewModel` and maps its load outcome onto the step's own three presentations.

**Rationale.** The seam's contract is one question and one answer, with the rows shaped for a single list. The catalogue is a shared, stateful surface with a filter, a saved preference and a selection, and forcing it through the seam would mean either widening the seam for every step or writing a second catalogue loader. `IGenericRequestStepData.ReadStepAvailabilityAsync` also stops claiming the dunnage step is unavailable, which it has done since the preview host was deleted.

**Alternatives considered.** Adding `ReadDunnageCatalogueAsync` to the seam and having the step view model feed a catalogue view model from it. Rejected: two loaders for one list, and the attempt time and retry would be handled twice.

## D5. The raised request carries the part, its picture and whether it is still offered

**Decision.** A new `GenericRequestDunnageAnswer` in `MTM_Waitlist.Core` records `dunnage.part`, `dunnage.type`, `dunnage.picture` and `dunnage.offered` into the request's existing `answers_json` column. The submission sets `InputValue` to the chosen part when the item asks for dunnage. `WaitlistRequestTitles.ResolveDunnagePartPicturePath` falls back to that recorded picture when no job snapshot is in hand.

**Rationale.** A generic request has no job, so `ResolveDunnagePartPicturePath` answers nothing for it today and the card would draw the shared placeholder despite the part having a picture, which is what SC-004 and FR-008 forbid. The card's second line resolves `{dunnage_part}` from `input_value`, so the part has to reach that column or the card names nothing. `answers_json` is the established place for a generic request to record what it is about, and `GenericRequestPartAnswer` already proves it needs no schema change.

**Alternatives considered.** Adding a dunnage column to the request table. Rejected: a schema artifact for a value the existing JSON column already carries, and out of step with how the part answer is recorded. Reading the dunnage catalogue on the board to find the picture. Rejected: the board would gain a dependency on the receiving store and a read per card.

## D6. Where a part that has left the catalogue is marked

**Decision.** The chosen part is checked against a fresh catalogue read when the request is raised. When it is absent, the request keeps it and records `dunnage.offered = false`, and the confirmation step and the detail show it as no longer offered.

**Rationale.** FR-009 requires the part to be kept and marked, and the marking has to survive the session, so it belongs on the request rather than in the draft. The wording of the mark is the design clarification the spec's own note of 2026-11-03 demands, so the mechanism is fixed here and the words are not.

**Alternatives considered.** Clearing the choice when the part disappears. Rejected: FR-009 forbids exactly that. Checking only on the page rather than at raise time. Rejected: a part removed between the visit and the raise would go unmarked.

## D7. Loading is shown by the shared step frame

**Decision.** `GenericRequestStepFrame` gains a loading state, and the dunnage page uses it. No page hand-rolls its own progress control for this.

**Rationale.** The frame currently draws only title, introduction, body, empty text and the unavailable panel. Its body is collapsed unless the step is populated, so on a first load the dunnage page would show nothing at all, and FR-010 forbids a still-loading catalogue reading as an answer. One control owns the three states already, so the fourth belongs there too.

**Alternatives considered.** A `ProgressRing` inside the page's body. Rejected: it would be collapsed for exactly the first load it is meant to cover. A ring inside the catalogue control. Rejected for the same reason.

## D8. The dialog's behaviour is the acceptance test for the extraction

**Decision.** The extracted control keeps the dialog's `AutomationId` values, its English text, its sort by part number, its single free-text filter over part number, type and location, its person-scoped picture preference under `Setup.DunnageImageSearch.ShowPartsWithoutImages`, and its Accept/Cancel gate where the accept button stays disabled until a part is selected. The dialog also keeps its flat failure sentence while the page reports the unavailable state, so the shared view model returns the outcome instead of choosing the words.

**Rationale.** A1 requires the extraction not to change the dialog, and the spec's own testing story compares the two surfaces. Keeping the outcome out of the view model is what lets one read serve a screen that shows an attempt time and a screen that does not.

**Alternatives considered.** Letting the dialog inherit the page's unavailable presentation. Rejected: it changes the dialog, which A1 forbids.

## D9. The catalogue's words move behind resource keys

**Decision.** The control's user-facing text moves to `.resw` keys whose English values are byte-identical to the strings the dialog shows today.

**Rationale.** Constitution V requires localized strings for user-facing text, and this is the first time the catalogue is drawn on a page the generic flow ships. Identical values mean the operator reads the same words on the dialog before and after.

**Alternatives considered.** Leaving the strings hard-coded, since the dialog already does. Rejected: it would ship a new screen against a non-negotiable principle when moving the words costs one resource file.

## D10. No stored procedure and no schema artifact

**Decision.** Nothing is added to `Database/`.

**Rationale.** The read is `sp_Dunnage_Parts_GetAll` on `mtm_receiving_application`, already called by `DunnageWorkflowService`. The write is the queue's existing submit path. `sp_Dunnage_Parts_GetAll` has no script in this repository at all, so there is no artifact to pair.

**Alternatives considered.** Adding a filtered read so the page could push the filter to SQL. Rejected: the filter is three columns of one in-memory list, the dialog filters in memory today, and a new procedure would be a second read path rather than the one source FR-002 requires.

## What the code does that the spec does not account for

These were found while reading and are recorded rather than silently fixed.

- `Services/DunnageSubstitutePicker.cs` maps `Id`, `TypeId`, `PartNumber`, `DisplayName` and `ImagePath` but never `HomeLocation`, although `RequestDunnagePart.HomeLocation` exists. The work-order substitute path therefore cannot answer a location. Out of scope here, because the shared control reads the catalogue directly.
- `sp_Dunnage_Parts_GetAll` and `sp_Dunnage_Types_GetAll` have no script in the repository. The table shape is inferable only from the projection in `DunnageWorkflowService`.
- Rows whose `id` or `part_id` is blank are dropped without a word.
- Dunnage **type** visibility overrides are applied to types but not to parts, so a hidden type's parts still appear in the catalogue.
- The dialog's view model accepts no cancellation token, and the one on `IDunnageSubstitutePicker` is never used.
- `MTM_Waitlist.GenericRequest/Views` holds nine step pages, which is the opposite of the constitution's "views in the app" line followed everywhere else. This chunk moves one page to the app and leaves the other eight where they are, because moving them is not this chunk's work.
- `MTM_Waitlist.GenericRequest.Controls` is an empty folder that both the solution-adjacent project list and the app's item exclusions already anticipate. It is left empty.
