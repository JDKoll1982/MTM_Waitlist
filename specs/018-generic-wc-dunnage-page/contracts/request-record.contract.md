# Request Record

## Purpose

This contract fixes what a raised generic dunnage request stores, because the board and the detail read it back. A generic request has no job, so the request itself must carry the chosen dunnage part, its type, its picture and whether it is still offered.

## The recorded answer

`GenericRequestDunnageAnswer` is defined in `MTM_Waitlist.Core.Models`. It is recorded into the request's existing `answers_json` column under four keys.

| Key | Meaning |
| --- | --- |
| `dunnage.part` | the chosen part |
| `dunnage.type` | the part's type |
| `dunnage.picture` | the part's picture reference |
| `dunnage.offered` | whether the part is still in the catalogue at raise time |

The keys are strings and are compared case-insensitively. This matches the existing `GenericRequestPartAnswer` in the same folder.

There is no schema change and no new stored procedure.

## The captured value

`input_value` carries the chosen dunnage part when the item asks for dunnage. This mirrors the rule already applied to a die request, where the stored die is the value the card names.

## Card and detail

The card's second line resolves the token `{dunnage_part}` from `input_value`, resolved by `WaitlistRequestTitles.ResolveContext`.

The picture is resolved by `WaitlistRequestTitles.ResolveDunnagePartPicturePath`. When no job snapshot is in hand, that method falls back to the request's own `dunnage.picture`.

## A part that left the catalogue

A part that is no longer in the catalogue at raise time is still named on the request. Its `dunnage.offered` key is `false`.

The wording that shows this to the requester is an open implement-time clarification, required by the spec's design note of 2026-11-03. The mechanism is fixed here and the words are not.

## Not part of this contract

There is no HTTP endpoint. There is no command line. No database artifact is added. The work order substitute dialog's behaviour is unchanged and is not re-specified here.
