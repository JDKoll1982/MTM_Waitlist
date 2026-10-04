# Contract: Account Creation Procedure

## 1. Name and artifacts

`sp_core_account_setup_create`, under
`Database/StoredProcedures/sp_core_account_setup_create/` with `create.sql` and `rollback.sql`.

## 2. Signature

| Parameter | Type | Meaning |
|---|---|---|
| `p_employee_identifier` | `VARCHAR(128)` | the employee number the plant system confirmed |
| `p_first_name` | `VARCHAR(128)` | the confirmed first name |
| `p_last_name` | `VARCHAR(128)` | the confirmed last name |
| `p_display_name` | `VARCHAR(128)` | the name shown for the account |
| `p_user_name` | `VARCHAR(128)` | the sign-in name the account is created with |
| `p_password_hash` | `VARBINARY(256)` | the chosen PIN's hash, produced by the application |
| `p_password_salt` | `VARBINARY(256)` | the salt for that hash |
| `p_is_temporary` | `TINYINT` | always 0 from this path |

## 3. Behaviour

- Inserts the profile row with the employee identifier, the name and the active flag.
- Inserts the credential with the supplied hash and salt and a non-temporary flag, so the account never enters
  the temporary-credential state.
- Assigns the lowest rank, read inside the procedure from the store's own rank list by that list's ordering.
  No rank name is passed in and none is named in application code.
- Is one transaction: a failure anywhere leaves no profile, no credential and no rank.
- Reports the created account's identifier through an output row so the caller can confirm.

## 4. Prohibited

- No rank name parameter. The caller must not be able to name a rank.
- No plaintext PIN. The application hashes before calling; the procedure never sees a PIN.
- No inline SQL anywhere in the calling code; the call goes through the existing non-query seam.
