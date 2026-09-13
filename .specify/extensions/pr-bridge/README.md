# spec-kit-pr-bridge

A [Spec Kit](https://github.com/github/spec-kit) extension that auto-generates pull request descriptions, reviewer checklists, and change summaries from spec artifacts — the missing final step in the SDD workflow.

## Problem

Spec Kit's workflow ends at implementation. After `/speckit.implement`, developers manually write PR descriptions from scratch — even though all the information already exists in spec.md, plan.md, and tasks.md:

- PR titles and descriptions are written from memory instead of spec artifacts
- Reviewers have no structured way to verify code against spec requirements
- Requirement traceability is lost between spec documents and the pull request
- Each PR takes 15-30 minutes of manual description writing
- No way to confirm whether the implementation matches the original spec intent

## Solution

The PR Bridge extension adds three commands that close the SDD loop:

| Command | Purpose | Modifies Files? |
|---------|---------|-----------------|
| `/speckit.pr.generate` | Generate a complete PR description with title, summary, and requirement checklist from spec artifacts | No — read-only |
| `/speckit.pr.checklist` | Generate a reviewer checklist that maps each spec requirement to its implementing files | No — read-only |
| `/speckit.pr.summary` | Generate a concise change summary comparing spec intent with actual implementation | No — read-only |

## Installation

```bash
specify extension add --from https://github.com/Quratulain-bilal/spec-kit-pr-bridge/archive/refs/tags/v1.0.0.zip
```

## How It Works

### The Missing Step

```
/speckit.specify    → spec.md      What to build
/speckit.plan       → plan.md      How to build it
/speckit.tasks      → tasks.md     Step-by-step tasks
/speckit.implement  → code         Build it
                    → ???          Tell reviewers what you built

/speckit.pr.generate → PR description   ← PR Bridge fills this gap
```

### What Gets Generated

**`/speckit.pr.generate`** reads all spec artifacts and git changes to produce:

```markdown
## Summary

JWT-based authentication with login, signup, and token refresh
endpoints — per spec 003-user-auth.

## What Changed

| Category | Files | Changes |
|----------|-------|---------|
| Source   | 5     | +320 / -12 |
| Tests    | 3     | +180 / -0  |
| Config   | 1     | +8 / -0    |

## Spec Requirements

- [x] Users can sign up with email/password → `src/auth/signup.ts`
- [x] Users can log in and receive JWT → `src/auth/login.ts`
- [x] Protected routes require valid token → `src/middleware/auth.ts`
- [ ] Rate limiting on login endpoint → ⚠️ Not implemented

## Task Completion

- Phase 1: 6/6 ✅
- Phase 2: 8/8 ✅
- Total: 14/14 (100%)

## Test Plan

- `login.test.ts` — Login flow, token generation, error cases
- `signup.test.ts` — Registration, validation, duplicate detection
```

**`/speckit.pr.checklist`** gives reviewers a requirement-traced review path:

```markdown
## Review Checklist

### User Scenario: New User Registration
- [ ] **Req**: Email validation on signup
  - Files: `src/auth/signup.ts` (L12-L28)
  - Tests: `src/auth/__tests__/signup.test.ts`
  - Verify: Check regex pattern matches RFC 5322

### Technical Decisions
- [ ] JWT uses RS256 signing (per plan.md)
- [ ] Passwords hashed with bcrypt, cost factor 12
```

**`/speckit.pr.summary`** compares intent vs reality:

```markdown
## Alignment

| Requirement | Status | Notes |
|-------------|--------|-------|
| User signup | ✅ Implemented | — |
| User login  | ✅ Implemented | — |
| Rate limiting | ❌ Not implemented | Deferred to next sprint |
```

## Workflow

```
/speckit.specify                     ← Define the feature
       │
       ▼
/speckit.plan                        ← Plan the implementation
       │
       ▼
/speckit.tasks                       ← Generate task list
       │
       ▼
/speckit.implement                   ← Build it
       │
       ▼
/speckit.pr.generate                 ← Generate PR description
/speckit.pr.checklist                ← Generate reviewer checklist
/speckit.pr.summary                  ← Generate change summary
       │
       ▼
Create PR with ready-to-paste content
```

## Commands

### `/speckit.pr.generate`

Produces a complete PR body from spec artifacts and git diff:

- **Title**: Conventional format (`feat:`, `fix:`, `refactor:`)
- **Summary**: Extracted from spec.md purpose and scenarios
- **What Changed**: File categories with addition/deletion counts
- **Spec Requirements**: Checklist with each requirement mapped to implementing files
- **Task Completion**: Progress from tasks.md by phase
- **Test Plan**: Test files with coverage descriptions
- **Spec Artifacts**: Links to the source spec documents

### `/speckit.pr.checklist`

Produces a reviewer-focused checklist:

- Maps every spec requirement to its implementing files and line ranges
- Maps every plan decision to the code that follows it
- Flags requirements with no matching tests
- Flags requirements with no matching code changes
- Includes quality checks derived from success criteria

### `/speckit.pr.summary`

Produces a stakeholder-friendly change summary:

- Compares spec intent with actual implementation
- Reports alignment, deviations, and gaps
- Includes scope metrics (files, tasks, test coverage)
- Identifies risks and incomplete requirements
- Supports audience targeting (product manager, tech lead, etc.)

## Hooks

The extension registers an optional hook:

- **after_implement**: Offers to generate a PR description after implementation is complete

## Design Decisions

- **All commands are read-only** — never creates or pushes PRs directly, only generates text
- **Artifact-driven** — every PR section traces to a specific spec artifact or git data
- **Graceful degradation** — works with just spec.md if plan.md or tasks.md are missing
- **Gap detection** — actively flags spec requirements with no matching code or tests
- **Template-aware** — adapts to `.github/PULL_REQUEST_TEMPLATE.md` if one exists
- **No opinions** — reports facts from artifacts and code, never adds subjective judgments

## Requirements

- Spec Kit >= 0.4.0
- Git >= 2.0.0

## License

MIT
