# Wording: Generic Work Center — Stated Scrap Type and Lugger

**Feature**: `019-generic-wc-scrap-type`
**STATUS: default, awaiting review**

The spec's design note of 2026-11-03 requires a design clarification before the screen is built: what the step
shows, what it shows when its source is unavailable, and the exact wording. No approved rendering of the screen
exists any more, so this file records the wording the implementation adopts. Every later task reads this file and
none invents its own words.

This run is unattended, so the strings below are the default: recorded because the run cannot stop for a human, and
flagged so a reviewer can ratify or replace them. Changing one means changing the value in the `.resw` file named
beside it and the row here together.

## The words this feature fixes

| # | Where it shows | Key | English | File |
|---|---|---|---|---|
| 1 | the scrap step, under its heading | `GenericRequest_Scrap_Intro.Text` | `Pick the scrap type this request is carrying. The card will name the lugger it goes to.` | `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw` |
| 2 | *deleted* | `GenericRequest_Scrap_Luggers.Text` | *(the key is removed with the lugger column it labelled)* | `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw` |
| 3 | the card's `Lugger Location` cell, where a lugger name would be | `WaitlistRequest_Scrap_LuggerNotAssigned.Text` | `Not assigned` | `Strings/en-us/Resources.resw` |
| 4 | the card's `Lugger Location` cell, where a lugger name would be | `WaitlistRequest_Scrap_LuggerUnavailable.Text` | `Temporarily unavailable` | `Strings/en-us/Resources.resw` |

Row 1 replaces today's `Pick the scrap type the request needs. The lugger count is how many are free to use.`,
which promises a count the step no longer shows and never populated.

Rows 3 and 4 are short because they stand where a lugger name stands: a sentence there would read as an
explanation of a place the handler is expected to walk to, and the cell's own label already says what the value is.
Row 3 marks a configuration gap (FR-009); row 4 marks an arrangement that could not be read (FR-010). They are
different states and are never interchanged.

## The two states the step shows, and the words it already has

| State | What the step draws | Keys |
|---|---|---|
| `Choices` / `Chosen` | the composed list, one row per type, and Continue enabled once one is chosen | heading `GenericRequest_Scrap_Title.Text`, line `GenericRequest_Scrap_Intro.Text` |
| `NoList` | no list at all, the attempt time and a retry | the step frame's own keys, `GenericRequest_StepFrame_Unavailable_Title.Text`, `GenericRequest_StepFrame_Unavailable_Attempted.Text` and `GenericRequest_StepFrame_Retry.Content` |

No new key is written for `NoList`: the shared step frame already carries that state's heading, attempt time and
retry, and the intro line is the same sentence in both states.

## Keys deliberately left alone

- `GenericRequest_Scrap_Title.Text` stays `State the scrap type`. It already says what the step asks.
- `GenericRequest_Scrap_Empty.Text` stays `No scrap type is recorded.` It is unreachable: the shared list always
  ships with the plant's types (spec A5), so an empty read is an outage and draws `NoList` instead. It is recorded
  here so nobody deletes it as dead or rewrites it for the outage state.
- The confirmation's row label `GenericRequest_Summary_Scrap.Label` (`Scrap type`) and the card's declared cell
  labels `Scrap Type` and `Lugger Location` are existing surface wording and do not change.

## Where the card's two wordings come from

The board resolves rows 3 and 4 from these keys and supplies the resolved words as the lugger cell's value, which
is the same shape the cell already reads a lugger name from. Because the cell's value is then non-empty, the cell
also draws the shared placeholder picture behind the words. That is today's behaviour for a lugger with no
picture, and this feature pins it rather than changing it: the spec forbids changing how a work-order lugger with
no picture draws.

## Added 2026-11-06: the add control (FR-017 … FR-019)

The scrap step now offers an add control for a type the list does not carry. Its words are recorded here for the
same reason as the rest of this file, and carry the same `STATUS: default, awaiting review`.

All of them live in `MTM_Waitlist.GenericRequest/Strings/en-us/Resources.resw`.

| Key | English | Where |
|---|---|---|
| `GenericRequest_Scrap_AddButton.Content` | `Add scrap type…` | the control that opens the panel |
| `GenericRequest_Scrap_AddTitle.Text` | `Add a scrap type the list does not carry` | the panel's heading |
| `GenericRequest_Scrap_AddTypeBox.Header` | `Scrap type` | the typed value's label |
| `GenericRequest_Scrap_AddTypeBox.PlaceholderText` | `Enter scrap type` | the typed value's hint, copied from the work-order dialog |
| `GenericRequest_Scrap_AddLuggerBox.Header` | `Lugger it dumps into` | the lugger chooser's label |
| `GenericRequest_Scrap_AddSaveButton.Content` | `Add` | the affirmative button, copied from the work-order dialog |
| `GenericRequest_Scrap_AddCancelButton.Content` | `Cancel` | the dismissive button, copied from the work-order dialog |
| `GenericRequest_Scrap_Add_Blank.Text` | `Enter the scrap type before adding it.` | the panel, when nothing was typed |
| `GenericRequest_Scrap_Add_NoLugger.Text` | `Choose the lugger this scrap type dumps into.` | the panel, when no lugger was chosen |
| `GenericRequest_Scrap_Add_Failed.Text` | `The scrap type could not be added. Try again.` | the panel, when the write did not land |

The last three are read in code rather than through `x:Uid`, so their spelling matters: the view model names them by
key.
