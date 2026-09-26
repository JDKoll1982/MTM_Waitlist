# Security review: nothing secret reaches the log store

Feature: 010-startup-rebuild (task T055, checklist subphase 6.1g). Requirements: FR-021, FR-032 to FR-037.
Contracts: `contracts/logging-contract.md` (section 6, "Never logged"),
`contracts/sql-contracts.md` (sections 1 and 2), `contracts/identity-contracts.md`.
Persona: Security Engineer. Date of the reviewed revision: 2026-09-26, at commit `d9bfbb2` plus this unit's
working-tree changes.

## The claim under review

Two claims, and they are different in kind. The first is a prohibition: **no credential, key material or
remembered secret can reach the log store.** The second is a permission with a boundary: **the shared key
file's path is the only secret-adjacent value that is ever written.**

"Reach the log store" means reach `ops_startup_logs`, the one log table (`contracts/logging-contract.md`,
opening paragraph; the identifier is pinned verbatim by the specification). It does not mean "reach a log
file": the file-based path is deleted by subphase 6.6, and this review is about the store.

## What is secret in this store, and where it lives

Four objects hold something that must never be read back out of a log:

| Object | The secret-bearing columns | Why |
|---|---|---|
| `core_users_profiles` | `password_hash`, `password_salt` | A digest and its salt: a replayable and crackable pair |
| `user_active_sessions` | `token_hash`, `token_salt` | The session digest and its salt; the digest is a bearer credential for the session |
| `auth_remembered_sign_ins` | `payload_ciphertext`, `payload_iv`, `key_fingerprint` | The encrypted remembered payload, its vector, and which key produced it |
| `auth_sessions_tokens` (retired by T060, still deployed) | `token_hash`, `token_salt` | The superseded session store, same shape |

None of the four is `ops_startup_logs`, and none of the four has a foreign key, trigger or procedure that
copies a column into it. The evidence for each of the statements below is a search over the shipped artifacts
or a read of the live local store; each is reproducible with the command named beside it.

## Evidence 1 — the log store has no credential column

`ops_startup_logs` is nineteen columns and every one of them is about the entry, not about the person's
secret: identity (`public_id`, `correlation_id`), time (`created_utc`), classification (`level`,
`event_action`, `outcome`), actor and machine names (`actor_kind`, `actor_id`, `host_id`, `mac_address`), the
four panel fields (`module`, `error_type`, `exception_detail`, `error_fingerprint`), the entry body
(`message`, `payload_json`) and the chain (`previous_hash`, `entry_hash`). There is no `password`, `token`,
`salt`, `key` or `credential` column, and there is no column whose declared type or comment invites one.

`Database/Tables/10_ops_startup_logs/create.sql`, and the same shape in
`Database/Tables/AllTables.sql`; asserted column-by-column by
`Database/Validation/startup_schema/validate.sql`.

## Evidence 2 — one writer, and the store is append-only apart from retention

Across the whole stored-procedure tree there are exactly **two** statements that address `ops_startup_logs`:
one `INSERT` (in `sp_ops_startup_logs_insert`) and one `DELETE` (in `sp_ops_startup_logs_purge`). There is no
`UPDATE` of the table anywhere, so a value written once cannot be rewritten in place, and the only removal is
the retention routine, which removes the oldest entries and nothing else.

Search: `(INSERT INTO|UPDATE|DELETE FROM)\s+ops_startup_logs` over
`Database/StoredProcedures/**/create.sql` — two matches, both named above.

This is also what makes the prohibition checkable in one place. Every entry, from every caller, arrives
through `sp_ops_startup_logs_insert`, whose parameter list is the entry's fields and no other value: there is
no parameter a caller could route a credential through, and the procedure reads nothing from any table but
the previous entry's hash.

## Evidence 3 — no trigger can smuggle a row in

Two triggers exist in the schema, both created by 008-part-pictures and both on `config_images_locations`:
`trg_config_images_locations_history_on_insert` and `trg_config_images_locations_history_on_update`. Neither
mentions `ops_startup_logs` in its body. No trigger is defined on any of the four secret-bearing objects, so a
write of a session or a remembered sign-in cannot cascade into a log entry.

Read on the live local store:
`SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE FROM information_schema.triggers WHERE trigger_schema='mtm_waitlist'`
— two rows; then the same query with `AND ACTION_STATEMENT LIKE '%ops_startup_logs%'` — zero rows.

## Evidence 4 — no log procedure reads a secret column

The four log procedures (`sp_ops_startup_logs_insert`, `_filter`, `_fingerprint_groups_get`, `_purge`) name
none of `token_hash`, `token_salt`, `password_hash`, `password_salt`, `payload_ciphertext`, `payload_iv` or
`key_fingerprint`. The single search hit in that folder is a **comment** in `sp_ops_startup_logs_insert`
pointing a reader at `sp_auth_temporary_credential_attempt_record` for the `DELIMITER` convention — a
cross-reference, not a read. The panel's two readers return columns from `ops_startup_logs` alone.

Search: `token_hash|token_salt|password_hash|password_salt|payload_ciphertext|payload_iv|key_fingerprint`
over `Database/StoredProcedures/sp_ops_startup_logs_*/create.sql`.

## Evidence 5 — the store holds nothing, and a scan for secret-shaped text finds nothing

On the live local store (`127.0.0.1:3306`, `mtm_waitlist`), `ops_startup_logs` holds **0 rows**, so there is
no historical entry that could have carried a secret. A scan of the columns a message would land in returns
zero rows:

```sql
SELECT COUNT(*) FROM ops_startup_logs
WHERE LOWER(CONCAT_WS(' ', message, COALESCE(payload_json, '')))
      REGEXP 'password|passwd|pwd|secret|token|salt|api[_ -]?key|private[_ -]?key|connection string|user id=';
```

Result: `0`. **The scan is weak evidence and is recorded as such.** It proves nothing about an entry that has
not been written yet; it only establishes that the store carries no secret today. Its value is that it is the
check a reviewer can re-run after the seam lands, when the store is no longer empty.

## Evidence 6 — the code that exists names a secret; it never carries one

Two call sites in the tree mention a credential in their message text, and both name it rather than
interpolate it: `PinRevealDialogViewModel` writes *"the credential stays on screen"* and `SignOutService`
writes *"clearing the remembered credential and session keys"*. Neither passes a value, a path or a hash. A
search for a logging call on the same line as `KeysFolder|KeyFile|SecretKey|Password|passwd|credential|token|salt`
finds those two lines and no third.

Search: `(logger|Log)\.(Info|Warn|Error|Debug|Critical|Write).*(KeysFolder|KeyFile|SecretKey|Password|passwd|credential|token|salt)`
over the application's own projects.

## The second claim: the key file's path is the only secret-adjacent value

The permitted value is a **path**. It is permitted because a support reader who sees a decryption failure
needs to know which key was in hand, and the path says that without disclosing anything about the key.

What makes the permission narrow is what is on the other side of it:

- The key file's **name** is a path component and is admissible with the path. The verbatim name
  `MTM_AUTH_USER_SECRET_KEY.txt` appears in this repository **only in documents** (the specification's
  Verbatim Constraints, the brief, `data-model.md`, `research.md`, and the analysis records) — not in any
  `.cs`, `.json` or SQL artifact. Nothing in the application reads or names the file today; the sign-in step
  that will (T113, T114) does not exist yet.
- The key's **bytes**, its **hash** and any **derived value** are not admissible. This is the boundary that
  matters, and it has one sharp edge: `auth_remembered_sign_ins.key_fingerprint` is *stored* by design
  (FR-014 — it is what lets a rotated key degrade to the sign-in form instead of raising), so a fingerprint is
  a secret-adjacent value that already exists in the store. It is **not** admissible in a log entry: section 6
  permits the path only, so a fingerprint must never be written to `ops_startup_logs`. That is a constraint
  this review passes forward to T064 to T067 and to the sign-in steps that read the fingerprint, because it is
  the one place where a future reader could reasonably think the two rules agree when they do not.
- The **connection string**, including the store credential, is inadmissible. Section 6 names it; nothing
  sanitizes it yet (see the limits below).
- Where the path comes from is itself reviewed: the folder is the machine-scope `keys_folder` picture-source
  row in `config_images_locations` (task T051), read through
  `sp_config_images_locations_computer_sources_get`, and machine setup refuses an empty path with
  `mtm_computer_source_path_missing`. So the value logged is one the store already holds as machine
  configuration, and it is written by one procedure with one parameter — not composed at a call site.

## What this review could not prove, and why

1. **The enforcing code does not exist yet.** The seam, its exception serializer and its `ILogger` provider
   are T064 to T067 and T175 to T179. Until they land there is no code that decides what goes into
   `message`, `exception_detail` or `payload_json`, so the prohibition holds *because nothing writes to the
   store at all* (0 rows) rather than because something refuses. This review is therefore, today, a review of
   shapes and of the write path — `Database/`, the procedures and the table — not of runtime behaviour.
2. **`payload_json` is free-form text, and its allowlists are contract-only.** The gathered-context table and
   the `Exception.Data` allowlist live in `contracts/logging-contract.md` sections 1.1 and 1.3. No
   implementation constrains them yet, so a call site that put a connection string into `Exception.Data`
   would be stored once the seam lands and the serializer honours the contract as written. This is the largest
   residual risk in the two claims and it cannot be closed by a review — it is closed by the code and by a
   test over the serializer.
3. **The four new columns were reviewed in the artifact, not in the store.** The live local store still holds
   the pre-T047 shape of `ops_startup_logs` (fifteen columns). The nineteen-column shape is what the artifact
   ships, and `Database/Validation/startup_schema/validate.sql` asserts it, but no run against a store has
   observed the four columns yet, and the validator's current report against this store is the ten known
   issues from the un-applied migration.
4. **No entry was inspected.** Nothing in the reviewed revision writes a log entry, so there is no live row to
   read. Claim one's dynamic half is untested by construction; the static half (evidence 1 to 4) is what this
   review establishes.

## Verdict

Claim one holds **on the shipped artifacts**: the log store has no credential column, has exactly one writer
whose parameters cannot carry a secret, is never updated, is reachable by no trigger, and is read by no
procedure that names a secret column. It is not yet enforced at runtime, because the runtime does not exist;
the checks above are the ones to re-run when T064 to T067 land, and evidence 5's scan is the query to re-run
against a populated store.

Claim two holds as a boundary **and needs one recorded carry-forward**: the key file's path may be written and
nothing else about the key may be, which puts `key_fingerprint` on the forbidden side of the line even though
it is legitimately stored. That constraint is the one item this review asks a later step to honour, and it is
stated here rather than left to the reader of two documents to reconcile.
