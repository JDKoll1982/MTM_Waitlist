# Contract: Permissions and the Role Ladder

This is the interface every gate, the permissions page, the who-holds-this view and the service host code against.
The identifiers below are pinned by the specification's Verbatim Constraints section. They are copied exactly:
never re-cased, pluralised, renamed or paraphrased, in code, in a test or in a seed.

## The permission keys

Each names exactly one gated action. A person holds it or does not.

```text
permission.requests.handle
permission.cache.refresh_api
permission.settings.ignored_locations
permission.settings.hot_work_centers
permission.settings.part_pictures
permission.settings.cache_refresh
permission.settings.urgency_minutes
permission.settings.defect_types
permission.settings.computers
permission.setup.dunnage_quick_add
permission.setup.work_centers
permission.admin.users
permission.admin.reset_password
permission.admin.permissions
```

## What each one gates, and the list it replaces

| Key | Gates | The hand-written list it replaces |
| --- | --- | --- |
| `permission.requests.handle` | Accepting, completing and releasing a request | `RequestActionPolicy.HandlerRoles` |
| `permission.cache.refresh_api` | Calling the service's cache-refresh API | `ServiceOperatorRoles.Approved` in `MTM_Waitlist.Mock.Service` |
| `permission.settings.ignored_locations` | Ignored locations in inventory lists | `SettingsViewModel.AllowedIgnoredLocationManageRoles` |
| `permission.settings.hot_work_centers` | Hot work centres | `SettingsViewModel.AllowedHotWorkCenterManageRoles` |
| `permission.settings.part_pictures` | Part pictures | `SettingsViewModel.AllowedImageLocationManageRoles` |
| `permission.settings.cache_refresh` | Pulling a cache refresh from the Settings screen | `SettingsViewModel.AllowedCacheRefreshRoles` |
| `permission.settings.urgency_minutes` | Allowed minutes per item | `UrgencyAllotmentEditorViewModel.AllowedUrgencyManageRoles` |
| `permission.settings.defect_types` | The defect-type catalogue | `DefectTypeCatalogService.AllowedRoles` |
| `permission.settings.computers` | The computer registry | `ComputerManagementViewModel.AllowedComputerManageRoles` |
| `permission.setup.dunnage_quick_add` | Quick Add dunnage definitions | `DunnageWorkflowService.AllowedQuickAddRoles` |
| `permission.setup.work_centers` | Work-centre setup | `SetupWorkCenterViewModel.AllowedManageRoles` |
| `permission.admin.users` | Creating, editing and deactivating a person, and opening the user list | new; the Settings entry's gate |
| `permission.admin.reset_password` | Resetting a password | new; read by the reset action on the person's page (`EditUserViewModel` in T048 and `EditUserPage` in T051), so it has a named gate site like every other key |
| `permission.admin.permissions` | Opening and editing the permissions page. Fixed: never editable from that page | new |

Eleven rows above replace a retired hand-written list. The three marked *new* have no predecessor, so their
baselines are stated sets rather than a comparison: `permission.admin.users` and `permission.admin.reset_password`
ship to Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT Department and Developer, and
`permission.admin.permissions` ships to IT Department, Plant Manager and Developer only. The retired lists and the
gate sites are the specification's Context counts.

The badge beside the signed-in name is **not** a permission and does not become one (FR-058). It is presentation,
and it is fixed rather than gated.

## The role ladder

The ranks, pinned verbatim:

```text
developer              100
it_department           90
plant_manager           80
production_lead         70
setup_lead              60
material_handler_lead   50
material_handler        10
production              10
setup                   10
```

The nine codes, pinned verbatim:

```text
developer
it_department
plant_manager
production_lead
setup_lead
material_handler_lead
material_handler
production
setup
```

The retired code that must not remain in the catalogue is `admin`. The display name that replaces it is
`IT Department`. The new role's code is `material_handler_lead` and its display name is `Material Handler Lead`.

The strings that name roles the catalogue has never held, and that must be removed rather than carried forward:

```text
administrator
supervisor
manager
quality
quality inspector
Setup Tech
```

## The declaration

`PermissionRegistry` is the one place a permission exists. It declares, per key: the label's resource key, what it
gates, the area it belongs to, the shipped fallback, and the gate sites that read it. Nothing else may add one.

It declares **no** per-role baseline. The baselines are the seeded rows `seed_permission_role_baselines` writes, and
the two are tied by the third check below rather than by construction — the deliberate resolution of the seed's
section 3.8 contradiction, recorded in `research.md` (FR-046, FR-048).

Two checks run over the declaration, and each is asserted by a test:

1. **No gate reads an undeclared key.** Every call site that asks for a permission names a key the declaration
   holds. A gate reading anything else is a failure, not a silent refusal (FR-061).
2. **No declared key goes unread.** Every key in the declaration is read by at least one named gate site, so the
   declaration cannot grow entries nothing uses (FR-062).

A third check compares the declaration to the seeded data: every key has a baseline row for every role the
catalogue holds, and every baseline row names a declared key (FR-047, FR-060).

## Resolution

For one permission and one person, in this order:

```text
the person's own stored row   ->   their role's baseline   ->   the shipped fallback
```

- The person's own row wins over any wider scope (FR-053), and the precedence order is corrected before the first
  per-person row is ever written.
- A person with no row of their own inherits their role's baseline, and holds nothing beyond it (FR-051).
- A role with no baseline row for a key answers from the shipped fallback, never with a refusal (FR-060).
- An unreachable store answers from the shipped fallback (FR-050), so a settings failure does not turn every
  control in the application into a refusal.
- One lookup answers both the control's visibility and the action it guards (FR-056), so a control is never shown
  to somebody whose use of it would be refused (FR-117).
- The answer is cached for the session and explicitly invalidated when a permission is saved, and it is never read
  inside a layout pass on the interface thread.

## The one subset rule

Whatever `permission.settings.cache_refresh` admits must also be admitted by `permission.cache.refresh_api`, so
the Settings screen can never show a control the service would refuse (FR-057). The two live in different
projects, so a test asserts the relationship against the shipped baselines rather than trusting either side.

## The one fixed row

`permission.admin.permissions` is present on the permissions page, shown locked, with the reason in words, and
cannot be cleared from that page by anyone (FR-059). This is not a second rule about rank: it is the one place
where reading the declaration to decide whether the declaration may be edited would be circular.

## Refusals

A refused change, wherever it happens, is reported plainly to the reader and writes no audit record, because
nothing changed (FR-026). A gate refused at the action rather than at the control is refused in the same words as
the control's absence, so a person never meets two accounts of the same rule.
