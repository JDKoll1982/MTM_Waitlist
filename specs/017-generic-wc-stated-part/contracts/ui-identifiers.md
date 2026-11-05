# Contract: the pinned names a consumer codes against

**Feature**: 017 Generic Work Center — Stated Part, Sequences and Quantity
**Plan**: [plan.md](../plan.md) | **Step data**: [step-data.md](./step-data.md) | **Navigation**: [navigation.md](./navigation.md)

These names are the contract. A rename is a change to this file, not a detail of the code that
happens to hold them.

## 1. Seam members and types

```text
MTM_Waitlist.GenericRequest/Contracts/IGenericRequestPartData.cs
  ReadCompletionsAsync(string term)
  ReadSequencesAsync(string partNumber)
  ReadQuantityAsync(string partNumber, string sequence)

MTM_Waitlist.GenericRequest/Models/GenericRequestOptions.cs
  GenericRequestPartOption(PartNumber, Description, Source)
  GenericRequestSequenceOption(Sequence, Description, Sources, OperationStatus, IsLastOperation, NextResource, HasCancelledOnlyOperations)
  SequenceSource          VisualOwnPart | VisualRequirement | WipFloor
  GenericRequestQuantityKind   OnHand

MTM_Waitlist.GenericRequest/Models/GenericRequestQuantityReading.cs
  Kind, Figure, AnsweredBy, Presentation, AttemptedAtUtc
  OnHand(figure, answeredBy, stage, attemptedAtUtc) | Unavailable(attemptedAtUtc)

MTM_Waitlist.GenericRequest/Contracts/GenericRequestStep.cs
  GenericRequestPartStage   Wip | FinishedGood | Unknown

MTM_Waitlist.GenericRequest.Data/Services/GenericRequestPartData.cs
  GenericRequestPartData : IGenericRequestPartData
```

## 2. Script paths

```text
Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql               extended: Description
Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequences.sql             new
Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequenceState.sql         new
Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql   reused, live only
Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipInventoryPartMatches.sql      new
Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipPartOperations.sql            new
Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipOperationQuantity.sql         new
```

Script parameters, as the app passes them:

| Script | Parameter |
| --- | --- |
| `GetPartMatches.sql` | `Term` |
| `GetPartSequences.sql` | `PartNumber` |
| `GetPartSequenceState.sql` | `PartNumber`, `Sequence` |
| `GetPartWarehouseQuantities.sql` | `PartNumber` |
| `GetWipInventoryPartMatches.sql` | `PartNumber` |
| `GetWipPartOperations.sql` | `PartNumber` |
| `GetWipOperationQuantity.sql` | `PartNumber`, `Operation` |

## 3. Row columns the app reads

| Script | Columns |
| --- | --- |
| `GetPartMatches.sql` | `PartNumber`, `Description` |
| `GetPartSequences.sql` | `Sequence`, `Description`, `OperationStatus`, `IsLastOperation`, `NextResource`, `SequenceSource`, `CancelledOnly` |
| `GetPartSequenceState.sql` | `OperationStatus`, `IsLastOperation`, `NextResource` |
| `GetPartWarehouseQuantities.sql` | `PartNumber`, `WarehouseQuantity` |
| `GetWipInventoryPartMatches.sql` | `PartNumber` |
| `GetWipPartOperations.sql` | `Sequence` |
| `GetWipOperationQuantity.sql` | `OperationQuantity` |

`SequenceSource` carries the values `OwnPart` and `Requirement`; the app maps them onto
`VisualOwnPart` and `VisualRequirement`.

## 4. Resource keys — part step

`MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`

| Key | Meaning |
| --- | --- |
| `GenericRequest_Part_Search.PlaceholderText` | the completion box's prompt |
| `GenericRequest_Part_Title.Text` | the step's heading |
| `GenericRequest_Part_Intro.Text` | the step's introduction |
| `GenericRequest_Part_Empty.Text` | the step read nothing |
| `GenericRequest_Part_NoMatch.Text` | the typed number named by neither source |
| `GenericRequest_Part_Sequence_Title.Text` | the sequence list's heading |
| `GenericRequest_Part_NoSequence.Text` | the part is known and no sequence can be attributed |
| `GenericRequest_Part_Sequences_Unavailable.Text` | the sequence list could not be read |
| `GenericRequest_Part_Sequences_Retry.Content` | that list's retry |
| `GenericRequest_Part_Sequence_Sources.Text` | `Sources: {0}` |
| `GenericRequest_Part_Source_VisualOwnPart.Text` | `Infor Visual (own part)` |
| `GenericRequest_Part_Source_VisualRequirement.Text` | `Infor Visual (requirement)` |
| `GenericRequest_Part_Source_WipFloor.Text` | `WIP floor` |
| `GenericRequest_Part_Quantity_Title.Text` | the quantity region's heading |
| `GenericRequest_Part_Quantity_OnHand.Text` | `On hand` |
| `GenericRequest_Part_Quantity_AnsweredBy.Text` | `Answered by {0}` |
| `GenericRequest_Part_NextResource.Text` | `Next resource` — the label beside the resource the WIP tag prints |
| `GenericRequest_Part_Quantity_Unavailable.Text` | the quantity could not be read |
| `GenericRequest_Part_Quantity_Retry.Content` | that reading's retry |
| `GenericRequest_Part_Quantity_NothingOnHand.Text` | the warning shown when the owner holds none (FR-015) |
| `GenericRequest_Part_Stage.Header` | `Stage` — the label beside the derived stage |
| `GenericRequest_Part_Stage_Wip.Content` | `Work in process` |
| `GenericRequest_Part_Stage_FinishedGood.Content` | `Finished good` |
| `GenericRequest_Part_Stage_Unknown.Content` | `Not recorded` |

The stage selector is gone (plan OD-2); `GenericRequest_Part_Stage.Header` survives as the label of the
read-only derived stage.

## 5. Card labels

`Strings/en-us/Resources.resw` — the board's shared card resources, because a generic request is drawn by
the same `WaitlistLineCardView` the work-order rows use.

| Key | Meaning |
| --- | --- |
| `Waitlist_Card_Part.Label` | `Part` |
| `Waitlist_Card_Sequence.Label` | `Sequence` |
| `Waitlist_Card_OnHandBy.Label` | `On hand ({0})` — the argument is the answering system |
| `Waitlist_Card_OnHand.Label` | `On hand`, drawn when a figure was read but no system was named |
| `Waitlist_Card_System_WipFloor.Text` | `WIP floor` |
| `Waitlist_Card_System_InforVisual.Text` | `Infor Visual` |

Card cell rules (`WaitlistViewViewModel.AddGenericPartCells`), added after the item's own declared cells:

```text
Part
Sequence
On hand (<system>)   ← drawn whenever a figure was read, including 0
```

A quantity that was not read draws no cell at all (FR-020). A reading of `0` draws the cell (FR-013).
There is no not-inventoried cell: outside service is a next resource a WIP tag names, so a request whose
operation hands the part to outside service carries its quantity like any other (FR-019).
