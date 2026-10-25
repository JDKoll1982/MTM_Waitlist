# Phase 0 Research: One set of rules for every screen, window and dialog

**Feature**: `014-adopt-lab-design-system`  
**Created**: 2026-10-25

## R1. How a dialog becomes its own window without breaking the api rule

**Decision**: keep `ContentDialog` as the surface, and show it through `DevWinUI.WindowedContentDialog`. The host
builds the `ContentDialog` content, then hands it to `WindowedContentDialog` with `Owner` set to the main window.

**Rationale**: the api rules say `ContentDialog` is used for confirmations. The request says
`DevWinUI.WindowedContentDialog`. These are not in conflict: `WindowedContentDialog` is a host that presents content in
an owned window, and the content is still the dialog family the rules ask for. The repository already proves the
pattern works in `LuggerEditorDialog`, which is the reference this change copies.

**Alternatives considered**: replacing `ContentDialog` outright with a custom `Window` and a hand-built button bar.
Rejected because it would break the api rule's letter, lose the framework's Escape and default-button handling, and
diverge from the one implementation already working here.

## R2. Where the shared message set lives

**Decision**: `MTM_Waitlist.Shared`, with a DevWinUI package reference added there, and a project reference added from
`MTM_Waitlist.Mock.Service` to it.

**Rationale**: Shared is the only project every other one references, and it already depends on Core. Putting the set
there gives the operator application and the service console one implementation, which is what makes the
no-exemption rule affordable. A copy in the console would be a second definition of button order.

**Alternatives considered**: putting it in `MTM_Waitlist.Core`. Rejected because Core is contracts and services, holds
no XAML, and adding XAML there would pull the UI toolkit into a layer that tests reference without one. Duplicating it
in the console was rejected outright: FR-026 exists to prevent exactly that.

## R3. The dialog sizing values

**Decision**: 480 minimum width, 720 preferred, 880 maximum, 640 maximum body height, 120 body floor, all declared as
resources in `Styles/Dialogs.xaml` and merged in `App.xaml`.

**Rationale**: the smallest supported window is a 15-inch screen at 150% scaling, which gives about 1280 by 720
effective pixels. A 720-wide dialog leaves the main window visible on both sides, and a 640-tall body keeps the dialog
inside that window with its title and buttons. The 880 maximum stops a dialog outgrowing the 1080 page column.

**Alternatives considered**: deriving from the page column (1080). Rejected because a dialog is sized by its owner, not
by the page behind it, and 1080 would fill a small screen edge to edge. Per-dialog literals were rejected because
FR-004 forbids them and today's four different values are the symptom.

## R4. Mapping today's raise sites to the six kinds

**Decision**: read each site and classify it, rather than introducing a seventh kind.

| Kind | Sites today |
|---|---|
| Acknowledge | save failed, refresh failed, store read failed |
| Confirm | deactivate account, reset password, clear all dunnage, add scrap type |
| Decide | leave setup with changes, discard preview |
| Destructive | remove workstation, remove lugger |
| Leave | setup navigation guard |
| Retry | store unreachable, cache refresh |

**Rationale**: the classification comes from the code, not from a guess about intent, and each kind's default action
follows from what the flow must protect. Leave and Decide are separated because one loses work and one does not, and
their defaults differ.

**Alternatives considered**: one generic confirm with caller-supplied labels. Rejected because that is what exists
now, and it is why two screens put the affirmative in different places.

## R5. The control inspector's layout

**Decision**: its fixed 172-pixel column becomes a proportional column, and its dense type sizes move onto the shared
scale. It keeps its window and its read-only role.

**Rationale**: it is the one surface whose content genuinely cannot fit a capped column at a fixed width. FR-027
authorises the redesign, and proportional columns keep the same information legible at any width. The api rule that
makes it a window rather than a page is untouched, because that rule is about `XamlRoot` blocking, not about layout.

**Alternatives considered**: exempting it. Rejected: the request removed exemptions. Leaving the fixed column and
allowing horizontal scrolling was rejected because FR-017 forbids horizontal scrolling at the smallest supported
window.
