# Contract: user-visible strings and number formatting

Owners: UX-001 (two keys), UX-002 (one convention), UX-003 (weight formatting).
Carried by: W3, W27, W28.

Consumers are the resw files, every view definition, and every code-side lookup. The key names pinned below
are contract strings; they come from the spec's Verbatim Constraints and must not be recased, suffixed or
renamed.

## The one convention

A string is declared in a `.resw` file and reached one of two ways.

| Reached from | Declared as | Looked up as |
|---|---|---|
| Markup, through an `x:Uid` on a control | `<data name="Key.Text">` | the `x:Uid` value, resolved by the framework |
| Code | `<data name="Key">`, no suffix | `"Key".GetLocalized()` |

The two forms are not interchangeable. `ResourceExtensions.GetLocalized` does not append `.Text`, and it
treats a dot in a key as a subtree separator, so a code-side lookup of `Key` cannot reach an entry declared
as `Key.Text`, and the caller is handed the key back as the visible string. Every declared key therefore has
to match the way it is reached.

## UX-001, the two keys to add

Declared in `Strings/en-us/Resources.resw`, reached from code, so declared without a suffix:

- `Setup_Review_Scrap_None`
- `Setup_Review_Section_SubordinateParts_Count`

Wording matches the existing `.Text` twins, which stay: `Not recorded` and a format string taking one
argument. The two call sites are
`MTM_Waitlist.Setup/ViewModels/SetupReviewViewModel.cs:95` and line 109.

The contract a person may rely on: the setup review shows a sentence in both places. Evidence for this
contract is a run, not a build, because a missing key resolves silently to its own name.

Constraint on the values: neither may be blank or a placeholder. `RetiredSymbolAuditTests` asserts that no
placeholder resource value ships.

## UX-003, the weight

`AverageCoilWeightService` returns the number only, formatted with the current culture and the machine's
group and decimal separators. The unit is not in that string.

The unit is resolved from a resource where the text is composed. The key is chosen at W27; it is not pinned
by the spec, so it is recorded there rather than invented here.

The contract a person may rely on: a coil weight on a machine whose culture uses a decimal comma shows that
machine's separators, and the unit is a translatable word rather than a literal in code.

## UX-002, the extraction

Every user-visible `Text`, `Header`, `Content` and `PlaceholderText` attribute in a view definition resolves
through the convention above rather than carrying a literal. The extraction does not change layout, wording
or control choice, and it does not touch the two tooltip resource files, which already hold their strings.

Two things it must not break, both asserted by existing guardrails:

- A key that is composed at run time from a permission or role name is not found by a quoted-literal search.
  `PermissionGateSiteAuditTests` asserts that each access site asks for a key the declaration holds, so an
  extraction that renames a composed key trips it.
- The surface inventory lists every XAML file once. Extraction changes attributes, not files, so the
  inventory is untouched by W28 and must remain so.

## What the evidence looks like

- W3: the setup review opened twice, once for an item with no scrap and once for one with subordinate
  parts, each read as a sentence.
- W27: the application run on a machine whose culture differs from the developer's, with the weight read.
- W28: a search of every view definition for a literal prose attribute returns nothing, and the surface
  inventory is unchanged.
