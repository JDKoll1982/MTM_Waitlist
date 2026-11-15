# Contract: the die search read

What the search asks, what it answers, and what the three answers mean. A test codes against these strings; the page does not.

## The queue script

`Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql`, run through `IVisualQueryExecutor.ExecuteAsync`, live only — no read shape, no `mtm_mock` mirror (research R8).

| Parameter | Type | Rule |
|---|---|---|
| `@Term` | `nvarchar(190)` | The term already normalised by the caller. The script trims it and matches it; it does not re-case or re-pad it. |

| Column | Type | Rule |
|---|---|---|
| `DieNumber` | `nvarchar` | The die's `PART.ID`. |
| `Description` | `nvarchar` | The die's `PART.DESCRIPTION`, trimmed, as recorded. |
| `Location` | `nvarchar` | The die's home location, or empty. Never null. |
| `Association` | `nvarchar` | `Die` when the term was the die's own number, `Explicit` when `PART_ALTERNATE` named it, `History` when the part's work-order requirements named it. |

Ordering and deduplication are the script's: one row per die number, `Die` before `Explicit` before `History`, then by `DieNumber`.

## The seam

```csharp
Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(
    string searchText,
    CancellationToken cancellationToken = default);
```

Reached through `IGenericRequestStepData.ReadDiesAsync`, which the die step already asks.

## What each answer means

| Source answer | Presentation | What the requester sees |
|---|---|---|
| `Ok` with rows | `Populated` | The dies, the count above them, Continue enabled once one is marked. |
| `Ok` with no rows | `Empty` | The step's own line, whose wording names Infor Visual as the source searched. Continue stays disabled (FR-002). |
| `Unreachable` | `Unavailable` | The step's sentence with the attempt time, a retry, and the typed term kept for the retry (FR-005). |
| `Failed` | `Unavailable` | As above. A read that failed proves nothing about the die, so no die may be accepted from it (FR-002). |
| Empty term | `Empty` | No search is run; nothing is claimed about any die. |

## Normalisation

`MTM_Waitlist.Module_Waitlist.Services.PartMatchSearchInput.Normalize`, unchanged: upper-case, and zero-pad the digits to ten characters when the term starts with a known prefix. A normalised term starting with `FGT` is a die number and is matched exactly; a term with no known prefix is also tried once as `FGT` plus the zero-padded term (research R2).
