# Implementation Plan: Generic Work Center — Dunnage Catalogue Page

**Feature Branch**: `018-generic-wc-dunnage-page`
**Spec**: [spec.md](./spec.md)
**Created**: 2026-11-05

## Scale note

This change reaches five assemblies: `MTM_Waitlist.Shared`, `MTM_Waitlist.Setup`, `MTM_Waitlist.GenericRequest`, `MTM_Waitlist.Core` and `MTM_Waitlist.Waitlist.View`, plus three app-project folders and the composition root. About eighteen files change, six of them new. Two boundary points carry the risk and deserve the reviewer's attention: the new seam between the generic flow and the Setup catalogue, which must not turn into an assembly reference, and the submission path, which must carry the chosen part onto the raised request without a schema change.

## Summary

The generic dunnage step stops being a two-list type-then-part screen and becomes the plant's image-backed dunnage catalogue drawn as a page of the wizard. The catalogue's markup and its state are lifted out of the Setup image-search dialog into one control and one shared view model, so the dialog and the page show one catalogue from one read. A chosen part is written into the draft, the step is gated so it cannot advance unanswered, and the part plus its picture are recorded on the raised request's existing `answers_json` column, which is what lets the confirmation, the card and the detail name it and draw it.

Two parts of the shipped code contradict the spec's own assumptions, and both are handled here rather than assumed away. The `Temp` preview source named by FR-011 is already absent from the tree, removed in commit `723cec0f`; nothing is left to delete, so FR-011 is satisfied by the tree as it stands and the audit test that keeps it out stays in place. And the catalogue is not reachable as a reusable view today (A1), so extracting it is part of this chunk.

The stack is unchanged. WinUI 3 on .NET 10, CommunityToolkit.Mvvm source generators, and the existing navigation and page-service stack.

## Project Structure

```text
MTM_Waitlist.Shared/
  Models/DunnageCataloguePart.cs                      new    the row both surfaces draw
  Services/IDunnageCatalogueReader.cs                 new    the one catalogue read, as a seam
  ViewModels/DunnageCatalogueViewModel.cs             new    filter, picture choice, selection, load outcome
MTM_Waitlist.Setup/
  Services/DunnageCatalogueReader.cs                  new    implements the seam over IDunnageWorkflowService
  ViewModels/SetupDunnageImageSearchDialogViewModel.cs changed  delegates to the shared view model
MTM_Waitlist.Core/
  Models/GenericRequestDunnageAnswer.cs               new    the part a raised request records
MTM_Waitlist.GenericRequest/
  ViewModels/GenericRequestDunnageViewModel.cs        rewritten  hosts the catalogue, gates the forward button
  Contracts/IGenericRequestStepData.cs                changed  ReadDunnageAsync removed
  Services/GenericRequestLiveStepData.cs              changed  submission carries the chosen part
  Controls/GenericRequestStepFrame.xaml + .xaml.cs    changed  gains a loading state
  Models/GenericRequestOptions.cs                     changed  GenericRequestDunnageOption removed
  Views/GenericRequestDunnagePage.xaml + .xaml.cs     deleted  the page moves to the app
MTM_Waitlist.Waitlist.View/
  Models/WaitlistRequestTitles.cs                     changed  the dunnage picture falls back to the request
Module_Shared/Controls/
  DunnageCatalogueView.xaml + .xaml.cs                new    the catalogue markup, out of the dialog
Module_GenericRequest/Views/
  GenericRequestDunnagePage.xaml + .xaml.cs           new    the step page, hosting the control
Module_Setup/Views/
  SetupDunnageImageSearchDialog.xaml + .xaml.cs       changed  thin host of the same control
Services/DependencyInjection/ServiceRegistrationExtensions.cs  changed  reader, view model, page route
Strings/en-us/Resources.resw                          changed  the page's own words
MTM_Waitlist.Tests/
  Module_Shared/ViewModels/DunnageCatalogueViewModelTests.cs   new
  Module_GenericRequest/GenericRequestDunnageViewModelTests.cs new
  Module_GenericRequest/GenericRequestDunnageAnswerTests.cs    new
  Module_GenericRequest/GenericRequestTestDoubles.cs           changed
  Module_Waitlist/Models/WaitlistRequestTitlesTests.cs         changed
```

**Structure Decision**: the view model and the part row go to `MTM_Waitlist.Shared` because that is the only assembly the Setup library and the generic-request library both reference, and it already owns every dunnage picture helper. The control and the step page go to the app project, which is where this repository's views and the composition root already live.

## What the change does, in the order it happens

```text
DunnageCatalogueReader.ReadAllAsync()          Setup      sp_Dunnage_Parts_GetAll on mtm_receiving_application
  -> IDunnageCatalogueReader                   Shared     the seam, so no library gains a reference
  -> DunnageCatalogueViewModel.LoadAsync()     Shared     sorts, filters, reports Ready / Empty / Unavailable
  -> DunnageCatalogueView.ViewModel            app        the same markup the dialog draws
  -> GenericRequestDunnageViewModel            GenericRequest  maps the outcome to the step presentation
  -> CanAnswer + CommitToDraft                 GenericRequest  gates the forward button, writes the draft
  -> IGenericRequestStepData.SubmitAsync       GenericRequest  merges the dunnage answers into answers_json
  -> WaitlistRequestTitles                     Waitlist.View   names the part and finds its picture
```

## Code pins

- `InputValue = draft.DunnagePart` for an item that asks for dunnage. The card's second line resolves `{dunnage_part}` from `input_value`, so a generic dunnage request that left that column empty would have a card naming nothing.
- The dialog keeps its flat failure sentence and the page reports unavailable with an attempt time and a retry. The shared view model returns the outcome and does not choose which of the two a surface shows.
- `CanAnswer => catalogue.SelectedPart is not null`. The old view model pre-selected the first type and the first part, which is why a part could be recorded that nobody chose.

## States and screens

```text
Loading: the catalogue read is in flight. Nothing is offered and nothing is claimed about what the plant holds. shows catalogue-page
Populated: the read returned parts. The filter and the picture choice narrow them. shows catalogue-page
Empty: the read succeeded and the plant holds no dunnage parts. This is an answer, not a failure. shows catalogue-page
Unavailable: the read failed. The attempt time and a retry are shown, never a shortened list. shows catalogue-page
Chosen: one part is selected. The forward button becomes available and the next visit shows that part as chosen. shows catalogue-page
Gone: a chosen part is absent from a later read. It is kept, and the request records that it is no longer offered. shows confirmation, shows detail
Raised: the request carries the part and its picture. shows confirmation, shows card, shows detail
```

```text
screen catalogue-page  the dunnage step of the generic request wizard
screen catalogue-dialog  the substitute picker laid over the work-order flow
screen confirmation  the summary step that names what was asked for
screen card  the board's tile for one request
screen detail  the opened request
```

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS. This plan satisfies spec 018 and adds no requirement it does not state. Tasks are ticked only against a clean build and a green suite. |
| II. Live Data Integrity | PASS. The catalogue is read live from `mtm_receiving_application` on every visit. No mock, sample or cached list is introduced, and `MTM_Waitlist.Mock` is untouched. A failed read is a per-screen unavailable state with the attempt time and a manual retry. |
| III. Stored-Procedure-First | PASS. No new SQL and no schema change. The read reuses `sp_Dunnage_Parts_GetAll` through the service that already calls it, and the write reuses the queue's existing submit path. No `create.sql` or `rollback.sql` is needed, and `InlineSqlAuditTests` is unaffected. |
| IV. MCP-First, Grounded Decisions | PASS. Every repository fact in this plan came from the code, through three mapped reader passes and direct reads. The WinUI decisions follow the ProgressRing and CommunityToolkit.Mvvm guidance retrieved from Microsoft Learn, and the hosting pattern is the one the existing step frame already uses. |
| V. WinUI 3 Platform Conformance | PASS. `Microsoft.UI.Xaml` throughout, MVVM Toolkit generators, existing navigation and page services, no new converter. The control's words move behind `.resw` keys while their English text stays byte-identical, so the dialog reads exactly as it does today. Layout stays fluid with `Auto` and star sizing. |
| VI. Evidence-Based Verification Gates | PASS. Clean solution build with zero errors, then the test project. A masked `WMC9999` is treated as a real XAML error. The dual-build lock and stale PRI techniques are used as documented. No live-DB integration test is affected because no database artifact changes. |
| VII. Readable, End-User-Facing Delivery | PASS. The report is plain English and the open actions come last. |
| Additional: Documentation & Extensibility | PASS. `CHANGELOG.md` and the XAML surface inventory are updated in the same change, because a control is added and one page moves between projects. |

No principle is violated, so there is no Complexity Tracking table.

## Size note that governs tasks

The recorded size is `oversized`, so no artifact is trimmed. The research, the data model and the contracts are all produced, and the tasks that follow are expected to be numerous. What this plan deliberately does not do is take on work the spec does not ask for: the Setup dialog keeps its current strings and structure, and no second dunnage read is written.

## Open item carried to implement

The spec's design note of 2026-11-03 requires a design clarification before the screen is built: what the step shows, what it shows when its source is unavailable, and the exact wording. The mechanism is settled here and the state names above are fixed. The words themselves are not, and the first implementation task must not paint the page until that wording is agreed.
