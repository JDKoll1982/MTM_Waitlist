# Contract: navigation between steps

`MTM_Waitlist.GenericRequest.Contracts.IGenericRequestNavigator`

```text
Draft        GenericRequestDraft   the answers given so far
CurrentStep  GenericRequestStep    the step on screen
GoTo(step)                         show that step
GoBack()                           show the step before the current one
```

`MTM_Waitlist.GenericRequest.Services.GenericRequestFlowRules` owns the order and the routing:

```text
NextStep(draft)  -> the step after the one the draft has reached
PreviousStep(step) -> the step before it, or the same step at the first step
```

Rules a caller may rely on:

1. The order is `WorkCenter`, `Category`, `Item`, `Part`, `Dunnage`, `Scrap`, `Die`, `Details`, `Summary`,
   `Result`, with the conditional steps visited only when the draft says they apply.
2. `Dunnage`, `Scrap` and `Die` are reached from `Part` according to what the chosen part and item need, so a
   request that needs a die never shows the dunnage page (US1 scenario 5).
3. `PreviousStep` never leaves the flow and never discards a draft value.

The preview application implements this interface by replacing the page it hosts. A later chunk implements it
against the shell's navigation service. No view calls a database, a service or a file directly (FR-005).
