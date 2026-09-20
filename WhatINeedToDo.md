Committed as 3951032 and pushed to origin/004-unified-card-item-picker.

What I verified (not assumed)

Build: 0 Warning(s), 0 Error(s). Tests: total 1089, failed 0, passed 1062, skipped 27 — 14 of those tests are new and cover this work.

Done and pushed

"No die ⇒ no die option" — the job's dies now pass through one shared rule, so a job whose only die row is the No Die placeholder has no die, is offered no die Item, and shows no die location anywhere. The picker already gated on that flag; the flag was simply wrong.
The waitlist line — the die's name is its own number (FGT0002000). Where the die lives is no longer folded into it, because the request page lists the locations for itself.
The details page — this was the actual cause of your complaint. It built its row from the request alone, so the die and its location, which are job values the request never stored, could not appear on it. It now reads the job and fills those rows.
Not done — one item

One request per selected die is not built. A die request has exactly one free value column, and pickup-die already spends it on the "where is the die going" answer, so several entries from one job would all read the same die. Adding a second column fixes it properly but changes the schema, and the die step also needs a trigger that doesn't key off the item's identity. I asked before starting; with no answer available I chose not to push a schema change and a new screen you couldn't review.

The full design, including the exact trigger ordering that trips up deliver-die, is written to repo memory as die-request-multi-die-blocker.md.

Spec state — my new code cited FR-054/055/056, which didn't exist. Those are now defined, with SC-025/026, and the batch is recorded as Phase 13 with only verified work ticked. I also ran the audit your spec's own Phase 12 note asked for: all 44 requirement IDs cited in source now resolve.

What you need to do
Nothing is blocking. The storage decision below was taken on 2026-09-20 — reuse `input_value` — so no database change is owed for it.
The UI and live-database gates are closed: T164, T165, T174, T184, T185 and T188 have all now been exercised, the die screens in the running app and the rest against the live store.
The preview gap is fixed and verified on screen: with two dies selected, the Preview and the Confirmation each now list a row per die, and the Confirmation shows the "This will raise 2 requests" line it had been computing but never displaying. Recorded as T233 in `specs/004-unified-card-item-picker/tasks.md`.