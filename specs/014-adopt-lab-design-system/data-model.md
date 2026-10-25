# Phase 1 Data Model: One set of rules for every screen, window and dialog

**Feature**: `014-adopt-lab-design-system`  
**Created**: 2026-10-25

This feature stores nothing. The entities below are the shapes that cross a boundary at runtime, and the surfaces the
audits read.

## MessageKind

The six standard questions. A closed set: an unforeseen need joins the set rather than becoming a seventh ad-hoc
dialog.

| Member | Actions | Default | Cancel |
|---|---|---|---|
| `Acknowledge` | OK | OK | n/a, OK closes |
| `Confirm` | Act, Cancel | Act | Cancel |
| `Decide` | Yes, No | Yes | No |
| `DestructiveConfirm` | Act, Cancel | Cancel | Cancel |
| `LeaveWithoutSaving` | Leave, Stay | Stay | Stay |
| `Retry` | Try again, Cancel | Try again | Cancel |

Validation: at most two actions, always in the order above. The default is fixed per kind and is never supplied by a
caller.

## MessageDialogRequest

What a screen asks for.

| Field | Required | Rule |
|---|---|---|
| `Kind` | yes | one of the six |
| `Title` | yes | names the subject, not only the action (FR-005) |
| `Message` | yes | names the consequence of the default action (FR-010) |
| `ActLabel` | for Confirm, Destructive, Retry | the verb for the affirmative, e.g. Remove |
| `CancelLabel` | no | defaults to the kind's own cancel word |
| `Subject` | no | the named thing, echoed in the body when supplied |

Validation: `Kind` must be defined; `ActLabel` must be present whenever the kind has an act; `Title` and `Message` must
be non-blank. A blank title or message is a programming error and is rejected before a window opens.

## MessageDialogOutcome

What comes back. One shape for every kind, so no caller has to know which kind it asked for.

| Field | Meaning |
|---|---|
| `Chosen` | `Act`, `Cancel`, or `Acknowledged` |
| `WasDefault` | whether the chosen action was the kind's default |

Validation: `Chosen` is never unset. Escape maps to the kind's cancel answer. Closing the window by its own close
button maps to cancel for every kind except `Acknowledge`, which maps to `Acknowledged`.

## DialogSizing

The four values FR-004 pins, declared as resources.

| Resource | Value | Why |
|---|---|---|
| `DialogMinWidth` | 480 | still readable on a small screen |
| `DialogPreferredWidth` | 720 | leaves the owner visible on both sides |
| `DialogMaxWidth` | 880 | never outgrows the 1080 page column |
| `DialogMaxBodyHeight` | 640 | fits inside a 720-tall window with title and buttons |
| `DialogBodyMinHeight` | 120 | one line plus padding never collapses |

## ScreenSurface

What the inventory and the audits read. Not a runtime type.

| Field | Meaning |
|---|---|
| Path | the XAML file, repo-relative |
| Kind | Window, Page, Dialog, Control, or ResourceDictionary |
| Area | the module it belongs to |
| IsModal | whether it presents as a modal surface |
| Owner | for a dialog, the window it is centred on |

Validation, checked by the audits:

- exactly one type per file (FR-024)
- a resource-only file declares no `x:Class` and is still checked for visuals (FR-026)
- every entry in the inventory resolves to a file on disk, and every file on disk appears in the inventory (FR-025)
