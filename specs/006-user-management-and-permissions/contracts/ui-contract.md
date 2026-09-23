# Contract: Screens, Routes and Identifiers

The interface the views, their view models and the tests code against. The page and view-model names below are the
ones the settled design names; they are used exactly as written.

## Routes

Every page is registered with the existing `PageService` in
`Services/DependencyInjection/ServiceRegistrationExtensions.cs`, and reached through the existing
`NavigationService`, so the shell's back behaviour and the Settings search keep working unchanged.

| View model | View | Reached from |
| --- | --- | --- |
| `UserManagementViewModel` | `UserManagementPage` | The Administration category's Users entry, shown by `permission.admin.users` |
| `CreateUserViewModel` | `CreateUserPage` | Add, on the user list |
| `EditUserViewModel` | `EditUserPage` | A row on the user list |
| `PermissionsViewModel` | `PermissionsPage` | The Administration category's Permissions entry, shown by `permission.admin.permissions` |

The two Administration entries sit in `SettingsPage.xaml` as `wct:SettingsCard` rows with
`IsClickEnabled="True"` bound to a navigation command, each wrapped in `x:Load` and carrying an `x:Name`, because
`x:Load` requires one. Neither is an expander: both navigate. A reader entitled to one entry sees that one and
still sees the category; a reader entitled to neither does not see the category at all. Both entries are reachable
from the Settings search, and both pages re-check the reader's entitlement when they are activated rather than
staying open on the answer they were opened with.

Back from either page returns to Settings, and opening the same page twice in a row does not walk back through
repeats of it.

## The user list

One row per person, and the whole row is one target that opens that person. Reset, deactivate and reactivate live
on that person's page and nowhere else. A row always shows all five facts, the sign-in name, the name, the employee
number, the role and the active state, at every width: with room it is one line, and without room it folds onto a
second line rather than cropping, and it stays one keyboard-reachable target that announces what it opens.

The list is virtualised, so it stays usable as the roster grows, and it never blocks the interface thread while it
loads. Its states are distinct and each is stated in the reader's words:

| State | What is shown |
| --- | --- |
| Everyone | Everyone, with switched-off people clearly marked. |
| A filter that matches nobody | "Nobody matches your filter", which is never the same message as an empty roster. |
| A saved filter that cannot be read | Everyone, with the failure stated. |
| A saved filter naming a role that no longer exists | Everyone, with the statement that the filter no longer applies. |
| The store cannot be read | An unavailable state with a manual retry. Never an empty list, a blank region or a sample row. |

An active filter is named, with the count of people it is hiding and a way to show everyone.

## The person's page

The fields sit in a form at the top and the actions in a clearly separated area below, each with its own heading.
The page is one page with a read-only state, not a second page. A reader who cannot act on the person is told so at
the top, and the fields and actions are shown unavailable with the reason in words rather than hidden. The create
page is a form only and carries no actions area at all.

Two things are shown unavailable with their reason on the reader's own account: deactivating it and changing its
sign-in name. An action chosen while edits are unsaved does not act on the half-edited person: the page asks the
reader to save and continue, or to discard, and never decides for them. A failed action keeps what the reader had
and states what failed, without blanking the page. Save is guarded against a repeated press, so one press creates
exactly one person or writes exactly one change.

The deactivation confirmation names both halves in one sentence: the person cannot sign in again, and a session
already open stays open until they close the application. The same sentence appears on the page itself, so it is
findable after the confirmation is gone. Reactivation says that access returns from the person's next sign-in.

## The PIN window

`PinRevealDialog` shows a random four-digit PIN once, after a create or a reset. It carries the PIN, whose account
it is for, the sign-in name, when it was issued, who issued it, and the instruction to change it at first sign-in
and to hand it over rather than file it. It can be printed. Printing never closes it, and a failed or cancelled
print costs nothing. It must be closed deliberately: a close that did not come from its own close button is
cancelled. After it closes, the PIN is not held anywhere, and a second reset replaces it and says so.

## The permissions page

One card per area of the declaration, in the declaration's order, and exactly five because the declaration names
five. Each card's heading row carries the area's name at the left and that area's permissions across the top, each
with its plain-language label and the sentence saying what it gates; the people are the rows, ordered by rung and
then by name, and a cell is one person against one permission. It is never a grid of roles against permissions, it
is not a second place where a role's baseline can be edited, and nothing on it changes a baseline.

The cells are three states rather than two, because a person with no row of their own inherits their role's
baseline and the screen has to say so. The difference is carried by shape, never by colour alone, and every cell
carries an accessible name that says its state in words.

| The cell's state | What the cell shows |
| --- | --- |
| On, chosen for this person | A filled square with a check. |
| On, from the role's baseline | An outlined circle with a check. |
| Off | An empty square. |
| A person who outranks the reader | Every cell of the row is present and unavailable, the row carries the reason in words, and nothing about the person can be changed. |
| `permission.admin.permissions` | Present, locked, with the reason, and clearable by nobody. |
| Changed but not saved | A mark beside the cell, a count on the row, and the row listed in the unsaved block. |

A card too wide for the window scrolls sideways rather than dropping a column, and no control carries a fixed
width: the column floor is declared once so the heading row and the rows beneath it line up.

Saving with nothing changed is unavailable and writes no history row. A save is written for one person, because
the change-set procedure takes one person and a whole set: the page lists the people with unsaved changes, and each
one is saved on its own, confirmed in the reader's words with one sentence per changed cell and a count when
several change. A completed save can be undone, and the undo reverses the whole save rather than half of it. Where
the value has moved since, the page says which permission moved, shows what it is now, and asks before restoring.
Leaving with unsaved changes warns and says how many changes are pending.

The second view on the same page answers who holds a feature: the roles whose baselines give it, taken from the
seeded baselines that the declaration's two-direction check ties to it rather than restated beside the view, and
then only the people who differ from their role's baseline, each marked granted or denied. It is
read-only, opening one of those people leads to their page, a feature nobody holds is stated in words rather than
shown as an empty region, and a person who holds it but is switched off is listed and marked switched off.

## Resource keys

Every label, title, field label, action and message is localised, and no two labels share a key. The prefixes are
fixed so a reviewer can find a screen's strings by name:

```text
UserManagement_*      the user list
CreateUser_*          the create form
EditUser_*            the person's page and its actions
Permissions_*         the permissions page and its who-holds-this view
PinReveal_*           the one-time PIN window
Administration_*      the two Settings entries and their descriptions
Role_<role_code>      a role's display text, keyed on the role code
Permission_<suffix>   a permission's label and its what-it-gates sentence, keyed on the key's own name
```

A string shown to a person resolves through the application's resource mechanism with a fallback, and no resource
key is ever shown to a person.

## What tests code against

- The fourteen permission keys and the nine role codes, spelled exactly as the specification pins them.
- `PermissionRegistry`, `PermissionKeys`, `RoleAuthorization`, `RoleBadgeCatalog`, `IPermissionService`,
  `IRoleCatalogService`, `IUserManagementService`, `IPermissionAdministrationService`.
- `PermissionAdministrationService`, the page-facing service behind the change set and its reversal.
- `UserManagementViewModel`, `CreateUserViewModel`, `EditUserViewModel`, `PermissionsViewModel`,
  `PermissionHoldersViewModel`, `PinRevealDialogViewModel`.
- `PermissionHoldersView`, both the view and its model, because the who-holds-this view is a type of its own rather
  than a region of `PermissionsViewModel`.
- The typed outcome a caller reads: a duplicate sign-in name arrives as the `1062` answer and becomes one typed
  "already taken" result, and each store refusal arrives as its `mtm_*` token and becomes one typed reason.

## Accessibility and scaling

No control has a fixed width. A long value wraps or trims with a tooltip rather than being cropped. Every screen
stays usable between 150 and 200 percent scaling and at the narrowest window the application permits, with all five
facts of a list row visible at both widths. No control is shown to a person who cannot use it, and no screen
freezes or refuses interaction while it loads or saves.
