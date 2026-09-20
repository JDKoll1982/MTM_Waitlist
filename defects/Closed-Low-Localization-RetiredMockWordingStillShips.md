# Low — Localization and comments — Retired sample/mock wording still ships

| Field | Value |
| --- | --- |
| **Criticality** | Low |
| **Feature area** | Work Center Setup result messages; code documentation across the Waitlist/Core services |
| **Type** | Dead localization entry stating removed behaviour; stale comments describing a retired mechanism |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`**. **Closure:** the orphan `Setup_Review.Status.MockSaved` entry was deleted from `Strings/en-us/Resources.resw`, the stale comments in §1 were rewritten to describe live behaviour, the retired-symbol audit was extended with the §4 wording patterns (now the standing gate), and the `OPEN-WORK-NEXT-SPEC.md` §10.2 documentation hygiene landed (retired sources labelled; `Module_Mock/Tasks.md` reconciled). **Remainder owned by: none** — the only open decision left is the optional `SampleOrder` rename in §7. **Proof:** `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests` (`NoRetiredWordingSurvives` over `.resw` values and the retired-wording patterns, `NoPlaceholderResourceValueShips`) plus the §5 `Select-String` checks. |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — deletes the orphan `MockSaved` entry, rewrites the stale comments, extends the retired-symbol audit with the wording patterns in §4 of this file, and absorbs the `OPEN-WORK-NEXT-SPEC.md` §10.2 documentation hygiene (retire the stale backlog sources; reconcile `Module_Mock/Tasks.md`). The `SampleOrder` rename is an open decision in that spec's §7. Seed: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md` |
| **Files** | `Strings/en-us/Resources.resw` (line 211), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewDetailViewModel.cs` (line 198), `MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` (lines 582, 705, 733), `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs` (line 18), `MTM_Waitlist.Core/Contracts/Services/IIgnoredLocationsService.cs` (line 7), `MTM_Waitlist.Setup/Services/SetupPersistenceService.cs` (line 303) |

---

## 1. Summary

The demo/mock system was deleted by `specs/001` (FR-014) and `RetiredSymbolAuditTests` guards its
symbol names — but wording that describes the removed behaviour is still in the shipped resources and
in the code comments a maintainer will read.

**Shipped localization entry with no consumer:**

```xml
<!-- Strings/en-us/Resources.resw:211 -->
<data name="Setup_Review.Status.MockSaved" xml:space="preserve">
  <value>Setup saved using sample data.</value>
</data>
```

Its live sibling is used by the setup save path
(`MTM_Waitlist.Setup/Services/SetupPersistenceService.cs:303`:
`LocalizeOrDefault("Setup_Review.Status.Saved", "Setup was saved successfully.")`). A repository-wide
search finds **no** consumer of `MockSaved`: if anything ever binds it, the app will tell a user their
setup was saved "using sample data" — a statement that can no longer be true.

**Comments that describe the retired mechanism as current:**

| File / line | Text |
| --- | --- |
| `WaitlistViewDetailViewModel.cs:196–199` | "replace the baked-in "Average coil weight" with the value pulled from `mtm_receiving_application.receiving_history` (**RecvMockData ON = sample, OFF = real query**) and rebuild the sections…" |
| `WaitlistViewViewModel.cs:582–585` | "(**sample/mock value** until the live Infor Visual coil lookup is wired)" — the honest note beside the fabricated coil number |
| `WaitlistViewViewModel.cs:705`, `:733` | "**Static sample rows** carry no requester number and are not "mine"" / "Static sample rows carry no requester identity and are excluded" |
| `WaitlistViewDetailViewModel.cs:417` | "or null for **static sample rows** / when the service is absent" |
| `SampleOrder.cs:18` | "The underlying request's requester employee number (empty for **static sample rows**)" |
| `IIgnoredLocationsService.cs:7` | "…(Waitlist/Coil, Setup, **sample/mock paths**) filters location lists…" |

`SampleOrder` is still the model name for a real request row, and "static sample rows" is still
described as a state a row can be in — but `CreateSessionOrder` only ever builds rows from real
`WaitlistRequest` values (`WaitlistViewViewModel.cs:359–390`), and the list is built from
`GetActiveRequests` only.

## 2. Impact

- Low but real: the next maintainer reading these comments will look for a mock short-circuit that no
  longer exists — the precise confusion `specs/001` set out to end — and may reintroduce one.
- `MockSaved` is a latent lie in the resource file, one binding away from being shown to an operator.
- SC-013's "0 references to the retired demo/sample systems" is met for *symbols* but not for
  *behavioural wording*, which is what the success criterion was written to protect.

## 3. Why the audit did not catch it

`MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` matches a fixed set of type/key/object names
(`ISampleDataService`, `SampleDataService`, `Sample\w*Catalog`, `Feature\.(InforVisualMockData|RecvMockData)`,
`I?MockToggleService`, `MockRouting\w*`, `MockMode\w*`, `MockConfigurationService`,
`I?MockMasterDataService`, `UseMockData|MockDataExpander`, `sp_mock_`) in `.cs/.xaml/.resw/.sql/.csproj/.json`.

- `MockSaved` is not in the pattern set (only `UseMockData` / `MockDataExpander` are matched for the
  settings control).
- A bare `RecvMockData` in a comment is not matched: the pattern requires the `Feature.` prefix.
- The word "sample" is deliberately *not* matched, because legitimate names such as `SampleOrder`
  remain — so free-text uses of "sample rows" pass by design.

## 4. Fix

1. **Delete the `MockSaved` entry** from `Strings/en-us/Resources.resw` (line 211–213) — it has no
   consumer, and its message describes removed behaviour. If a distinct "saved with fallback" message
   is ever wanted, write it against the fallback that actually exists (cached Infor Visual reads),
   not against sample data.
2. **Rewrite the stale comments** listed in §1 so they describe the live behaviour, and delete the
   "sample/mock value until the live Infor Visual coil lookup is wired" note once the fabricated
   values it excuses are gone (`defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md`).
   Where a comment exists to explain a *gap*, keep it as an explicit gap statement ("not yet sourced —
   see defects/…"), not as a justification for sample content.
3. **Rename `SampleOrder` → `WaitlistOrder` (or similar)** if the team wants the naming to stop
   implying sample data; this is a mechanical rename across `MTM_Waitlist.Waitlist.View`, the card
   views' `x:DataType`, the template selector, and the tests. Optional, but it is the last
   "sample"-named type in the live path.
4. **Extend the audit** with the wording patterns that must not return — at minimum
   `\bMockSaved\b`, a bare `\bRecvMockData\b`, `\bInforVisualMockData\b`, and the phrase
   "sample data" / "sample rows" in `.resw` values (the resource file is already scanned).
   Do this *after* the other fix in this file so the audit can be green.

## 5. Verification

1. `Select-String -Path Strings\en-us\Resources.resw -Pattern 'MockSaved'` → no output.
2. `Select-String -Path **\*.cs -Pattern 'RecvMockData|static sample rows|sample/mock'` → no output
   outside `defects/` and `specs/` (both are excluded from the audit on purpose; the defect record and
   the spec legitimately name what was removed).
3. `RetiredSymbolAuditTests` and the full suite green; build `0 Warning(s) 0 Error(s)`.
4. Spot-check the Setup review screen still shows "Setup saved successfully." after the save.

## 6. Related

- `specs/001-module-mock-visual-fallback/spec.md` FR-014, SC-013; `.specify/memory/constitution.md` II.
- `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` — scope, exemptions, and pattern set.
- `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` — the values these comments
  excuse.
- `FEATURES.md` §14.
