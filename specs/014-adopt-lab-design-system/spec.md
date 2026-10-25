# Feature Specification: One set of rules for every screen, window and dialog

**Feature**: `014-adopt-lab-design-system`
**Created**: 2026-10-25
**Status**: Draft

## User Scenarios & Testing

### User Story 1 - A dialog opens where the person is looking (Priority: P1)

A material handler taps Edit on a lugger card. The editor opens as its own window, centred on the waitlist window and in front of it. They drag the waitlist window to their second monitor: the editor follows and stays centred. On a small screen the editor's body scrolls instead of the window growing past the edge.

**Why this priority**: it is the change every person meets, on every screen, whatever their role. Today twenty-two places raise a dialog and they behave four different ways.

**Independent Test**: open each dialog in turn, move and resize the main window while it is open, and confirm the dialog is still centred and still in front. Repeat at the smallest supported window size and confirm the body scrolls rather than the window growing.

**Acceptance Scenarios**:

1. **Given** the lugger editor is open, **When** the handler drags the main window to another monitor, **Then** the editor follows it and stays centred.
2. **Given** the main window is smaller than a dialog's preferred size, **When** the dialog opens, **Then** it opens no larger than the window and its body scrolls.
3. **Given** a dialog is open, **When** the person clicks the main window behind it, **Then** the dialog stays in front.
4. **Given** a dialog is open, **When** the person presses Escape, or the dialog's default action, **Then** it closes and returns exactly the answer it returned before this change.
5. **Given** a dialog acts on a named subject, **When** it opens, **Then** its title names that subject, such as the lugger or the work centre, not only the action.

---

### User Story 2 - Every question is asked the same way (Priority: P2)

Somebody tries to move away from work centre setup with changes in progress. They are asked a plain yes or no question in a small own window, with Yes as the default and No where Cancel always sits. Later they remove a workstation and are asked a confirm question, with Remove as the default and Cancel beside it. Both feel like the same application asking, because both come from one set of standard message dialogs.

**Why this priority**: these are the questions a person cannot avoid. A mixed set, where one screen puts the affirmative on the left and another on the right, is what makes people click the wrong button. It sits after US1 because a message dialog is a dialog, so it inherits the windowed behaviour.

**Independent Test**: trigger every kind of question the application can ask (acknowledge, confirm, decide, destructive confirm, leave without saving, retry after a failure) and confirm each opens in its own window, presents its actions in the standard order, and returns the answer its caller expects.

**Acceptance Scenarios**:

1. **Given** a person leaves setup with unsaved changes, **When** the question appears, **Then** it offers Yes and No, Yes is the default, and No sits where every cancel sits.
2. **Given** a person removes a workstation, **When** the confirmation appears, **Then** it offers Remove and Cancel, names the workstation, and Cancel is not the default.
3. **Given** an action fails and can be tried again, **When** the failure appears, **Then** it offers Try again and Cancel and states what failed.
4. **Given** a screen needs to tell the person something with no decision to make, **When** the message appears, **Then** it offers one action, OK, and nothing else.
5. **Given** any question, **When** the person presses Escape, **Then** the answer is the cancel answer for that kind of question.

---

### User Story 3 - Every screen looks like it belongs to the same application (Priority: P3)

A person moves between the waitlist, the setup steps and the settings pages. Spacing, text sizes, colours and page width are the same everywhere, and each screen keeps its content in one readable column however wide the monitor is.

**Why this priority**: consistency is what makes the application learnable, and it removes the defects that come from screens each inventing their own measurements, such as a waitlist card that stretches two thousand pixels wide with its buttons at the far edge.

**Independent Test**: open every screen at three window widths, including the smallest supported, and confirm the content sits in the same centred column, uses the same spacing scale, and needs no horizontal scrolling.

**Acceptance Scenarios**:

1. **Given** any screen, **When** it is opened on a wide monitor, **Then** its content stays in the shared capped column and does not stretch to the window.
2. **Given** any screen, **When** it is measured, **Then** its spacing uses the shared scale and its text uses the shared sizes, with no bespoke values.
3. **Given** the smallest supported window, **When** any screen is opened, **Then** no content is clipped and no screen requires horizontal scrolling.
4. **Given** a screen whose current arrangement cannot meet the rules, **When** it is brought into line, **Then** its layout is rearranged and every flow it supported still works, with the same wording and the same answers.

---

### User Story 4 - The application can be inventoried and reasoned about (Priority: P4)

A developer, or an auditor, asks what surfaces the application has. One document answers with every window, page, dialog and reusable control, including the ones in the service console, where each lives and whether it is modal. Every type sits in its own file, so the answer points at exactly one file per surface.

**Why this priority**: the other stories are only provable against a complete list. Without it "every screen" is a guess, and a screen added next month silently falls outside the rules.

**Independent Test**: compare the inventory against the files on disk. Every file appears once, every entry resolves to one file, and adding a screen without updating the inventory fails an automated check.

**Acceptance Scenarios**:

1. **Given** the inventory, **When** it is compared with the files on disk, **Then** the two lists match exactly, in both directions.
2. **Given** any entry in the inventory, **When** it is opened, **Then** the file declares exactly one type and no other surface is defined inside it.
3. **Given** a new screen is added, **When** the automated check runs, **Then** it fails until the inventory is updated.

---

### User Story 5 - Every control can be reached and named (Priority: P5)

A person who uses a keyboard only, a screen reader, or the plant's own inspection tooling can reach every control, tell what it is and operate it. No control is reachable only by mouse, and none is anonymous.

**Why this priority**: it is an accessibility obligation and it is also what makes the surfaces drivable by the plant's automation scripts. It comes after layout because a control's name is stable only once its screen is stable.

**Independent Test**: run the scripted control audit across every screen and confirm it reports no operable control without an accessible name and no duplicated identifier on a screen. Tab through each screen and confirm every control is reachable in a sensible order.

**Acceptance Scenarios**:

1. **Given** any screen, **When** the control audit runs, **Then** every operable control reports an accessible name and a stable identifier.
2. **Given** any screen, **When** the person tabs through it, **Then** every operable control is reachable and the focus order follows the reading order.
3. **Given** one screen, **When** its identifiers are collected, **Then** no two controls share one.

---

### User Story 6 - Nothing on screen uses an API the project forbids (Priority: P6)

The screens keep working across a Windows App SDK update, because no screen uses a call the project has ruled out, and where the rules name a replacement, that replacement is the one in use.

**Why this priority**: it prevents breakage rather than delivering new behaviour, so it ranks last. It is still checked on every screen, because one forbidden call in one file breaks that screen only.

**Independent Test**: run the rule check across every screen and its code, and confirm no forbidden call remains and every required replacement is present.

**Acceptance Scenarios**:

1. **Given** any screen and its code-behind, **When** the rule check runs, **Then** it finds no forbidden API.
2. **Given** a forbidden API the rules pair with a replacement, **When** the screen is inspected, **Then** the named replacement is in use.
3. **Given** a developer tool, the developer log panel or the service console, **When** the checks run, **Then** it is checked exactly like an operator screen, with no exclusion list anywhere.

---

### Edge Cases

- A question raised from inside another dialog, such as a failure reported by the picture picker while it is open.
- A dialog opened while the main window is minimised, or on a second monitor.
- The subject of a dialog is deleted or renamed elsewhere while the dialog is open.
- A window smaller than a dialog's declared minimum width.
- A display scaling change while a dialog is open.
- Surfaces that look modal but are not dialogs: the inline flyouts and the picture enlarge overlay. These are out of scope for the conversion.
- The one dialog that already opens as its own window. It is the reference for the others and must not be regressed.
- A developer or diagnostic surface. Three surfaces are not shop floor screens: the control inspector window, reached by holding Ctrl and Alt and right-clicking a control; the developer log panel, opened from Settings by someone holding the Diagnostics permission; and the service console, which the person who runs the refresh service sees. **These are in scope with no exemption**, and each is tested and automatable like any other screen.
- The control inspector opened from inside a dialog. The api rules require a diagnostic surface raised over a dialog to be a window rather than a page, so this one stays a window while still meeting every other rule.
- The service console is a separate executable. Its window, its status page and its settings page are in scope by decision, recorded in FR-023.
- A surface whose current layout cannot satisfy the rules without being rearranged. Its layout is redesigned, and its behaviour stays as it is.
- A resource-only file, such as a dictionary of styles or templates. There is no page to lay out, but every visual it declares is still checked: spacing, type, colours and tooltips.

## Requirements

### Functional Requirements

**Dialogs**

- **FR-001**: Every dialog MUST open as its own window owned by the main window, so that it is centred on that window and stays in front of it.
- **FR-002**: A dialog MUST stay centred on its owner while the owner is moved or resized.
- **FR-003**: A dialog MUST NOT open larger than its owner window. When its content does not fit, its body MUST scroll rather than the window growing.
- **FR-004**: A dialog MUST declare a minimum width and a preferred width, taken from the shared sizing values rather than chosen per dialog.
- **FR-005**: A dialog's title MUST name the subject it acts on, not only the action.
- **FR-006**: A dialog that only reports a message or asks a question MUST also open as its own window owned by the main window, and MUST be raised from the standard set in FR-008 rather than built where it is needed.
- **FR-007**: A converted dialog MUST keep the visual styling it had: its cards, its headings and its typography.

**Standard message dialogs**

- **FR-008**: One shared set of message dialogs MUST cover every kind of message the application asks: an acknowledgement with one action, a confirmation with an act and a cancel, a decision with yes and no, a destructive confirmation, a leave without saving, and a retry after a failure. Every message the application raises MUST come from that set.
- **FR-009**: For each kind, the actions MUST appear in the same order and position, the default action MUST be the same, and the cancel action MUST be in the same place, on every screen.
- **FR-010**: A message dialog's wording MUST name the thing it is about and MUST state the consequence of its default action, so the person can answer without reading the screen behind it.
- **FR-011**: A message dialog MUST return the same kind of answer its callers already expect, so no flow changes: a yes or no, a confirm or cancel, or the action the person chose.
- **FR-012**: A screen MUST NOT build a message dialog itself. One shared entry point MUST serve every caller, so the action order, the default action and the wording are fixed in one place.

**Layout**

- **FR-013**: Every screen MUST draw its content in one capped, centred column of the shared declared width, and MUST NOT stretch to the window.
- **FR-014**: Every screen MUST use the shared spacing scale. No bespoke margins or paddings.
- **FR-015**: Every screen MUST take its colours from the theme brushes. No literal colours.
- **FR-016**: Every screen MUST take its text sizes and weights from the shared type scale. No bespoke font sizes.
- **FR-017**: Every screen MUST adapt at the declared breakpoints, so that no screen is clipped and none needs horizontal scrolling at the smallest supported window.

**Automation**

- **FR-018**: Every control a person can operate MUST carry an accessible name and a stable automation identifier.
- **FR-019**: Automation identifiers MUST be unique within a screen.
- **FR-020**: Every screen MUST be operable by keyboard alone, with a focus order that follows reading order and a visible focus indicator.

**Rules for the platform**

- **FR-021**: No XAML file, and no code behind one, MUST use an API that the project's api-rules instruction file forbids.
- **FR-022**: Where that file forbids an API and names its replacement, the named replacement MUST be the one in use. Navigation in particular MUST stay on the existing shell and navigation services, with no second navigation stack.

**Inventory**

- **FR-023**: A single inventory MUST list every window, page, dialog and reusable control in the application, including the service console's window and its pages, with each one's kind, its area, and whether it is a modal surface.
- **FR-024**: Every surface in the inventory MUST live in its own file, and a file MUST declare exactly one type.
- **FR-025**: The inventory MUST be checked automatically, so that a surface added later cannot leave it stale.

**No exemptions**

- **FR-026**: Every XAML file in the repository MUST meet the rules, including developer tooling, diagnostic surfaces and the service console. There MUST be no exclusion list and no per-file exemption, so every surface is testable and automatable in the same way.
- **FR-027**: Where a surface cannot meet the rules without being rearranged, its layout MUST be redesigned to meet them. Its behaviour, its wording and the answers it returns MUST be preserved.
- **FR-028**: Every tooltip MUST be declared through the shared tooltip behaviour, never through the platform's own tooltip property, so the developer-facing tooltip map and the control inspector keep working.
- **FR-029**: Any converter or resource dictionary introduced during this work MUST be registered in the application's resource merge in the same change, because a missing registration is a build failure.
- **FR-030**: A dialog MUST await its own show call directly, without deferring its continuation, as the api rules require.
- **FR-031**: A diagnostic surface raised over a dialog MUST be a window rather than a page, so the dialog cannot block it.

### Key Entities

- **Screen surface**: one XAML file. Its kind (window, page, dialog, reusable control, resource dictionary), the area it belongs to, whether it is modal, its owner when it is a dialog, and its current standing against each of the three rule files.
- **Message kind**: one of the six standard questions. Its action labels, which action is the default, where the cancel action sits, and the shape of the answer it returns.
- **Rule file**: one of the three instruction documents a surface is checked against, each carrying the rules and the reasons for them.
- **Dialog conversion**: one dialog's move from the in-page pattern to its own window: the dialog, its declared minimum and preferred size, its owner, and the answer it returns.
- **Shared measurements**: the declared page column width, the spacing scale, the type scale and the theme brushes that every surface reads instead of inventing its own.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Zero dialog surfaces use the in-page pattern. Measured today: eight dialog files and fourteen screens that raise a dialog in code, of which one already opens as its own window.
- **SC-002**: Every dialog stays within 8 pixels of its owner's centre after the owner is resized, measured on the running application.
- **SC-003**: Every dialog opens fully visible at the smallest supported window size.
- **SC-004**: Every message the application raises comes from the standard set. Measured today: fourteen screens build their own dialog, and only two shared entry points exist.
- **SC-005**: For each message kind, the action order and the default action are identical on every screen.
- **SC-006**: 100% of screens draw inside the shared capped column.
- **SC-007**: The scripted control audit reports zero operable controls without an accessible name, on every screen.
- **SC-008**: Zero literal colours and zero bespoke font sizes remain in screen markup.
- **SC-009**: The inventory matches the files on disk exactly, in both directions. Measured today: 77 XAML files.
- **SC-010**: Zero files declare more than one type.
- **SC-011**: Zero exemptions. Every XAML file in the repository, including the control inspector, the developer log panel and the service console, is covered by every check, with no exclusion list to point at.
- **SC-012**: The existing tooltip coverage check passes with no exemptions, so no surface declares a tooltip outside the shared behaviour.
- **SC-013**: Every surface that was rearranged keeps its behaviour: the flows it supported before still work, with the same wording and the same answers.

## Assumptions

1. **The separation of embedded types is already satisfied.** Measured across all 77 XAML files: each declares exactly one type, no code-behind nests a class deriving from a window, page, dialog or control, and no template creates a window or dialog. The work is therefore to prove it, keep it and list it, not to split files.
2. **Flyouts and the picture enlarge overlay are not modal windows** and stay as they are. They are anchored surfaces, not dialogs.
3. **The capped column already declared is the single width.** Screens that still carry a literal width of their own adopt it.
4. **A dialog gets its own sizing contract** (a minimum, a preferred width and a maximum height) rather than the page column width, because a dialog is sized by its owner.
5. **The six message kinds are enough.** No dialog in the application uses three actions today, so the standard set needs at most two per question. An unforeseen kind is added to the set, never built beside it.
6. **Two partial shared prompts already exist** and are the seed of the standard set rather than an addition to it.
7. **The service console is in scope.** It is a different executable, and it has a window, two pages and a message dialog of its own.
8. **The three instruction files are the authority.** Where two of them disagree, the stricter rule wins and the disagreement is recorded.
9. **Nothing is exempt, and layout may be redesigned.** Every XAML file is treated as a production page: developed, tested and automated the same way. Where a surface cannot meet the rules without being rearranged, its layout is redesigned.
10. **Behaviour is preserved through that redesign.** Flow wording, actions, gestures and return values stay as they are. What changes is arrangement and presentation, not what a flow does.
11. **The api rules' dialogs line stays satisfied.** It asks for `ContentDialog` for confirmations and inline results. A windowed dialog still presents that same dialog content, so the standard message set is a `ContentDialog` family shown in its own window rather than a replacement for it.

## Verbatim Constraints

Exact values the request pinned. These must be used as written.

- `DevWinUI.WindowedContentDialog`
- `winui3-adoption.instructions`
- `winui3-api-rules.instructions`
- `winui3-ui-automation.instructions`
- `PageContentMaxWidth`
