# Contract: Identifier-First Sign-In

## 1. Identifier classification

| Identifier | Kind |
|---|---|
| Exactly four digits (`4330`, `6229`) | Employee number |
| Anything else (`johnk`, `123`, `12345`, `12 3`) | User name |

The rule is the value's whole shape. No other inference is permitted.

## 2. Resolution order

1. The store, by `employee_identifier` for an employee number, by user name otherwise.
2. Only when the store holds nothing: the plant system read shape `employee_by_number`.

An inactive store account ends resolution immediately. The plant system is not consulted and no set-up is
offered.

## 3. Outcomes

| Outcome | Surface |
|---|---|
| `ResolvedInStore` | The credential (PIN) stage, then the existing launch unchanged |
| `KnownToPlantSystemOnly` | The set-up panel |
| `Unknown` | A plain refusal, form still open |
| `NotActive` | "That employee is not active", form still open |
| `Unknown`, plant system unreachable and mirror empty | "Set-up cannot be completed right now" |

## 4. Set-up panel contract

- Shows the plant system's `FirstName` and `LastName`, prefilled and editable.
- Asks for the PIN twice under the existing PIN rules: at least four characters, no upper bound, both typed the
  same way, both travelling as one argument and never as properties of the screen.
- States that the person is not yet set up for the waitlist, and that a higher permission level must be
  requested from IT.
- Creation bypasses the default PIN `4330`. The chosen PIN is the credential from creation, so the account
  never enters the temporary-credential state.
- The account's rank is the lowest rank in the store's rank list, read inside the creation procedure. No rank
  name appears in application code.
- Leaving the panel creates nothing.

## 5. Keyboard contract

- Identifier stage: Enter submits, exactly as the continue button.
- Credential stage: Enter submits, exactly as the submit button.
- Set-up panel: Enter moves to the next field, and submits only on the last field.
- A blank required field shows its hint rather than doing nothing.

## 6. Unchanged behaviour (must not be disturbed)

The attempt counting, the session check, the machine gate, the temporary-credential check and the new-PIN write
behave exactly as they do today for a person resolved in the store.
