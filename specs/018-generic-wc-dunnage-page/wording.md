# Wording: Generic Work Center — Dunnage Catalogue Page

**Feature**: `018-generic-wc-dunnage-page`
**STATUS: default, awaiting review**

The spec's design note of 2026-11-03 requires a design clarification before the screen is built: what the step
shows, what it shows when its source is unavailable, and the exact wording. No approved rendering of the screen
exists any more, so this file records the wording the implementation adopts. Every later task reads this file and
none invents its own words.

The strings below are the unattended default: recorded because the run cannot stop for a human, and flagged so a
reviewer can ratify or replace them. Changing one means changing the value in `Strings/en-us/Resources.resw` and
this row together.

## The five strings

| # | State | Key | English |
|---|---|---|---|
| 1 | loading | `GenericRequest_Dunnage_Loading.Text` | `Loading the dunnage catalogue…` |
| 2 | empty | `GenericRequest_Dunnage_NoParts.Text` | `No dunnage parts to offer.` |
| 3 | unavailable | `GenericRequest_Dunnage_Unavailable.Text` | `The dunnage catalogue could not be read at {0}. Check the connection and retry.` |
| 4 | gone marker | `WaitlistRequest_Dunnage_Gone.Text` | `No longer in the catalogue.` |
| 5 | second-part note | `GenericRequest_Dunnage_OnePartOnly.Text` | `One dunnage part per request. Raise a second request for another part.` |

`{0}` in the unavailable string is the attempt time, formatted for the reader. The frame's own unavailable heading,
attempted-at label and retry button keep the keys they already have (`GenericRequest_StepFrame_Unavailable_Title`,
`GenericRequest_StepFrame_Unavailable_Attempted`, `GenericRequest_StepFrame_Retry`); the sentence above is the
step's own line for the same state.

## Where each string is shown

| Key | Surface | Notes |
|---|---|---|
| `GenericRequest_Dunnage_Loading.Text` | catalogue page | drawn while the read is in flight, in place of the empty text |
| `GenericRequest_Dunnage_NoParts.Text` | catalogue page | the `Empty` presentation: the read succeeded and the plant holds no dunnage parts |
| `GenericRequest_Dunnage_Unavailable.Text` | catalogue page | the `Unavailable` presentation, beside the attempt time and the retry |
| `WaitlistRequest_Dunnage_Gone.Text` | confirmation and detail | shown when the raised request records `dunnage.offered` false |
| `GenericRequest_Dunnage_OnePartOnly.Text` | catalogue page | shown once, below the catalogue, whether or not a part is chosen |

The gone marker is deliberately one sentence with no part number in it: the part number is already the line above
on both surfaces, and repeating it would let the two readings disagree.

## Additional wording this step needs beyond the five

The step's introduction sentence currently describes the retired two-list flow ("Choose a dunnage type, then the
part that stands in for it."). The page no longer offers a type list, so the sentence would be wrong. It is not one
of the five strings the spec named, and it is recorded here so it is not left to a later task to invent:

| Key | English | STATUS |
|---|---|---|
| `GenericRequest_Dunnage_CatalogueIntro.Text` | `Pick the dunnage part this request needs from the plant's catalogue.` | default, awaiting review |

## Not changed by this file

The substitute dialog's own words are pinned by `contracts/catalogue-surface.contract.md` and stay byte-identical:
its filter placeholder, refresh label, show-all toggle labels, empty-state sentences and its flat failure sentence
(`Failed to load Dunnage parts.`). The catalogue control resolves those from its own keys, whose English text is
copied verbatim from the dialog markup it came from.
