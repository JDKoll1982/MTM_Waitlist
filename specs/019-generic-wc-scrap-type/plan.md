# Implementation Plan: Generic Work Center — Stated Scrap Type and Lugger

**Feature Branch**: `019-generic-wc-scrap-type`
**Spec**: [spec.md](./spec.md)
**Created**: 2026-11-05

## Scale note

This change reaches five assemblies (`MTM_Waitlist.Core`, `MTM_Waitlist.Settings`, `MTM_Waitlist.GenericRequest`, `MTM_Waitlist.Waitlist.View` and the composition root), plus one app-project view and one resource file. About fifteen files change, three of them new. Two boundary points deserve the reviewer's attention: the scrap step stops being a stub, so the step, its availability and its draft answer change together; and the board starts supplying a card fact that until now only a work order supplied. What it must not do is give the generic card a second lugger resolution.

## Summary

The scrap step stops being a stub and asks the requester which scrap type the pickup is. Its choices are the plant's compiled scrap-type catalogue in its own order, followed by every type the lugger arrangement records that the catalogue does not name, with the two answers that mean "there is no scrap" left out. A chosen type is written into the draft, gated so the step cannot advance without one, recorded on the raised request as an answer, and read back by the board.

On the board, a generic scrap card resolves its lugger from the arrangement at draw time, through the same routine the work-order scrap card already uses. The board merges the stated type and the resolved lugger into the availability record the card already consumes, so no title, cell or template code needs a second branch.

Three parts of the shipped code contradict the spec's framing, and all three are handled here rather than assumed away. The scrap step, its page, its draft field and its route past the details step already exist, so FR-015 is a property of code that is already there. The `Temp` preview source and the scrap card variant named by FR-014 are already absent from the tree, removed in commit `723cec0f` and guarded by `TempArtefactBoundaryAuditTests`, so nothing is deleted on that account. And the option shape still carries an available-lugger count that nothing populates, a leftover of that preview, which this change removes.

The stack is unchanged. WinUI 3 on .NET 10, CommunityToolkit.Mvvm source generators, the existing navigation and page services.

## Project Structure

```text
MTM_Waitlist.Core/
  Models/GenericRequestScrapAnswer.cs                new        the type a raised request records
  Models/Lugger/LuggerArrangementSnapshot.cs         changed    says whether the read happened
  Services/ScrapTypeCatalog.cs                       changed    the one choices rule, promoted from Settings
  Services/LuggerArrangementService.cs               changed    an outage stamps an unread snapshot
MTM_Waitlist.Settings/
  Models/StatedScrapLugger.cs                        new        the lugger a stated type resolves to
  Services/IRequestJobPartAvailabilityProvider.cs    changed    a second entry point for a stated type
  Administration/ViewModels/LuggerArrangementViewModel.cs  changed  calls the promoted rule
MTM_Waitlist.GenericRequest/
  Models/GenericRequestOptions.cs                    changed    the option loses the lugger count
  ViewModels/GenericRequestScrapTypeViewModel.cs     changed    gates Continue, re-selects case-insensitively
  Services/GenericRequestLiveStepData.cs             changed    the real read, availability, answers_json
  Views/GenericRequestScrapTypePage.xaml             changed    the count columns go
  Strings/en-us/Resources.resw                       changed    the intro stops claiming a count
MTM_Waitlist.Waitlist.View/
  ViewModels/WaitlistViewViewModel.cs                changed    merges the stated facts into the card's availability
Services/
  RequestJobAvailabilityProvider.cs                  changed    one resolution routine, two entry points
Strings/en-us/Resources.resw                         changed    the gap and outage words
MTM_Waitlist.Tests/
  Module_Core/Services/ScrapTypeCatalogTests.cs      changed    the choices rule and its order
  Module_Core/Models/LuggerArrangementSnapshotTests.cs changed  WasRead, and the three outcomes
  Module_GenericRequest/GenericRequestScrapAnswerTests.cs new    round trip, missing keys, casing
  Module_GenericRequest/GenericRequestScrapTypeViewModelTests.cs new  gating, re-selection, the unavailable list
  Module_GenericRequest/GenericRequestLiveStepDataTests.cs changed  the read, the availability arm, the answers
  Module_GenericRequest/GenericRequestTestDoubles.cs changed    the read no longer answers unavailable
  Module_Waitlist/ViewModels/WaitlistViewViewModelTests.cs changed  the generic scrap card's lugger cell
  Module_Settings/ViewModels/LuggerArrangementViewModelTests.cs changed  the promoted rule is the one used
```

**Structure Decision**: the two facts this feature stores belong to `MTM_Waitlist.Core`, which is the leaf assembly every consumer already references: the recorded answer beside `GenericRequestDunnageAnswer`, and the choices rule inside the `ScrapTypeCatalog` that already owns the list. The lugger a stated type resolves to belongs to `MTM_Waitlist.Settings`, because that is the assembly holding the `RequestJobPartAvailability` record every card is drawn from. The composition root keeps the one routine that reads the arrangement, exactly where the work-order card's resolution already lives.

## The change, in the order it happens

```calls the stated scrap type, from the step to the card
  ReadScrapTypesAsync @ MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs:185
    GetSnapshotAsync @ MTM_Waitlist.Core/Services/LuggerArrangementService.cs:137
+   ScrapTypeCatalog.Compose @ MTM_Waitlist.Core/Services/ScrapTypeCatalog.cs:50
      LuggerArrangementSnapshot.ScrapTypes @ MTM_Waitlist.Core/Models/Lugger/LuggerArrangementSnapshot.cs:60
    LoadCoreAsync @ MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs:36
      CommitToDraft @ MTM_Waitlist.GenericRequest/ViewModels/GenericRequestScrapTypeViewModel.cs:51
    AnswersForAsync @ MTM_Waitlist.GenericRequest/Services/GenericRequestLiveStepData.cs:314
```

```calls how the generic scrap card learns its lugger
  CreateSessionOrder @ MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs:1158
    ResolveDeclaredCell @ MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs:1440
      ResolveContext @ MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs:81
+   GetStatedScrapLuggerAsync @ MTM_Waitlist.Settings/Services/IRequestJobPartAvailabilityProvider.cs:25
      ApplyLuggerLocationAsync @ Services/RequestJobAvailabilityProvider.cs:180
```

## Code pins

```csharp sketch MTM_Waitlist.Core/Models/GenericRequestScrapAnswer.cs
public sealed record GenericRequestScrapAnswer(string ScrapType)
{
    public const string ScrapTypeKey = "scrap.type";

    public Dictionary<string, string> ToAnswers() =>
        new(StringComparer.OrdinalIgnoreCase) { [ScrapTypeKey] = ScrapType };

    public static GenericRequestScrapAnswer? TryRead(IReadOnlyDictionary<string, string>? answers) =>
        Read(answers, ScrapTypeKey) is { Length: > 0 } type ? new GenericRequestScrapAnswer(type) : null;
}
```

- `ScrapTypeCatalog.Compose` takes the arrangement's own type names and returns the choices. It adds the catalogue first, in `All`'s order, then each assigned name it does not already hold, comparing trimmed and case-insensitively, and drops `ScrapDecisionRules.RequiredPlaceholder` and `ScrapDecisionRules.NoScrap`.
- `LuggerArrangementSnapshot.WasRead` is the only thing that separates an outage from an arrangement with no rows. `Create` sets it true; `Empty`, which the service returns when the read failed and which is also the type's initialiser, leaves it false.
- The stated-scrap merge runs only where `WaitlistCardCellRules.IsScrapItem(request.Item)` and `request.IsGeneric` both hold, so a work-order scrap card takes exactly the path it takes today.

## States and screens

```states the stated scrap type, from the step to the card
Unread: The arrangement read is in flight and nothing is offered. shows scrap-step (start)
Choices: The read answered and the composed list is offered. shows scrap-step
Chosen: One type is selected and Continue is enabled. shows scrap-step
NoList: The read failed, so no list at all is offered. shows scrap-step
Stated: The type is on the request and the details step is skipped.
Resolved: The card names the type and the lugger assigned now. shows board-card
Gap: The card names the type and reports no assignment. shows board-card (final)
LuggerUnavailable: The card names the type and reports the lugger unavailable. shows board-card (final)
Unread -> Choices: the read answered
Unread -> NoList: the read failed
NoList -> Choices: a retry answered
Choices -> Chosen: a type is picked
Chosen -> Stated: Continue
Stated -> Resolved: a lugger is assigned
Stated -> Gap: no lugger is assigned
Stated -> LuggerUnavailable: the arrangement is unreadable
Resolved -> Gap: the type is unassigned later
Resolved -> LuggerUnavailable: a later read fails
grid:
Unread Choices Chosen Stated
NoList . Resolved .
. . Gap LuggerUnavailable
```

```screen scrap-step The scrap type step of the generic request wizard
title: State the scrap type
row:
  text: the step's own question, from GenericRequest_Scrap_Title.Text
row: (1)
  list: one row per choice, in the catalogue's order then the arrangement's outside types
  field: the chosen type, kept when the step is revisited
row: (2)
  button: Continue, enabled only once a type is chosen
  button: Back
row:
  text: the attempt time and a retry, with no list at all
```

1: The two answers that mean there is no scrap are not destinations, so they are never offered.
2: The unavailable state offers no shortened list, because a shorter list would silently drop types the plant ships against.

```screen board-card The generic scrap card on the board
title: the scrap type the requester stated
row:
  text: the type in the wording the requester chose
row: (1)
  text: the lugger the arrangement assigns now
row:
  chip: Not assigned when no lugger is assigned
  chip: Temporarily unavailable when the arrangement could not be read
row:
  button: the card's own actions, unchanged
```

1: The lugger is a live read at draw time, so a reassignment shows on the next load and nothing is copied onto the request.

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS. Every requirement FR-001 to FR-016 is met by this plan or by code already present, and the plan adds no requirement the spec does not state. Tasks are ticked only against a clean build and a green suite. |
| II. Live Data Integrity | PASS. The arrangement is read live from `mtm_waitlist` on every visit and every card draw. No mock, sample or cached list is introduced, `MTM_Waitlist.Mock` is untouched, and a failed read is the step's own unavailable state with the attempt time and a manual retry. |
| III. Stored-Procedure-First | PASS. No new SQL and no schema change. The read reuses `sp_waitlist_lugger_arrangement_list` through `ILuggerArrangementService`, and the write reuses the queue's existing submit path, storing the type in `answers_json`. No `create.sql` or `rollback.sql` is needed and `InlineSqlAuditTests` is unaffected. |
| IV. MCP-First, Grounded Decisions | PASS. Every repository fact came from the code, through four mapped reader passes and direct reads of the files they named. No library choice is new, so no external grounding was needed. |
| V. WinUI 3 Platform Conformance | PASS. `Microsoft.UI.Xaml` throughout, MVVM Toolkit generators, the existing step frame and page services, no new converter and no new page. The only XAML change removes two columns and a label from an existing `DataTemplate`. |
| VI. Evidence-Based Verification Gates | PASS. Clean solution build with zero errors, then the test project. A masked `WMC9999` is read as a real XAML error. No live-database test is affected because no database artifact changes. |
| VII. Readable, End-User-Facing Delivery | PASS. The report is plain English and the open actions come last. |
| Additional: Documentation & Extensibility | PASS. `CHANGELOG.md` and the XAML surface inventory are updated in the same change, because a card cell gains two wordings and a step loses two columns. |

No principle is violated, so there is no Complexity Tracking table.

## Size note that governs tasks

The recorded size is `oversized`, so no artifact is trimmed. The research, the data model and the contracts are all produced, and the tasks that follow are expected to be numerous. What this plan deliberately does not do is take on work the spec does not ask for: the work-order scrap card keeps its current path, the Setup picker keeps its own sentinel-bearing list, and no second scrap read is written.

## Open item carried to implement

The spec's design note of 2026-11-03 requires a design clarification before the screen is built: what the step shows, what it shows when its source is unavailable, and the exact wording. The mechanism is settled here and the state names are fixed. The words are not. Three resource keys are fixed and their English is agreed at implement time: `GenericRequest_Scrap_Intro.Text` is rewritten because it currently promises a lugger count that is being removed, `GenericRequest_Scrap_Luggers.Text` is deleted with the count, and `WaitlistRequest_Scrap_LuggerNotAssigned.Text` and `WaitlistRequest_Scrap_LuggerUnavailable.Text` are new. The first implementation task must not paint the page until that wording is agreed.
