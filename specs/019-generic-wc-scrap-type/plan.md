# Implementation Plan: Generic Work Center — Stated Scrap Type and Lugger

## Summary

A generic work-center request that asks for a scrap pickup must let the requester state the scrap type, because there is no job to read a decision from. The step already exists and is wired into the flow; what it lacks is a real source, a gate on the choice, and a way to keep the answer. The choices come from the plant's one shared scrap-type list with any arrangement-only type appended, the answer rides the request's existing answers column, and the card names the type and the lugger the shared arrangement assigns to it. The lugger is resolved live through the same rule set the work-order scrap card uses, so the two cannot disagree.

## Project Structure

```text
MTM_Waitlist.Core/
  Models/GenericRequestScrapAnswer.cs          new: the stated type, read from and written to answers_json
  Models/Lugger/LuggerArrangementSnapshot.cs   unchanged: Resolve stays the one rule set, used by both cards
  Services/ScrapTypeCatalog.cs                 unchanged: the one list, in its own order

MTM_Waitlist.GenericRequest/
  Models/GenericRequestOptions.cs              unchanged: GenericRequestScrapOption already carries the count
  Models/GenericRequestDraft.cs                unchanged: ScrapType already exists
  ViewModels/GenericRequestScrapTypeViewModel.cs   gate Continue on a choice; match the restored choice loosely
  ViewModels/GenericRequestWorkCenterViewModel.cs  clear the stated type when the work center changes
  Services/GenericRequestLiveStepData.cs       real scrap read; Scrap step no longer unavailable; store the answer

MTM_Waitlist.Waitlist.View/
  Models/WaitlistRequestTitles.cs              fill {scrap_type} from the stored answer when there is no job
  ViewModels/WaitlistViewViewModel.cs          resolve a generic scrap card's two cells from the answer + arrangement

MTM_Waitlist.Tests/
  Module_Core/Models/GenericRequestScrapAnswerTests.cs        new
  Module_GenericRequest/GenericRequestScrapTypeViewModelTests.cs   new
  Module_GenericRequest/GenericRequestScrap*Tests.cs          new: the read, the stored answer, the card cells
```

**Structure Decision**: the answer model goes in `MTM_Waitlist.Core` beside the two answer records it copies, because both the flow that writes it and the board that reads it already reference that project. Everything else stays in the project that already owns it, so no new project, route or registration is introduced.

## Calls

The scrap step and the card reach the same rule set from two directions, and neither crosses into the other's project:

```text
GenericRequestScrapTypeViewModel.ContinueCommand
  → GenericRequestStepViewModel.ContinueAsync
  → CommitToDraft  (Draft.ScrapType = Selected.ScrapType)

GenericRequestLiveStepData.ReadScrapTypesAsync
  → ScrapTypeCatalog.All                       (order)
  → ILuggerArrangementService.GetSnapshotAsync (outside types + the free-lugger count)
  → ScrapDecisionRules.IsNoScrap / IsRequiredPlaceholder   (both filtered out)

GenericRequestLiveStepData.SubmitAsync
  → AnswersForAsync → GenericRequestScrapAnswer.ToAnswers() → answers_json

WaitlistViewViewModel.ResolveDeclaredCells   (request.IsGeneric)
  → GenericRequestScrapAnswer.TryRead(request.Answers)   → the "scrap type" cell
  → LuggerArrangementSnapshot.Resolve(statedType)        → the "lugger location" cell
```

## States

The card's lugger cell has three outcomes, and they are separated by the snapshot, not by the resolver alone.

```text
Resolved:      the arrangement holds rows and Resolve names a lugger — including the all-blocked case, where the
               name arrives with its status beside it. shows generic-scrap-card
Gap:           the arrangement holds rows and Resolve reports a configuration gap. The gap is recorded once and no
               lugger is drawn; the type is still named. shows generic-scrap-card
Unavailable:   the arrangement was read as empty, so it could not be read or holds nothing. The type is still named
               and the lugger is reported unavailable, never a stale or guessed one. shows generic-scrap-card
```

The step itself needs no new state: the shared frame already draws loading, populated, empty and unavailable with a retry.

```screen generic-scrap-card The waitlist card of a generic scrap request
title: Scrap
row:
  text: Scrap Type
  text: Lugger Location
text: The type reads in the wording the requester chose.
text: The lugger is what the arrangement assigns to that type right now, with blocking and backup applied.
```

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS — spec, plan, tasks precede code; each requirement lands with a test. |
| II. Live Data Integrity | PASS — both sources are read live; a failed arrangement read reports unavailable and substitutes nothing. |
| III. Stored-Procedure-First | PASS — no new read or write; the arrangement read already goes through `sp_waitlist_lugger_arrangement_list`, and the answer rides the existing insert and list procedures. |
| IV. MCP-First, Grounded Decisions | PASS — the design follows patterns read out of this codebase (`LuggerArrangementSnapshot.Resolve`, the two existing answer records); no unfamiliar platform API is introduced. |
| V. WinUI 3 Platform Conformance | PASS — the existing page and the shared step frame are reused; no XAML change is needed. |
| VI. Evidence-Based Verification Gates | PASS — the suite runs before the step closes; the three card outcomes and the gate each get a test that fails without the change. |
| VII. Readable, End-User-Facing Delivery | PASS — replies and docs are plain English. |

**Complexity Tracking**: none. No violation is required.

## Phase 1 design

The entities and their validation rules are in [data-model.md](./data-model.md). The identifiers a test codes against are in [contracts/](./contracts/). The four choices the codebase did not settle are in [research.md](./research.md).
