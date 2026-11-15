# Implementation Plan: Generic Work Center — Die Search

**Scale note**: this change reaches across five areas — a new Infor Visual queue script, the generic request data layer, the flow's draft and submit path, the die page, and the waitlist card — plus six test files. Read the Calls block before touching a file, because the die number travels from the search to the card through four of them.

## Summary

A generic die request has no job, so the die has to be found by asking Infor Visual: the page accepts a die (FGT) number or a part number, lists every die that matches with the location each one lives in, and lets the requester choose one or several. The search is one live read through `IVisualQueryExecutor` and a checked-in queue script, in the same shape the part step already uses, with no cached mirror. Each chosen die is its own request, and the die's number and location are written into the request's answers so the card can name them without a job.

## Project Structure

```text
Database/InforVisual/Queues/Module_Waitlist/Queries/
  GetDieSearch.sql                                   new: dies by die number, explicit part association, or work-order history

MTM_Waitlist.Core/Models/
  GenericRequestDieAnswer.cs                         new: die.number + die.location, read from and written to answers_json

MTM_Waitlist.GenericRequest/
  Contracts/IGenericRequestDieData.cs                new: the die read's own seam
  Models/GenericRequestOptions.cs                    GenericRequestDieOption gains the selectable card and its location text
  Models/GenericRequestDraft.cs                      SelectedDies beside DieNumber
  Services/GenericRequestLiveStepData.cs             real die read, Die step ready, one request per chosen die
  ViewModels/GenericRequestDieViewModel.cs           multi-select, visible count, refusal, selection kept across searches
  ViewModels/GenericRequestSummaryViewModel.cs       one row per chosen die
  ViewModels/GenericRequestResultViewModel.cs        the raised count when several requests were raised
  Views/GenericRequestDiePage.xaml                   count line, selectable rows, location cell
  Strings/en-us/Resources.resw                       die wording: count, no location recorded, nothing chosen, empty names Visual

MTM_Waitlist.GenericRequest.Data/Services/
  GenericRequestDieData.cs                           new: normalise the term, run the script, shape the rows

MTM_Waitlist.Waitlist.View/
  Models/WaitlistRequestTitles.cs                    a generic die request's die and location from its stored answer
  ViewModels/WaitlistViewViewModel.cs                a generic die request's job-sourced die cell from its stored answer

Services/DependencyInjection/ServiceRegistrationExtensions.cs   register the die seam

MTM_Waitlist.Tests/Module_GenericRequest/
  GenericRequestDieDataTests.cs                      new: the read, its three answers, the order of associations
  GenericRequestDieViewModelTests.cs                 new: search, count, selection, refusal, unavailable
  GenericRequestDieSubmissionTests.cs                new: one request per die, the stored answer
  GenericRequestTestDoubles.cs                       the die read on the step stub
MTM_Waitlist.Tests/Module_Waitlist/
  WaitlistRequestTitlesDieTests.cs                   new: a generic die card names the die and its location
```

**Structure Decision**: the answer record goes in `MTM_Waitlist.Core` beside `GenericRequestScrapAnswer` because both the flow that writes it and the board that reads it already reference that project. The read goes in `MTM_Waitlist.GenericRequest.Data`, which already owns the only other Infor Visual read the flow makes, so no new project, route or page registration is introduced.

## Calls

```text
GenericRequestDiePage
  → GenericRequestDieViewModel.SearchCommand → LoadAsync → IGenericRequestStepData.ReadDiesAsync(SearchText)
  → GenericRequestLiveStepData.ReadDiesAsync → IGenericRequestDieData.ReadDiesAsync
  → GenericRequestDieData.ReadDiesAsync
      PartMatchSearchInput.Normalize                    the same normalisation the part search applies
      IVisualQueryExecutor.ExecuteAsync(".../GetDieSearch.sql", {Term})
      Ok + rows            → Populated
      Ok + no rows         → Empty          (the requester sees no die matched, naming Infor Visual)
      Unreachable/Failed   → Unavailable    (attempt time and a retry, the typed term kept)

GenericRequestDieViewModel.ContinueCommand → CommitToDraft  (Draft.SelectedDies, Draft.DieNumber = first chosen)

GenericRequestSummaryViewModel.LoadCoreAsync → one row per chosen die

GenericRequestLiveStepData.SubmitAsync
  → AnswersForAsync → GenericRequestDieAnswer.ToAnswers()      die.number + die.location
  → IWaitlistRequestService.SubmitAsync                        once per chosen die, InputValue = that die's number

WaitlistRequestTitles.ResolveContext
  → GenericRequestDieAnswer.TryRead(request.Answers)           a generic request's die and location
```

## States

The die step keeps the shared frame's four presentations and adds no state of its own; what it adds is the rule that separates a search that matched nothing from a source that could not be asked.

```text
Ready:        the executor is configured. A search runs on entry (empty term) and on every Search. shows die-search
Populated:    at least one die matched. The count is drawn above the list, chosen dies stay marked. shows die-search
Empty:        the read succeeded and matched no die. The flow cannot continue; the line names Infor Visual. shows die-search
Unavailable:  the source could not be reached or the read failed. The attempt time and a retry are drawn, the
              typed term is kept, and Continue stays disabled. shows die-search
```

```screen die-search The die step of a generic request
title: Find the die
field: Die number or part number
button: Search
text: "3 dies match" — drawn above the list, the full number even when the list scrolls
list: FGT0602-01 | the die's own description | U-B0-01, or "No location recorded"
field: chosen — one checkbox per die, so one or several can be marked
text: "Choose at least one die before continuing." — when Continue is pressed with nothing chosen
```

```screen generic-die-card The waitlist card of a generic die request
title: Pickup Die — or Deliver Die, the Item's own umbrella word
field: the die's number
field: the die's location, or the card's own absence rather than a blank cell
text: One card per die chosen, each naming its own die.
```

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS — spec, plan and tasks precede the code, and every requirement lands with a test that fails without it. |
| II. Live Data Integrity | PASS — the die search is a live Infor Visual read with no mirror and no substitute; an unreachable source is reported, never filled from cache. |
| III. Stored-Procedure-First Database Discipline | PASS — the only new statement text is a checked-in Infor Visual queue script, which is how every Visual read this application makes is written; nothing is added to the internal stores. |
| IV. MCP-First, Grounded Decisions | PASS — every API and data shape in this plan was read out of this codebase (`IVisualQueryExecutor`, `PartMatchSearchInput`, `GetSubordinateParts.sql`, the Infor Visual schema exports under `docs/database`). The plant SQL Server is unreachable from this workstation, so no live data was consulted; the design follows the exported schema. |
| V. WinUI 3 Platform Conformance | PASS — the existing `Page`, the shared `GenericRequestStepFrame`, `ListView` and `CheckBox` are reused; no new control, no window, no notification. |
| VI. Evidence-Based Verification Gates | PASS — `dotnet build` and the test project run before the step closes; the read, the step and the submission each carry tests. |
| VII. Readable, End-User-Facing Delivery | PASS — replies and documents are plain English. |

**Complexity Tracking**: none. No violation is required.

## Phase 1 design

The entities and their rules are in [data-model.md](./data-model.md). The identifiers a consumer or test codes against are in [contracts/](./contracts/). The choices the codebase did not settle are in [research.md](./research.md).
