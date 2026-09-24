# Low — New Request / Work Center Setup — part pictures are drawn at their own natural size instead of as thumbnails

| Field | Value |
| --- | --- |
| **Criticality** | Low — nothing is misreported and no data is affected; the picture is simply drawn far too large, which pushes the surrounding step out of shape |
| **Feature area** | New Request → Preview step (and the Confirm step, the die card) and Work Center Setup → part selection and review |
| **Type** | Layout defect: a picture frame bounded only by `Min*`, so the picture is measured at its own pixel size |
| **Found** | 2026-09-24, by the owner using the built app at `0b78b2c`, on the Preview step |
| **Status** | **FIXED** in the working tree on top of `0b78b2c`. Not visually re-verified in the running app — see §5. |
| **Fixed by** | A fixed-size thumbnail frame on each of the five surfaces that declared only `Min*` |
| **Files** | `Module_Waitlist/Views/NewRequestPreviewPage.xaml`, `Module_Waitlist/Views/NewRequestSummaryPage.xaml`, `Module_Waitlist/Views/NewRequestDiePage.xaml`, `Module_Setup/Views/SetupPartSelectionPage.xaml`, `Module_Setup/Views/SetupReviewPage.xaml` |
| **Superseded in part** | **2026-09-24:** `NewRequestPreviewPage.xaml` and its step were removed — the preview repeated a subset of the confirmation step and changed nothing. The fix recorded here lives on in `NewRequestSummaryPage.xaml` and the other three surfaces; the preview's copy of it went with the page, as did the `Preview_*` guards named in §5 |

---

## 1. Reported symptom

> "the images on the new waitlist request should show up in a cleaner smaller format, dont show the images in full size"

On the New Request **Preview** step the part's picture is drawn at its own resolution — the shared no-image
placeholder, which is the largest of the picture assets, filled roughly half the window and pushed the
"Part / MMC0001000" label off to one side. The part picture had just been added to these surfaces by
`0b78b2c`; every other surface added by that commit drew a small card thumbnail and looked correct.

## 2. Root cause

Each of the five surfaces declared its picture box as a `Border` with **`MinWidth`/`MinHeight` and no upper
bound**:

```xml
<Border MinWidth="72" MinHeight="72" Padding="4" ...>
    <Image Source="{... Converter={StaticResource ResolvedImagePathToSourceConverter}}" Stretch="Uniform" />
</Border>
```

`Min*` is a floor, not a size. The `Image` inside carries no `Width`/`Height`, so it reports the picture's
**natural pixel size** as its desired size, and the frame around it grows to fit. Whether that happens depends
only on how much room the parent offers:

| Surface | Parent offers | Result |
| --- | --- | --- |
| Preview / Confirm | horizontal `StackPanel` (`Orientation="Horizontal"`) | measured with infinite width → picture drawn at natural size, frame sized to it |
| Setup part list / Setup review row | `Grid` column `Auto` | column sized to the picture's natural size |
| Die card | `Grid` column `Auto` inside a card capped at `MaxWidth="240"` | width capped, but the card's height is `MinHeight="130"` only → a tall picture stretches the card |

The neighbouring surfaces added by the same commit are bounded — `NewRequestComponentPage`, `NewRequestDunnagePage`
and `NewRequestJobTypePage` use a fixed `Width`/`Height` on the frame; `WaitlistLineCardView` uses `96`/`96`;
`PartPictureCardTemplate` uses paired `Min*` **and** `Max*`; `PartPictureManagerPage` uses `120`/`120`. The five
offenders are the ones that took only the floor.

## 3. Impact

- The Preview step's picture displaced the page content: the operator saw a large picture where a thumbnail was
  intended, and the part number read as a stray label beside it.
- The Confirm step draws the same block and had the same problem.
- The Setup part list and review rows had the same problem in a smaller space, where a large picture made a
  single row dominate the list.
- No reported data was wrong, so severity is Low; it is a visual defect on a step the operator reviews before
  submitting.

## 4. Fix

Each frame now declares a fixed size, which is what the correct surfaces already did:

| Surface | Frame |
| --- | --- |
| `NewRequestPreviewPage` | `Width="72" Height="72"` |
| `NewRequestSummaryPage` | `Width="72" Height="72"` |
| `NewRequestDiePage` | `Width="64" Height="64"` |
| `SetupPartSelectionPage` | `Width="72" Height="72"` |
| `SetupReviewPage` | `Width="64" Height="64"` |

The sizes are the floors those surfaces already declared, so the intended layout is preserved and only the
unbounded growth is removed. The `Max*` pair is equally valid — the guard in §5 accepts either — and the shared
`PartPictureCardTemplate` keeps its paired `Min*`/`Max*`.

## 5. Evidence

| Item | Evidence |
| --- | --- |
| Guard added | `NewRequestPartPictureMarkupTests.DieCard_/ConfirmationStep_/Preview_DrawsThePictureInAFixedSizeThumbnail`, `SetupPartSelectionPageMarkupTests.PartListEntry_DrawsThePictureInAFixedSizeThumbnail`, `SetupReviewPageMarkupTests.SubordinatePartRow_DrawsThePictureInAFixedSizeThumbnail` — each fails unless the picture's frame declares a fixed `Width`+`Height` or a paired `MaxWidth`+`MaxHeight`. **The two `Preview_*` guards were removed on 2026-09-24 with the page they guarded** |
| **Fails before the fix** | The five tests were run against the reverted markup (`git stash push` of the five `.xaml` files): **5 failed, 0 passed**, each with the message *"bounds its picture with Min* only, so a large picture is drawn at its own natural size instead of as a thumbnail."* |
| Passes after the fix | Same five tests: **5 passed, 0 failed** |
| Build | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` — succeeded, no XAML errors |
| Suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — **1551 total, 0 failed, 1496 passed, 55 skipped** (environment-gated) |
| **Not verified** | The rendered layout in the running app. The fix is proven at the markup level and by the XAML build; no UI-automation pass was run to re-drive the wizard to the Preview step |
