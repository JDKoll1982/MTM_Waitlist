# Contract: the step data seam

`MTM_Waitlist.GenericRequest.Contracts.IGenericRequestStepData`

One method per data need of the flow (FR-010). The interface is real, so it carries no `Temp` prefix; the
preview implementation is `TempGenericRequestStepData`.

```text
ReadWorkCentersAsync(CancellationToken)                    -> GenericRequestStepResult<GenericRequestWorkCenterOption>
ReadCategoriesAsync(workCenter, CancellationToken)         -> GenericRequestStepResult<GenericRequestCategoryOption>
ReadItemsAsync(category, CancellationToken)                -> GenericRequestStepResult<GenericRequestItemOption>
ReadPartsAsync(itemCode, CancellationToken)                -> GenericRequestStepResult<GenericRequestPartOption>
ReadSequencesAsync(partNumber, CancellationToken)          -> GenericRequestStepResult<GenericRequestSequenceOption>
ReadDunnageAsync(partNumber, CancellationToken)            -> GenericRequestStepResult<GenericRequestDunnageOption>
ReadScrapTypesAsync(CancellationToken)                     -> GenericRequestStepResult<GenericRequestScrapOption>
ReadDiesAsync(searchText, CancellationToken)               -> GenericRequestStepResult<GenericRequestDieOption>
ReadStepAvailabilityAsync(step, CancellationToken)          -> GenericRequestStepAvailability
```

The last method exists because three screens read no rows and are still screens: the details, summary and result
steps ask whether they have anything to show at all, so all ten steps report the same three presentations.

Guarantees a caller may rely on:

1. The method returns a result; it never throws for a read that failed. A failure is
   `Presentation = Unavailable` with `AttemptedAtUtc` set.
2. `Presentation = Populated` implies `Items` is non-empty, for the row-shaped results.
3. `Presentation = Empty` implies the read succeeded and found nothing.
4. Cancelling produces `OperationCanceledException`, because a cancelled read is not a failed read.

Nothing in this interface reads, writes or names a store. The preview implementation opens no connection
(FR-013).
