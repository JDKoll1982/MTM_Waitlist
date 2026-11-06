# Scrap Surface

## Purpose

The generic request flow needs a scrap step that asks the requester which scrap type the pickup is. The board needs a scrap card that names the stated type and the lugger assigned now. This contract fixes the one step read, the option it returns, the availability answer the board uses, the card outcomes, the step's gating and the resource keys that change.

## The step read

`IGenericRequestStepData.ReadScrapTypesAsync` is the one entry point the scrap step uses. It returns `GenericRequestStepResult<GenericRequestScrapOption>`.

| Presentation | Meaning |
| --- | --- |
| `Populated` | the read answered and the composed list is offered |
| `Empty` | the read answered and there are no rows |
| `Unavailable` | the read failed, so no list at all is offered |

For the scrap step, `Populated` and `Unavailable` are the presentations that occur. `Empty` does not arise, because the shared scrap-type list always ships with the plant's types and the only empty outcome is a failed read.

## The option

`GenericRequestScrapOption` is the scrap type and nothing else.

| Member | Meaning |
| --- | --- |
| `ScrapType` | the type, in the catalogue's or the arrangement's wording |

The available-lugger count leaves the record. The option no longer carries `AvailableLuggerCount` or its display.

## The availability answer

`IRequestJobPartAvailabilityProvider.GetStatedScrapLuggerAsync` returns the `StatedScrapLugger` record. The board calls it only for a request that `WaitlistCardCellRules.IsScrapItem` accepts and that `WaitlistRequest.IsGeneric` marks generic.

| Field | Meaning |
| --- | --- |
| `LuggerLocation` | the lugger name, or null when none is assigned and when the arrangement was not read |
| `LuggerImagePath` | the picture that belongs to the lugger |
| `ArrangementRead` | whether the arrangement read answered |

The `Resolution` the `LuggerArrangementSnapshot` returns decides the outcome. It is the same routine the work-order scrap card uses, so the two card kinds cannot disagree.

The board merges the stated type and the resolved lugger into the `RequestJobPartAvailability` it already hands `CreateSessionOrder` and `ResolveDeclaredCell`. `GenericRequestScrapAnswer.TryRead` supplies the `ScrapType`. The provider supplies the `LuggerLocation` and the picture.

## The card outcomes

The lugger cell carries one of three outcomes.

| Outcome | Cell value |
| --- | --- |
| a lugger resolves, including a name with its status when every lugger is blocked | the name the `Resolution` returned |
| `NotConfigured` | `WaitlistRequest_Scrap_LuggerNotAssigned.Text` |
| the arrangement was not read | `WaitlistRequest_Scrap_LuggerUnavailable.Text` |

The keys `WaitlistRequest_Scrap_LuggerNotAssigned.Text` and `WaitlistRequest_Scrap_LuggerUnavailable.Text` are new, and their English is agreed at implement time. The mechanism is fixed here and the words are not.

## The step's gating

`CanAnswer` is false until a type is chosen. The flow does not advance without a chosen type. Going back to the step re-selects the chosen type with a trim and an `OrdinalIgnoreCase` comparison, so a draft that differs only in case or surrounding spaces still shows as chosen.

## Resource keys

| Key | Change |
| --- | --- |
| `GenericRequest_Scrap_Intro.Text` | rewritten, because it currently promises a lugger count that is being removed |
| `GenericRequest_Scrap_Luggers.Text` | deleted with the count |

## Not part of this contract

There is no HTTP endpoint. There is no command line. No database artifact is added. The work-order scrap card keeps its current path and is not re-specified here.
