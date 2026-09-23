# MTM Waitlist — Features in the Live Application

**Audience:** plant-floor operators, material handlers, setup teams, leads, and IT Department.
**Written in plain language — no code, no jargon.**
**Last verified against:** the working tree at commit `692091f` (2026-09-20). The previous pin was
`7bf6857` (2026-09-12); since then `specs/002-truthful-data-and-controls` and
`specs/003-waitlist-handler-fulfilment` shipped and `specs/004-unified-card-item-picker` went into
flight, so §14 and the defect table below were re-read against those specs on 2026-09-20 and the
claims they falsified were corrected. Where a row says **corrected 2026-09-20**, that re-read is why.

> **How this list was built (no assumptions).** Every entry was read out of the shipped application
> itself — each screen, the wording it shows, and the behaviour behind it. Nothing here is copied from
> a changelog, a specification, or a plan, and nothing is inferred from a feature name alone.
>
> Where a feature is **visible in the app but does not work**, it is still listed (because you will see
> it), marked with a ⚠️ and a pointer to its file in `defects/`. Those files hold the technical detail
> for a maintainer; this file stays in plain language.
>
> **What was covered.** Every screen area in the app (start-up, the shell, Waitlist list and detail,
> the whole New Request flow, the whole Work Center Setup flow, Settings, the cached-data indicator),
> the cache service's own windows and its tray menu, the wording files that supply the on-screen text,
> the data and service layer those screens call, the database artifacts the app depends on, and the
> test project. Areas with no user-facing surface (for example reporting, which is a single service
> with no screen) are not listed as features.

## 1. Starting the app and signing in

- **Launch screen.** A "Launching MTM Waitlist" screen appears while the app checks its own database.
- **Database-recovery screen.** If the database cannot be reached, the app says so plainly — "We could
  not connect to the database. Retry or close the application." — and offers **Retry**, **Restore
  Defaults** (to undo a bad saved connection setting) and **Close**.
- **Sign in.** Enter your user name and password and choose **Sign In**. Any problem is reported on the
  same screen.
- **Remember me on this device.** A checkbox that keeps you signed in on this computer.
- **Temporary password change.** If your account is still on a temporary password, you sign in with it as
  usual and the app then switches to an **Update Password** panel — new password and confirmation.
- **Create Account.** Offered to a new user with a **Create Account** button.
- **Computer registration gate.** The app recognises the machine you are on from its name and hardware
  address. A machine that is not registered is held at the registration step; a machine whose hardware
  address cannot be read is let through rather than blocked.

## 2. The app window and navigation

- **Two main destinations** — **Waitlist** and **Work Center Setup** — plus the built-in **Settings**
  destination.
- **Facility selector.** A drop-down in the header chooses the active facility; the waitlist reloads
  for your selection.
- **Your name and role.** A badge in the header shows who is signed in, with a colour and icon for the
  role.
- **Step progress strip.** The wizard screens show numbered steps across the top and tick each one as
  it is completed — for Setup: Work Center → Work Order → Part → Operation → Dunnage & Scrap → Review →
  Result; for a new request: Work Center → Category → Item → Details → Preview → Confirm → Complete.
- **Search box.** In the title bar. As you type, it suggests matching requests (up to eight) and
  opening a suggestion takes you straight to that request. It is a way to jump to a request, not a
  filter for the list.
- **Back button.** Standard back navigation.

## 3. Waitlist — viewing the list

- **A live list of the real requests** for the facility you have selected. It reloads when you open the
  screen, when you change facility, and when requests change.
- **A card per request, and it reads the same way for every item.** Each card has a coloured edge, a
  square picture of the item, the item's umbrella phrase as its first line, the item's identifier as its
  second line, four facts (Requested by, Press, Remaining time, Waiting), and the status. There is one card
  shape for every item — no card is laid out differently because of what was requested.
- **Remaining time, with overdue made obvious.** The time left before the request is due; anything
  overdue turns bold red instead of green.
- **How long it has been waiting**, and what it is waiting for.
- **A status badge** showing where the request has got to, in words rather than codes.
- **My Requests.** A switch in the header that narrows the list to the requests you submitted yourself.
  It appears only while you are on the Waitlist screen.
- **A friendly empty state.** Instead of a blank list you get "No Requests Right Now" with an
  illustration and a hint to add the first request.
- **A clear message if the database is unavailable.** The screen shows "Database unavailable" with a
  **Try again** button, and it clears itself as soon as the database answers again.
- **The green + button** in the bottom-right corner starts a new request.
- ⚠️ **The Cancel (✕) and Accept (✓) buttons are gone from the cards, because they did nothing.** A
  control that cannot act is no longer offered, and the card shows the request's real status instead.
  **Corrected 2026-09-20 — the working actions are now shipped:** `specs/003-waitlist-handler-fulfilment`
  put Accept / Complete / Release / Cancel back on the card, wired and gated on who may use them. See
  `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md`.

## 4. Waitlist — request detail

- **A summary card at the top** with the request title, who asked for it, the work center, and the
  remaining time — plus the request picture on the left and the work center's picture on the right.
- **Pictures you can enlarge.** Clicking either picture opens a full-size view; close it with the X or
  by clicking the dark background.
- **Request Details sections**, generated from what the item's configuration declares. Each item's page
  shows the labelled values its own configuration names, in the order it declares them, so the page follows
  the configuration rather than the app.
- **Inventory by Location** — a sortable list of Part #, Location and Quantity (lb). Click a heading to
  sort by it, click it again to reverse the order. The hidden-locations list is respected here.
- **The remaining time updates while you are on the page.** **Corrected 2026-09-20:** this page used to
  say "Does not update automatically". That note and its string are **gone from the app**, and the page now
  refreshes itself every 30 seconds (`WaitlistViewDetailViewModel.RefreshInterval`), rebuilding the field
  rows — including "Remaining time" — on each pass.
- **"Nothing to show"** with a short reason when there is no request to display.
- ⚠️ **This page no longer shows material values it cannot source.** The coil number, part numbers,
  quantities, customer, packlist, vendor, destination and die values that used to be fixed text built
  into the app are gone; a value with no read behind it is simply not shown, and a read that fails shows
  the failure in place with a retry. The real values are owned by **`specs/004-unified-card-item-picker`**
  (in flight) — see `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md`.

## 5. Waitlist — creating a request

- **Seven steps:** Work Center → Category → Item → Details → Preview → Confirm → Complete.
- **Choose a work center.** Search by work center, work order, sequence or part number; choose the
  building; pick from the presses saved to this computer ("Local Work Centers") or from the rest
  ("Other Workcenters"). Each tile shows the work center's current job, part number and when it was
  last updated.
- **Choose the category, then the item**, from the real catalog — the lists come from the database, not
  from a file shipped with the app. The item list offers only what the requesting job actually supports, and
  it is filtered before it is shown, so an unsupported item is never offered and then refused.
- **Add the details** — a short multi-line description of what you need.
- **Preview, then Confirm.** Preview reads everything back before you continue. Confirm shows the
  request summary and refuses a duplicate or a request whose current job has changed. The **Coil
  details** section (requested coil, quantity in house, coil description, average coil weight) was
  removed — nothing on that screen could source those values, so they are not shown. Restoring them is
  owned by **`specs/004-unified-card-item-picker`** (in flight).
- ⚠️ **The "Queue & wait time" section on Confirm is gone, because it was not real.** It always read
  the same figure no matter how busy the work center was, so the card was removed rather than shown. Any
  substantiated queue count or wait estimate is owned by the analytics workstream — seed
  `WeekendProject/SpecTemplates/04-waitlist-analytics.md`, not yet specified — see
  `defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md`.
- **Finish.** The last screen confirms the outcome and offers **Add Another Request** or **Return to
  Waitlist**; if something went wrong it offers **Retry** and **Close**.
- ⚠️ **New-request alerts cannot ring in this installation, and the switch now says so.** The setting is
  disabled and explains why, instead of accepting a preference the app cannot honour — see
  `defects/Closed-Partial-Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md`.

## 6. Work Center Setup

- **A five-step wizard** — Work Center → Work Order → Part → Operation → Dunnage & Scrap — followed by
  Review and Result, with the step strip across the top.
- **Choose the work center.** Search by work center, work order, sequence or part number, see each work
  center's current job, and (for Setup Leads and anyone the work-centre setup permission is given to)
  **Manage Work Centers** to add, rename or remove one.
- **Enter the work order.** You may type it in any of the usual forms — `76951`, `076951` or
  `WO-076951` — and the app normalizes it. If the format is wrong or no parts exist, it says so.
- **Choose the part**, then **choose the operation** from the ones available for that part.
- **Choose the dunnage.** Pick a dunnage type, then the specific option, with a picture of each option
  and a search box ("Search by part number, name, or details"). Options already assigned to the part
  and operation are listed separately, and a missing picture says "Image preview unavailable".
- **Adjust the dunnage list.** Add Selected to Pair, Remove, Remove All for Type, or Clear All for Pair
  (which asks for confirmation first).
- **Add a new dunnage type or part** inline when the list does not contain what you need — available to
  Admin, Developer, Plant Manager, Setup Lead and Production Lead.
- **Choose the scrap type**, or pick **No Scrap** explicitly.
- **Review.** Three tabs — Job Context, Subordinate Parts, and Dunnage & Scrap Assignments — with the
  work order, part, operation and the dunnage you selected.
- **Sensible confirmations.** "Replace Active Job?" when the work center already has one, "Continue
  without dunnage?" when none was chosen, and "Leave Work Center Setup?" if you try to leave with
  unsaved work.
- **Result.** Success ("Setup was saved successfully.") or Failure with the reason, and **Start Over**
  or **Return to Waitlist**. Setups are saved to the live database, never to sample data.

## 7. Infor Visual data and the outage fallback

- **Live data always wins.** Five things are read from Infor Visual: work-order lookup, operation
  sequences, subordinate parts, inventory locations, and request disposition.
- **If Infor Visual cannot be reached, the app keeps working.** Those five reads are answered from the
  app's own cached copy instead, and everything on screen looks and behaves the same. There is nothing
  to switch on. When Infor Visual comes back, reads return to live data by themselves.
- **An empty answer is an answer.** If Infor Visual is reachable but genuinely has no rows, you get the
  empty result — the app does not quietly substitute cached data.
- **A read-only bar explains what you are looking at.** While cached data is in use, a bar says Infor
  Visual is unreachable, that cached data is being shown, and how old that data is (minutes, hours or
  days) — or that only the initial baseline content is available because no refresh has succeeded yet.
  It has nothing to click, and it disappears on its own.
- **If one of the app's own databases is unavailable**, the screen you are on says "Database unavailable",
  names the database, tells you when it last tried and when it will try again, and offers **Try again**.
  Nothing is substituted for your data, and the message clears itself once the database answers.
- **WIP floor quantities** are read live from the WIP application so pickup decisions use the real floor
  picture; if that read is unavailable the app falls back to the Infor-only figures rather than guessing.
- ⚠️ **Average coil weight is not shown, and the fixed figure is gone.** It used to be fetched for one
  fixed part and fell back to a fixed "5,000 lb", so it never described the coil on the request; the
  lookup and the fallback were removed and no value is shown. Resolving it per coil is owned by
  **`specs/004-unified-card-item-picker`** (in flight) — see
  `defects/Closed-Partial-High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md`.

## 8. Settings

**Appearance and search**

- **App theme** — Light, Dark or Default, for the whole app.
- **Search box** — filters which settings panels are shown, each panel matching on its own keywords
  (for example "appearance", "theme", "version", "privacy"). Every search-aware panel re-evaluates when
  the search changes, including Image Location Settings, New Request Alerts and Max Allotted Time — see
  `defects/Closed-Low-Settings-SearchIgnoresThreeSettingsPanels.md`.

**Operations**

- **Image Location Settings** — manage the picture each item uses and the picture each work center uses,
  including the fallback chain (an item with no picture of its own shows its category's, inherited or
  custom), with Browse, Reset and "Reset all", a "Custom only" filter and a search over the rows. For
  authorized staff.
- **Local Work Centers** — pin the work centers this computer should show first, or unpin them again.
  For Admin and lead roles.
- **Dunnage Type Visibility** — choose which MTM Receiving dunnage types appear in this app, moving them
  between "Shown" and "Hidden".
- **Computers** — add, edit and delete the computers registered on the shop floor (display name,
  computer name, hardware address, description, active or not). For Admin and Developer.
- **Ignored Infor Visual Locations** — keep the list of locations left out of inventory lists and totals
  (for example WC, NCM, V-WC, NCM-VITS, SHIP). For roles above Material Handler.
- **New Request Alerts** — a personal switch (off by default) for the new-request alert. ⚠️ It is disabled
  in this installation, and says why, because the app as installed cannot deliver a notification — see
  `defects/Closed-Partial-Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md`.
- **Max Allotted Time** — how long each item should reasonably take, which drives the due time and the
  overdue countdown, shown beside the average its completed requests have actually taken. An item with no
  configured figure is measured by a 15-minute default, labelled as a default. Plant Manager and above can
  change the configured figure; everyone else can see both numbers.
- **Cached Infor Visual data** — ask the cache service on the server to rebuild the cached Infor Visual
  data right away instead of waiting for its next scheduled run, and see the result in plain language
  (including "this workstation is not pointed at a service"). For Plant Manager, Admin and Developer.
- **Part pictures** — give a Visual part or a WIP part its own picture. Type or paste a part number,
  choose the system it belongs to, and pick the picture; the screen shows the picture the rest of the
  application would draw for that part, who set or replaced it and when, and the list of parts that
  still have no picture, which can be exported to a file to work through away from the shop floor. For
  IT Department, Developer, Setup Lead and Plant Manager. A part with no picture is offered too — type
  its number in and give it one.

**Adding part pictures without the application (the file-drop convention).** Photographing hundreds of
parts is a file copy, not a dialog opened once per part. These are the rules the share follows, and a
file placed this way is used the next time that part is drawn.

- **Three collections, three folders.** Everything under the picture folder sits in one of three
  folders: `Waitlist` (the pictures the application keeps for its own screens — the request items, the
  work centres and the categories, each in its own folder inside it), `Visual` (the Infor Visual parts)
  and `WIP` (the WIP floor parts). A Visual part and a WIP part are two parts even when they share a
  number, so each keeps its picture in its own collection and neither one borrows the other's.
- **Family folders inside the part collections.** A part's number decides its folder: `MMC` for coil,
  `MMF` for flatstock, `FGT` for a die, and `Categorized Parts` for every number no prefix recognises,
  so an unrecognised part is still picturable rather than silently unpicturable.
- **The file is named after the part.** One picture per part per system, and its name is the part
  number — for example `Visual\MMC\MMC0001000.png`. Characters a file name cannot hold are replaced
  with an underscore when the application itself stores a picture; when setting one from a screen, a
  name that would land on a part that already owns it is refused and says which part that is.
- **`.png`, `.jpg` and `.jpeg` are drawn.** Anything else in those folders is ignored rather than
  reported as a problem, and so is a picture that is too small or not square: the part simply draws the
  application's one "no picture" image instead of a blank space.

**About**

- **App version** — the version you are running.
- **About MTM Waitlist** — a short description. The **Privacy Policy** link was removed: it pointed at
  an unfinished placeholder address, and an app that cannot open a real policy does not offer one — see
  `defects/Closed-Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md`.
- **Each heading in this area now shows its own text** — the section header, the version card and the
  about card each have their own label, instead of all three reading "About this application" — see
  `defects/Closed-Medium-Settings-AboutLabelsCollideOnOneResourceKey.md`.

## 9. Notifications

- **A new-request alert.** When a request is submitted, a notification titled "New Waitlist Request"
  naming the work center and the item raised for users who have switched alerts on (off by
  default). Tapping it is meant to open that request.
- ⚠️ **It cannot ring in this installation, and the surface says so instead of promising otherwise.** The
  alert needs the app installed in the way Windows notifications require, and this app is installed
  differently; making it arrive is owned by the notification-delivery workstream — seed
  `WeekendProject/SpecTemplates/08-notification-delivery-and-packaging.md`, not yet specified — see
  `defects/Closed-Partial-Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md`.
- ⚠️ **Tapping a notification no longer shows an internal "TODO" message.** The placeholder dialogs were
  deleted: a recognised tap opens the request, and an unrecognised one does nothing visible and is recorded
  for diagnosis. Proving it on a delivered notification is owned by the notification-delivery workstream —
  seed `WeekendProject/SpecTemplates/08-notification-delivery-and-packaging.md`, not yet specified — see
  `defects/Closed-Partial-Low-Notifications-ToastActivationShowsTodoDialog.md`.

## 10. The cache service (a separate small app that runs on the database server)

- **Runs quietly in the notification area** with **Settings**, **Status**, **Back up now** and **Quit**
  on its menu. Closing its window only hides it; the service keeps running.
- **Starts automatically when the server signs in**, and reports it if the saved setting and the
  Windows start-up entry disagree instead of ignoring it.
- **Status screen** — whether Infor Visual is reachable, the refresh interval, and for each of the five
  cached reads: the last outcome, when it last ran, how many rows it holds, how fresh it is, and any
  validation problem. Anything switched off on that machine explains itself ("Disabled here — reason").
- **Settings screen** — refresh interval, network interface and port, start-up, an explanation of how
  callers are identified, the Infor Visual server/database/login/timeout, and the database host and
  backup tool.
- **Keeps the cache fresh on a schedule**, per read, writing the new copy to one side and swapping it in
  so nobody ever sees a half-updated cache.
- **Backs up all four databases**, each with its own on/off, schedule, retention and destination. A
  database that cannot be reached switches off only its own backup, not the others.
- **Emergency restore** — restore a chosen database from a chosen backup, only on the server itself and
  only after a confirmation, with a picker for which backup to use.
- **A small network interface** for "refresh now" and "what is the status". Callers identify themselves
  by user name, and the role must be Admin, Developer, Plant Manager, Setup Lead or Production Lead —
  there is no shared password to hand out.
- **Its own daily log file**, which records every refusal (with the reason) and never a password.
- **A one-step installer** that puts it on the server, sets everything up, starts it and checks it — and
  refuses to run on a machine that is not the server, so it cannot be deployed onto the wrong host by
  mistake.

## 11. Tools for IT Department and developers

- **Control inspector** — shows the details behind a control on screen (its tooltip wording, related
  files, and the fallback text used when no wording is loaded). It has no menu entry, so it is not
  something an operator opens; it is used by the maintainer alongside the tooltip-inventory tooling.
- **Printable control report** — the inspector's findings can be sent to be printed.
- **Tooltips throughout** — controls across the app carry help text, and every one of them is kept in
  the wording files so it can be translated.

## 12. Installing and deploying

- **The desktop app is published as a self-contained folder** — it runs on a workstation without the
  .NET runtime being installed first.
- **It installs into your own user folder**, so no administrator rights are needed on the workstation,
  and it is copied to `%LOCALAPPDATA%\Programs\MTM_Waitlist`.
- **Database setup** — a one-step database installer builds or repairs the four databases and the cache
  schema and puts the initial content in place.
- **On a machine that cannot reach the plant**, the cache service reports scheduled refresh as disabled
  (with the reason) instead of failing, and backup/restore are switched off per database.

## 13. What the app talks to

- **Its own database** — requests, statuses, history, setups, computers, images and settings.
- **The WIP database** — the floor/WIP side.
- **The receiving database** — receiving history, dunnage types and parts, and setup assignments.
- **The cache database** — a mirror of the five Infor Visual reads, used only when Infor Visual cannot
  be reached; it is never the source of truth for the app's own data.
- **Infor Visual** — the external manufacturing system the app reads from. It is read-only, and it is
  the only source that may be unreachable, which is why it is the only one with a cached copy.
- **Every record the app keeps or changes is read and written live**, never replaced by sample content,
  and every database operation goes through a named, pre-approved routine rather than a statement typed
  into the app.

## 14. What is not finished yet

These gaps are already recorded by the project team. They are listed here so this file is not read as a
claim that everything works. **Re-read 2026-09-20** against the shipped specifications: three entries
that were true on 2026-09-12 are no longer true, and they are struck below rather than deleted so the
change is visible.

- ~~**Accept, Complete and Release — the handler's side of the job — are not available.**~~ **CORRECTED
  2026-09-20 — this is done.** `specs/003-waitlist-handler-fulfilment` shipped the handler's side: **Accept**
  (a handler or above, on an unclaimed request), **Complete** and **Release** (the recorded assignee only),
  and **Cancel** (a requester withdrawing their own waiting request). Each control is shown only when the
  viewer may use it. The request's **note** is editable and its **history** is readable on the detail page.
  See §4 for what the screen shows and `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` for
  the proof.
- **The material attributes on a request are not yet sourced.** Coil, part, quantity, customer, vendor and
  destination values have no read behind them yet, so the card and detail page show only what the request
  itself carries. The item-resolution workstream — now `specs/004-unified-card-item-picker`, in flight —
  restores the rest.
- **The hidden-locations list is not applied everywhere.** It is respected in the Setup location lookup
  and in the detail page's inventory list, but not yet across the Waitlist/Coil lists, the "Quantity in
  house" totals, and the Setup location lists.
- ~~**The handler list is not sorted most-urgent-first.**~~ **CORRECTED 2026-09-20 — this is done.**
  `specs/003` US3 shipped it: the list sorts most-urgent-first by default
  (`WaitlistViewViewModel.SortOrder` falls back to `WaitlistSortOrder.MostUrgent` and the ordering runs
  through `UrgencyCalculator.OrderBy`), with the due and overdue sums as before.
- **Analytics, the cancelled-request view for IT Department, and retention/archiving of old requests**
  are planned work that has not started. (The *requester's own* cancel shipped with `specs/003` — see the
  first entry above. A view for **IT Department** over all cancelled requests is still not built.)
  **User management and permissions shipped** — see section 15. Seeds: `WeekendProject/SpecTemplates/04-…`,
  `…/05-…`, `…/06-…`.
- ~~**The remaining time on the detail page does not tick**~~ — **CORRECTED 2026-09-20.** The detail page
  now refreshes itself every 30 seconds (`WaitlistViewDetailViewModel.RefreshInterval`) and each refresh
  rebuilds the field rows including "Remaining time", so the countdown does update while the page is open.
  The **list** has its own once-a-minute ticker aligned to the wall-clock minute.
- **The 30-day measurements** for refresh success and backup success were started on 2026-09-12 and are
  due on 2026-10-12; until then they are unproven.

### Defects found while building this inventory

Each of these has its own file in `defects/` with the evidence needed to fix it. `specs/002` closed all
ten; a ⚠️ marks the ones where a **follow-on** specification owns the remainder, and each row names the
checks that prove the closure. **Re-read 2026-09-20:** the follow-on specifications are now named
concretely — `specs/003-waitlist-handler-fulfilment` (shipped) and `specs/004-unified-card-item-picker`
(in flight) — instead of "the handler-fulfilment specification" and "the item-resolution workstream".
The two rows whose follow-on has no spec yet name their **seed template** in
`WeekendProject/SpecTemplates/`.

| Criticality | Defect | Affected feature | Status |
| --- | --- | --- | --- |
| Critical | `defects/Closed-Partial-Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` — card and detail fields show hard-coded coil/part/quantity/customer/vendor values | Waitlist cards, request detail | **Closed by `specs/002-truthful-data-and-controls`** — T005/T009/T010/T012 with the T015 verification: the literals are gone and no substitute renders. ⚠️ The real values are owned by **`specs/004-unified-card-item-picker`** (in flight). |
| High | `defects/Closed-High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` — the Cancel and Accept buttons on every card do nothing | Waitlist cards | **Closed by `specs/002`** — T007/T011: the buttons are gone and the real status is shown. ✅ **Completed by `specs/003-waitlist-handler-fulfilment`** (2026-09-13): Accept / Complete / Release / Cancel came back wired and gated, with the most-urgent-first ordering. |
| High | `defects/Closed-Partial-High-NewRequest-QueueAndWaitTimeIsHardcoded.md` — confirm screen always reads "0 active request(s)" / "~15 minutes" | New Request confirm step | **Closed by `specs/002`** — T016/T019: the fabricated card is deleted. ⚠️ Any substantiated figure is owned by the analytics workstream — seed `WeekendProject/SpecTemplates/04-waitlist-analytics.md`, not yet specified. |
| High | `defects/Closed-Partial-High-Coil-AverageWeightIsResolvedForOneHardCodedPart.md` — the average is always looked up for the sample coil part `MMC0001000` | Coil cards and detail | **Closed by `specs/002`** — T017/T020: the fixed-part lookup and its `5,000 lb` fallback are removed and nothing is shown. ⚠️ Per-coil resolution is owned by **`specs/004-unified-card-item-picker`** (in flight). |
| Medium | `defects/Closed-Partial-Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` — the alerts toggle cannot fire in the shipped unpackaged app | Settings, notifications | **Closed by `specs/002`** — T018/T021/T022: the switch is disabled with the reason and no preference is stored. ⚠️ Delivery is owned by the notification-delivery workstream — seed `WeekendProject/SpecTemplates/08-notification-delivery-and-packaging.md`, not yet specified. |
| Medium | `defects/Closed-Medium-Settings-AboutLabelsCollideOnOneResourceKey.md` — three labels share one resource key, so all three read "About this application" | Settings → About | **Closed by `specs/002`** — T024/T027: each element has its own key and its own resource entry. |
| Low | `defects/Closed-Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` — the Privacy Policy link points at `YourPrivacyUrlGoesHere` | Settings → About | **Closed by `specs/002`** — T028: the link and its resource entries are removed; T026's placeholder scan is the standing check. |
| Low | `defects/Closed-Low-Settings-SearchIgnoresThreeSettingsPanels.md` — three panels do not re-evaluate when the settings search changes | Settings search | **Closed by `specs/002`** — T025/T029/T030: the three panels are registered and the coverage check fails on the next unregistered one. |
| Low | `defects/Closed-Low-Localization-RetiredMockWordingStillShips.md` — "Setup saved using sample data" string and stale sample/mock comments | Setup result, code docs | **Closed by `specs/002`** — T037/T038 with T036's wording scan as the standing check: the orphan entry and the stale comments are gone. |
| Low | `defects/Closed-Partial-Low-Notifications-ToastActivationShowsTodoDialog.md` — tapping a toast while the app runs shows a "TODO" dialog | Notifications | **Closed by `specs/002`** — T032/T033/T034: the placeholder dialogs are deleted and one shared deep-link helper handles activation. ⚠️ Proving it on a delivered notification is owned by the notification-delivery workstream — seed `WeekendProject/SpecTemplates/08-notification-delivery-and-packaging.md`, not yet specified. |

---

## 15. User management and permissions

Everything here lives in **Settings → Administration**, and the area appears only for people who may use it.

- **Users** — the roster of everyone in the store, including the people who are switched off. Search it by
  sign-in name, name, employee number or role, and narrow it by role. The filter you leave is applied again when
  you come back, and the page says how many people it is hiding.
- **Open a person** — their details and their actions on one page. Save a correction, switch the account off or
  back on, and reset their password. An account you cannot change is shown read-only with the reason in words
  rather than hidden, and your own account cannot be switched off or renamed from here.
- **Add a person** — five fields and one role. The role list offers the roles at or below your own, and a new
  account is created active.
- **The one-time password** — a create or a reset issues a random four-digit password and shows it once, in a
  window that must be closed deliberately and can be printed. It stops working after five wrong tries, the count
  survives closing the application, and a fresh reset is the way back.
- **Permissions** — one card per area of the permission set, that area's permissions across the top under their
  plain-language labels with what each one gates, and the people down the side. A cell says whether the person
  holds that permission because it was chosen for them, because their role's baseline gives it, or not at all: the
  two "yes" states are drawn as different shapes so they can be told apart without colour, and each cell says its
  state in words for a screen reader. Saving is confirmed, names the person, and can be undone afterwards. The
  permission that opens the page is shown locked, and a person whose role is above yours has every cell of their
  row shown unavailable with the reason in words.
- **Who holds this** — on the same page: which roles' baselines give a chosen feature, and then only the people
  who differ from their own role.
- **One named permission per gated action.** A person starts from their role's baseline, and a change made for
  them takes effect for them and nobody else. Role baselines are shipped data, so what a role may do changes
  without a code change.

---

## For the maintainer

**The specifications behind this file** (read them in this order — each one is a superset of the last
state described here):

- `specs/001-module-mock-visual-fallback/` — the specification behind the Infor Visual outage fallback
  and the cache. **Shipped.**
- `specs/002-truthful-data-and-controls/` — the compliance-closure spec that removed every value the app
  could not substantiate and every control it could not honour. **Shipped** — this is the spec that closed
  all ten defects in the table above.
- `specs/003-waitlist-handler-fulfilment/` — the handler's side of the request: Accept / Complete /
  Release / Cancel, the most-urgent-first ordering, the note and the request history. **Shipped**
  (49/49, 2026-09-13).
- `specs/004-unified-card-item-picker/` — one card shape for every item, the Item catalog and the
  Category → Item picker. **In flight.**

**Other places to look:**

- `defects/` — one file per defect found while building this inventory. Each one records what a user
  sees, the exact evidence in the code, why it happens, how to fix it, and how to prove the fix. Each
  file's **Planned fix** row names specs by *template* number; the map to real spec numbers is in
  `WeekendProject/SpecTemplates/00-INDEX.md`.
- `WeekendProject/SpecTemplates/` — the seed briefs for the workstreams that have not been specified yet
  (analytics, request administration, user management, startup-gate polish, notification delivery).
- `WeekendProject/ChangeLog.Simple.md` — the plain-language change log for the Waitlist work.
- `WeekendProject/OPEN-TASKS.md` — the carry-forward task counts. `WeekendProject/OPEN-WORK-NEXT-SPEC.md`
  — the itemised detail behind them.
- `capabilities/DRIFT.md` — what the drift detector watches, what it found, and what it cannot see.

> **Corrected 2026-09-20.** This section previously pointed at **`RELEASE-NOTES.md`**, which **is not in
> the tree** — it was the release notes for the Infor Visual outage fallback, and it has been removed.
> Its five remaining references were provenance, not instructions. The specification it documented,
> `specs/001-module-mock-visual-fallback/`, is listed above and is the thing to read instead.
