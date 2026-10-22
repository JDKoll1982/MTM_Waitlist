# Low — Waitlist — the Deliver Component card drew the no-image placeholder while the pickup card beside it drew its picture

| Field | Value |
| --- | --- |
| **Criticality** | Low — the request is still readable and actionable; only the picture is wrong. It matters because the tile is how a handler recognises the part, and a component card is the one card whose identifier *is* a part the handler must fetch |
| **Feature area** | Waitlist → request card → the material part's picture tile (and the request page's picture) for `pickup-component` / `deliver-component` |
| **Type** | Resolution gap: the rule that says which part a request is about never answered for the two Items whose identifier is the captured answer |
| **Found** | 2026-10-22, by the owner using the built app on the live store |
| **Status** | **FIXED** — a component Item is now about the component the operator chose, on the card, the request page and the confirmation step |
| **Fixed by** | `WaitlistRequestTitles.ResolveMaterialPartNumber` answers `{component}` from the captured answer |
| **Files** | `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs`, `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs`, `MTM_Waitlist.Tests/Module_Waitlist/Models/WaitlistRequestTitlesTests.cs` |

---

## 1. Reported symptom

> "also fix why the pickup image shows the correct image on the left but the deliver does not when both have images"

Two Waiting requests on work centre `100-08` for the same job: a Pickup card naming `A0006376159`, which drew its
picture, and a Deliver card naming `A0006376159-Blank`, which drew the application's no-image placeholder. Both parts
have a picture on the share.

## 2. Root cause

The card's picture comes from `SampleOrder.MaterialPartNumber` + `MaterialPartScope`, and the number is resolved once,
where the card's second line is resolved, so the picture and the identifier cannot name two parts
(`WaitlistRequestTitles.ResolveMaterialPartNumber`).

That rule only answered when the Item's own templates named `{part_number}`:

- `pickup-wip` (the Pickup card in the report) names `{part_number}`, which resolves to the job's own part
  `A0006376159` — a part with a picture, so its tile drew.
- `deliver-component` names `{component}` and nothing else (`RequestItemCatalog`: `Item(Deliver, 5,
  "deliver-component", "component", "{component}", "deliver-component")`), so `NamesToken(item, "part_number")`
  was false, the rule answered `null`, `MaterialPartScope` was left empty, and `ApplyResolvedPartPictureAsync`
  returned before asking any resolver for a picture. The tile fell to `ImagePicturePolicy.NoImagePath`.

The part was there for the asking: `\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist
Application\Images\Visual\Categorized Parts\A0006376159-Blank.jpg` is 1,522 × 1,522, so it passes the picture rule's
square-and-at-least-48-pixels test, and the New Request component step already pictured the same part with the same
`visual_part` scope. Only the card's rule was missing.

Note `SampleOrder.MaterialPartNumber`'s own documentation already said the number is "the coil or flatstock the job
holds, the die the request is for, or **the component the operator picked**" — the doc described the intent; the
code never supplied the third case.

## 3. Fix

`ResolveMaterialPartNumber` now answers a component Item from the request's captured answer:

- an Item whose templates name `{component}` is about **that answer** — the component the operator chose — trimmed,
  and an empty answer is no part at all (the shared placeholder, never a guess);
- every other Item is unchanged: `{part_number}` still resolves to the coil/flatstock the job holds or to the job's
  own part number.

`NewRequestSummaryViewModel` asks the same request-shaped overload with the answer the operator just gave, so the
confirmation step names and pictures the same component the card will. `MaterialPartScope` stays `visual_part`, which
is the scope the component step already uses.

## 4. Verification

- `WaitlistRequestTitlesTests` gains three cases: both component Items answer the chosen component; a blank answer
  answers no part; a job-part Item still answers the job's part and ignores the captured answer.
- Full suite: 2,302 passed, 52 skipped, 0 failed; solution builds with 0 warnings.
- The card's picture itself resolves through `PartPictureResolver` at draw time, so the fix is confirmed by that
  path's own tests plus the live file above.
