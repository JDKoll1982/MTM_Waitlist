# High — Waitlist — Every card's picture goes blank after a visit to New Request, because the seeded Item pictures are single-pixel stand-ins

| Field | Value |
| --- | --- |
| **Criticality** | High — every card on the list is affected at once, and an empty tile reads as lost data rather than a missing configuration |
| **Feature area** | Waitlist → request card → the Item's picture tile (and the request page's two pictures) |
| **Type** | Latent state bug: a file that exists but holds no picture taken as a picture |
| **Found** | 2026-09-15, by the owner using the built app |
| **Status** | **FIXED.** See §5. The six pictures themselves are still stand-ins — see §7. |
| **Fixed by** | `ImageFileProbe` + `RequestImagePathPolicy.IsUsableResolvedPicture`, applied by the list and the request page |
| **Files** | `MTM_Waitlist.Waitlist.View/Helpers/ImageFileProbe.cs`, `MTM_Waitlist.Waitlist.View/Helpers/RequestImagePathPolicy.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs`, `Assets/RequestTypes/*.png`, `Database/Seeds/seed_waitlist_request_item_images/create.sql` |

---

## 1. Reported symptom

> "After submitting a new request the images / graphical details on each waitlist line end up using the fallback,
> they all look normal on startup."

The first list load after start-up draws every card's own picture. As soon as a request has been raised through the
New Request workflow, every card's 96×96 tile becomes an **empty white square** and stays that way until the app is
restarted.

## 2. Root cause

Two things that are each reasonable on their own combine:

1. **`Assets/RequestTypes/*.png` carry no picture.** All six files are **68 bytes**, byte-for-byte identical
   (MD5 `5AD12D671F9D99A5E2A09B0C3CD433FA`), and decode to a **1×1 fully opaque white pixel**. They were added by
   `df15519 "feat: Add new branding and placeholder assets"` as stand-ins for artwork that was never delivered, and
   they are copied into the build output like every other asset.
2. **`Database/Seeds/seed_waitlist_request_item_images` points nine override rows at them** — six `request_item`
   rows (`pickup-die`, `deliver-die`, `deliver-flatstock`, `assist-table-place`, `assist-table-remove`, `other`) and
   three `request_category` rows (`Pickup`, `Assist`, `Other`). Under spec 004's Item-keyed picture every request
   resolves through one of those two scopes, so *every* card finds a "configured" picture.

Nothing between the seed and the tile ever asked whether the file holds a picture:

- `ImageLocationService.ResolveExistingPathAsync` validates the path with `DoesPathExist` — a `File.Exists`.
  A 68-byte file passes.
- `RequestImagePathPolicy.IsUsableResolvedPath` refused only the resolver's own "nothing configured" placeholder
  (`Assets\Placeholders\default-request-type.png`). A stand-in is not that string, so it passed too.
- `SampleOrder.EffectiveImagePath` prefers any non-empty `ResolvedImagePath` over the picture the row already has.

So the tile was handed a single white pixel, stretched to fill the tile, and the card's own picture — which was
correct on the first load — was overwritten.

**The trigger is initialization timing**, exactly as in the earlier
[image-placeholder defect](High-NewRequestVisitReplacesCardImagesWithThePlaceholder.md): nothing initializes the
image service during start-up, and the card skips resolution entirely while it is uninitialized
(`WaitlistViewViewModel.ApplyResolvedImagesAsync`). `NewRequestFlowService.EnsureImageServiceAsync` initializes it
the first time the New Request workflow is used, so the next list load — the one after a request is submitted —
resolves every row for the first time and takes the stand-ins.

## 3. Evidence

| Claim | Evidence |
| --- | --- |
| The six Item pictures hold no picture | Each is 68 bytes, one MD5, decoded: `size=1x1`, `A=255 R=255 G=255 B=255` |
| They are deployed, so `File.Exists` finds them | All six present under `bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/Assets/RequestTypes/` at 68 bytes |
| The seed points the Item and family scopes at them | `Database/Seeds/seed_waitlist_request_item_images/create.sql` — nine `INSERT` rows |
| The resolver cannot tell them apart from a real picture | `ResolveExistingPathAsync` gates on `DoesPathExist` only |
| The card could not tell them apart either | `IsUsableResolvedPath` refused the placeholder string, not a file with nothing in it |
| The service is uninitialized on the first load | `ImageLocationServiceInitializer` is defined but never called at start-up |

## 4. Why the obvious fixes were not taken

- **Replace the six pictures with real artwork.** That is content the owner has to supply, it is not in the
  repository, and it fixes only these six files: the next stand-in anybody commits would silently blank the cards
  again. The rule below is what makes the class impossible. (§7 records the content gap.)
- **Change the resolver to answer "nothing configured" when the file holds no picture.** That changes a shared
  contract for every caller — including the Settings image screens, which legitimately want the placeholder as the
  *effective* path to show an admin — and it was rejected for exactly that reason when the earlier defect was
  fixed. The card's need is narrower than the service's.
- **Refuse by file size.** The stub is 68 bytes and the app's own upload floor is 1 KB, so a size test would have
  caught this one — but a corrupt or truncated picture of a plausible size would sail through. The question is
  whether the file carries a picture, and that is what reading its header answers.

## 5. Fix

`ImageFileProbe.CarriesPicture` reads the picture's own header — PNG, JPEG, GIF and BMP — and reports whether a
picture with at least the application's own minimum picture size (`ImageValidationRules.MinDimensionPixels`, 48 px)
is really there. It reads at most 64 KB, never decodes, needs no WinRT, and is synchronous, because the list
resolves every row of every load. It resolves an application-relative path the way the resolver and the card's
converter already do.

`RequestImagePathPolicy.IsUsableResolvedPicture` states the whole question in one place: a resolved path is taken
only when it is not the resolver's placeholder **and** the file behind it carries a picture. Both the list card and
the request page use it, and the work-centre picture is held to the same test — a work-centre file that carries
nothing now leaves the path unset so the page draws its own placeholder instead of a hole.

The refusal is logged, naming the path the resolver offered and the picture the row kept, so the substitution is
visible next time rather than silent.

Guards: `MTM_Waitlist.Tests/Module_Waitlist/Helpers/ImageFileProbeTests.cs` (seven tests, including the shipped
68-byte file reproduced byte for byte) and two new cases in
`MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestImagePathPolicyTests.cs` (ten tests in that file). Full suite:
1109 tests, 0 failed.

## 6. Related

- `defects/High-NewRequestVisitReplacesCardImagesWithThePlaceholder.md` — the same seam, reached the first time
  from the *placeholder* string instead of from a picture-less file.
- `specs/004-unified-card-item-picker/contracts/card-and-identifier.md` §4 — the picture cascade
  (Item → Category family → placeholder) and the rule that a "nothing configured" answer never replaces a picture
  a request already resolves.
- `Database/Seeds/seed_waitlist_request_item_images/` — the seed that carries the intent for the six Item pictures.
- `MTM_Waitlist.Settings/Models/ImageValidation.cs` — the application's own picture rules, whose minimum size this
  probe reuses so that "a picture" means one thing in this codebase.

## 7. What is still outstanding

The six `Assets/RequestTypes/*.png` files are still 1×1 white stand-ins, so the Item and Category pictures that
the seed names cannot be drawn: those cards now fall back to the picture the card builds for itself (the
`Assets/pickup_*.png` set). Real artwork under those six names is what makes the seeded pictures appear, and no
code change is needed when it lands.
