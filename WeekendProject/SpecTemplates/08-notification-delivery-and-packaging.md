# Spec seed 08 — Notification delivery and the packaging decision

| Field | Value |
| --- | --- |
| **Suggested feature name** | `notification-delivery` |
| **Source** | Defect-driven; no backlog workstream. It is the packaging/delivery decision the New Request Alerts feature has been waiting on since it was built |
| **Carry-forward boxes** | none |
| **Depends on** | template 01 (which makes the alerts control honest about what this installation can do) |
| **Defect files it completes** | `defects/Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` (the real fix), `defects/Low-Notifications-ToastActivationShowsTodoDialog.md` (verifies the cleaned-up path) |
| **Validation** | a real notification delivered and tapped on a real installation, both with the app running and with it closed; build clean; suite `Failed: 0` |

---

## 1. Why this spec exists

The new-request alert is built end to end — the toggle, the gate, the payload, the deep link, the
activation handler and the tests all exist — and it can never fire, because the application ships
unpackaged and Windows notifications require package identity. The application therefore offers a
setting it cannot honour, and `specs/001`'s decision to deploy self-contained into a per-user folder
is in direct tension with that feature.

This spec exists to **make the decision, once**, and then deliver whichever answer it chooses. It is
small but it must not be guessed at: the two options change deployment, signing and updates.

## 2. Input to paste into `/speckit.specify`

```text
Settle how MTM Waitlist delivers new-request notifications and then deliver it. The application
already has the whole feature: a per-user switch that defaults to off, a decision gate, a notification
payload that names the work center and request type, a deep link that identifies the request, an
activation handler that opens that request, and tests for all of it. What it does not have is a way to
show the notification, because the application is installed as an unpackaged per-user application and
Windows notifications require package identity, so the switch is offered and can never fire. This
feature resolves that contradiction rather than working around it. It must choose between packaging
the application so notifications work natively, or keeping the current unpackaged self-contained
install and delivering the alert a different way, and it must record the choice and its consequences
because it affects how the application is deployed, signed, updated and rolled back. Whichever way it
goes, the outcome for the user is one of two honest states: either the switch works, a notification is
delivered when a request is submitted, tapping it opens that request, and tapping it while the
application is already running also opens that request instead of showing any internal message; or the
switch is not offered at all and the setting explains plainly that this installation cannot show
notifications. Nothing in between is acceptable: no control that does nothing, no placeholder dialog,
no notification that fails silently. Deactivating the alert, changing the per-user switch, or having no
notification service configured must never prevent a request from being submitted, and the
notification path must never throw into the submit path. If the decision is to package the application,
the packaging must preserve the existing package identity settings, must keep the unpackaged launch
path working for development, must not break the runtime checks that ask whether the process has
package identity, and must be reflected in the documented deploy and update steps.
```

## 3. What this spec must settle (the decision)

| Option | What it means | What it costs |
| --- | --- | --- |
| **A — Package the application** (MSIX) | Notifications work natively; `RuntimeHelper.IsMSIX` becomes true on installed machines, so every gate in `AppNotificationService` opens | Signing, a package identity, an install/update path that replaces today's "copy the folder" deploy (`Properties/PublishProfiles/win-x64-selfcontained.pubxml` → `%LOCALAPPDATA%\Programs\MTM_Waitlist`), and a rollback story |
| **B — Keep it unpackaged** | Nothing about deployment changes | The alert cannot be delivered natively. Either the switch is withdrawn (template 01's honest state becomes permanent) or an unpackaged alternative is built as a new feature |
| **C — Unpackaged with an alternative alert** | A tray balloon or in-app prompt, no package identity needed | A new delivery mechanism to build, test and support — and a second notification surface to keep consistent with the setting |

`Package.appxmanifest` and `Package.appinstaller` already exist in the repository (the manifest
carries `Identity Version="1.0.0.0"` and target families down to Windows 10 1809; the appinstaller is
still an unfilled template). Packaging guidance for this repo lives in
`.github/instructions/packaging.instructions.md` and applies to any option that touches those files:
preserve the existing package identity settings unless the task explicitly asks to change them, keep
display name / description / publisher consistent across manifest metadata layers if Store-readiness is
in scope, and cross-reference every packaging change against the `RuntimeHelper.IsMSIX` dependencies so
no runtime activation path is broken.

## 4. Carried requirements (the feature that already exists — do not rebuild it)

- Per-user toggle, default **off**, on the Settings → Operations surface.
- The alert is raised when a request is created, and only when the toggle is on **and** the
  installation can deliver it.
- Payload: a two-line notification titled "New Waitlist Request" naming the work center and request
  type, carrying a launch argument that identifies the request
  (`action=openrequest&request=<guid>`).
- **Tapping it opens that request** — both when the application is starting (the activation handler
  already parses the argument and navigates to the detail page) and while it is already running (which
  is the path that currently shows a placeholder dialog).
- Unrecognised arguments do nothing visible and are logged.
- The notification path must never break request submission and must never surface an exception on
  the submit path.
- The cache service's operator API is unrelated to this: it identifies callers by user name and role,
  and nothing here reintroduces a shared token.

## 5. Defects this spec completes

| Defect file | What this spec delivers |
| --- | --- |
| `defects/Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` | the chosen option: either a packaged install where the alert fires, or a permanently withdrawn switch with an honest explanation |
| `defects/Low-Notifications-ToastActivationShowsTodoDialog.md` | the placeholder dialogs are gone (template 01) and the real activation path is proven for both cold start and while-running |

Update both files' status lines when this spec lands, naming the commit and the manual proof.

## 6. Verification / gates

1. Build clean (`0 Warning(s) 0 Error(s)`); full suite `Failed: 0`.
2. **A real notification on a real installation**: submit a request from a second account, with the
   alert switch on for the recipient, and confirm the notification appears.
3. **Both activation paths**: tap it with the application closed (cold start → the request opens) and
   with the application running (while-running → the request opens, and no dialog appears).
4. Confirm the switch's behaviour when notifications cannot be delivered: either it is absent, or it
   is present and cannot be switched on, with the reason stated in the UI.
5. Confirm request submission is unaffected with notifications unavailable, refused, or the alert
   service absent — no exception, no blocked submit.
6. If packaged: install from the package, confirm the unpackaged development launch still works,
   confirm `RuntimeHelper.IsMSIX` gates behave on both, and update the deploy/update documentation
   (`README.md` minus the folder-copy step, plus the new install path).
7. `RetiredSymbolAuditTests` and `InlineSqlAuditTests` stay green; the packaging files match
   `.github/instructions/packaging.instructions.md`.

## 7. Open decisions to resolve before `/speckit.specify`

1. **The option itself (A, B or C)** — this is the whole point of the spec; settle it first, in
   writing, with whoever owns deployment.
2. **If packaged: who signs it, and how updates and rollbacks reach a shop-floor workstation.**
3. **If unpackaged: is an alternative alert genuinely wanted**, or is the honest answer to withdraw
   the switch until packaging happens? (Withdrawing is a one-line change and is already delivered by
   template 01.)
4. **Whether the alert stays per-user or becomes role-scoped** to the people who actually dispatch
   work.
5. **Whether the running-application activation should surface the request in the existing window or
   bring the window forward first** — the current code brings it forward; confirm that is still the
   intent.
