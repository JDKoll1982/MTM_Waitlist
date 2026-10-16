# Contract — The request detail page

What the page you reach from a card's **Open details** draws: the hero card, the one card beneath it, and the
inventory block below that. The list *card* is a different surface and is pinned by
`contracts/card-and-identifier.md`; this file does not describe it.

Amended 2026-10-16 from the owner's review of the running page, which added **FR-107** to **FR-110** to `spec.md`.

## 1. The hero card

The page's first block, and the only place the request's framing is stated:

| Row | Value |
|---|---|
| title | the Item's own umbrella phrase |
| `Requested By` | the person who raised the request |
| `Work Center` | the work centre the request was raised at |
| `Remaining Time` | the time left, **against the time the Item allotted** — `00:09 / 00:15` (FR-110) |

- Two pictures sit on it: the Item's picture on the left and the requesting work centre's picture on the right.
  **Each carries a caption beneath it saying what it is** — `Request type` and `Work center` (FR-109). A picture
  alone beside a title names nothing, and the two are not interchangeable to a reader who has not been told which
  is which.
- The caption under the remaining time was **removed** on 2026-10-16 (*"Material, workflow, and handoff details
  are shown below."*). It said where things were rather than anything about the request, and the page's own
  headings already say what each block below is.
- The remaining-time figure is stated against the allotment because the countdown alone says nothing about how
  close the request is: nine minutes left is most of a fifteen-minute allotment and half of an eighteen-minute
  one. The allotment shown is the same `AllottedMinutes` the ring's fraction uses, so the row and the ring can
  never disagree about the scale. A request with no due time states no allotment either — nothing is appended to
  a value that is not a countdown.

## 2. The one card

**One card, not two** (FR-107). Until 2026-10-16 the page drew a second card, *Request context*, carrying

| Row it drew | Why it is gone |
|---|---|
| `Work center` | the hero card states it |
| `Requesting user` | the hero card's `Requested By` states it, from the same value |
| `Remaining time` | the hero card states it, now against its allotment |

Every row on it was a second saying of something the page had already said, so the card is gone rather than
emptied. What remains is the card that belongs to the *request itself*:

| Row | Source |
|---|---|
| `Item` | the Item's umbrella phrase |
| `Identifier` | the Item's identifier line |
| `Request details` | the answer the request captured, when it captured one |
| `Work order` | the job's own work order (section 3) |

A row whose value has no source is **not drawn**, and the card itself is dropped when no row survives — the same
rule every other block on this page follows (FR-002, FR-101).

## 3. The Work order row

The row states the work order the request's **job** is for, as `WO-######`, and it is read from the requesting
work centre's **job snapshot** — the same source every other job-derived value on this page reads.

It is deliberately **not** read from the request's stored `active_setup_job_id`. Until 2026-10-16 the wizard wrote
the *work centre* into that column, so the row printed `100-03` where a work order belongs. Reading the job also
puts every request written before that fix right, because their stored value is a work centre and could not have
rendered as a work order whatever the page did.

Where the job cannot be resolved, the row is **left out** rather than filled with the work centre, a part number
or a blank (FR-002, FR-101). The producer was fixed in the same change, so a request raised from now on stores the
job's real work order; the stored column remains the record of the job the request was raised against.
