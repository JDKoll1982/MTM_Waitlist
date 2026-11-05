# Contract: what the part step must have before it advances

**Feature**: 017 Generic Work Center — Stated Part, Sequences and Quantity
**Plan**: [plan.md](../plan.md) | **Data model**: [data-model.md](../data-model.md)

`GenericRequestStepViewModel.ContinueCommand` is enabled only when `IsPopulated && CanAnswer`. The
part step overrides `CanAnswer` to say what it needs, and `CommitToDraft` writes what it has. This
contract states both, so the screen and the flow rules agree about when the step may be left.

## 1. Steps in the flow

`GenericRequestFlowRules` decides which step comes next from the draft. This chunk changes none of
its ordering. It changes what the part step writes into the draft, which is what the later steps read
(FR-016).

## 2. The part step's `CanAnswer`

True only when all three hold:

1. a part is accepted — a number that at least one source named (FR-002);
2. a sequence is chosen from the list the accepted part produced (FR-007);
3. the chosen sequence's quantity reading is **not** `Unavailable` (FR-017).

A reading of `OnHand` with a figure of `0` satisfies (3): the requester may raise a request for a part that
is not on hand, and the step has already warned them (FR-015).

## 3. The part step's `CommitToDraft`

| Draft field | Written from |
| --- | --- |
| `PartNumber` | the accepted part |
| `Sequence` | the chosen sequence |
| `SequenceSource` | the chosen sequence's joined sources (FR-016) |
| `PartStage` | the reading's derived stage |
| `QuantityReading` | the reading |

`Draft.Quantity` is the requested quantity and is never written here (R8).

## 4. Clearing on a part change (FR-008)

When the accepted part changes, the step clears `Sequence`, `SequenceSource`, `PartStage` and
`QuantityReading` before it asks for the new part's sequences. It never clears another step's answer.

## 5. Refusals

| Situation | What the step says | Where |
| --- | --- | --- |
| no source named the typed number | `GenericRequest_Part_NoMatch.Text`, naming both sources | FR-002 |
| the part is known and no sequence can be attributed | `GenericRequest_Part_NoSequence.Text` | FR-003 |
| the part read could not reach either system | the frame's own unavailable state, with the attempt time and a retry | FR-017 |
| the sequence list could not be read | an inline line in the sequence region, with that read's own retry | plan OD-1 |
| the quantity could not be read | an inline line in the quantity region, with that read's own retry | plan OD-1 |

The three independent reads each carry their own state. A failed quantity read leaves the sequence
list the requester is looking at visible (plan OD-1).

## 6. What is not asked

The step never asks which system owns the quantity (FR-012) and never asks the requester to type a
quantity (FR-012). The stage selector that spec 015 carried is removed; the derived stage is shown as
a read-only label beside the chosen sequence (plan OD-2).
