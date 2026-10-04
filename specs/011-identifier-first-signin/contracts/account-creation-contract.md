# Contract: Account Creation Procedure

## 1. Name and artifacts

`sp_core_account_setup_create`, under
`Database/StoredProcedures/sp_core_account_setup_create/` with `create.sql` and `rollback.sql`.

## 2. Signature

The shipped `create.sql` is authoritative; this is its signature as the caller invokes it.

| Parameter | Type | Meaning |
|---|---|---|
| `p_username` | `VARCHAR(128)` | the sign-in name, stored upper case. The caller passes the employee number the plant system confirmed |
| `p_first_name` | `VARCHAR(128)` | the confirmed first name |
| `p_last_name` | `VARCHAR(128)` | the confirmed last name |
| `p_employee_identifier` | `VARCHAR(128)` | the employee number the plant system confirmed |
| `p_password_hash` | `VARCHAR(128)` | the chosen PIN's hash, produced by the application |
| `p_password_salt` | `VARBINARY(32)` | the salt for that hash |

There is no rank or temporary-credential parameter: the rank is read inside the procedure from
`auth_roles_catalog`, and the account is written with `require_password_change = 0` so it never enters the
temporary-credential state.

## 3. Behaviour

- Inserts the profile row with the employee identifier, the name and the active flag, carrying the credential
  columns (`password_hash`, `password_salt`, `require_password_change = 0`) on that same row, which is where
  every credential read in this schema looks.
- Assigns the lowest rank, read inside the procedure from the store's own rank list by that list's ordering.
  No rank name is passed in and none is named in application code.
- Is one transaction: a failure anywhere leaves no profile, no credential and no rank.
- Reports the created account's identifier through an output row so the caller can confirm.

## 4. Prohibited

- No rank name parameter. The caller must not be able to name a rank.
- No plaintext PIN. The application hashes before calling; the procedure never sees a PIN.
- No inline SQL anywhere in the calling code; the call goes through the existing non-query seam.
