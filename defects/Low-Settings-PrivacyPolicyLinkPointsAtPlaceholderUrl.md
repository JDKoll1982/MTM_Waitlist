# Low — Settings — "Privacy Policy" link points at an unfinished placeholder address

| Field | Value |
| --- | --- |
| **Criticality** | Low |
| **Feature area** | Settings → About / App Info → Privacy Policy |
| **Type** | Shipped template placeholder; broken user-facing link |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — points the link at a real policy, ships an in-app document, or removes the link and its resource entries, and adds the placeholder-value audit from §6 of this file. Seed: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md` |
| **Files** | `Strings/en-us/Resources.resw` (lines 490–492), `Module_Settings/Views/SettingsPage.xaml` (line 1249) |

---

## 1. Summary

The About card carries a **Privacy Policy** hyperlink whose destination is the scaffold placeholder left
by the project template:

```xml
<!-- Strings/en-us/Resources.resw:490–492 -->
<data name="SettingsPage_PrivacyTermsLink.NavigateUri" xml:space="preserve">
  <value>https://YourPrivacyUrlGoesHere/</value>
</data>
```

The link itself is real — `Module_Settings/Views/SettingsPage.xaml:1249` declares
`<HyperlinkButton x:Uid="SettingsPage_PrivacyTermsLink" VerticalAlignment="Center" Content="Privacy Policy" />`
and takes its `NavigateUri` from that resource — so clicking **Privacy Policy** opens the user's
browser at a domain that does not exist and lands on an error page.

## 2. Impact

- A user or auditor following the app's only privacy statement reaches a dead address; the app looks
  unfinished in exactly the area where an organization is normally expected to publish a policy.
- The placeholder is not obvious from the UI (the label reads "Privacy Policy"), so nothing warns the
  user before the dead navigation.
- It also indicates the template's remaining scaffolding was never swept: the same resource file
  still carries `AppNotificationSamplePayload` (line ~495) and the "Privacy Statement" string.

## 3. Evidence

| Item | Evidence |
| --- | --- |
| Placeholder value | `Strings/en-us/Resources.resw:490–492` → `https://YourPrivacyUrlGoesHere/` |
| Live consumer | `Module_Settings/Views/SettingsPage.xaml:1249` `HyperlinkButton x:Uid="SettingsPage_PrivacyTermsLink"` |
| Other template leftovers | `Strings/en-us/Resources.resw` `SettingsPage_PrivacyTermsLink.Content` = "Privacy Statement" (unused by the page, which sets `Content="Privacy Policy"`), and `AppNotificationSamplePayload` (the template's toast sample XML, still shipped) |

## 4. Root cause

The page was generated from the WinUI template, which ships a `YourPrivacyUrlGoesHere` placeholder and
a matching resource entry. The page's own copy was edited, but the URI resource was never replaced.

## 5. Fix

Choose one and make the artifact match:

1. **Publish the real policy** — set `SettingsPage_PrivacyTermsLink.NavigateUri` to the company's
   privacy statement URL. If the statement is internal, prefer an intranet address that resolves on
   the plant network.
2. **Ship the policy in-app** — point the link at a local document (for example a
   `Assets/…/privacy.html` opened with the launcher, or a `ContentDialog`), so the app does not depend
   on the network to satisfy the requirement.
3. **Remove the link** — if no statement exists and none is planned, delete the `HyperlinkButton` and
   both resource entries rather than shipping a broken link.

Also remove the unused `SettingsPage_PrivacyTermsLink.Content` string if the page's own `Content` is
kept, and delete `AppNotificationSamplePayload` unless it is genuinely used (a repository search shows
no consumer).

## 6. Verification

1. Manual: open Settings → About / App Info, click **Privacy Policy**, confirm the destination
   resolves (or that the link is gone).
2. Add (or extend) a resource audit test that fails when a shipped resource value matches a
   placeholder pattern — at minimum `YourPrivacyUrlGoesHere`, and ideally `example.com`, `TODO`,
   `Contoso`, `lorem`. There is precedent for a resource-level audit in
   `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` (it scans `.resw`), so extend that
   pattern set rather than adding a new mechanism.
3. Build `0 Warning(s) 0 Error(s)`; suite `Failed: 0`.

## 7. Related

- `defects/Medium-Settings-AboutLabelsCollideOnOneResourceKey.md` — the same card block (labels).
- `.github/instructions/packaging.instructions.md`, `Package.appinstaller` — the packaging/template
  scaffolding this placeholder belongs to.
- `FEATURES.md` §8.
