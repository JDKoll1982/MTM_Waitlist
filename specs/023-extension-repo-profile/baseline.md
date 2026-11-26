# Pre-change baseline

Recorded before any edit of `extensions/csharp-xaml-graph`, from that directory, on
`extension-profile-update` at version **0.5.1**. SC-003 is measured against these numbers: with no
`csharp-xaml-graph.profile.json` present, the new build must reproduce them exactly.

## `npm test`

| What | Value |
|---|---|
| Test files | 29 passed (29) |
| Tests | 786 passed (786) |
| Failures | 0 |
| Duration | 15.80 s |

## `npm run selftest`

| What | Value |
|---|---|
| Checks | 116 PASS, 0 FAIL — `ALL CHECKS PASSED` |
| C# files | 815 |
| XAML files | 93 |
| Parse errors | 0 |
| **Nodes** | **1095** (view=82, viewmodel=76, service=475, model=418, external=44) |
| **Edges** | **4776** (constructs=21, field=483, inherits=192, reference=2803, injects=730, getservice=54, implements=219, composes=96, binds=71, resolves=47, template=60) |
| **Findings** | **21** (cycle=8, dead-binding=0, unbound-view=0, unused-type=9, unused-member=4) |

The three numbers SC-003 compares are **1095 nodes, 4776 edges, 21 findings**.

## Extension version

`0.5.1` in `extensions/csharp-xaml-graph/package.json`. This change bumps it to `0.6.0`.
