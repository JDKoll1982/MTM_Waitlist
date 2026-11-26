# Proposal register

Every candidate change gets one ID, one record, and one decision.

## Identifiers

| Prefix | Category |
| --- | --- |
| `DEP-001` | Dependency update, replacement, removal or consolidation |
| `MOD-001` | Application modernization or custom-code replacement |
| `SEC-001` | Security improvement |
| `PERF-001` | Performance improvement |
| `UX-001` | User experience improvement |
| `AI-001` | AI tooling, agent, skill, MCP or instruction change |
| `DEV-001` | Development workflow, build, test or tooling change |
| `DB-001` | Database schema, stored procedure, index, or mirror change |

Numbers are zero-padded and never reused, including for a proposal later rejected.

## Required record

| Field | Content |
| --- | --- |
| ID | As above |
| Name | Short, unambiguous |
| Current state | Existing package version or the exact code being replaced, with file and line |
| Recommended change | The concrete action |
| Why | The problem it solves or the opportunity it takes |
| Expected benefit | Performance, security, stability, maintainability, UX, productivity |
| Affected projects | Every project touched |
| Affected files | Repository-relative paths, one per line |
| Compatibility | Framework, runtime, SDK and API constraints |
| Security implications | Both gain and new risk |
| Drawbacks and risks | Migration, behaviour change, support horizon |
| Effort | Small, medium or large, with the reason |
| Alternatives considered | At least one other option plus the native option, or why none exist |
| Sources | Official URL and date checked |
| Decision | One of the statuses below |

## Decision statuses

| Status | Meaning for scope |
| --- | --- |
| `APPROVED` | May appear as an implementation requirement |
| `REJECTED` | Excluded from scope; keep the finding for traceability |
| `DEFERRED` | Future consideration only |
| `MORE_RESEARCH_REQUIRED` | Incomplete; ask again after investigating |
| `KEEP_CURRENT` | Existing implementation explicitly protected |
| `PENDING_USER_DECISION` | Unanswered; never treated as approved |

Presented options that map to these: `APPROVE`, `REJECT`, `DEFER`, `MORE RESEARCH`, `KEEP CURRENT`.

## Priority scale

`--min-priority` filters on this scale, so use these exact words in the register.

| Level | Means |
| --- | --- |
| `critical` | An active vulnerability, a data-loss risk, or something blocking the app today |
| `high` | A real defect risk, an unsupported dependency, or a security gap that will bite |
| `medium` | Meaningful maintainability, performance or productivity gain |
| `low` | Hygiene, preference, or speculative improvement |

A proposal cut by `--min-priority` or `--top` stays in the register with its priority and the reason it
was not asked. Never delete a finding to fit a limit.

## Machine-readable register (`--json`)

`register.json` mirrors the register table so other tooling can consume it:

```json
{
  "generated": "2026-01-31",
  "areas": ["dependencies", "security"],
  "proposals": [
    {
      "id": "DEP-001",
      "name": "Update WinUIEx",
      "category": "dependency",
      "priority": "high",
      "decision": "APPROVED",
      "affected_files": ["MTM_Waitlist.csproj"],
      "sources": ["https://example.invalid/release-notes"],
      "notes": ""
    }
  ]
}
```

`id`, `priority` and `decision` use the vocabularies on this page — the checker treats any other value as a
failure.

## Candidate comparison table

Use this when more than one alternative is viable. One row per candidate.

| Field | Content |
| --- | --- |
| Proposal ID | Register ID |
| Candidate | Package, tool, framework or capability |
| Type | Runtime, development, AI, security, … |
| Current solution | What exists today |
| Latest version | Stable version, where applicable |
| License | License and compatibility |
| Maintenance | Activity and support |
| Security | Vulnerability and practice assessment |
| Compatibility | Fit with this workspace |
| Performance | Documented or measured effect |
| Complexity | Integration and upkeep cost |
| Additional dependencies | What it drags in |
| Risks | Migration and operational concerns |
| Recommendation | Adopt, replace, pilot, keep |

Adopt only when benefit justifies cost and risk. State explicitly why a candidate was not chosen.

## Register table

The specification carries the authoritative register:

| Proposal ID | Proposed Change | Category | Priority | Decision | Notes |
| --- | --- | --- | --- | --- | --- |

Rules:

* One row per proposal, no grouping.
* `Decision` holds exactly one status token from the table above — the checker script fails on
  anything else.
* `Notes` carries the rationale for rejected, deferred and keep-current rows.
* A changed decision adds a row recording the replacement and keeps the earlier one visible.
* Approving one proposal never approves a related one, even when the second was mentioned inside the
  first.

## Splitting and grouping

Split when the parts can be adopted or reverted independently, or when one part carries most of the
risk. Group only when the edits are inseparable parts of a single change — and then name every affected
file inside the one record.

## Scope protection

The implementation spec may report anything, but scope follows decisions only. A rejected or pending
proposal appearing inside an approved phase is a defect, not a detail.
