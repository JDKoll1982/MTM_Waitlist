# Contract: Launch Concurrency, Budget and Feed

## 1. Step sequence and groups

The catalogue remains the sequence and the displayed count stays derived from it. `picture-cache` moves to the
first position. `visual-priming` stays last before `shell` and must not be moved or removed.

| Group | Members | Why concurrent |
|---|---|---|
| Local reads and first store contact | `read-local-settings`, `read-hardware-identity`, `store-reachability` | none depends on another |
| Session checks | `judge-session`, `check-computer-against-store`, `check-temporary-credential` | each reads the store independently |

Everything else keeps its current order. Members of a group start together and the group ends when every member
has ended.

## 2. Bounds

- Every step keeps its own stated maximum (FR-036). The ceiling stays 30 seconds; the two best-effort steps stay
  bounded more tightly.
- The launch-wide budget is 45 seconds (FR-024). It is additional to each step's maximum and never replaces it.
- Passing the budget ends the walk at `LaunchOutcome.Blocked` with a diagnosis naming the budget, and the
  remedies offered are only those that could remove it.
- A step that passes its own maximum is still recorded as failed and is not waited on, exactly as today.

## 3. Feed

- The feed stays append-only and immutable per entry.
- Every individual operation keeps its own line, written before its work begins (FR-031).
- The surface renders every started-and-not-ended line at once, so a concurrent launch has several lines.
- A best-effort failure is a line, never a launch failure.

## 4. Prohibited

- No call that refreshes the cached plant data may exist on any launch path (FR-030). Refreshing the mirror
  belongs to `MTM_Waitlist.Mock.Service` alone.

## 5. Pinned step identifiers

`read-local-settings`, `store-reachability`, `read-hardware-identity`, `read-computer-record`,
`read-machine-configuration`, `save-machine-configuration`, `read-remembered-sign-in`, `resolve-person`,
`resolve-roles`, `check-credential`, `judge-session`, `check-computer-against-store`,
`check-temporary-credential`, `set-new-password`, `picture-cache`, `visual-priming`, `shell`.
