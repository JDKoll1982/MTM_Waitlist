# Phase 06 - Recovery Flows and Data Repair

## Goal

Implement startup self-healing flows for duplicate and damaged records.

## Scope

- Duplicate user record resolution.
- Corrupted setting remediation path.
- Startup-safe continuation after repair.
- Recovery behavior compatible with schema artifacts maintained under `./Database`.

## Implementation Tasks

- Detect duplicate user rows during identity load.
- Delete oldest duplicate and retain newest record.
- Apply corrupted-setting recovery rules from startup reset flow.
- Continue startup without exposing internal data errors to end user.

## Suggested NuGet Packages (If Relevant)

- FluentValidation (optional validation rules for repair eligibility)
- No additional Phase 06-specific package is required after the data/config stack from earlier phases is in place (for example, MySqlConnector plus Dapper from Phase 03 and existing JSON config services)

## Done When

- Duplicate and corruption cases are resolved without startup crash.
- Recovery behavior matches spec wording.

## Testable End State

Manual tests:

1. Seed duplicate user rows and confirm oldest is removed.
2. Seed corrupted setting and confirm targeted reset before full reset fallback.
3. Confirm startup resumes after successful repair.
