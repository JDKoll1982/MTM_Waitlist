# QOL Refactor — Before and After Design

Workflow: `workorder-new-request` · Screen: work-centre selection · Refactor: `fewer-clicks`

Proposed by: <name> · Date: <yyyy-mm-dd> · Design record: `fewer-clicks-design.json`

## A. Current State

What the user sees today, how the interaction works, and what makes it inefficient. Cite the XAML and
the workflow sheet this came from.

![BEFORE — current implementation](fewer-clicks-before.png)

## B. Proposed Improvement

What is changing, why it helps, and how the user interacts with the result. State any functionality that
moves or is removed, and why.

![AFTER — proposed QOL improvement](fewer-clicks-after.png)

## C. Visual Comparison

![BEFORE and AFTER, side by side](fewer-clicks-comparison.png)

1. The press is chosen from cards instead of a picker.
2. The running job is visible before the press is chosen.

## D. Expected User Benefits

Observable improvements, not invented numbers:

- One selection instead of open-picker → choose → confirm.
- The running job is visible before committing, so a wrong press is caught earlier.

## E. Implementation Impact

| Area | What changes |
| --- | --- |
| XAML | `Module_Waitlist/Views/WaitlistViewPage.xaml` |
| View model | `NewRequestWorkCenterViewModel` |
| Commands | `SelectWorkCenterCommand` |
| Shared components | existing press card template |
| Navigation | unchanged |
| Regression risk | press list now loads on page entry rather than on flyout open |

## Approval

Design rendered from the mockups in `mockups/` and approved on <yyyy-mm-dd> by <name>.
The approval hash covers the mockup sources and shared design assets; any change to them invalidates
the approval and requires re-approval.
