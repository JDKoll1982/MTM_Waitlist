# High — Waitlist — Opening the New Request workflow replaces every card image with the "no image available" placeholder

> **Naming note (2026-09-20).** The **Planned fix** row below names specs by their **template** number, from
> `WeekendProject/SpecTemplates/` -- the seeds written before the specs existed -- so `Spec 01-...` is *not*
> `specs/001-...`. Those templates have since shipped under different numbers: `01-truthful-data-and-controls`
> is now **`specs/002-truthful-data-and-controls`** (shipped), `02-handler-fulfilment-and-urgency` is
> **`specs/003-waitlist-handler-fulfilment`** (shipped), and `03-unified-card-and-taxonomy` is
> **`specs/004-unified-card-item-picker`** (in flight). Templates **04-08** have no spec yet. The single copy
> of this map is `WeekendProject/SpecTemplates/00-INDEX.md`. The **Status** row above already uses the real
> `specs/00N` names -- read that one for what actually happened.


| Field | Value |
| --- | --- |
| **Criticality** | High — every card on the list is affected, and it looks like data corruption rather than a missing configuration |
| **Feature area** | Waitlist → request card → request-type image tile |
| **Type** | Latent state bug: a service's "nothing configured" answer taken as a configured answer |
| **Found** | 2026-09-13, by the owner using the built app |
| **Status** | **FIXED.** See §5. |
| **Fixed by** | `RequestImagePathPolicy` + the guard in `WaitlistViewViewModel.ApplyResolvedImagesAsync` (task T049) |
| **Files** | `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs`, `MTM_Waitlist.Waitlist.View/Helpers/RequestImagePathPolicy.cs`, `MTM_Waitlist.Settings/Services/ImageLocationService.cs` |

---

## 1. Reported symptom

> "When I open the new request workflow and then go back to the waitlist view the request type image changes
> to a faded image of the work center image."

The card's 96×96 tile is correct when the app starts, and every tile on the list drops to a pale generic
illustration the first time the user enters the New Request wizard and returns.

## 2. Root cause

The pale illustration is `Assets/Placeholders/default-request-type.png` — the app's own **"DEFAULT REQUEST
TYPE — NO IMAGE AVAILABLE"** placeholder. Nothing about the work center is involved; the placeholder art
depicts generic machines, which is what suggested a work-center image.

Four verified facts combine into the bug:

1. **The store has no images configured.** `waitlist_request_types.default_image_path` is `NULL` for all eight
   types, and `config_images_locations` holds **zero rows**. So for a request type the image service can only
   ever fall back to its default.
2. **The service reports that fallback as a path, not as an absence.**
   `ImageLocationService.ResolveRequestTypeImagePathAsync` ends with `return defaultPath;` where
   `ImageLocationDefaults.RequestTypeDefaultPath` is
   `Assets\Placeholders\default-request-type.png`. It returns the placeholder without logging anything, so the
   caller cannot tell "here is your image" from "there is no image".
3. **The card already has a working image of its own.** Every row carries
   `ImagePath = "pickup_ncm.png"` (and peers), resolved by the legacy static mapping. `Assets/pickup_ncm.png`
   and the other four exist on disk and are copied to the build output, so `EffectiveImagePath`'s fallback
   branch draws a real picture — which is why the first load looks right.
4. **`EffectiveImagePath` prefers any non-empty `ResolvedImagePath`.** So once the service hands back the
   placeholder, it *replaces* the row's good image.

The trigger is initialization timing. Nothing initializes the image service at startup —
`ImageLocationServiceInitializer.InitializeImageLocationServicesAsync` is defined but never called — and
`ApplyResolvedImagesAsync` skips resolution entirely while `IsInitialized` is false. `NewRequestFlowService`
then initializes the service on demand. From that moment the waitlist resolves every row, receives the
placeholder for each, and overwrites the images that were rendering correctly.

So the sequence is: first load skips resolution and shows real images → New Request initializes the shared
singleton → every later load resolves to the placeholder and takes it.

## 3. Evidence

Reproduced against the built app on 2026-09-13 (maximized shell, captured before and after the round trip):

- Before: the NCM, FG and Outside Service tiles each drew their own image.
- After New Request → Back: **all three** tiles drew `default-request-type.png`.
- The startup log carried no warning from the image service: the substitution is silent by construction.
- After the fix, the same round trip leaves all three tiles intact, and the log now records the refusal:

  ```text
  Request 'f0000000-0012-…' has no configured image; keeping the row's own image 'pickup_ncm.png' instead
  of the resolver's placeholder.
  ```

## 4. Why the obvious fixes were not taken

- **Return `null` from the service when it has nothing.** That changes a public contract with several callers —
  the Settings image dialogs legitimately want the placeholder *as* the effective path to show an admin — and
  would make "no image configured" indistinguishable from a failure for all of them. The card's need is
  narrower than the service's, so the rule belongs at the card.
- **Initialize the image service at startup.** That would make the bug appear on the *first* load instead of
  after New Request — it fixes the inconsistency, not the defect. With the card fixed, initialization timing
  no longer affects the tile.
- **Store an image path in the catalog for every type.** A data change that hides the code defect and depends
  on someone remembering to keep it populated.

## 5. Fix

`RequestImagePathPolicy.IsUsableResolvedPath` states the rule in one testable place: a resolved path is worth
taking only when it is something other than the service's own placeholder (compared separator- and
case-insensitively, because the service returns backslashes and resolved catalog paths use forward slashes).
`ApplyResolvedImagesAsync` takes the resolved path only when the policy allows it, and otherwise keeps the
row's own image and records why. The service's silent fallback is now logged at Debug, so the substitution is
no longer invisible.

Guard: `MTM_Waitlist.Tests/Module_Waitlist/Helpers/RequestImagePathPolicyTests.cs` (5 tests).

## 6. Related

- `specs/003-waitlist-handler-fulfilment/` — the feature that owns the card.
- `Database/Tables/config_images_locations` — the override table, currently empty; an admin adding a row for a
  request type restores the override branch, which the policy deliberately still honours.
