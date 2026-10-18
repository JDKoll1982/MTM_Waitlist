# Medium — Waitlist cards and the building flyout — the picture took the click that belonged to the card

| Field | Value |
| --- | --- |
| **Criticality** | Medium — the wrong action ran, on the app's main screen: the picture is the biggest target on a card, and clicking it opened the enlarged picture instead of doing what the card does. Nothing is misreported and no data is at risk, and the card's own action stayed reachable by clicking its text |
| **Feature area** | The waitlist list cards; the shell's building flyout; every card grid that acts on a click |
| **Type** | Interaction defect: one gesture with two owners |
| **Found** | 2026-09-24, by the owner using the built app. Reported twice: first as "use a normal image for the waitlist line control's left image, not custom control that opens the larger image", then as the same thing for the building flyout |
| **Status** | **FIXED** and verified in the running app — see §5 |
| **Fixed by** | The shared picture control reads the gesture from its host: inside a clickable thing, a plain click is the host's and **Shift+click** enlarges the picture (009 requirement A7, decision D11/D12) |
| **Files** | `Module_Shared/Controls/ClickToEnlargeImageView.xaml(.cs)`, `Module_Waitlist/Controls/WaitlistLineCardView.xaml`, `Strings/en-us/Resources.resw` |

---

## 1. Reported symptom

> "use a normal image for the waitlist line control's left image, not custom control that opens the larger image"

> "set the building flyout image to a normal image, not the custom control like you did with the waitlist line image.
> Actually go through the app and reset all images to use the custom control BUT in the event where the image is part
> of a button such as the waitlist line or the flyout window … make the shift modifier + click be what toggles the
> custom control"

A click on the picture area of a waitlist card opened the picture full size instead of opening the request, and the
same thing happened on a building card in the shell's facility flyout. A screenshot supplied by the owner shows the
enlarged picture over the request page: the card had opened the request *and* the picture had opened the viewer.

## 2. Root cause

The shared control answered the click itself (`Tapped` on its own picture), and the card answered the same click
again through its list's `ItemClick`. Nothing in the control knew it had been placed inside something that already
acts on a click, so one gesture had two owners and the reader got both.

The first fix was to stop using the control on the waitlist card and draw a plain `Image` there. That fixed one card
and left the conflict in place everywhere else — the building flyout, the New Request option cards, the Setup
work-centre and part lists — while taking the picture away from the one control that is supposed to own how pictures
behave. The owner corrected that within the hour: reset every picture to the control, and make the *gesture* the
thing that changes where the picture is part of a button.

## 3. Impact

- On the waitlist, the largest target on the card did not do what the card does. An operator aiming at the picture
  got a full-screen picture instead of the request.
- In the building flyout the same click did not choose the building.
- Because the viewer covers the page, the mistake also hid the page for as long as the reader took to dismiss it.
- The picture could not be enlarged at all on the surfaces that drew a plain `Image` as a workaround, so the fix as
  first attempted traded one defect for another.

## 4. Fix

The control works out the gesture from where it sits, on the press rather than on the tap, so the host never sees a
click that belongs to the picture:

- Inside a `ButtonBase`, or inside a `ListViewBase` that raises `ItemClick` or selects on a click → a plain click is
  the host's, and **Shift+click** enlarges the picture. The Shift of the click is read from the pointer event itself
  (`PointerRoutedEventArgs.KeyModifiers`).
- Everywhere else (a picture standing on its own, and a card list that acts through its own buttons — the Settings
  dunnage lists) → a plain click still enlarges it.
- The tooltip follows the rule: "Shift+click to view this picture full size." where the host owns the click, "Click
  to view this picture full size." where the picture does.

No surface declares which it is, so a card placed in a clickable list later cannot quietly take the click away from
the picture. The **nearest** host is what decides, and the walk stops at the first list the picture sits in: a card
list that does not act on a click (the Settings dunnage lists) keeps the click even though the page around it is
built from expanders and lists as well. The shared card template — used by both a clickable list and a
non-clickable one — therefore gets the right gesture in both.

## 5. Verification

Driven on the built `Debug` app (UI Automation plus real mouse and keyboard input), one check per line, all passing:

| Check | Result |
| --- | --- |
| Plain click on a waitlist card's picture | opened the request; the enlarged view stayed closed |
| Plain click on the request page's own picture (nothing claims that click) | still opened the enlarged view |
| Plain click on a **Settings dunnage** card picture (a list that does not act on a click) | still opened the enlarged view — this is the case a first version of the rule got wrong, see below |
| Shift+click on a waitlist card's picture | opened the enlarged view; the page stayed on the waitlist |
| `Esc`, the X in the top-right corner, and a click on the backdrop | each closed the enlarged view |
| Plain click on a building card's picture in the facility flyout | chose the building (`Expo Drive` → `Vits Drive`); the enlarged view stayed closed |
| Shift+click on the same card's picture | opened the enlarged view; the chosen building did not change |
| The view's size while open | backdrop `744×491` at `0,0` in a `744×492` client area — it fills the page — with the X `40×40`, 16 px from the top-right corner |

Two defects were found by this same round of driving and fixed with it: the Shift gesture initially never fired
because the keyboard's *current* state did not see the Shift of the click being handled, and the view closed itself
when the application lost focus because the popup was light-dismissable. A third surfaced on the Settings page: the
first rule walked outward until it found *any* claiming list and so treated the dunnage cards (whose list does not
act on a click) as claimed, which silently disabled their plain click. All three are recorded in
`specs/009-shared-picture-and-text-controls/spec.md`.

**Left open:** the two work-centre photo cards now draw through the control inside their rounded frame, and the
picture inside that frame is not clipped to its corner radius — the same look every other converted picture in the
app has. Giving the control a corner radius would close it; recorded as an omission in the same spec.
