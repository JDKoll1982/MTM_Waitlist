# Request Record

## Purpose

This contract fixes what a raised generic scrap request stores, because the board reads it back. A generic request has no job, so the request itself must carry the scrap type the requester stated.

## The recorded answer

`GenericRequestScrapAnswer` is defined in `MTM_Waitlist.Core.Models`. It is recorded into the request's existing `answers_json` column under one key.

| Key | Meaning |
| --- | --- |
| `scrap.type` | the type as the requester chose it, in their wording |

The key is a string and is compared case-insensitively. The answer is merged into `answers_json` only when the item requires scrap and the type is not empty.

There is no schema change and no new stored procedure.

## The value that is not used

`input_value` deliberately does not carry the scrap type. The dunnage answer used it because the card's second line reads `{dunnage_part}` from that column. A scrap card's `{scrap_type}` token is not bound to `input_value`, so writing the type there would add a second authority for the same fact.

## A type that changes after the request

A type removed from, or renamed in, the arrangement after the request was raised leaves the stored wording intact. The request keeps saying what the requester said. This is FR-011 and FR-013.

The lugger is never stored on the request, because the dump point is a live fact read from the arrangement at draw time. This is FR-012 and assumption A2.

## No description and no details step

A scrap request goes straight from the scrap step to the confirmation. It visits no details step and records no description. This is FR-015, and it is existing behaviour in `GenericRequestFlowRules.BuildOrder`, not something built by this feature.

## Not part of this contract

There is no HTTP endpoint. There is no command line. No database artifact is added. The work-order scrap card keeps its current behaviour and is not re-specified here.
