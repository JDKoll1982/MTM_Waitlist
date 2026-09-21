# Feature Specification: User Management and Permissions

**Feature Branch**: `006-user-management-and-permissions`
**Created**: 2026-09-20
**Status**: Draft
**Input**: Spec seed `WeekendProject/SpecTemplates/06-user-management.md` section 2, verbatim, and its carried-forward
requirements in section 3 including the permissions section 3.8. The seed's brainstorm decisions 1 to 30 are settled
owner decisions and are treated as requirements, not as open questions. A decision marked SUPERSEDED or MERGED is
represented here by its replacement.

> Add user management to MTM Waitlist under a new Administration category in Settings, and give the
> application a single definition of role ranking. Today a user can only be created or changed by
> editing the database directly, and each screen that gates on a role carries its own hand-written list
> of allowed roles, so the application has several definitions of who is senior to whom. This feature
> adds a user list with search and filtering by role, a create flow and an edit flow as separate pages
> rather than dialogs, and per-user fields for username, first name, last name, employee identifier,
> role and active status, with password reset behind a confirmation that sets the temporary default
> password and forces a change at next sign-in. Editing is governed by rank: a person may not assign a
> role above their own rank, may not deactivate themselves, and may not escalate anyone past themselves,
> and that rule is enforced in the database transaction as well as on the screen, because the screen
> cannot be trusted. Creating a user writes the profile and the role assignment in one transaction, so a
> half-created user can never exist, and a duplicate user name returns a clear, typed result rather than
> retrying. Every create, edit and password reset writes an audit record naming the actor, the field
> that changed and the time. User names are normalised to upper case so that signing in matches either
> case, and the existing lower-case rows are migrated. Employee identifiers are deliberately not unique,
> because two people can share one, and the search matches user name, display name, employee identifier
> and role. Deactivating a user does not revoke their existing session tokens. The feature introduces a
> roles catalog that replaces the retired administrator role with an IT Department role, adds a shared
> rank helper with a comparison operation and a list of roles at or above a rank, and refactors the
> existing allowed-role lists onto it so there is exactly one definition of the hierarchy. The new
> screens follow the application's existing settings pattern, render role badges and active indicators
> without fixed widths, virtualise the list, guard the save action against a double click, never block
> the interface thread, and localise every string.
>
> The same feature also gives the application one named permission per gated action, in place of the twelve
> hand-written role lists it carries today. Each gated action is named by a settings key, and a person either
> holds that key or does not: if the signed-in user's key is false, they cannot perform the gated action. A new
> Administration page in Settings, openable only by IT Department, Plant Manager and Developer, shows **one
> person at a time**, a column of people, and the chosen person's permissions as a list of switches, and saving
> a change writes it to the settings store together with an audit record naming the acting user, the permission,
> whose set was changed, the previous value, the new value and the time. **Permissions belong to the person, and
> a role is the baseline its people start from**: a new account begins with its role's baseline, and the page
> adjusts the person rather than the role, so changing what someone may do never means changing the privilege
> level. Every gate the application has today becomes an entry in that set, accepting requests, ignored
> locations, hot work centres, part pictures, cache refresh, allotted minutes, Quick Add dunnage, work-centre
> setup, defect type management, and computer management. The badge beside the signed-in name is deliberately
> **not** one of them: it decides how a person looks, not what they may do. The shipped baseline for each role
> reproduces exactly what that role allows today, so nobody's access changes on the day it ships. A permission a
> person has no row for falls back to their role's baseline and then to the shipped default, and a settings store
> that cannot be reached yields that same fallback rather than a refusal, following the order the image-storage
> configuration already uses: take the stored override when there is one, otherwise the default. The set of
> permissions, and each role's baseline for them, is declared in one place so the page can be drawn from it and so
> a newly added gate cannot be introduced without a baseline. Because one person's set can differ from their
> role's, a second view answers *"who holds this?"* by naming the roles whose baseline gives it and only the
> people who differ from theirs. The permission that opens and edits this page is itself not editable from the
> page, so an administrator cannot remove their own way back in.

## Context in the application today

These facts are the counts every other artifact of this feature points at rather than restates, and they were
established by reading the shipping code on 2026-09-20 rather than from a summary.

> **Twelve hand-written role lists live in nine files across five projects; counting the developer check that
> decides from a display name, thirteen gate sites sit under audit in ten files. Eleven of the twelve lists decide
> access and become named permissions; the twelfth is the badge map, which is re-keyed to role codes and stays out
> of the permission set.**

- The twelve lists disagree with each other and with the catalogue. One entry names a role the catalogue does not
  hold, so it can never match a live person and the worker it appears to intend is refused by the very list that
  names them. Another names the retired administrator role. A missed entry fails silently: the role simply counts
  as an outsider and nothing reports it.
- **Every one of those sites compares role display names** rather than role codes, so renaming a role for display
  ripples through every gate in the application.

The role catalogue the application ships with holds eight roles, which this feature takes to nine by adding
Material Handler Lead; the retired administrator role is removed from within that nine and IT Department takes its
place, so the count stays nine.

The request quoted above says twelve lists are replaced by named permissions, and its own next sentence excludes
the badge from that set. The count in this section is the arithmetic that reconciles the two: eleven of the twelve
lists decide access and become permissions, and the badge map is the twelfth but is re-keyed rather than gated
(FR-058).

## Clarifications

### Session 2026-09-20

The seed settles decisions 1 to 30, so nothing was left for the owner to answer and no `[NEEDS CLARIFICATION]`
marker was carried. This pass resolved six points the specification had left silent or implied. Each answer below
is the seed's own, named beside it, with two deliberate exceptions: the entry recording the resolution of a
contradiction inside the seed, and the last, which is an informed default the seed does not cover.

- Q: Does the permissions page change a role's baseline, or only one person's own values? → A: Only a person's own
  values. A role's baseline ships as data and is never edited from the page (seed section 3.8 and decision 29).
- Q: What does the printed handover slip carry? → A: The PIN, whose account it is for, the sign-in name, when it
  was issued, who issued it, and the instruction to change it at first sign-in (seed decision 17).
- Q: What happens when an account is created but the PIN window does not appear? → A: The account still exists,
  and a password reset from that person's page is the way to a PIN (seed decision 27).
- Q: How long may a sign-in name, a first name and a last name be? → A: Each is between 1 and 128 characters, and
  the display name derived from the two names does not exceed 256 characters (seed section 1.6, the locked
  validation contract).
- Q: Does the one declaration carry each role's baseline, or are the baselines seeded data? → A: They are seeded
  data, and the declaration does not carry them. The seed states both readings in different paragraphs of its
  section 3.8 — its registry paragraph says the declaration carries "the baseline each role gives its people",
  while its database paragraph requires the baselines to be seeded "data, not literals in code". This is the
  deliberate resolution of that contradiction, taken in favour of the database paragraph because FR-048 and the
  seed's own "defaults are data" principle are the stronger rule, and because a baseline held in code makes
  "change what a role may do" a code change again. The declaration therefore carries the key, the label, what it
  gates, the area, the shipped fallback and the gate sites; the per-role baselines are seeded rows; and a check in
  both directions ties the two together (FR-046, FR-047, FR-048).
- Q: What happens when two people change the same account? → A: The later save wins, and each change is recorded
  separately against its own actor and time, so the earlier change stays visible in the record rather than being
  erased without a trace (default taken in this unattended run; the seed does not cover it).

## User Scenarios & Testing

### User Story 1 - Find a person and open their account (Priority: P1)

Today the roster can only be inspected by opening the database. This feature adds an Administration category in
Settings whose Users entry opens a list of people. The list shows everyone, marks the people who have been
switched off, searches across the four facts people actually know, and filters by role. A row's single job is to
open that person.

**Why this priority**: it is the doorway to every other part of the feature, and on its own it replaces a database
query with a screen.

**Independent Test**: open Settings, open Users, and confirm the roster appears with each person's role and status.
Search by part of a sign-in name, by a name, by an employee number and by a role, and confirm the matching people
appear. Choose a role filter, open a person, return, and confirm the filter is applied again and the page says so.

**Acceptance Scenarios**:

1. **Given** the signed-in person holds the account-management permission, **When** they open Settings, **Then** an
   Administration category is shown with a Users entry that opens a page rather than expanding in place.
2. **Given** the signed-in person holds neither administration permission, **When** they open Settings, **Then** the
   Administration category is absent entirely, not shown empty and not shown as a dead row.
3. **Given** the roster holds both active and switched-off people, **When** the list is shown, **Then** everyone is
   listed and the switched-off people are clearly marked.
4. **Given** the list is shown, **When** a search term matches part of a sign-in name, a first or last name, an
   employee number, or a role, **Then** the matching people are listed.
5. **Given** a role filter has been applied and a person has been opened and left, **When** the list returns,
   **Then** the saved filter is applied again, and the page names the filter and says how many people it is hiding.
6. **Given** a saved filter matches nobody, **When** the list is shown, **Then** the page says nobody matches the
   filter, and that message is distinguishable from there being no people at all.
7. **Given** a saved filter cannot be read, **When** the list is shown, **Then** everyone is shown and the page says
   the filter could not be read, rather than showing an empty list.
8. **Given** a row, **When** it is chosen, **Then** the whole row acts as one target and opens that person.
9. **Given** the window is at its narrowest, **When** a row is shown, **Then** all five facts (sign-in name, name,
   employee number, role and active state) remain visible, the row folds onto a second line if it needs to, and it
   remains a single target.
10. **Given** a saved filter naming a role the catalogue no longer holds, **When** the list is shown, **Then**
    everyone is shown and the page says the filter no longer applies, rather than showing an empty list.
11. **Given** the store cannot be read, **When** the list is shown, **Then** an unavailable state with a retry is
    shown, not an empty list and not a blank region.
12. **Given** a page is open and the reader's entitlement changes while it is, **When** the page is next used,
    **Then** it re-evaluates the entitlement rather than acting on the answer it was opened with.
13. **Given** either Administration page, **When** Back is pressed, **Then** Settings is shown, and opening the same
    page twice does not walk back through repeats of it.

### User Story 2 - Create a person, reset a password, and correct one (Priority: P1)

A lead hires someone and needs an account. They open Users, press Add, fill in the sign-in name, first name, last
name, employee number and role, and save. The account is created active and a window shows a one-time PIN to hand
over in person. Later the same lead resets someone's forgotten password, corrects a misspelled name or sign-in name,
or switches someone off and back on.

**Why this priority**: creating, resetting and correcting accounts is the request. Without it the feature is a roster
nobody can change.

**Independent Test**: create a person, confirm the PIN window appears and can be printed, sign in as that person with
the PIN and confirm the forced password change, reset that person's password and confirm a new PIN is issued, then
correct their name and sign-in name and confirm the change is recorded.

**Acceptance Scenarios**:

1. **Given** the create page, **When** a sign-in name, two names, a four-digit employee number and a role are entered
   and saved, **Then** the account exists, is active, and holds that role.
2. **Given** the save succeeded, **When** the page finishes, **Then** a window shows the one-time PIN, names the
   person it is for, and says it is shown only this once.
3. **Given** the PIN window is showing, **When** the reader clicks outside it or presses Escape, **Then** it stays
   open and only its own close button closes it.
4. **Given** the PIN window is showing, **When** printing is chosen and the print fails or is cancelled, **Then**
   the window stays open with the PIN still shown.
5. **Given** the PIN window has been closed, **When** the stored data and the logs are inspected, **Then** the PIN
   cannot be read back anywhere.
6. **Given** an existing person, **When** their first name, last name, employee number, role, sign-in name or active
   state is changed and saved, **Then** the change is stored and their account reflects it.
7. **Given** a sign-in name that another account already holds, **When** it is saved, **Then** the page says plainly
   that the name is taken, names it, keeps what was typed, and does not retry.
8. **Given** a sign-in name typed in lower case, **When** it is saved, **Then** it is stored so that signing in
   matches either case.
9. **Given** the signed-in person's own account, **When** it is opened, **Then** deactivating it and changing its
   sign-in name are shown as unavailable with the reason, while its other fields remain editable.
10. **Given** a person whose role ranks above the reader's, **When** their account is opened, **Then** it is
    readable, its fields and its actions are shown unavailable with the reason in words, and the top of the page
    says why.
11. **Given** unsaved edits and an action on the page, **When** the action is chosen, **Then** the page does not act
    on a half-edited person: it requires the reader to save or discard first.
12. **Given** the Save button, **When** it is pressed several times in quick succession, **Then** exactly one person
    is created, or exactly one change written.
13. **Given** a deactivation is confirmed, **When** the confirmation is shown, **Then** it says both that the person
    cannot sign in again and that a session already open stays open until they close the application.
14. **Given** the store cannot be reached while a save is attempted, **When** the save fails, **Then** the page keeps
    what the reader had, states what failed, and offers a retry rather than blanking the page.
15. **Given** an existing person, **When** a password reset is confirmed from their own page, **Then** a new random
    four-digit PIN is shown once in the same window and the person must change it at the next sign-in. No reset is
    offered anywhere on the sign-in screen, a person cannot reset their own password, and a switched-off person is
    not reactivated by the reset.
16. **Given** a person who is signed in, **When** an entitled person resets their password, **Then** that person's
    session continues until they close the application.
17. **Given** a reset has been performed, **When** a second reset is performed, **Then** a new PIN replaces the
    earlier one and the screen says the earlier one no longer works.
18. **Given** a person's page, **When** it is shown, **Then** the fields are in a form at the top and the actions are
    in a clearly separated area below, and the create page carries no actions area at all.
19. **Given** a switched-off account, **When** it is reactivated and the change confirmed, **Then** the confirmation
    says access returns from that person's next sign-in.
20. **Given** a first name and a last name, **When** they are saved, **Then** the display name is derived from them
    and does not exceed 256 characters.
21. **Given** the PIN window is showing, **When** printing is chosen, **Then** the printed handover slip carries the
    PIN, whose account it is for, the sign-in name, when it was issued and by whom, and the instruction to change it
    at first sign-in, and the slip is handed over rather than filed.
22. **Given** a sign-in name, a first name or a last name below or above the permitted length, **When** it is saved,
    **Then** the save is refused with the limit stated in the reader's words, and what the reader typed is kept.

### User Story 3 - One definition of who outranks whom (Priority: P1)

The application carries several answers to "who is senior to whom". One list names an administrator role that no
longer exists, another names a role the catalogue has never held, and the badge beside the signed-in name recognises
three of the eight real roles. This story gives the application one ladder, replaces the retired administrator role
with IT Department, and makes rank the thing that decides whose account a person may touch.

**Why this priority**: every other part of the feature reads this ladder, and the existing lists already disagree
with each other. Fixing them one at a time is how the disagreement survives.

**Independent Test**: compare the ladder's answers against each retired list for every role and confirm they agree.
Confirm the administrator role is gone, that IT Department holds the accounts that held it, and that a change to an
account that outranks the actor is refused when attempted directly, without the screen.

**Acceptance Scenarios**:

1. **Given** the role catalogue, **When** it is read, **Then** the retired administrator role is absent, IT
   Department is present, and Material Handler Lead is present.
2. **Given** the accounts that held the administrator role, **When** the change has run, **Then** every one of them
   holds IT Department, and no account at all is left without a role.
3. **Given** the change is interrupted partway, **When** it is attempted, **Then** nothing changes at all and no
   account is left without a role.
4. **Given** the paired reversal, **When** it is applied, **Then** the administrator role is restored and the
   accounts move back, with the same no-account-without-a-role comparison made afterwards.
5. **Given** two roles, **When** they are compared, **Then** the answer comes from the one ladder and nowhere else.
6. **Given** a person who holds more than one live role, **When** rank is consulted, **Then** the highest of their
   roles decides.
7. **Given** a person whose role ranks strictly above the reader's, **When** any change to that account is attempted
   directly, **Then** it is refused, whether it is the role, the active state or the password.
8. **Given** two people holding the same role, **When** one edits the other, **Then** it is allowed, because peers
   are editable.
9. **Given** the person's own account, **When** they change their own password, **Then** it is allowed, because their
   own account does not rank above them.
10. **Given** the create page's role picker, **When** a person with a given rank opens it, **Then** it offers every
    role at or below that rank and no role above it, and it states the bound in the reader's words rather than
    listing the withheld roles as dead entries.
11. **Given** the ladder, **When** any role's rank is read, **Then** it matches the declared value for that role,
    and a role added later takes a rung between two existing ones without the others being renumbered.

### User Story 4 - One named permission for every gated action (Priority: P1)

The application decides access from twelve separate hand-written lists, the count in the Context above. This story
replaces the eleven of them that decide access with one named permission per gated action, held by the person, with
the person's role supplying the baseline they start from. The twelfth is the badge map: it is part of the same
count and the same defect, but it decides how a person looks rather than what they may do, so it is re-keyed to
role codes and stays out of the permission set.

**Why this priority**: it is the change that makes a missed gate impossible to ship silently, and it is what turns
"change what a role may do" from a code change into a settings change.

**Independent Test**: for each of the eleven gates that becomes a permission, confirm the shipped baseline gives
that role exactly what its retired list gave it; confirm the twelfth list is re-keyed to role codes and is not
present in the permission set; confirm no part of the application decides access from its own list of role names;
and confirm that a person with no entry of their own receives their role's baseline.

**Acceptance Scenarios**:

1. **Given** the application, **When** it is searched for a hand-written list of role names used to decide access,
   **Then** none remains.
2. **Given** a person with no stored choice of their own, **When** a gated action is checked, **Then** the answer
   comes from their role's baseline.
3. **Given** a person whose stored choice differs from their role's baseline, **When** the gate is checked, **Then**
   the person's own choice wins, and no other person's answer changes.
4. **Given** a role whose baseline has no stored entry for a permission, **When** the gate is checked, **Then** the
   shipped fallback answers, never a refusal.
5. **Given** the settings store cannot be reached, **When** a gate is checked, **Then** the shipped fallback answers,
   so an unreachable store does not turn every control in the application into a refusal.
6. **Given** a permission declared in the one place, **When** it is read, **Then** it carries its key, its plain
   language label, what it gates, the area it belongs to, the shipped fallback and the gate sites that read it, and
   it does not carry the per-role baselines, which are seeded data that a check ties to it in both directions.
7. **Given** a new gated action added without declaring its permission and baseline, **When** the application is
   checked, **Then** that is reported as a failure rather than shipping as a silent refusal.
8. **Given** a declared permission that no gate reads, **When** the application is checked, **Then** that is
   reported, so the declaration cannot quietly grow entries nothing uses.
9. **Given** the Settings panel that pulls a cache refresh, **When** the person's entitlements are compared, **Then**
   whatever that panel admits is also admitted by the service's own cache-refresh permission, so the panel can never
   offer a control the service would refuse.
10. **Given** a role added to the catalogue after this feature ships, **When** its people are checked, **Then** it
    answers from its baseline or from the shipped fallback, and no gate fails to recognise it.
11. **Given** a new account, **When** it is first used, **Then** it holds exactly its role's baseline and nothing
    more.
12. **Given** a person a gate refuses, **When** the screen holding that gate is drawn, **Then** the control they
    would not be allowed to use is not shown to them, so no control is ever offered and then refused.

### User Story 5 - Change what one person may do (Priority: P2)

A Plant Manager needs one Material Handler to be able to change hot work centres, without promoting them. They open
the Permissions page, choose that person from the column of people, switch the feature on for them, and save. The page
says what the change means before writing it, and offers to undo it afterwards.

**Why this priority**: it is the reason permissions belong to a person rather than to a role. It is P2 because the
gates themselves (User Story 4) must work before there is anything to adjust.

**Independent Test**: open the Permissions page as a Plant Manager, change one permission for one person, confirm the
confirmation sentence names that person and that feature, confirm the change takes effect for that person and for
nobody else, then undo it.

**Acceptance Scenarios**:

1. **Given** the Permissions page, **When** it is shown, **Then** it presents a column of people and the chosen
   person's features as a list, one person at a time, and not a grid of roles against features. It is reached from
   its own Administration entry in Settings, which navigates rather than expanding in place.
2. **Given** a person is chosen, **When** their list is shown, **Then** each row says what the feature is, what it
   gates, whether the value comes from their role's baseline or from a choice made for them, and what the value is.
3. **Given** a change is made and saved, **When** the confirmation is shown, **Then** it states in the reader's words
   what will change and for whom, one sentence per changed row, with a count when several change.
4. **Given** nothing has been changed, **When** the page is shown, **Then** saving is unavailable and no history row
   is written for a press.
5. **Given** several rows are changed in one save, **When** it is undone, **Then** the whole save is reversed rather
   than half of it.
6. **Given** a change has been made and someone else has since changed the same row, **When** an undo is attempted,
   **Then** it says the value has moved, shows what it is now, and asks before restoring.
7. **Given** a person whose role ranks above the reader's, **When** their list is shown, **Then** they are selectable
   to view, their rows are shown unavailable with the reason in words, and nothing about them can be changed.
8. **Given** the permission that opens this page, **When** the page is shown, **Then** that row is present, locked,
   with the reason, and it cannot be cleared from the page by anyone.
9. **Given** a save is refused because the account outranks the reader, **When** the refusal is reported, **Then** it
   is stated plainly and no history row is written, because nothing changed.
10. **Given** the page is open with unsaved changes, **When** the reader tries to leave, **Then** the page says how
    many rows are pending and warns before leaving.
11. **Given** a permission change is written, **When** the history is read, **Then** there is exactly one record per
    changed permission naming the acting user, the permission, the scope it was written for, the previous value, the
    new value and the time.
12. **Given** a permission is changed for a person and saved, **When** the change is stored, **Then** it belongs to
    that person and every role's baseline is exactly as it shipped, because the page adjusts a person and never a
    privilege level.

### User Story 6 - Ask who holds a feature (Priority: P2)

A Plant Manager is asked who can change hot work centres. Because each person's set can differ from their role's,
the answer is no longer one screen's worth of reading. A second view on the same page answers it: choose a feature
and see the roles whose baseline gives it, plus only the people who differ.

**Why this priority**: it is the mitigation for the cost the per-person model creates, and without it the per-person
model is not reviewable.

**Independent Test**: choose a feature, confirm the roles whose baselines give it are listed from the seeded
baselines the declaration's check ties to it rather than from a second copy of the answers, confirm only the people
who differ from their role are named individually, and confirm that opening one of those people leads to their page.

**Acceptance Scenarios**:

1. **Given** the second view, **When** a feature is chosen, **Then** the roles whose baselines give it are listed,
   taken from the seeded baselines that the declaration's two-direction check ties to it rather than repeated.
2. **Given** a feature where some people differ from their role, **When** it is chosen, **Then** only those people
   are named, each marked as granted or denied, and each opens their own page.
3. **Given** a feature nobody holds, **When** it is chosen, **Then** the view says in words that nobody holds it,
   rather than showing an empty region.
4. **Given** a person who holds the feature but has been switched off, **When** the view is shown, **Then** they are
   listed and marked as switched off.
5. **Given** the second view, **When** anything in it is used, **Then** nothing is changed there: it is a view, and
   every change still happens on the person's own page.

### User Story 7 - A temporary credential stops after five wrong tries (Priority: P2)

A four-digit PIN handed over on paper has ten thousand possibilities and nothing stopping someone working through
them. After this story, five wrong tries with a temporary value stop that value being accepted, and a person entitled
to do so issues a fresh reset.

**Why this priority**: it is what gives the PIN any strength at all. It is P2 because the PIN arrives with User Story
2 and is weak until this ships, not unsafe-because-absent.

**Independent Test**: create a person, sign in wrongly five times with their PIN, confirm the sixth attempt is refused
even with the correct PIN, confirm that closing and reopening the application does not restore it, then confirm a
fresh reset clears it and issues a working PIN.

**Acceptance Scenarios**:

1. **Given** an account holding a temporary value, **When** five sign-in attempts fail, **Then** that temporary value
   stops being accepted.
2. **Given** the count has been reached, **When** the correct temporary value is entered, **Then** it is refused, and
   the person is told that someone entitled must issue a fresh reset.
3. **Given** the count has been reached, **When** the application is closed and reopened, **Then** the temporary value
   is still refused.
4. **Given** the count has been reached, **When** time passes, **Then** the temporary value is still refused, because
   the count does not expire with time.
5. **Given** a person signs in successfully with a temporary value, **When** they sign in, **Then** the count is
   cleared.
6. **Given** a fresh reset is issued, **When** it is issued, **Then** the count is cleared and the new value works.
7. **Given** the first failed attempt, **When** the failure is reported, **Then** the remaining attempts are shown in
   the reader's words, and they are not shown before any attempt has failed.
8. **Given** an ordinary account with a real password, **When** sign-in is attempted, **Then** no attempt limit
   applies and behaviour is exactly as it is today.
9. **Given** a sign-in name that does not exist, **When** sign-in is attempted repeatedly, **Then** the behaviour is
   exactly as it is today, so the limit cannot be used to discover which names exist.
10. **Given** an account that still holds the legacy temporary value, **When** sign-in is attempted, **Then** the limit
    applies to it too.

### User Story 8 - Every role gets its own badge (Priority: P3)

The badge beside the signed-in name currently recognises three of the eight roles, so five roles, the Plant Manager
among them, show the plain grey default. This story rewrites the lookup against the real roles so each has its own,
and deliberately does not make the badge something a manager can switch off.

**Why this priority**: it is presentation rather than access, so it is the last thing to fix, but it is the same
silent-omission defect the rest of the feature removes.

**Independent Test**: sign in as a holder of each role in the catalogue and confirm each shows its own badge, and that
none shows the grey default.

**Acceptance Scenarios**:

1. **Given** a person holding any role the catalogue holds, **When** the shell is shown, **Then** their badge is the
   one belonging to that role and not the grey default.
2. **Given** a role added to the catalogue later without its own badge, **When** the application is checked, **Then**
   that omission is reported rather than falling quietly to the grey default.
3. **Given** a genuinely unknown or blank role, **When** the badge is shown, **Then** the grey default is used, and no
   role the catalogue holds reaches it.
4. **Given** the badge, **When** the permission page is inspected, **Then** the badge does not appear there, because
   it decides how a person looks rather than what they may do.
5. **Given** the vocabulary that names roles the catalogue has never held, **When** the lookup is read, **Then** those
   branches are gone rather than left in place.

### User Story 9 - The role rename cannot strand an account (Priority: P1)

The administrator role is removed and IT Department takes its place, with the accounts that held it moved across.
This story is the part of that change that has to be proved rather than asserted: nothing is left without a role, and
the reversal actually runs.

**Why this priority**: the change is two steps around one moment, and an interruption between them leaves people with
no access while the application reports nothing wrong. It is P1 because it is the same silent-failure class the whole
feature exists to remove.

**Independent Test**: run the change against a live store and compare the accounts on IT Department with the accounts
that held the administrator role beforehand, and the count of accounts with no role against zero; then run the paired
reversal and repeat both comparisons, and finally re-apply the change so the store ends with IT Department in place
rather than with the retired role restored.

**Acceptance Scenarios**:

1. **Given** accounts holding the administrator role, **When** the change has run, **Then** the set of accounts on IT
   Department equals the set that held the administrator role.
2. **Given** the change has run, **When** the accounts are counted, **Then** no account has no role.
3. **Given** a failure forced partway through the change, **When** it is attempted, **Then** nothing changed at all.
4. **Given** a switched-off account that held the administrator role, **When** the change has run, **Then** it holds IT
   Department and remains switched off, because activation is a separate act.
5. **Given** the paired reversal, **When** it has run, **Then** the administrator role is back, the accounts are back
   on it, and no account has no role.
6. **Given** the permissions this feature seeds, **When** they are seeded, **Then** they are seeded after the rename
   and keyed to IT Department, so no baseline is written against a role that no longer exists.

## Edge Cases

- **An account ranking above the reader's.** It is shown and can be opened, so the reader can check what they are
  looking at, and nothing about it can be changed. The reason is stated in words.
- **The last holder of the top role.** By the rank rule nobody can act on it. The recovery route is the documented
  workstation-elevation path, not a screen, and the application states that rather than leaving it undefined.
- **Two people holding the same lead role.** They are peers and may edit each other.
- **A person holding more than one live role.** The highest rank decides both whether they may be edited and what they
  may edit.
- **A person who changes role while holding choices made for them.** Their own choices survive, because they belong to
  the person, and the page shows them so the fact is visible rather than discovered.
- **The person's own account.** It is editable, with two stated exceptions: it cannot be deactivated, and its own
  sign-in name cannot be changed by its holder.
- **Two people sharing one employee number.** Allowed, and the list shows both.
- **A role inserted into the ladder later.** The gaps in the numbering are deliberate: the new role takes its place
  without renumbering, and it is automatically above the roles it outranks and untouchable by them.
- **An account already holding the legacy temporary value.** It keeps working until its owner changes it, and the
  attempt limit applies to it like any other temporary value.
- **A second reset before the first is used.** The later one replaces the earlier one, and the person doing the reset
  is told the earlier one no longer works.
- **A PIN that is lost.** Resetting again issues a new one. The account exists but cannot be signed into until someone
  reset it, and the application says so before that happens.
- **A deactivated account.** It can be reset, and the reset does not reactivate it. It cannot sign in, so any choices
  still stored against it are harmless.
- **A deactivated account that still holds a session.** It stays signed in until it closes the application, because
  deactivation does not end a session. The confirmation and the person's page both say so.
- **A role filter saved against a role that later goes away.** The list falls back to showing everyone and says the
  filter no longer applies, rather than showing an empty list for ever.
- **A store that cannot be reached.** Gates answer from the shipped fallback; the roster shows an unavailable state
  with a retry; a save keeps the reader's work. Never sample data, never a blank screen, never a persistent banner.
- **A change made through something other than the screens.** The confirmation is a screen affordance for a person;
  the rank rule and the permission check still refuse it where the change is written.
- **An undo where the value has moved since.** The reader is told and asked, so the newer value is not overwritten
  silently.
- **An undo attempted by someone who could not have made the change.** It is refused, because the reversal obeys the
  same rule as the change.
- **One save touching several permissions.** One confirmation listing them, one change group, and an undo that
  reverses the whole save.
- **A permission nobody holds, and a person who holds a permission no gate reads.** The first is stated in words in
  the who-holds-this view; the second is reported by the check over the declaration.
- **A rename attempted on someone above the reader's rank, and on the reader's own account.** Both are refused, with
  the reason in words, exactly like every other field on that page.
- **A duplicate sign-in name on creation and on correction.** Both produce the same definite answer, naming the name,
  with what the reader typed kept.
- **A very long name or role, and the narrowest window.** Values wrap or fold and trim with a tooltip; nothing is
  cropped, and no control has a fixed width.
- **An account created but its PIN window never appears.** The account still exists and is not rolled back, because
  the window failing to draw is not the save failing. A reset from that person's page issues a PIN, which is the
  stated way out.
- **Two people changing the same account.** The later save wins. Each change is recorded separately against its own
  actor and time, so the earlier change stays visible in the record rather than being erased without a trace.

## Requirements

### Functional Requirements

#### Accounts and identity

- **FR-001**: An account MUST carry a sign-in name, a first name, a last name, a display name, an employee number, a
  role, and an active flag.
- **FR-002**: A sign-in name MUST be stored and matched in upper case, so that signing in succeeds whatever case the
  person types.
- **FR-003**: Accounts whose sign-in name is not upper case MUST be migrated to that same rule, so an older account
  behaves like a newer one.
- **FR-004**: An employee number MUST be exactly four digits and MUST NOT be unique, because two people can share one.
- **FR-005**: A sign-in name, a first name and a last name MUST each be between 1 and 128 characters, and the display
  name derived from the two names MUST NOT exceed 256 characters.
- **FR-006**: Creating a person MUST write the profile and the role assignment together, so a person with no role
  cannot come into existence.
- **FR-007**: A new account MUST be created active, and the create screen MUST NOT offer an active control.
- **FR-008**: A duplicate sign-in name MUST produce a definite "already taken" answer that names the name, keeps what
  the reader typed, and is never retried blindly.
- **FR-009**: A person MUST be created with exactly one role, and nothing in this feature MAY give them a second.
  The stored role assignments already permitted more than one before this feature and still do, so every reader
  MUST take the highest-ranked live role (FR-017).

#### The ladder

- **FR-010**: The application MUST define the rank of every role in one place, and nothing else MAY define it again.
- **FR-011**: The retired administrator role MUST be removed from the role catalogue and MUST be replaced by IT
  Department.
- **FR-012**: Every account that held the retired role MUST be moved onto IT Department in the same change that
  removes it, as one all-or-nothing act.
- **FR-013**: The change MUST ship with a paired reversal that restores the retired role and moves the accounts back,
  and that reversal MUST be run as part of verifying this feature.
- **FR-014**: After the change, no account MUST be without a role, and the accounts on IT Department MUST equal the
  accounts that held the retired role.
- **FR-015**: Material Handler Lead MUST be added to the catalogue as a role in its own right, MUST NOT be mapped onto
  Plant Manager, and MUST be given its own rank.
- **FR-016**: The rank comparison and the list of roles at or above a rank MUST come from that one definition, so both
  readers agree.
- **FR-017**: Where a person holds more than one live role, the highest rank MUST decide.
- **FR-018**: The ladder MUST leave room between its rungs, so a role added later can be placed without renumbering
  the others, and the rung of each role MUST be checkable.

#### The rank rule

- **FR-019**: A person MUST NOT change an account whose role ranks strictly above their own. This covers the role, the
  active state, the sign-in name and the password alike.
- **FR-020**: Peers MUST remain editable, because a person's own rank is not above their own rank.
- **FR-021**: A person's own account MUST NOT rank above them, so their own password change continues to work.
- **FR-022**: A person MUST NOT assign a role above their own rank, and the role picker MUST offer only roles at or
  below the rank of the person using it.
- **FR-023**: A person MUST NOT deactivate their own account.
- **FR-024**: A person MUST NOT change the sign-in name of the account they are signed in as.
- **FR-025**: The rank rule, the self-lockout rule and the sign-in name rule MUST all be enforced where the change is
  written, so a caller that never draws the screen is still refused.
- **FR-026**: A refused change MUST be reported plainly to the reader and MUST NOT write an audit record, because
  nothing changed.
- **FR-027**: An account the reader cannot change MUST still be readable, MUST show its fields and actions as
  unavailable rather than hidden, and MUST state the reason in words.

#### Password reset and temporary credentials

- **FR-028**: A password reset MUST be performed from a person's page, behind a confirmation. There MUST be no reset
  offered on the sign-in screen and no way for a person to reset their own password.
- **FR-029**: A reset MUST set a random four-digit PIN, MUST require a password change at the next sign-in, and MUST
  NOT reactivate a switched-off account.
- **FR-030**: The reset PIN MUST be shown once, to the person who performed the reset, and MUST NOT be readable
  back anywhere. Only a salted hash of it MAY be stored; the PIN itself MUST NOT be written to the store, to a log
  or to the audit record. "Read back" means recovered as the plaintext PIN, and because the four-digit space is
  small, a stored hash MUST NOT be treated as a place the PIN can be read from.
- **FR-031**: The value previously used for a reset MUST remain recognised as a temporary value, so accounts still
  holding it keep working until their owner changes it.
- **FR-032**: A second reset MUST replace the earlier PIN, and the screen MUST say so, so a superseded PIN is not
  handed over.
- **FR-033**: Creating a person MUST issue a PIN the same way a reset does, because a new person has no other way in.
- **FR-034**: The PIN MUST be shown in a window that must be closed deliberately, that MUST NOT be dismissed by
  clicking outside it or by Escape, that MUST offer printing, and whose printing MUST NOT close it.
- **FR-035**: A reset MUST NOT end an existing session for that person.

#### Wrong attempts on a temporary value

- **FR-036**: After five failed sign-in attempts, an account holding a temporary value MUST stop accepting that
  value.
- **FR-037**: The count MUST be held with the account, so closing and reopening the application does not clear it.
- **FR-038**: The count MUST NOT expire with time.
- **FR-039**: Only a successful sign-in or a fresh reset MUST clear the count.
- **FR-040**: The limit MUST apply only to accounts holding a temporary value. Ordinary sign-in MUST keep today's
  behaviour, including having no attempt limit at all.
- **FR-041**: Once at least one attempt has failed, the remaining attempts MUST be shown to the person in their own
  words, and MUST NOT be shown before any attempt has failed.
- **FR-042**: A failed sign-in for a sign-in name that does not exist MUST behave exactly as it does today, so the
  limit cannot be used to discover which names exist.
- **FR-043**: The wrong-attempt count MUST NOT be presented as a record of who tried.
- **FR-044**: When the count has been reached, the person MUST be told that someone entitled must issue a fresh
  reset, so the way out is known.

#### Permissions

- **FR-045**: Every gated action MUST be named by exactly one permission key, and a person either holds that key or
  does not.
- **FR-046**: The application MUST hold one declaration of every permission, carrying its key, a plain-language
  label, what it gates, the area it belongs to, the shipped fallback and the gate sites that read it. The
  declaration MUST NOT carry the per-role baselines: those are seeded data (FR-048), and this is the deliberate
  resolution of the seed's contradiction about where they live.
- **FR-047**: A permission MUST NOT be able to exist in the application without appearing on the permissions page,
  and MUST NOT appear on that page without a baseline for every role. Because the declaration and the baselines
  live in different places, the check that ties them MUST run in both directions: every declared key MUST have a
  baseline row for every role the catalogue holds, and every baseline row MUST name a declared key.
- **FR-048**: The shipped baselines MUST be data rather than values fixed in the code, so what a role may do can be
  changed without a code change. This is the rule that settles FR-046's question of where the baselines live.
- **FR-049**: A permission MUST resolve for the person: their own stored value if there is one, otherwise their
  role's baseline, otherwise the shipped fallback.
- **FR-050**: An unreachable store MUST yield the shipped fallback rather than a refusal, so a settings failure does
  not turn every control in the application into a refusal.
- **FR-051**: A new account MUST start from its role's baseline and MUST hold nothing beyond it.
- **FR-052**: The shipped baselines MUST reproduce exactly what each role is allowed today, so nobody's access
  changes on the day this ships.
- **FR-053**: A person's own stored value MUST beat a value that applies to a wider group, and the order MUST be
  corrected and shipped before any value is stored for an individual person, so nothing is resolved under the wrong
  order.
- **FR-054**: The application MUST NOT decide access from a hand-written list of role names anywhere. All twelve of
  the lists that exist today, the count in the Context above, MUST be removed as hand-written lists, including the
  two that were not on the earlier count: the one deciding who may manage defect types and the one deciding who may
  manage the computer registry. Eleven of them are replaced by their named permission; the twelfth, the badge map,
  is re-keyed to role codes and MUST NOT become a permission (FR-058).
- **FR-055**: A gate MUST be refused when the person does not hold its permission, and that refusal MUST hold
  wherever the gated action is performed, not only where the control was drawn.
- **FR-056**: A control's visibility and the action it guards MUST be answered from the same lookup, so a control is
  never shown to someone whose use of it would be refused.
- **FR-057**: Whatever the Settings cache-refresh control admits MUST also be admitted by the service's own
  cache-refresh permission, so the screen can never offer a control the service would reject.
- **FR-058**: The badge beside the signed-in name MUST NOT become a permission, because it decides how a person
  looks rather than what they may do.
- **FR-059**: The permission that opens and edits the permissions page MUST NOT be changeable from that page, so
  nobody can remove their own way back in.
- **FR-060**: A role added to the catalogue after this feature ships MUST be answered by its baseline or by the
  shipped fallback for every permission, so no gate fails to recognise it.

#### And the check over the declaration

- **FR-061**: A gated action that reads a permission the declaration does not hold MUST be reported as a failure
  rather than shipping as a silent refusal.
- **FR-062**: A permission the declaration holds that no gate reads MUST be reported, so the declaration cannot
  quietly grow entries nothing uses.

#### The permissions page

- **FR-063**: The permissions page MUST show one person at a time, presenting a column of people and the chosen
  person's features as a list, MUST NOT be a grid of roles against features, and MUST NOT change any role's
  baseline, because it adjusts a person rather than a privilege level.
- **FR-064**: The page MUST be openable only by IT Department, Plant Manager and Developer.
- **FR-065**: Each row MUST state what the feature is, what it gates, the value in force, and whether that value
  comes from the person's role's baseline or from a choice made for the person.
- **FR-066**: A person whose role ranks above the reader's MUST be selectable to view, with their rows shown
  unavailable and the reason stated in words.
- **FR-067**: Saving MUST confirm what is changing and for whom, in the reader's words, one sentence per changed row
  and a count when several change.
- **FR-068**: Saving with nothing changed MUST be unavailable, and MUST NOT write a history record.
- **FR-069**: A completed save MUST be reversible, and the reversal MUST obey the same rule as the change it
  reverses.
- **FR-070**: A reversal MUST NOT overwrite a value that has moved since, and MUST show what the value is now and ask
  before restoring.
- **FR-071**: A reversal of a save that changed several rows MUST reverse the whole save.
- **FR-072**: Every permission change MUST write one history record per changed permission, naming the acting user,
  the permission, the scope it was written for, the previous value, the new value and the time.
- **FR-073**: The page MUST warn before it is left with unsaved changes, and MUST state how many rows are pending.
- **FR-074**: The page MUST show pending changes as pending until they are saved.

#### Who holds this

- **FR-075**: A second view MUST answer which roles' baselines give a feature, taken from the seeded baselines that
  the declaration's two-direction check ties to it rather than repeated, and never from a second copy of the answers
  kept beside the view (FR-047, FR-048).
- **FR-076**: The same view MUST name only the people who differ from their role's baseline, in either direction,
  each marked as granted or denied.
- **FR-077**: The view MUST be read-only, and choosing a differing person MUST open that person's own page, so a
  permission still has exactly one place it is written.
- **FR-078**: A feature nobody holds MUST be stated in words rather than shown as an empty region.
- **FR-079**: A person who holds a feature but has been switched off MUST be listed and marked as switched off.

#### The Administration area

- **FR-080**: Settings MUST gain an Administration category holding two entries, one opening the user list and one
  opening the permissions page.
- **FR-081**: Each entry MUST be shown by its own permission, so a reader entitled to one sees that one, and a reader
  entitled to neither does not see the category at all.
- **FR-082**: Neither entry MUST expand in place, because both navigate, and each MUST carry a short description of
  what it opens.
- **FR-083**: Leaving either page MUST return to Settings, and repeating a page MUST NOT walk back through repeats of
  it.
- **FR-084**: Both entries MUST be reachable from the Settings search.
- **FR-085**: A page MUST re-check the reader's entitlement rather than staying open on a stale answer.

#### The user list

- **FR-086**: The list MUST search sign-in name, display name, employee number and role.
- **FR-087**: The list MUST be filterable by role.
- **FR-088**: The list MUST show everyone by default, with switched-off people clearly marked.
- **FR-089**: The list's search and role filter MUST be remembered for the person, and the page MUST name the active
  filter and say how many people it is hiding, with a way to show everyone.
- **FR-090**: A filter that matches nobody MUST be stated distinctly from there being no people at all.
- **FR-091**: A saved filter that cannot be read MUST cause everyone to be shown, with the failure stated, and MUST
  NOT cause an empty list.
- **FR-092**: A filter naming a role that no longer exists MUST fall back to showing everyone and say that the filter
  no longer applies.
- **FR-093**: A row MUST do exactly one thing, which is to open that person. Reset, deactivate and reactivate MUST
  live on that person's page.
- **FR-094**: A row MUST remain a single target, MUST keep all five facts visible at every width, and MUST fold onto
  a second line rather than cropping when there is no room.
- **FR-095**: The list MUST stay usable, and MUST NOT freeze while it loads, with a roster of 1,000 people.
- **FR-096**: Where the store cannot be read, the list MUST show an unavailable state with a retry, and MUST NOT show
  an empty list, a blank region or sample data.

#### The person's page

- **FR-097**: The page MUST present the fields in a form at the top and the actions in a clearly separated area
  below.
- **FR-098**: A page the reader cannot act on MUST say so at the top and MUST show its actions as unavailable rather
  than hiding them.
- **FR-099**: The create page MUST be a form only, with no actions area.
- **FR-100**: An action chosen while edits are unsaved MUST NOT act on a half-edited person: the reader MUST be
  required to save or discard first.
- **FR-101**: A failed action MUST keep what the reader had and state what failed, and MUST NOT blank the page.
- **FR-102**: Deactivating an account MUST state both halves in one sentence: the person cannot sign in again, and a
  session already open stays open until they close the application. The same sentence MUST appear on the person's
  page, so it is discoverable after the confirmation is gone.
- **FR-103**: Reactivating an account MUST state that access returns from the next sign-in.
- **FR-104**: Saving MUST be guarded against a repeated press, so one press creates exactly one person or writes
  exactly one change.

#### The badge

- **FR-105**: The badge lookup MUST be keyed on the role code and MUST cover every role the catalogue holds.
- **FR-106**: The fallback badge MUST remain, and MUST be reachable only by a genuinely unknown or blank role.
- **FR-107**: The vocabulary naming roles the catalogue has never held MUST be removed rather than left in place.

#### The audit of account changes

- **FR-108**: Every creation, change and password reset MUST write an audit record naming the actor, the field that
  changed, the previous and new values, and the time.
- **FR-109**: The actor's name, employee number and role MUST be recorded as they were at the time, so a later rename
  or role change cannot re-attribute what they did.
- **FR-110**: One save changing several fields MUST write one record per changed field, sharing one identifier for
  the change, so a reader sees both the individual changes and the single act that caused them.
- **FR-111**: A password reset MUST be recorded as having happened with no value on either side, because the
  credential is never stored.

#### The screens, as a class

- **FR-112**: Every label, title, field label, action and error message MUST be localised through the application's
  resource mechanism.
- **FR-113**: No control MUST have a fixed width, and a role name, badge or long value MUST wrap or trim with a
  tooltip rather than being cropped.
- **FR-114**: No screen MUST freeze or refuse interaction while it loads or saves.
- **FR-115**: Every list MUST remain usable with 1,000 rows.
- **FR-116**: The screens MUST remain usable between 150 percent and 200 percent scaling, and at the narrowest
  window the application permits.
- **FR-117**: No control MUST be shown to a person who cannot use it.

### Key Entities

- **Person**: one person's account. Carries a sign-in name, a first name, a last name, a display name, an employee
  number, a role, an active flag, and the state of any temporary credential (whether a change is required, and how
  many wrong attempts have been made).
- **Role**: a job title. Carries a code, a display name, and a rank. The code is the identity; the display name is
  presentation and may change.
- **The ladder**: the rank of every role, declared once, with deliberate gaps between rungs.
- **Permission**: one gated action. Carries a key, a plain-language label, a description of what it gates, the area
  it belongs to, the shipped fallback and the gate sites that read it. It does not carry the per-role baselines:
  those are seeded data that a check ties to it in both directions.
- **Person's permission value**: a choice made for one person, which beats that person's role baseline. Stored only
  when it differs, so a person with none inherits.
- **Role baseline**: what a role gives every one of its people, shipped as data.
- **Shipped fallback**: the answer used when neither a person's value nor a role baseline exists, and when the store
  cannot be reached.
- **Temporary credential**: a random four-digit PIN, shown once and never stored, that both opens an account for the
  first time and forces a real password to be set.
- **Account change audit record**: one record per changed field, carrying the field, the previous and new values,
  the actor as they were, the time, and a shared identifier for the change that produced it.
- **Permission change record**: one record per changed permission, carrying the acting user, the permission, the
  scope it was written for, both values and the time. It is also what a reversal reads.
- **Saved list preference**: one person's search text and role filter for the user list, held against the person
  rather than the workstation.

## Success Criteria

### Measurable Outcomes

- **SC-001**: For every one of the eleven keys that replaces a retired list, and every person in the store, the
  effective set of permissions on ship day equals exactly what that person's role was allowed before the change.
  Zero differences are tolerated, and the comparison is made per person rather than asserted. The comparison covers
  the keys that replace a retired list, because the other three keys have no predecessor: those three are checked
  against their stated baseline sets instead — the six roles the accounts and reset keys ship to, and the three the
  permissions key ships to.
- **SC-002**: Zero hand-written role lists remain in the application, and adding one back is reported as a failure.
- **SC-003**: Adding a new gated action without declaring its permission and every role's baseline is impossible:
  the omission is reported before it can ship as a silent refusal.
- **SC-004**: Zero stored values for individual people are required for SC-001 to hold, so ship day changes nobody's
  access while the store holds no per-person permission values at all.
- **SC-005**: After the role rename, the count of accounts with no role is zero, and the set of accounts on IT
  Department equals the set that held the retired role.
- **SC-006**: The paired reversal is executed as part of verifying this feature, and both comparisons in SC-005 hold
  again afterwards.
- **SC-007**: A password reset hands over a credential that cannot be read back as plaintext anywhere in the
  application, its logs or the store, verified by searching all three after a reset, with the store holding no more
  than a salted hash of it.
- **SC-008**: Five wrong tries with a temporary credential stops it working. A sixth attempt with the correct value
  is refused, closing and reopening the application does not restore it, and waiting does not restore it.
- **SC-009**: A save changing N permissions writes exactly N records, each naming the acting user and both values,
  and reversing it restores the previous state of all N.
- **SC-010**: One impatient press of Save creates exactly one person, and one impatient press of a permission save
  writes exactly one change.
- **SC-011**: Every screen in this feature is usable between 150 percent and 200 percent scaling and at the
  narrowest window the application permits, with all five facts of a list row visible at both widths.
- **SC-012**: No screen in this feature freezes or refuses interaction while it loads or saves, verified against a
  live store.
- **SC-013**: Changing a permission for one person takes effect for that person and changes no other person's
  effective set. A person with a value of their own and a person relying on their role baseline both behave as
  specified.
- **SC-014**: For any chosen feature, the who-holds-this view names every role whose baseline gives it and no role
  whose baseline does not, and names only those people who differ from their role's baseline.
- **SC-015**: Every role in the catalogue shows its own badge, and the count of roles falling to the grey default is
  zero.
- **SC-016**: A change to an account that outranks the actor is refused when attempted without the screen, for the
  role, the active state and the password alike.
- **SC-017**: Opening either Administration page and returning to Settings takes one Back press, and the search
  reaches both entries.
- **SC-018**: The full test suite passes with zero failures and the build is clean with zero warnings.
- **SC-019**: For every creation, change and password reset, the recorded audit names the actor as they were at the
  time, carries one record per changed field, and shares one change identifier across the records of a single save.
  A reset's record carries no value on either side.
- **SC-020**: Every string on this feature's screens renders from the shipped language resources, with no resource key
  ever shown to a person.

## Out of Scope

Deliberately not part of this feature, and not to be reintroduced:

- The retired demo-data toggle requirement from the source, and its parity test case.
- The request-type and subtype editor, and every other cancelled catalogue-administration screen.
- Analytics over people.
- A second role for one person, or a hierarchy deeper than the single ladder this feature defines.
- Any reworking of sign-in, session or token behaviour beyond the upper-case sign-in name, with one stated exception:
  the five-attempt limit for temporary credentials. Ordinary sign-in keeps today's behaviour, including having no
  attempt limit.
- Self-service password reset. A person cannot reset their own password, and the sign-in screen offers no reset.
- An audit screen. The account audit is written by the application and read by support.
- A record of who attempted a failed sign-in. The count is a guard, not a trail.
- Making the badge switchable. It is presentation, and it is fixed rather than gated.

## Assumptions

The seed settles decisions 1 to 30 and leaves nothing open. Where a decision delegated a choice, the choice is
recorded here rather than left to be invented later.

- **The count of wrong attempts lives with the account**, beside the flag that requires a password change, so the
  credential state and its guard sit together. It is read as part of the same answer that validates the credential,
  so an unreachable store fails sign-in as it does today rather than admitting a credential that has been stopped.
- **The account-management permission ships to Production Lead, Setup Lead, Material Handler Lead, Plant Manager, IT
  Department and Developer.** The password-reset permission ships to the same set. This is the owner's decision 3 and
  8 read together, and each lead role is named individually so a later rank change cannot quietly drop one.
- **The permissions page ships to IT Department, Plant Manager and Developer**, and that permission is fixed rather
  than editable, which is the accepted price of never being able to lock yourself out of the page.
- **Material Handler Lead's baseline is the most restrictive sensible one**: the worker level, plus handling requests
  and the service cache refresh, and nothing else. Nobody holds the role on ship day, so this changes nothing for
  anybody.
- **The dead entry naming a role the catalogue has never held is dropped from work-centre setup's baseline**, so a
  plain worker in that role is not given work-centre setup by default. The owner is expected to flip this on the
  permissions page if it was meant to be there. Recorded rather than decided silently.
- **Material Handler Lead is deliberately not granted the ignored-locations feature**, although a plain worker in a
  production role is. This follows the most-restrictive rule above and is the one place it looks odd, so it is
  recorded for the owner to flip on the page rather than decided here.
- **Overrides survive a change of role.** The choices were made for the person, so they stay with the person, and the
  permissions page shows them so the fact is visible rather than discovered.
- **The stored preference for the user list belongs to the person, not the workstation**, so two people sharing one
  application account share the filter.
- **The precedence correction ships in the same change as this feature and before any value is stored for an
  individual person**, so no value is ever resolved under the old order.
- **An action chosen while edits are unsaved requires an explicit choice.** The page offers to save and continue, or
  to discard, and never acts on the half-edited person by itself.
- **The in-page reversal covers the change just made.** The durable route is the recorded history, where a person
  entitled can reverse any recorded change later, and the reversal obeys the same rule as the change.
- **The recovery route for an account nobody outranks, including the last holder of the top role, is the documented
  workstation-elevation path**, not a screen. The application states this rather than leaving the situation
  undefined.
- **`Production Manager` is the Plant Manager** and gets no role of its own.
- **The living `startup` capability governs the sign-in surface.** The forced password change after a temporary
  credential, the hint explaining why, and the rule that an unanswerable optional lookup does not block startup all
  stay as that capability describes them. This feature adds only the attempt limit on temporary credentials.
- **The screens follow the existing Settings and workflow page patterns**, including the Settings search refresh,
  which is the defect class recorded in the seed's section 5.
- **No label may share an existing resource key with another label.**
- **Two people changing the same account at once: the later save wins.** The seed does not cover this case, so the
  default taken in this run is that a save is not blocked by another person's concurrent change. Each change is
  recorded against its own actor and time, so the earlier value is still readable in the record.

## Verbatim Constraints

These values are pinned by the request and MUST be used exactly as written, without paraphrasing, re-casing or
renaming.

### Permission keys

Each names one gated action:

```text
permission.requests.handle
permission.cache.refresh_api
permission.settings.ignored_locations
permission.settings.hot_work_centers
permission.settings.part_pictures
permission.settings.cache_refresh
permission.settings.urgency_minutes
permission.settings.defect_types
permission.settings.computers
permission.setup.dunnage_quick_add
permission.setup.work_centers
permission.admin.users
permission.admin.reset_password
permission.admin.permissions
```

### Role codes and the ladder

`developer` 100, `it_department` 90, `plant_manager` 80, `production_lead` 70, `setup_lead` 60,
`material_handler_lead` 50, and `material_handler`, `production` and `setup` all at 10.

The nine codes are:

```text
developer
it_department
plant_manager
production_lead
setup_lead
material_handler_lead
material_handler
production
setup
```

The retired code that MUST NOT remain in the catalogue is `admin`. The display name that MUST replace it is
`IT Department`. The new role's code is `material_handler_lead` and its display name is `Material Handler Lead`.

### Strings the request pins for removal

These name roles the catalogue holds no such code for, and MUST be removed from the application rather than carried
forward:

```text
administrator
supervisor
manager
quality
quality inspector
Setup Tech
```

### Values the request pins

- The temporary credential is exactly four digits.
- The legacy temporary value that MUST keep being recognised is `0000`.
- The number of failed attempts after which a temporary credential stops being accepted is five.
- A duplicate sign-in name MUST be answered as an already-exists result, and the duplicate-key answer the request
  names is `1062`.
