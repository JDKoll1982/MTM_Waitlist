# Contract — Verification gates

**Feature**: `002-truthful-data-and-controls` | **Spec**: [../spec.md](../spec.md) | Success criteria: SC-001…SC-014

---

## G1 — Build gate

```
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
```

Expected `0 Warning(s) 0 Error(s)`. `WMC9999` is treated as a real, masked XAML error (constitution VI), not
as noise. Stale `PRI175`/`PRI224` means a running `MTM_Waitlist.exe` or stale `*.pri` — stop the process and
delete the artifacts rather than weakening the check.

## G2 — Suite gate

```
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

Expected `Failed: 0`. The suite stands at 787 total (768 passed, 19 skipped) before this feature; the checks
below raise it. `RetiredSymbolAuditTests` and `InlineSqlAuditTests` stay green.

## G3 — Scan gates (each must return empty)

| Pattern | Excluded scope |
| --- | --- |
| `YourPrivacyUrlGoesHere` | none |
| `MockSaved` | none |
| `TODO: Handle notification` | none |
| `RecvMockData` | `specs/`, `defects/` |
| the fabricated-literal set from `defects/Critical-…FabricatedMaterialData.md` §3 | `specs/`, `defects/` |
| placeholder-resource patterns (`example.com`, `Contoso`, `lorem`) in `.resw` values | `specs/`, `defects/` |

`defects/` is excluded by design: those files quote the removed values as evidence.

## G4 — Per-defect automated checks (each must fail against the pre-change behaviour)

| # | Defect | Check | Fails today because |
| --- | --- | --- | --- |
| 1 | Fabricated card/detail data | build an order from a request with distinctive part/quantity/customer values and assert the card fields carry **those** values; assert no field value matches a currency/dimension/quantity pattern | today the values come from literals, so the assertion cannot pass |
| 1b | Same | assert `FieldValue` exposes no fallback parameter and no `"Not available"` default is rendered | the default parameter exists today |
| 2 | Inert card actions | assert no element in `WaitlistLineCardView.xaml` is a `Button` without a `Command` (and that the two buttons are gone) | the two buttons exist with no `Command` |
| 3 | Fixed queue/wait | assert the confirm step's view model exposes no queue/wait member and the page carries no such literal | literals are in the XAML today |
| 4 | Constant-keyed coil weight | stub the weight service with an argument recorder, build an order whose work order resolves to part *X*, assert the service was called with *X* | it is called with `MMC0001000` today |
| 4b | Same | assert that with no resolvable part the field is empty and **not** `"5,000 lb"` | the fallback literal exists today |
| 5 | Alerts capability | assert the settings view model reports the capability and the toggle is disabled when the installation cannot deliver | the toggle binds only `IsOn` today |
| 6 | Colliding About labels | assert every `x:Uid` in `SettingsPage.xaml` is unique and resolves to its own resource entry | `x:Uid="Settings_About"` appears three times today |
| 7 | Privacy placeholder | shipped-resource scan for the placeholder patterns (extends the existing audit) | the placeholder value ships today |
| 8 | Settings search | assert a `PropertyChanged` is raised for each of the three panels when `SearchQuery` changes, plus the reflection coverage test | the three names are absent from `RefreshSearchVisibility()` today |
| 9 | Retired wording | extended audit for the retired wording patterns, including "sample data"/"sample rows" in `.resw` values | the patterns are outside the current audit set today |
| 10 | Notification placeholder | assert no `ContentDialog` is reachable from either activation path for a mapped and an unmapped argument | both paths show a placeholder dialog today |

## G5 — Manual pass (running application)

Per `.github/instructions/winui3-ui-automation.instructions.md`: launch the built executable, dump the UIA
text, and walk the four surfaces — waitlist list, request detail, New Request confirm, every Settings panel
— confirming that nothing invites an action the application cannot perform and nothing states a fact it
cannot support. Navigation between shell destinations uses `SelectionItemPattern.Select()` on the
`NavigationViewItem`; window placement is read through `GetWindowPlacement`.

**Environment gate**: reaching the shell requires a signed-in account. Where no credential is available,
the pre-shell surfaces (startup gate, sign-in) and the UIA text dump are recorded, and the shell walk is
recorded as environment-gated — the same treatment `specs/001` gave its live checks. Never report an
unwalked surface as passed.

## G6 — Evidence

Each closed defect gets its automated check named in the defect's status line, together with the owning
follow-on specification, so a later reader can tell what is proven and what is still owed.
