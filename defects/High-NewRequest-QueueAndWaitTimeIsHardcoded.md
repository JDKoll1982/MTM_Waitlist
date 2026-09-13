# High — New Request — "Queue & wait time" on the confirm screen is hard-coded

| Field | Value |
| --- | --- |
| **Criticality** | High |
| **Feature area** | New Request — Confirm step (step 6 of 7) |
| **Type** | Fabricated operational data presented as fact; unimplemented promised feature |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED (removed, not replaced) — closed by `specs/002-truthful-data-and-controls`**. **Closure:** the "0 active request(s)" / "approximately 15 minutes" card was deleted from the confirm step, and the view model exposes no queue-count or wait-estimate member. **Remainder owned by Spec `04-waitlist-analytics`**: if a real count or estimate is wanted, that spec must adopt it as a requirement. **Proof:** `MTM_Waitlist.Tests/Module_Waitlist/ViewModels/NewRequestSummaryHonestyTests`. |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — removes the fabricated "0 active request(s)" / "approximately 15 minutes" card (or binds it to a substantiated figure). If a real queue count and wait estimate are wanted, **Spec `04-waitlist-analytics`** is the spec that can substantiate them; that spec must either adopt it as a requirement or this file must be closed as "removed, not replaced". Seeds: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`, `…/04-waitlist-analytics.md` |
| **Files** | `Module_Waitlist/Views/NewRequestSummaryPage.xaml` (lines 161–167), `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs` |
| **Related** | `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` (same pattern, different screen) |

---

## 1. Summary

The Confirm step shows a **Queue & wait time** card. Both of its lines are static markup — they are
not bound to anything, so every user sees the same text on every request at every work center:

```xml
<Border Style="{StaticResource SetupSectionCardStyle}">                                   <!-- 161 -->
    <StackPanel Spacing="6">
        <TextBlock Style="{StaticResource RequestDialogTitleStyle}" Text="Queue &amp; wait time" />   <!-- 163 -->
        <TextBlock Style="{StaticResource RequestDialogFieldValueStyle}" Text="0 active request(s) for this work center." />        <!-- 164 -->
        <TextBlock Style="{StaticResource RequestDialogFieldValueStyle}" Text="Estimated wait time: approximately 15 minutes." />   <!-- 165 -->
    </StackPanel>
</Border>
```

The card has no `Visibility` binding either, so it is displayed for every request type including
non-coil requests. The view model's own documentation claims the opposite is implemented:

```csharp
/// <summary>
/// Final confirmation step of the New Request wizard. Replaces the inline
/// <c>ShowConfirmationAsync</c> dialog: it shows the request summary plus queue and
/// wait-time information and submits the request when the user confirms.
/// </summary>
```

There is **no** queue count and **no** wait estimate anywhere in the view model: its only state is
`WorkCenter`, `RequestType`, `SubtypeName`, `Detail`, the four coil fields, and submit status.

## 2. Impact

- A requester is told the queue is **empty** ("0 active request(s) for this work center") even when
  it is not, and is promised a ~15-minute wait that has nothing to do with the real workload. The
  number is authoritative-looking, on the screen where the user decides whether to submit.
- Because it always reads "0" and "15 minutes", it hides exactly the conditions it exists to
  communicate (a busy work center, a long queue), and it will contradict the "Remaining time" /
  "Overdue" values the same user sees on the cards minutes later.
- It is the second instance of the same honesty problem as the fabricated card fields, on a
  different screen, so a fix for one does not fix the other.

## 3. Evidence

- `Module_Waitlist/Views/NewRequestSummaryPage.xaml:161–167` — literal `Text=` values, no
  `x:Bind`, no `Visibility` binding.
- `MTM_Waitlist.Waitlist.NewRequest/ViewModels/NewRequestSummaryViewModel.cs` — property surface is
  `WorkCenter`, `RequestType`, `SubtypeName`, `HasSubtype`, `Detail`, `HasDetail`, `IsCoilVisible`,
  `CoilNumber`, `CoilQuantityOnHand`, `CoilDescription`, `CoilAverageWeight`, `IsSubmitting`,
  `IsStatusVisible`, `IsStatusError`, `StatusMessage`, `CanSubmit`; commands are `SubmitCommand` and
  `BackCommand`. No queue/wait member exists in the file or in any of its injected services
  (`IWaitlistRequestService`, `ICoilAvailabilityService`, `INavigationService`).
- A repository-wide search for `Queue` / `WaitTime` / `Estimated` in the Setup, Waitlist and New
  Request projects finds no queue-count or wait-estimate producer — only `DispatcherQueue` and the
  SQL "Queues" folder naming.
- Contradicting data that *is* available: the live active-request list per building
  (`IWaitlistRequestService.GetActiveRequests(building)`, used by
  `WaitlistViewViewModel.LoadOrdersAsync` and `WaitlistViewDetailViewModel.OnNavigatedTo`) and the
  per-sub-type allowance behind "Remaining time" / "Overdue" (Max Allotted Time settings,
  `MTM_Waitlist.Core/Services/UrgencySettingsService.cs`, `UrgencyDeadlineService.cs`,
  `UrgencyCalculator.cs`).

## 4. Root cause

The screen was built as the UI replacement for the old inline confirmation dialog and the card was
painted with placeholder text from the design pass. When the Confirm step was rebuilt as a page, the
placeholder survived as literal XAML and was miscounted as finished, while the view model's summary
comment recorded the intended behaviour.

## 5. Fix

1. **Bind, or remove.** Either compute and bind both lines, or delete the card. Do not ship a
   stand-in number: an absent card is truthful, "0 active request(s)" is not.
2. If it is kept, compute from real data:
   - **Queue count** — active requests for the selected work center/building from the live store.
     Reuse `IWaitlistRequestService.GetActiveRequests(building)` and filter by
     `WaitlistRequest.WorkCenter`; if a dedicated aggregate is wanted, add it as a stored procedure
     (`create.sql` + `rollback.sql` in the same change) rather than counting in memory over a list
     the server can count.
   - **Estimate** — derive from the sub-type's configured max-allotted minutes
     (`UrgencySettingsService` / `UrgencyDeadlineService`) rather than a constant, and label it as an
     estimate. If no estimate is defensible, show only the queue count.
3. **Localize** both strings into `Strings/en-us/Resources.resw` (constitution V — all UI text is
   resource-backed; the surrounding page currently hard-codes its own strings too).
4. **Fix the view model's summary comment** so it describes what the screen actually does.
5. Consider whether the card belongs on the Confirm step at all — the queue can change between
   Confirm and submit, and the submit path already guards against a changed current job
   (`IsStatusVisible` / `StatusMessage`, "Current job changed").

## 6. Verification

1. Unit test: with a stubbed `IWaitlistRequestService` returning *n* active requests for the selected
   work center, assert the bound queue text contains *n* (not 0) — this fails today and cannot pass
   against the literal.
2. Unit test: with no allowance configured for the sub-type, assert the estimate line shows the
   agreed fallback text rather than a number.
3. Manual: with several requests already open on a work center, start a New Request for that work
   center and confirm the count matches the list behind it.
4. Full suite `Failed: 0` and build `0 Warning(s) 0 Error(s)`. If new XAML text is added, remember
   the `WMC9999` masking trap: a real XAML error surfaces only as that generic code, so bisect the
   changed XAML if it appears.

## 7. Related

- `MTM_Waitlist.Core/Services/UrgencySettingsService.cs`, `UrgencyDeadlineService.cs`,
  `UrgencyCalculator.cs` — the existing sources for due/remaining/overdue maths.
- `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §4.4 — urgency ordering (the ordered handler list is the
  other half of this topic and is still open).
- `FEATURES.md` §5, §14.
