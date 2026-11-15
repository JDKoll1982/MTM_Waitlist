# Contract: the die a request carries

What the flow stores about a chosen die, and what the board reads back. These strings are the interface; renaming one is a change to the contract.

## Answers written on the request

| Key | Written when | Value |
|---|---|---|
| `die.number` | a die is stated | The die's Infor Visual number, trimmed. |
| `die.location` | a die is stated and its location is known | The die's location, trimmed. Omitted when unknown, never written empty. |
| `input_value` | always for a die request | The die's number. This is what keeps two requests raised from one item apart (FR-011). |

`answers` also keeps the part, dunnage and scrap answers a request may carry; the die keys never displace them.

## One request per chosen die

- Choosing `n` dies raises `n` requests, each with `input_value` set to that die's number and the same `die.number` / `die.location` pair.
- The confirmation step lists every chosen die before anything is raised.
- The result step reports how many requests were raised; with one it reports that request's number as it does today.
- Choosing no die does not advance the flow (FR-013).

## The card

| Card read | Work-order request | Generic request |
|---|---|---|
| die number | the stored `input_value`, matched against the job's dies | the stored `die.number` |
| die location | the matched job die's location | the stored `die.location` |

A generic request has no job, so nothing is looked up on the board and nothing is borrowed from a job snapshot. A generic die request whose stored answer carries no location draws the card's own absence wording rather than a blank cell (FR-006, FR-012).

## Step availability

`IGenericRequestStepData.ReadStepAvailabilityAsync(GenericRequestStep.Die)` answers `Ready`. It answered `Unavailable` while this chunk was outstanding, and every other step's answer is unchanged.
