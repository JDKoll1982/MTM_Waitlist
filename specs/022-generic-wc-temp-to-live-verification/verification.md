# Screen Verification: Generic Work Center, Live Data Against the Approved Preview

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Recorded**: 2026-11-16

This is the evidence FR-014 asks for: one entry per screen, naming what was compared, what proves it, and the
outcome. It is written for a reviewer, not for the application.

Two things bound what it can claim. The preview host that produced the owner's approval has been deleted
(spec assumption A2), so no screen can be re-opened side by side with it; the approved presentations are the
ones the owning chunk's specification states and its tests assert. And the owner's re-confirmation, which the
spec's design note asks for screen by screen, could not be taken during this run: it is listed under **Open
items** rather than silently assumed done.

## 1. Work center

- **Page**: `GenericRequestWorkCenterPage`
- **Live read**: `ReadWorkCentersAsync`, answered from the work center catalogue the work-order wizard already uses
- **Presentations compared**: populated, the catalogue drawn as cards; empty, the step's own empty line; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestWorkCenterViewModelTests` covers the populated list and, added here, an unreadable list offering the attempt time and a retry
- **Outcome**: matches. Nothing the live source supplies replaces an approved cell; the cards are the approved
  cards, drawn from the catalogue rather than from a preview list.

## 2. Category

- **Page**: `GenericRequestCategoryPage`
- **Live read**: `ReadCategoriesAsync` for the chosen work center
- **Presentations compared**: populated, the categories that work center is configured for; empty, the step's empty line; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestCategoryViewModelTests`: `LoadAsync_ReadsTheCategoriesForTheChosenWorkCenterAndMarksTheStoredOne` for populated, `LoadAsync_AnUnreadableCategoryListCannotAdvance` for unavailable
- **Outcome**: matches.

## 3. Item

- **Page**: `GenericRequestItemPage`
- **Live read**: `ReadItemsAsync`, then the item's own configured question
- **Presentations compared**: populated, the configured items with their questions; empty, an item that asks nothing advances without an answer; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestItemViewModelTests`: `AnItemThatAsksNothingAdvancesWithoutAnAnswer` for empty, `AnUnreadableItemConfigurationReportsTheSourceUnavailable` for unavailable; `GenericRequestItemQuestionTests` for the configured question
- **Outcome**: matches.

## 4. Part

- **Page**: `GenericRequestPartPage`
- **Live read**: `ReadPartsAsync` for completions and `ReadSequencesAsync` for the sequences, both through `IGenericRequestPartData` so an unreachable plant system is answered from the mirror
- **Presentations compared**: populated, completions from both systems naming their source; empty, a number neither system knows is refused rather than invented; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestPartDataTests`: `CompletionsComeFromBothSystemsAndNameTheirSource` for populated, `AnUnreachableSourceMakesTheCompletionsUnavailableWhenThereIsNoMirror` for unavailable; `GenericRequestPartViewModelTests_ATypedNumberNeitherSystemKnowsIsRefused` for empty
- **Outcome**: matches.

## 5. Dunnage

- **Page**: the dunnage step inside the item flow
- **Live read**: `IDunnageCatalogueReader`
- **Presentations compared**: populated, the plant's catalogue; empty, an empty catalogue presents as empty rather than unavailable; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestDunnageViewModelTests`: `APopulatedCatalogueIsPopulatedAndAnEmptyOneIsEmpty` for both settled states, `AFailedCatalogueIsUnavailableWithTheAttemptTime` for unavailable
- **Outcome**: matches.

## 6. Scrap

- **Page**: `GenericRequestScrapTypePage`
- **Live read**: `ReadScrapTypesAsync`, the plant's one shared list with arrangement types appended
- **Presentations compared**: populated, the shared list in its order; empty, the two names that mean no scrap are never offered; unavailable, no choice and no advance
- **Evidence**: `GenericRequestScrapOptionsTests` for the shared list; `GenericRequestScrapTypeViewModelTests_LoadAsync_AnUnreadableSourceOffersNoChoiceAndNoAdvance` for unavailable
- **Outcome**: matches.

## 7. Die

- **Page**: `GenericRequestDiePage`
- **Live read**: `ReadDiesAsync`, Infor Visual read shape 12
- **Presentations compared**: populated, every die the match owns with its location; empty, a term that matches nothing answers empty rather than unavailable; unavailable, the term is kept and the retry offered
- **Evidence**: `GenericRequestDieDataTests`: `ADieNumberAnswersThatDieAndIsMatchedAsTyped`, `ATermNoDieMatchesAnswersEmptyRatherThanUnavailable`, `AFailedReadAnswersUnavailable`; `GenericRequestDieViewModelTests_AFailedSearchTakesTheBodyAndOffersTheRetry`
- **Outcome**: matches.

## 8. Details

- **Page**: `GenericRequestDetailsPage`
- **Live read**: `ReadStepAvailabilityAsync(Details)` for the step, and the quantity reading for the figure
- **Presentations compared**: populated, the whole number on hand; empty, zero is a figure rather than an absence; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestQuantityTests`: `AnOpenOperationIsAnsweredByTheFloor`, `ZeroOnHandIsAFigureRatherThanAnAbsence`, `AnUnreadableStateIsUnavailableAndReportsNoFigure`
- **Outcome**: matches.

## 9. Summary

- **Page**: `GenericRequestSummaryPage`
- **Live read**: `ReadStepAvailabilityAsync(Summary)`, plus every answer the draft carries
- **Presentations compared**: populated, the review listing each answer; empty, an item with no configured time adds no estimate; unavailable, the attempt time and a retry
- **Evidence**: `GenericRequestSummaryViewModelTests`: `LoadAsync_TheReviewListsTheAnswerAndTheConfiguredTime`, `LoadAsync_AnItemWithNoConfiguredTimeAddsNoEstimate`, and, added here, `LoadAsync_AnUnreadableReviewOffersTheAttemptTimeAndARetry`
- **Outcome**: matches.

## 10. Result

- **Page**: `GenericRequestResultPage`
- **Live read**: `SubmitAsync`, the same write door every other request uses
- **Presentations compared**: populated, a raised request that never shows its identifier; empty, an unfinished draft states something; unavailable, a failed write offers a retry and the queue's own refusal keeps the requester on the review
- **Evidence**: `GenericRequestResultViewModelTests`: `LoadAsync_ARaisedRequestIsSuccessAndNeverNamesItsIdentifier`, `LoadAsync_AWriteThatFailedOffersARetry`; `GenericRequestSummaryViewModelTests_ContinueCommand_ARefusedRequestStaysOnTheReviewAndSaysWhy`
- **Outcome**: matches.

## Awkward real values

FR-011 names four. Two are proven, two are not, and the difference is recorded rather than smoothed over.

| Value | Evidence | State |
|---|---|---|
| Scrap type in lower case | `GenericRequestScrapTypeViewModelTests` restores a stored type ignoring case and spaces; `GenericRequestScrapAnswerTests` keeps the stored wording | proven |
| Outside-service part with no quantity | `GenericRequestQuantityTests` answers an outside-service operation as work in progress; `GenericRequestPartAnswerTests` stores the WIP stage and figure | proven |
| Long work center name | No dedicated case. The card text is bound to the catalogue name and wraps in the frame | not proven by a test |
| Part number containing a slash | No dedicated case. The number is drawn as text with no path or formatting applied | not proven by a test |

## A deferred chunk

Chunks 017 to 021 all shipped, so no screen is in the deferred state today. What a deferred screen would look
like is fixed anyway, and proven: a screen whose source cannot be read reports the source unavailable from a
live check, with the attempt time and a retry, and offers no text box, no offline list and no substitute
(`GenericRequestStepViewModel` records the presentation, `GenericRequestStepFrame` draws the unavailable panel
with `AttemptedAtDisplay` and `RetryCommand`). Every step's unreadable-source case above is the same
presentation, so a chunk that ships later needs no other screen edited for its own screen to start working.

No `Temp` artefact of any chunk survives, so a deferred chunk would leave nothing behind either
(`TempArtefactBoundaryAuditTests`).

## Open items

1. **Owner re-confirmation is outstanding.** The spec's design note asks for each screen's approved layout,
   states and wording to be re-confirmed with the owner before it is compared. This run had no owner to ask,
   so the comparison above is against the owning chunk's specification and tests. The re-confirmation is the
   one thing that can still change an outcome from `matches`.
2. **Two awkward values have no dedicated case** (long work center name, part number containing a slash). They
   are drawn as bound text with no transformation, so the risk is layout, not data. Closing them belongs to
   the chunk that owns the card, not here.

## How to re-prove this record

- `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj --filter "FullyQualifiedName~TempArtefactBoundaryAuditTests"` — the boundary guard, including its deliberate-offence case.
- `dotnet test MTM_Waitlist.Tests\MTM_Waitlist.Tests.csproj` — the whole suite, including the per-screen presentation tests cited above.
- Search the solution for `Temp` — it must return no artefact of this feature. The only matches are the guard
  itself, the temporary-credential test file, and the temporary-directory fixture, each exempted by name in the
  guard with its reason.
