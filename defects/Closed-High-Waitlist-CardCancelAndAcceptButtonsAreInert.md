# High — Waitlist — Card Cancel and Accept buttons are inert

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
| **Criticality** | High |
| **Feature area** | Waitlist — request cards (all request types) |
| **Type** | Visible control with no behaviour; documented feature does not exist in the UI |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/003-waitlist-handler-fulfilment`** (working tree at `1b00f3e`). **Closure:** the action area now carries only controls that act — Accept (handler-or-above, unclaimed), Complete and Release (the recorded assignee only) and Cancel (the requester's own waiting request) — each gated on a per-row flag, each carrying a command, each reaching the store. Visibility is gated rather than the buttons being drawn unconditionally, and a control the viewer may not use is hidden rather than disabled. **Proof:** `MTM_Waitlist.Tests/Module_Waitlist/Controls/WaitlistLineCardMarkupTests` (every `Button` carries a `Command`; every action button's `Visibility` is bound to a per-row gate; the approved card anatomy is unchanged), `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewViewModelActionTests` (the gate matrix matches `RequestActionPolicy` for every viewer and state; concurrent claims resolved first-claim-wins with the loser warned), `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/WaitlistViewDetailActionTests` (the detail page offers the same actions and the note/history round-trips), and `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests` (release is not a cancellation). |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** removed the inert buttons and showed the request's real status instead. **Spec `003-waitlist-handler-fulfilment`** (seed `WeekendProject/SpecTemplates/02-handler-fulfilment-and-urgency.md`) is where they came back working, wired to the commands and gates set out in §5 of this file, plus the list's most-urgent-first ordering, the requester's cancel-with-reason, the handler note, and the request history. |
| **Files** | `Module_Waitlist/Controls/WaitlistLineCardView.xaml` (lines 137–181), `Module_Waitlist/Controls/WaitlistLineCardView.xaml.cs`, `Module_Waitlist/Controls/*/…WaitlistLineView.xaml`, `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` |
| **Blocks** | `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4.3 (Accept / Complete / Release); the ✅ marks on features 1–2 of `WeekendProject/ChangeLog.Simple.md` |

---

## 1. Summary

Every waitlist row draws two buttons — a red **Cancel** (✕, glyph `E10A`) and a green **Accept**
(✓, glyph `E10B`) — in the card's action column. Neither button has a `Command`, a `Click` handler,
or even an `IsEnabled` binding, so pressing either one does nothing: no dialog, no state change, no
message. The card's own trailing comment states the requirement that was never met:

```xml
<!--
    Actions - Edit, cancel, and accept controls. Each action must be wired to the appropriate command before being enabled.
-->
```

The capability underneath is fully built and tested — cancelling a request you created, and
accepting / completing / releasing as a handler, all exist in the service layer and in a purpose-built
policy class. Only the UI wiring is missing.

## 2. Impact

- Operators see two prominent colour-coded action buttons on every request and get **no response at
  all** — the worst failure mode for a UI control, because nothing explains that it is broken.
- The app has **no way to accept, complete, release, or cancel any request**. Every lifecycle action
  in the product is currently unavailable by UI, which makes the request list read-only for everyone.
- The buttons are unconditional: they are drawn on requests the viewer does not own, on requests
  already accepted, and on completed requests, so they also misrepresent who may act.
- Two project documents advertise this as shipped:
  `WeekendProject/ChangeLog.Simple.md` feature 2 (✅ "Cancel your own request … you can cancel it
  **from the detail view** with a confirmation") and `WeekendProject/ChangeLog.md` `[Unreleased]` →
  Added → "Cancel your own request". The detail page carries **no** action buttons at all, so the
  documented surface does not exist either.

## 3. Evidence

**The buttons — `Module_Waitlist/Controls/WaitlistLineCardView.xaml`:**

```xml
<!-- Action buttons (vertically centered) + compact status pill below spanning button width -->   <!-- 137 -->
<Button
    MinWidth="44"
    MinHeight="44"
    AutomationProperties.Name="Cancel"                        <!-- 157 -->
    Background="{ThemeResource SystemFillColorCriticalBrush}"
    CornerRadius="9">
    <FontIcon FontSize="18" Foreground="White" Glyph="&#xE10A;" />   <!-- 163 -->
</Button>
<Button
    MinWidth="44"
    MinHeight="44"
    AutomationProperties.Name="Accept"                        <!-- 168 -->
    Background="{ThemeResource SystemFillColorSuccessBrush}"
    CornerRadius="9">
    <FontIcon FontSize="18" Foreground="White" Glyph="&#xE10B;" />   <!-- 174 -->
</Button>
```

No `Command=`, no `Click=`, no `IsEnabled=` on either element.

**The code-behind — `Module_Waitlist/Controls/WaitlistLineCardView.xaml.cs`:** registers dependency
properties only (`Order`, `RemainingTimeBrush`, `RemainingTimeFontWeight`, `AccentBrush`,
`AccentSurfaceBrush`, `BadgeBackgroundBrush`, `BadgeText`, `DetailsContent`) and hooks
`Loaded`/`Unloaded` to subscribe to the row's per-minute tick. There is no click handler of any kind.

**The card is used by every waitlist row** — `WaitlistLineCardView` is the root of each type view:

```
Module_Waitlist/Controls/Coil/CoilWaitlistLineView.xaml:10      <local:WaitlistLineCardView
Module_Waitlist/Controls/PickupFg/PickupFgWaitlistLineView.xaml:10
Module_Waitlist/Controls/PickupNcm/PickupNcmWaitlistLineView.xaml:10
Module_Waitlist/Controls/PickupOs/PickupOsWaitlistLineView.xaml:10
Module_Waitlist/Controls/PickupWip/PickupWipWaitlistLineView.xaml:10
Module_Waitlist/Controls/Scrap/ScrapWaitlistLineView.xaml:10
```

and `Module_Waitlist/Views/WaitlistViewPage.xaml` maps **every** template — including
`DefaultTemplate` — to one of those six views, so no row escapes the buttons.

**No command exists to bind to.** A repository-wide search for `AcceptCommand` / `CancelCommand`
finds only the unrelated `CancelCommand` of the Setup work-order page and the login page.
`WaitlistViewViewModel` declares a single `[RelayCommand]` (line 240, the add-request entry point).

**The capability is already built:**

| Layer | Evidence |
| --- | --- |
| Cancel-own policy | `WaitlistViewViewModel.CanRequesterCancel(SampleOrder)` (~line 724) — true only for a real request in `Pending`; plus `IsRequesterOrder(...)` (~line 707). Both are unit-tested and have **no caller in the UI**. |
| Cancel-own service | `IWaitlistRequestService.CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason, …)` → `WaitlistRequestService.cs:283`, which enforces creator identity and the Waiting state and returns a typed `WaitlistRequestCancelResult`. |
| Transition service | `TransitionStatusAsync(Guid requestId, string status, string? cancellationReason, string? canceledByEmployeeNumber, …)` → `WaitlistRequestService.cs:188`, validated against `Pending / Accepted / Completed / Canceled` (line 206) and written through the request-update stored procedure with an audit row (line 255). |
| Accept / Complete / Release policy | `MTM_Waitlist.Core/Services/RequestActionPolicy.cs` — `CanViewerAccept(status, viewerCanHandleRequests)`, `IsAssignedToViewer(...)`, `CanViewerCompleteOrRelease(...)`. Fully implemented, unit-testable, and referenced only by tests. |
| Tests already covering the service side | `MTM_Waitlist.Tests/Module_Waitlist/Services/WaitlistRequestServiceTests.cs` (cancel-own, transitions, audit, timestamps, metadata), `RequestActionPolicy` tests. |

## 4. Root cause

The card chrome (including its placeholder buttons) was built against a static design and shipped
before the lifecycle layer existed. When the service and policy were written, they were tested
directly from the test project and never connected to a view model command or to the card's markup.
The `OPEN-WORK-NEXT-SPEC.md` §4.3 workstream is the outstanding item that owns this wiring.

## 5. Fix

1. **Add commands to `WaitlistViewViewModel`** (CommunityToolkit.Mvvm source generators are already
   in use in this project):
   - `CancelRequestAsync(SampleOrder)` → confirm with a `ContentDialog`, then call
     `IWaitlistRequestService.CancelOwnRequestAsync(order.RequestId!.Value, currentEmployeeNumber, reason)`
     and map each `WaitlistRequestCancelStatus` to a plain message.
   - `AcceptRequestAsync(SampleOrder)` → `TransitionStatusAsync(order.RequestId!.Value, "Accepted")`,
     then assign the signed-in handler per §4.3 (auto-assign is specified there; the assignee column
     is already persisted by the service).
   - Complete / Release as separate commands per §4.3 (`"Completed"` / release returning the request
     to the open list).
   After any successful transition, refresh from the service and reload the list (the VM already has
   `RefreshAsync()` and the `_waitlistRequestService.RequestsChanged` subscription used by
   `OnRequestsChanged`).
2. **Expose them to the card.** The card binds `Order` by `ElementName=Root` from its own
   `DataContext`, so a `Command` must reach the page's view model: add `Command` / `CommandParameter`
   dependency properties (mirroring the existing `AccentBrush` etc. pattern) to
   `WaitlistLineCardView`, or bind through the item's ancestor. Prefer the dependency-property route —
   it matches this file's existing style and keeps the per-type views unchanged.
3. **Make visibility and enablement truthful** using the code that already exists:
   `CanRequesterCancel(order)` for Cancel, `RequestActionPolicy.CanViewerAccept(status, viewerCanHandleRequests)`
   for Accept, and `RequestActionPolicy.CanViewerCompleteOrRelease(...)` for the handler actions.
   Hide the buttons rather than showing disabled ones when the viewer may not act.
4. **Decide the detail-page surface.** Either add the documented action bar to
   `Module_Waitlist/Views/WaitlistViewDetailPage.xaml` or correct the two changelog claims; do not
   leave the docs describing a UI that does not exist.
5. **Route through the app's gates.** `RequestAlertGate` / `NewRequestAlertNotifier` and the role
   vocabulary in `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` (Material Handler is the
   lowest handler role) should supply the `viewerCanHandleRequests` input, so the UI gate and the
   service gate agree.

## 6. Verification

1. New unit tests on `WaitlistViewViewModel`: cancel executes for a `Pending` own request and calls
   `CancelOwnRequestAsync` with the signed-in employee number; Accept calls
   `TransitionStatusAsync(id, "Accepted")`; both commands are absent/hidden for a foreign or
   non-Pending row (extend the existing `CanRequesterCancel` tests rather than replacing them).
2. New tests asserting the visibility gates match `RequestActionPolicy` for: available vs taken vs
   done statuses, and assigned-to-viewer vs assigned-to-someone-else.
3. Manual UI pass (the app ships unpackaged; `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`):
   raise a request as user A, confirm Cancel is offered to A and not to B, cancel it, and confirm the
   card leaves the active list and the audit row is written. Then accept as a handler and confirm the
   Accept button disappears for other handlers.
4. Full suite must stay `Failed: 0` and the build `0 Warning(s) 0 Error(s)`; the XAML compiler
   requires `x:Name` on anything using `x:Load` (`WMC0907`), so if `x:Load` is added to the new
   buttons, name them.

## 7. Related

- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4.2–§4.3 — the authoritative requirement text for Accept /
  Complete / Release, including "icon button (not text)", auto-assignment, and the note/Complete
  expectations.
- `WeekendProject/ChangeLog.Simple.md` features 1 and 2 — the ✅/🚧 claims that need correcting
  alongside the fix.
- `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` — the same card surface.
- `FEATURES.md` §3, §14.
