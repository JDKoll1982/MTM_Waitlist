# WinUI 3 Workflow Stress-Test Report: Settings → User Management Panel

**Subject:** The planned **Administration → User Management** collapsible panel in Settings
(create/edit users + privileges), as specified in `User-Management-Clarifying-Questions.md`,
stress-tested against the existing MTM_Waitlist WinUI 3 / Modular / MySQL architecture.

---

## Phase 0 — Workflow Route & Decomposition

**Route:** User (rank ≥ Production Lead) opens **Settings → Administration → User Management**
→ list loads (async SP) → search/filter/sort → **New User** / **Edit** (navigated pages) →
save (role-rank + validation) → MySQL `mtm_waitlist` SP transaction → audit insert → UI refresh.

**Ecosystem dependencies touched:**

- `Module_Settings` (VM + `SettingsPage.xaml`, `muxc:Expander`, `x:Load`)
- New `Module_Settings` views (`UserListPage` / `CreateUserPage` / `EditUserPage`) + VMs
- `Module_Core` DI (`ServiceRegistrationExtensions`), `NavigationService` / `PageService`
- `StartupState.CurrentRole`, new shared `RoleAuthorization` helper
- `MySqlConnector` via `StartupDatabaseOptions` connection string + `ExecuteWithRetryAsync` pattern
- `Module_Shared` mock-data toggle (`Feature.RecvMockData` / Infor mock)
- New audit table + `auth_roles_assignments` / `core_users_profiles`

**Silent assumptions:**

- "The DB connection string is always resolvable / env var present."
- "The current user's role is always a valid catalog role (rank resolvable)."
- "The DispatcherQueue is alive when async save completes."
- "Two admins are never editing the same user simultaneously."
- "The mock-data toggle, when on, produces a fully-consistent in-memory user store."

---

## 1. Executive Summary

- **App Stability & Sizing Rating:** **Medium** — several async/threading and transactional
  write patterns in the current repo (sync-over-async in the Settings VM constructor, fire-and-forget
  init, retry-around-transaction) are **high-risk to copy** into the new User Management flow.
- **Total Desktop Edge Cases Found:** **24**
- **Top 3 Desktop Risks:**
  1. **Transactional create wrapped in the existing `ExecuteWithRetryAsync`** can double-insert or
     silently swallow a committed insert (duplicate user / lost audit).
  2. **UI updates to `ObservableCollection<UserListItem>` from a background await** will throw a native
     cross-threading exception unless marshalled to the UI thread.
  3. **Deactivation with live sessions** (per F3, tokens are *not* revoked) leaves a window where an
     "inactive" user's already-validated session may still act until expiry.

---

## 2. Critical App-Crashing Vulnerabilities (Must Fix)

### 2.1 Fire-and-forget init copied from `SettingsViewModel` constructor

`[Severity: Critical]`

- **Workflow / Code Segment:** Existing pattern `_ = InitializeHotWorkCentersAsync();` in the
  `SettingsViewModel` ctor. If the new VM mirrors `_ = LoadUsersAsync();`, an unhandled SP/DB
  exception escapes as an **unobserved task exception**.
- **Trigger Condition:** DB unavailable / SP missing when Settings opens.
- **Impact:** Unhandled `Task` exception → possible app crash / silent failure with no user feedback.
- **Mitigation:** Wrap init in `try/catch`, surface a `StatusMessage`/`IsUsersBusy` state, and log via
  `StartupDebugLog`. Do **not** use `async void`; use `async Task` + awaited or caught invocation.

### 2.2 Sync-over-async blocking copy (`GetAwaiter().GetResult()`)

`[Severity: Critical]`

- **Workflow / Code Segment:** Existing `SettingsViewModel` ctor calls
  `_localSettingsService.ReadSettingAsync<bool?>(...).GetAwaiter().GetResult()` on the **UI thread**.
- **Trigger Condition:** If the new User Management VM repeats this for mock-toggle reads, it blocks the
  UI thread and can deadlock under certain synchronization contexts.
- **Impact:** UI freeze / hang on the UI thread.
- **Mitigation:** Read settings/roles **asynchronously in `OnNavigatedTo`/init** (`await`), never
  `.Result`/`.GetAwaiter().GetResult()` on the UI thread. Only touch the mock toggle after await.

### 2.3 Un-marshalled `ObservableCollection<UserListItem>` mutation on background thread

`[Severity: Critical]`

- **Workflow / Code Segment:** `UserListViewModel` populating `ObservableCollection<UserListItem>`
  from `await _userManagementService.ListUsersAsync(...)`.
- **Trigger Condition:** The SP result is processed on a thread-pool continuation and directly
  `Add()`ed to the bound collection.
- **Impact:** Native `RPC_E_WRONG_THREAD` / cross-threading exception → crash.
- **Mitigation:** Ensure `await` continuations resume on the UI thread (default for UI context, but if any
  `.ConfigureAwait(false)` sneaks in, marshal via `DispatcherQueue.TryEnqueue`). Keep collection
  mutation strictly on the UI thread.

### 2.4 Retry wrapper around a **transactional** create

`[Severity: Critical]`

- **Workflow / Code Segment:** New `sp_user_management_create` wrapped in the repo's
  `ExecuteWithRetryAsync` (which retries on any `MySqlException`/`TimeoutException`).
- **Trigger Condition:** Connection drops *after* the transaction commits but *before* the response is
  received; retry re-runs the insert.
- **Impact:** **Duplicate user** profile / duplicate role assignment, or an exception masked into
  "already exists" ambiguity.
- **Mitigation:** Do **not** wrap the create transaction in blind retry. Rely on the
  `uq_core_users_profiles_username_normalized` unique key; on duplicate-key (`MySqlException 1062`)
  return a clear "username already exists" result. If retry is kept, make the SP idempotent (upsert /
  check-exists) or add a client-generated `public_id`/idempotency key.

### 2.5 Concurrent create of the same username

`[Severity: High]`

- **Workflow / Code Segment:** Two admins click "New User" with the same username at once (also rapid
  double-click on Save).
- **Trigger Condition:** Both inserts race past the rank check.
- **Impact:** One succeeds, the other throws duplicate-key → user sees an error without guidance.
- **Mitigation:** Disable the Save button while `IsSaving`; catch duplicate-key and show
  "Username already exists"; guard the RelayCommand with `CanExecute`/reentrancy flag.

---

## 3. Scaling & Layout Anomalies (Degraded UX / Broken Accessibility)

### 3.1 Hardcoded widths in the new create/edit forms

`[Severity: High]`

- **Workflow / Code Segment:** Planned `CreateUserPage`/`EditUserPage` with fixed-width TextBoxes.
- **Description:** If username/display-name fields use fixed `Width="300"`, text truncates at 150% DPI or
  large fonts; labels clip.
- **Impact:** Controls clip / overlap at 150%+ scaling; unusable for low-vision users.
- **Mitigation:** Use `*`/`Auto` columns (`ColumnDefinition Width="Auto"` label + `Width="*"` input),
  wrap form in a `ScrollViewer`, and add `MaxWidth` constraints on the page; never hardcode input widths.

### 3.2 User list with many rows lacking vertical ScrollViewer + virtualization

`[Severity: High]`

- **Workflow / Code Segment:** `User Management` expander list bound to the user collection.
- **Description:** A large user catalog (e.g. hundreds) inside a fixed-height expander with a plain
  `ItemsControl`/`StackPanel` breaks accessibility and performance; rows become unreachable.
- **Impact:** Elements unreachable on low-res displays; janky scrolling.
- **Mitigation:** Use a virtualizing `ListView`/`GridView` with bounded `MaxHeight` inside a
  `ScrollViewer`; enable `ItemsStackPanel` virtualization.

### 3.3 Role badge / status pill truncation

`[Severity: Medium]`

- **Workflow / Code Segment:** Role badges ("IT Department", "Plant Manager") + active/inactive pill.
- **Description:** Long role names in fixed-size badges clip or wrap inconsistently across locales/scaling.
- **Impact:** Truncated role text at 150% scaling.
- **Mitigation:** Use `TextTrimming="CharacterEllipsis"` with a `ToolTip`/`AutomationProperties.Name`, and
  `MinWidth`/`MaxWidth` (not fixed) on badges; keep badges in an `Auto` column.

### 3.4 Missing `AdaptiveTrigger`/responsive layout on the new Administration category

`[Severity: Medium]`

- **Workflow / Code Segment:** New Administration category grid in `SettingsPage.xaml`.
- **Description:** Existing categories don't use `AdaptiveTrigger`; a new full-width form/list won't reflow
  on narrow windows.
- **Impact:** Overlapping columns on narrow/small windows.
- **Mitigation:** Keep consistent with repo pattern but favor `*`-based columns and a wrapping
  `ScrollViewer`; add `MaxWidth` on content area.

---

## 4. Desktop Input & Data Validation Boundaries

| XAML Control / Bound Property | C# Type | Required | Min/Max / Format | UI Feedback Pattern | Filtering |
| --- | --- | --- | --- | --- | --- |
| `Username` (`TextBox`) | `string` | Yes | 1–128; normalized to **UPPERCASE** (F1) | `INotifyDataErrorInfo` + hint | Trim; reject whitespace-only; disallow control chars |
| `FirstName` / `LastName` (`TextBox`) | `string` | Yes | 1–128 each; combined → `display_name` (≤256) | `INotifyDataErrorInfo` | Trim; collapse internal duplicate spaces |
| `EmployeeId` (`TextBox`/`NumberBox`) | `string` (4-digit) | Yes | Exactly **4 digits** `\d{4}`; **not unique** (F6) | inline error | Digits only; reject letters/symbols |
| `Role` (`ComboBox`) | role from catalog | Yes | Must be **≤ current user's rank** (A1/F2); own-rank allowed | rank-guard error text | Only list roles ≤ actor's rank |
| `Active` (`ToggleSwitch`) | `bool` | No | N/A; **cannot deactivate self** (B4) | disable when target == self | — |
| `ResetPassword` action | — | Optional | Set to temp `0000` + `require_password_change=1` (D4) | confirmation dialog | — |
| `SearchQuery` | `string` | No | matches username/display/employeeId/**role** (F5) | live filter | Trim; case-insensitive |
| Actor rank (server-side) | `RoleAuthorization` | Yes | Re-validated in **SP/service**, not just UI | error result | — |

---

## 5. Recommended WinUI Automation & Unit Test Cases

1. **Rank-guard (unit):** `RoleAuthorization` — a `Production Lead` actor cannot create/assign a
   `Developer` or `IT Department` user; can assign up to their **own** rank (F2).
2. **Self-lockout guard (unit):** `EditUser` with `target == actor` rejects deactivation and role change (B4).
3. **Transactional create (repo/SP):** `sp_user_management_create` inserts profile + role assignment in
   one transaction; a forced failure mid-way rolls back **both** (D2).
4. **Duplicate username (repo):** creating an existing `username_normalized` returns a typed
   "already exists" result (no crash, no duplicate).
5. **Uppercase normalization (repo/VM):** entering `jsmith` stores `JSMITH`; login with `jsmith`/
   `JSMITH` both match (F1).
6. **Employee ID non-uniqueness (repo):** two users may share `6229` (F6) and the list shows both.
7. **Search/filter (VM):** search term matches username, display name, employee id, **and** role (F5).
8. **UI-thread collection safety (VM/automation):** loading users via awaited SP then adding to the
   bound collection does **not** throw a cross-threading exception (2.3).
9. **Deactivate without session revocation (repo):** `is_active=0` does not clear `auth_sessions_tokens`
   (F3) — but `ReadSessionSnapshotAsync` (is_active filter) rejects the deactivated user on next login.
10. **Reentrancy / double-click (automation):** double-clicking "New User → Save" creates exactly one row (2.5).
11. **Mock-toggle parity (service):** with `Feature.RecvMockData` on, CRUD short-circuits to sample data;
    with it off, it hits the real SP (E5).
12. **Audit trail (repo):** every create/edit/reset writes a row to the new audit table with acting user + timestamp (F4).

---

## Phase 3 — Self-Check

- (a) Mitigations use modern WinUI 3 structures: `DispatcherQueue.TryEnqueue`, `ObservableRecipient` +
  MVVM Toolkit `[RelayCommand]`/`CanExecute`, `INotifyDataErrorInfo`, `{x:Bind}` — **yes**.
- (b) Dynamic-scaling violations called out with fluid rules (`*`/`Auto`, `MaxWidth`, `ScrollViewer`,
  `CharacterEllipsis`) — **yes**.
- Data-layer mitigations respect the repo's MySQL `ExecuteWithRetryAsync`/SP conventions and add
  idempotency/typed results where the transactional path demands it.
