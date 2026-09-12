# Spec seed 01 — Truthful data and truthful controls

| Field | Value |
| --- | --- |
| **Suggested feature name** | `truthful-data-and-controls` (the command assigns the number — expect `specs/002-…`) |
| **Source** | Defect-driven; no backlog workstream. Completes `specs/001` FR-001 / FR-003 / FR-014 and SC-013 on the screens and files those requirements were never checked against |
| **Carry-forward boxes** | none (10 verified defects) |
| **Depends on** | nothing — deliberately dependency-free so it can run first |
| **Defect files it closes** | all ten in `defects/` — see §5 |
| **Also absorbs** | `OPEN-WORK-NEXT-SPEC.md` §10.2 documentation hygiene |
| **Validation** | build clean (`0 Warning(s) 0 Error(s)`), full suite `Failed: 0`, plus a manual pass on each screen named below |

---

## 1. Why this spec exists

An inventory of the running application (`FEATURES.md`, 2026-09-12) found ten defects where the app
shows something it cannot substantiate or offers something it cannot do. Seven of them have no owning
workstream in the backlog, and four are aggravated by the planned features: template 03 rewrites the
field-population code that currently shows fabricated values, so leaving them until then keeps them on
screen for the whole of that work.

This spec applies one rule, everywhere, before any feature work:

> **Never display a value we cannot substantiate; never offer a control we cannot honour.** Where the
> real value or behaviour does not exist yet, remove the field or the control, or mark it unavailable.
> Where wording — on screen, in a resource file, or in a code comment — describes behaviour that was
> deleted, correct it or delete it.

This is not new policy. `specs/001` FR-001/FR-003/FR-014 forbid substituted or sample data and SC-013
requires zero references to the retired demo systems; this spec closes the gap between those
requirements and the screens.

## 2. Input to paste into `/speckit.specify`

```text
Close the gap between what the MTM Waitlist application displays and what it can actually support.
A full inventory of the running application found ten verified defects, and the rule for this feature
is that the application must never display a value it cannot substantiate and must never offer a
control it cannot honour: where the real value or real behaviour does not exist yet, the field or the
control is removed or marked clearly unavailable rather than filled or shown as if functional, and
where wording (on screen, in a resource file, or in a code comment) describes behaviour that was
deleted, it is corrected or deleted. The ten defects are: (1) every waitlist card and request detail
page shows hard-coded material values — coil number, part numbers, on-hand quantity, customer,
packlist, traceability identifier, WIP destination, outside-service vendor, die number, quantity — for
every real request, so an operator can act on a wrong coil, part, customer or vendor; (2) every card
draws a red Cancel and a green Accept button that have no behaviour at all; (3) the New Request
confirm screen shows a fixed "0 active request(s) for this work center" and "Estimated wait time:
approximately 15 minutes" for every user and every work center; (4) the average coil weight is always
looked up for one fixed part and falls back to a fixed 5,000 lb, so it never describes the coil on the
request; (5) the New Request Alerts switch cannot fire because the application is installed in a way
Windows notifications do not support, yet the control is offered as if it worked; (6) three separate
labels in Settings share one resource key, so all three read "About this application"; (7) the
Privacy Policy link opens an unfinished placeholder address; (8) three Settings panels ignore the
settings search because they are never told the search changed; (9) the application still ships a
"Setup saved using sample data" message and code comments describing the retired sample/mock
mechanism; (10) tapping a new-request notification while the application is already running shows an
internal "TODO" dialog instead of the request. Each defect must end with the application telling the
truth about what it knows and what it can do; the fields and controls that cannot yet tell the truth
are removed or disabled rather than populated with placeholder content, and the feature specifications
that follow this one restore the real values and the real behaviour (item resolution, the handler
workflow, analytics, and notification delivery). Documentation that describes the retired sample/mock
system — including the backlog and changelog files that still present it as current — must be
corrected or retired in the same change so no reader is sent to re-implement deleted behaviour.
```

## 3. Scope — what this spec must deliver

1. **Remove every fabricated value** from the waitlist card and the request detail page, and remove the
   fallback literals behind them, so a field with no real source shows nothing (or a localized "not
   available") instead of a plausible number, customer or part. The values to remove are listed
   exhaustively in `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §3 (about
   thirty literals across coil, finished goods, NCM, WIP, outside service, flatstock, table handling,
   die handling and scrap), plus the invented content the detail page adds on its own ("Tipping
   strategy", "Allowed categories", the "Pending…" statuses).
2. **Stop offering the card's Cancel and Accept buttons** until they work: hide them, or replace the
   action column with the request's real status, so nothing on screen implies an action that does not
   happen.
3. **Remove the fabricated queue/wait card** on the New Request confirm screen, or replace it with a
   real figure that can be substantiated without new analytics work.
4. **Remove the average-coil-weight lookup that asks for one fixed part**, and remove its fixed
   `5,000 lb` fallback, so the field is either correct for the coil on the request or absent.
5. **Make the New Request Alerts control honest**: gate it on whether this installation can deliver a
   notification at all, and say in the UI why it is unavailable.
6. **Fix the three colliding Settings labels** so the section header, the version card and the about
   card each show their own text from their own resource key.
7. **Replace or remove the Privacy Policy link** — a real policy address, an in-app document, or no
   link at all.
8. **Make the settings search refresh every search-aware panel**, including Image Location Settings,
   New Request Alerts and Max Allotted Time, and add the coverage test that fails when the next panel
   is added without registering (a value assertion alone is what let this ship).
9. **Remove the retired-mock wording**: the `Setup_Review.Status.MockSaved` resource entry, the
   comments that describe `RecvMockData ON/OFF` and "static sample rows" as live states, and the stale
   `SampleOrder` naming if the rename is wanted. Extend the retired-symbol audit with the wording
   patterns that must not return.
10. **Delete both placeholder "TODO" dialogs** on the notification path and log instead; unrecognised
    notification arguments must do nothing visible.
11. **Documentation hygiene** (`OPEN-WORK-NEXT-SPEC.md` §10.2, without re-opening §10.1): retire the
    stale backlog sources named in `OPEN-TASKS.md` §3.1 with a "retired sources" note, reconcile
    `WeekendProject/Module_Mock/Tasks.md` (72 boxes behind reality), and confirm no remaining document
    presents the removed manual Infor Visual toggle as current.

**Carried constraint — do not weaken.** The five Infor Visual read shapes keep working exactly as
`specs/001` delivered them, including the automatic cached fallback and the read-only indicator. This
spec removes *invented* values; it must not touch the fallback, the indicator, the retry behaviour, or
the SP-first rule (any `Database/**` change ships `create.sql` + `rollback.sql` together).

## 4. Explicitly out of scope — do not resurrect

- **Any reappearance of a demo/sample/mock mode**, in any form, behind any key. FR-003/FR-014 and
  constitution II forbid it, and `RetiredSymbolAuditTests` fails the build if one returns.
- **Implementing the features the removed fields belonged to** — real item resolution, the handler
  workflow, analytics, notification delivery. Those are templates 02, 03, 04 and 08; this spec only
  stops the app lying in the meantime.
- **The request-type/subtype editor** — cancelled by owner decision (2026-09-11). Not deferred, not
  rescoped.
- **Cosmetic redesign** of the card, the detail page or the settings page. Layout work belongs to
  template 03.

## 5. Defects this spec closes

| Defect file | What "closed" means here |
| --- | --- |
| `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` | no fabricated material value is rendered anywhere; template 03 later restores real ones |
| `defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` | the buttons are gone (or the card shows real status only); template 02 later wires them |
| `defects/High-NewRequest-QueueAndWaitTimeIsHardcoded.md` | the fixed queue/wait text is gone; template 04 may later deliver a real figure |
| `defects/High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` | the fixed-part lookup and the `5,000 lb` fallback are gone; template 03 later implements it per coil |
| `defects/Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` | the control no longer promises what this installation cannot do; template 08 decides packaging |
| `defects/Medium-Settings-AboutLabelsCollideOnOneResourceKey.md` | each label has its own key and its own text |
| `defects/Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` | no shipped resource value is a placeholder address |
| `defects/Low-Settings-SearchIgnoresThreeSettingsPanels.md` | every search-aware panel is refreshed, and a coverage test prevents recurrence |
| `defects/Low-Localization-RetiredMockWordingStillShips.md` | no retired-behaviour wording ships in `.resw`, comments or docs |
| `defects/Low-Notifications-ToastActivationShowsTodoDialog.md` | no placeholder dialog exists on the notification path |

## 6. Verification / gates

1. `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64` → `0 Warning(s) 0 Error(s)`.
2. `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → `Failed: 0`
   (the suite stands at 787 total; the tests this spec adds will raise it).
3. New tests, one per defect class, each written so it fails against today's code: an order built from
   a request carries that request's values; the card exposes no action it cannot perform; the confirm
   screen carries no fixed figure; the coil average is never requested with a constant part; each
   settings label resolves to its own key; every property whose getter consults the settings search
   raises a change notification.
4. Grep gates that must come back empty: `YourPrivacyUrlGoesHere`, `MockSaved`, `TODO: Handle
   notification`, `RecvMockData` (outside `specs/` and `defects/`), and the literal set listed in
   defect 1.
5. Manual pass on the running app: Waitlist list and detail, New Request confirm, and every Settings
   panel — confirming nothing on screen invites an action or states a fact the app cannot support.
6. `RetiredSymbolAuditTests` and `InlineSqlAuditTests` stay green.

## 7. Open decisions to resolve before `/speckit.specify`

1. **"Not available", or nothing at all?** For a field whose real value arrives in template 03, choose
   one and hold it everywhere: render a localized "Not available", or omit the row. (The defect files
   favour omission — a missing row is truthful.)
2. **Hide or disable the action buttons in the interim** — hidden is recommended; a disabled button
   still advertises a capability the app does not have.
3. **Privacy Policy**: a real URL, an in-app document, or the link removed?
4. **`SampleOrder` rename** to something that no longer says "sample" — do it here (mechanical: the
   view, the card templates and the tests) or leave the name and only fix the wording?
5. **Where the retired-sources note lives** — `OPEN-TASKS.md` §3.1 already names the six files; decide
   whether the note goes there, into `OPEN-WORK-NEXT-SPEC.md`, or into both.
