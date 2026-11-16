# Data Model: Generic Work Center — Temp-to-Live Conversion and Verification

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Nothing here is runtime state. The chunk produces two records and one guard, and these are their shapes.

## Verification record entry

One entry per screen, written in `verification.md`.

| Field | Meaning | Rule |
|---|---|---|
| Screen | The page a person reaches, named by its page and step | One entry per screen; no entry covers two |
| Live read | The read the screen performs to answer | Named from `IGenericRequestStepData`, or the seam that step reads directly |
| Presentations compared | Populated, empty, unavailable | All three, per FR-008; unavailable also carries the attempt time and a retry, per FR-009 |
| Evidence | The tests that prove each presentation | Repository paths and test names, one per presentation |
| Outcome | Match, or the disagreement | `matches` or a reference to a recorded disagreement |
| Notes | Awkward real values exercised | Part number with a slash, lower-case scrap type, long work center name, outside-service part with no quantity (FR-011) |

Validation: an entry with no evidence for any of the three presentations is incomplete. An entry whose
outcome is `matches` carries no open disagreement.

## Recorded disagreement

| Field | Meaning |
|---|---|
| Screen | The entry it belongs to |
| Difference | What the approved preview showed, and what the live source shows instead |
| Resolution | Which chunk owns the correction, or the decision that the live presentation stands |

## Temp boundary exemption

An entry in the guard's exemption list. Two forms, both exact:

| Form | Key | Used when |
|---|---|---|
| Path | Repository-relative path | The whole file is unrelated to the convention |
| Path and name | Repository-relative path, then the declared name | One declared name in an otherwise checked file is unrelated to the convention |

No entry may be a pattern, a suffix or a directory. An exemption without a written reason is a defect.

## Deferred screen

A screen whose owning chunk has not shipped. It is not a new artifact: it is an entry in the verification
record whose outcome names the unavailable presentation as the whole of what the screen offers, per FR-015
and FR-016.
