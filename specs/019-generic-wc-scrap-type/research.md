# Research: Generic Work Center — Stated Scrap Type and Lugger

**Feature Branch**: `019-generic-wc-scrap-type`
**Spec**: [spec.md](./spec.md)
**Created**: 2026-11-05

## What the code already settles

Four things the spec assumes are missing are already built, and one thing it asks to delete is already gone. Recording them here keeps the plan from rebuilding or re-deleting them.

- **The scrap step exists.** `GenericRequestStep.Scrap`, `GenericRequestScrapTypeViewModel`, `Views/GenericRequestScrapTypePage.xaml`, `IGenericRequestStepData.ReadScrapTypesAsync` and `GenericRequestDraft.ScrapType` all ship. Only the read behind them is a stub that returns `Unavailable`, and `ReadStepAvailabilityAsync` marks `Scrap` unavailable beside `Die`.
- **The route past the details step exists.** `GenericRequestFlowRules.BuildOrder` adds `Scrap` when `RequiresScrap` is set and treats it as `answeredOnItsOwnStep`, so the order is Scrap, Summary, Result. FR-015 is a property of code that is already there. `GenericRequestDraft.Description` initialises to `string.Empty` and the details page is the only place that sets it, so a scrap request records no description. Nothing is built for FR-015.
- **The confirmation names the type.** `GenericRequestSummaryViewModel` adds the row from `Draft.ScrapType` under `GenericRequest_Summary_Scrap.Label`.
- **The arrangement resolution is in Core and job-free.** `LuggerArrangementSnapshot.Resolve(string?)` matches on `Trim().ToLowerInvariant()`, returns `NoScrapDecision` for the two sentinels, `NotConfigured` for a type the arrangement does not carry, the usable dump point, the first usable lugger, or `"Door 9 Lugger (Full)"` when every lugger is blocked. FR-007, FR-008, FR-009 and FR-013 all describe this method.
- **The `Temp` preview is gone.** No `Temp*` project, namespace, resw key or card variant exists in the tree. `MTM_Waitlist.Tests/Module_GenericRequest/TempArtefactBoundaryAuditTests.cs` already fails the build if one returns. FR-014 is satisfied by the tree as it stands, and this chunk deletes nothing on that account.

## D1: Where the choices come from

**Decision.** The shared list is `ScrapTypeCatalog.All`, a compiled list in `MTM_Waitlist.Core`. The types the arrangement adds are `LuggerArrangementSnapshot.ScrapTypes`, read through `ILuggerArrangementService.GetSnapshotAsync` and `sp_waitlist_lugger_arrangement_list` in `mtm_waitlist`. The composition rule moves from the arrangement editor into Core as `ScrapTypeCatalog.Compose(IEnumerable<string>? assignedTypes)`: the catalogue in its own order, then every assigned type it does not already name, dropping `ScrapDecisionRules.RequiredPlaceholder` and `ScrapDecisionRules.NoScrap` and comparing case- and space-insensitively. `LuggerArrangementViewModel.BuildScrapTypeOptions` is replaced by a call to it.

**Rationale.** FR-002 forbids a second scrap-type list, and today the ordering rule exists only as a private static in the Settings editor. Leaving it there and writing a third copy in the generic flow is exactly what the requirement forbids, and the two copies would drift on the first spelling change. `ScrapTypeCatalog` is the file that already owns the list's own order, so the rule belongs beside it.

**Alternatives considered.** A scrap-type table with its own stored procedure: rejected, because the list is deliberately compile-time so that `Setup` and `Settings` agree without a database round trip, and no requirement asks for a fifth source of types. `SetupWorkflowService.s_defaultScrapTypes` is not a counter-example: it prepends the two sentinels because the Setup picker asks *whether* there is scrap, which is a different question from *where* it goes.

## D2: How the step learns the arrangement could not be read

**Decision.** `LuggerArrangementSnapshot` gains `WasRead`. `Create` sets it true; `Empty`, which the service returns when the read failed and which is also the type's own default, leaves it false. The generic step reports `Unavailable` when `WasRead` is false.

**Rationale.** The edge case in the spec requires the step to offer no list at all when the arrangement cannot be read, because the shared list alone would silently drop types the plant ships against. Today `LuggerArrangementService.GetSnapshotAsync` swallows the failure, logs `"the arrangement is reported as unavailable rather than as empty"`, and returns `Empty`, so no caller can tell an outage from an arrangement with no rows. `WasRead` makes the distinction the type already claims to make, at one property and two assignments.

**Alternatives considered.** Making `GetSnapshotAsync` throw on an outage, the way `IDunnageCatalogueReader` throws: rejected, because `GetLuggersAsync` beside it has the same swallow, so the Settings page would still render an empty list and the two reads would disagree; and every existing caller would gain an exception path it does not have. A second member on `ILuggerArrangementService` that throws: rejected as two ways to do one read, with the old one left lying. A separate spelling probe: rejected, because it asks the store a second question to learn something the first answer already held.

## D3: The available-lugger count

**Decision.** `GenericRequestScrapOption` loses `AvailableLuggerCount` and `AvailableLuggerDisplay`. The option becomes the scrap type and nothing else. The page's two count columns and the `GenericRequest_Scrap_Luggers.Text` label go with it.

**Rationale.** Nothing populates the count today, because the read behind it has always been the stub, so it is a leftover of the `Temp` preview that FR-014 deletes rather than a fact the plant ever saw. No requirement asks the step to show it: FR-002 and FR-003 say what the choices *are*, and every lugger fact in the spec (FR-006 to FR-012) belongs to the card. A2 fixes the lugger as a live read at card time, so a count taken when the step loaded would be stale by the time a handler read the card. Worse, it would tell a second story: the step would say "0 luggers" for a type that FR-008 deliberately draws as `Door 9 Lugger (Full)`.

**Alternatives considered.** Keep the count and fill it from the snapshot the step already reads (`Rows.Count(row => row.LuggerStatus == Available)` per type). Rejected as scope the spec does not ask for, on a screen the requester has 30 seconds to get through (SC-001), and it invites choosing a type by free-lugger count, which is not a decision the requester makes. Keep the field and leave it at zero: rejected, because a drawn `0` is a claim about the plant rather than an absence.

## D4: How the stated type survives on the request

**Decision.** `GenericRequestScrapAnswer` in `MTM_Waitlist.Core/Models`, beside `GenericRequestDunnageAnswer`, records one key into `answers_json`:

| Key | Meaning |
| --- | --- |
| `scrap.type` | the type as the requester chose it, in their wording |

`GenericRequestLiveStepData.AnswersForAsync` merges it when `draft.RequiresScrap` is set and `draft.ScrapType` is not empty.

**Rationale.** FR-005 asks for the type on the confirmation, the card and the stored request. The confirmation and the draft already have it. `answers_json` is the column the generic flow already uses for exactly this, and the dunnage answer is the shape to copy: a `sealed record` with a `const string` key, `ToAnswers()` returning a case-insensitive dictionary, and a `TryRead` that answers null rather than throwing. Storing the wording is what makes FR-011 and FR-013 work: a type renamed or removed from the arrangement afterwards leaves the request saying what the requester said.

**Alternatives considered.** `input_value`: rejected. That column is documented as "the one value the request is about", and for dunnage it is there because the card's second line reads `{dunnage_part}` straight out of it. A scrap card's `{scrap_type}` token is not bound to `input_value`, so writing the type there would add a second authority for the same fact. A new column and procedure: rejected, because it needs a schema artifact for a value `answers_json` already holds, and Constitution III makes that a cost with no return.

## D5: How the card resolves the lugger

**Decision.** `RequestJobAvailabilityProvider` gains `GetStatedScrapLuggerAsync(string scrapType, CancellationToken)`, returning the new `StatedScrapLugger` record. The body of `ApplyLuggerLocationAsync` is split so that one private routine, `ResolveLuggerAsync(scrapType, workCenter, cancellationToken)`, does the snapshot read, the gap warning, the picture lookup and the outcome. Both the work-order path and the stated-type path call it.

**Rationale.** FR-007 and SC-003 require the two card kinds to name the identical lugger for the same type, in 100% of cases. The only way to guarantee that is one routine, not two callers of the same helper that could each grow a condition. The provider is already the place the composition root turns a job into the `RequestJobPartAvailability` a card consumes, and it already holds `ILuggerArrangementService` and `ILuggerImages`.

**Alternatives considered.** Resolving in `WaitlistViewViewModel`: rejected, because the board would then hold arrangement and picture knowledge beside the provider that already holds it, and the picture lookup would be duplicated. Resolving in `WaitlistRequestTitles`: rejected, because a model with no service would have to be handed a snapshot, which moves the read to every caller of the titles. Copying the lugger name onto the request at raise time: rejected outright by A2 and FR-012.

## D6: How the card names the type, and what the lugger cell says

**Decision.** The board merges the stated scrap facts into the `RequestJobPartAvailability` it already hands `CreateSessionOrder` and `ResolveDeclaredCell`, and only for a request that `WaitlistCardCellRules.IsScrapItem` accepts and `WaitlistRequest.IsGeneric` marks generic. `GenericRequestScrapAnswer.TryRead(request.Answers)` supplies the wording for `ScrapType`; the provider supplies `LuggerLocation` and the picture. The lugger cell then carries:

| Outcome | Cell value |
| --- | --- |
| a lugger resolves, including `Name (Status)` for an all-blocked type | the name, as `Resolve` returns it |
| `NotConfigured` | `WaitlistRequest_Scrap_LuggerNotAssigned.Text` |
| the arrangement was not read | `WaitlistRequest_Scrap_LuggerUnavailable.Text` |

**Rationale.** `RequestJobPartAvailability` is already the record that carries `ScrapType`, `LuggerLocation` and `LuggerLocationImagePath` from wherever they are known to the two places that draw them. Filling it from the request's own answer instead of from a job means `WaitlistRequestTitles`, `RequestJobFieldValues`, `WaitlistCardCellRules`, `CreateSessionOrder` and `ResolveDeclaredCell` need no branch at all, and the work-order card is untouched because the merge never runs for it. The words for the gap and the outage are chosen in a view model, where this repository keeps its wording, not in the provider.

**Alternatives considered.** Reading the answer inside `WaitlistRequestTitles.ResolveContext`, the way the dunnage part reads `InputValue`: rejected because it fixes `{scrap_type}` only, and the lugger would still arrive by a second mechanism, so a card could name the type from one source and a lugger for a different one. Making `RequestJobPartAvailability.LuggerLocation` null for a gap or an outage and leaving the cell out: rejected because SC-002 requires no card drawn blank where a lugger should be named, and because FR-010 asks for the outage to be *reported*.

**Accepted consequence.** When the cell carries the gap or outage words, `ResolveDeclaredCell` still sets `DrawsPicture` because the value is non-empty, so the cell draws the shared placeholder behind the words. The alternative is a branch in the cell rule that would change how a work-order lugger with no picture draws, which the spec forbids.

## D7: The step's read, and its seam

**Decision.** `GenericRequestLiveStepData` takes `ILuggerArrangementService` as a constructor parameter and calls `GetSnapshotAsync` in `ReadScrapTypesAsync`: `Populated` over `ScrapTypeCatalog.Compose(snapshot.ScrapTypes)` when `WasRead` is true, and `Unavailable(attemptedAtUtc)` when it is false. The step frame's retry re-runs the same read. `ReadStepAvailabilityAsync` stops marking `Scrap` unavailable.

**Rationale.** 018 put its reader interface in `MTM_Waitlist.Shared` because the implementation belonged to `MTM_Waitlist.Setup` and the shared library could not see it. That reason does not exist here: `ILuggerArrangementService` is a `MTM_Waitlist.Core` contract, `MTM_Waitlist.Core` has no project references of its own, and `MTM_Waitlist.GenericRequest` already imports `MTM_Waitlist.Module_Core.Contracts.Services`. A new interface over an existing one would be an interface with one implementation for nothing.

**Alternatives considered.** A `IScrapTypesReader` in `MTM_Waitlist.Shared` mirroring `IDunnageCatalogueReader`: rejected as the copy 018's own reason does not justify. Having the step read only `ScrapTypeCatalog.All`: rejected by the spec's edge case, which requires no list at all when the arrangement is unreadable.

## D8: Back navigation keeping the choice

**Decision.** `GenericRequestScrapTypeViewModel` overrides `CanAnswer => Selected is not null`, calls `NotifyAnswerChanged()` when `Selected` changes, and re-selects on load with a trim and `OrdinalIgnoreCase` comparison rather than `==`.

**Rationale.** `CanAnswer` is `true` in the base class, so FR-004 is currently unimplemented: the forward button is enabled with nothing chosen, and `CommitToDraft` silently keeps the previous value. The re-selection is the FR-016 path, and the stored draft could carry a spelling that differs only in case or surrounding spaces, which the arrangement and the catalogue already treat as the same type.

**Alternatives considered.** Comparing through a new Core helper: rejected as two lines of comparison do not need a member.
