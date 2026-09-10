# Release Notes

## Infor Visual Failover & Caching

### Automatic Failover

When Infor Visual cannot be reached — a network blip, a maintenance window, or a full outage — the application switches to the new Infor Visual cache database on its own. There is nothing for a user to turn on, no dialog to accept, and no error banner to dismiss.

#### What keeps working

The five Infor Visual reads the application depends on continue to return results (for now):

- Work-order lookup
- Operation sequences
- Subordinate parts
- Inventory locations
- Request disposition input

#### What the user experiences

When the cache is engaged, the user gets the same results from the cache database that they would get from Infor Visual. The screen looks and behaves normally; only the small status indicator (described below) hints that the data came from cache rather than directly from Infor Visual.

#### How the switch happens

A lightweight service runs on the server and probes the Infor Visual database every 30 seconds — just a ping. Two consecutive failed probes are required before it decides the system is unreachable, so a single dropped packet never triggers a switch. At that point it signals every running client to start reading from the cache.

While cached, probing continues on a backed-off schedule — 30 seconds, then 1 minute, then 5 minutes, repeating. The first successful probe tells the service that Infor Visual is back: it immediately signals the running clients to return to the live database, and if the cache has not been refreshed recently it refreshes it at that moment as well. Only one success is needed to switch back, so recovery does not have to wait out a timer.

---

## UI Indicator & Cache Logic

### InfoBar Indicator

While cached data is being served, a small notification bar appears inside the existing application shell. It tells the user two things: that Infor Visual is unreachable and cached data is in use, and how old that cached data is (the time of the last successful refresh, so the user can judge freshness).

#### It is purely informational

The bar has no buttons, no toggle, no link, and no command behind it. Clicking or tapping it does nothing. There is no way for a user to change a data mode, because there is no mode to change.

#### It is not dismissible

Because it reports a real condition rather than a one-off message, it cannot be closed while that condition is true. It currently sits in the lower-right corner of the waitlist screen.

#### It does not block work

It sits in the shell chrome and never covers or disables the content behind it, and it adapts to the window width so it stays readable on narrow layouts.

#### Cached data is never refused for being old

The indicator reports the age; it does not stop the application from using the cached copy at any age.

### Auto-Clear

As soon as Infor Visual becomes reachable again, the indicator disappears on its own — no refresh of the page, no restart of the application, and no user action. Users do not have to remember that a fallback was in effect or manually clear a warning.

### Live Data Priority

Live data always wins.

- Whenever Infor Visual is reachable, the application reads from it — even if a perfectly good cached copy exists.
- A reachable Infor Visual that legitimately returns **no rows** is a valid empty result, and it is **not** replaced by cached data. Fallback covers an unreachable Infor Visual only — never "the answer is empty".
- If Infor Visual is unreachable and the cached data store is also unavailable, the affected screen reports the store as unavailable rather than showing a substitute or a stale answer without explanation.

---

## Background Refresh Service — Lightweight Server Application

### Atomic Updates

The cache stays correct even while it is being updated.

- A refresh writes the new snapshot into a staging table, then swaps it into place in a single step.
- Because the swap is atomic, a reader running at the exact moment of a refresh sees either the complete previous snapshot or the complete new one — never a mixture, and never an empty table.
- If a refresh fails or is interrupted partway through, the last good snapshot stays intact and in use. A failed refresh never leaves the cache partially written, and is retried immediately rather than waiting for the next scheduled cycle.
- A single refresh cycle runs at a time; a second request while one is in progress is refused rather than queued on top of it.

### Decoupled Architecture

Refresh scheduling is owned entirely by the standalone background service, not by the client applications.

- The application never schedules or performs its own refresh.
- The application does not decide for itself that Infor Visual is down. It follows the signal broadcast by the service, so every running client switches at the same moment instead of each one probing on its own.
- The application remains fully functional when the service is not running — it simply serves whatever the cache already contains.
- There is no hard dependency in either direction: stopping or crashing the service does not break the application, and the application does not need to be open for the cache to stay fresh.

### Service Deployment

The service is a separate, unpackaged WinUI 3 application that runs on the database host itself.

- **Auto-start at logon.** It registers itself to start when the host logs in (a per-user `Run` key entry, deliberately not an MSIX-only startup task, because the service ships unpackaged). If the stored setting and the registry entry ever disagree at startup, the mismatch is reported rather than silently ignored.
- **Tray-only lifetime.** It has no main window. It sits quietly in the notification area and only opens a settings or status window when the operator asks for one; closing that window hides it rather than exiting.
- **Single instance.** Only one copy can run at a time. A second launch redirects to the running instance instead of starting a competing refresher.
- **Scheduled refresh engine.** Reads are refreshed on a configurable interval, with per-shape overrides, so a shape that is cheap to refresh can run more often than one that is expensive.
- **Graceful degradation.** If Infor Visual is unreachable during a scheduled cycle, that cycle is skipped and logged, the previous snapshot is left untouched, and the next cycle is attempted on schedule. An outage produces a log entry, not a failure alarm.
- **Run records.** Each shape's last outcome and timestamp are recorded on the host in a durable local store — deliberately not in MySQL. Error text is sanitized and never contains the shared credential.
- **Observability.** The service records and surfaces, per item, the outcome and timestamp of the last refresh and the last backup, so an operator can tell at a glance whether the cache is keeping up.
- **Network API, token-gated.** An authorized caller can request an immediate refresh and retrieve status over the network using a shared token. Requests without it are refused and logged, and the token is never displayed, logged, echoed in a response, or written into a status payload.
- **Backups and restore.** The service also produces per-database scheduled backups of the Waitlist Application, WIP Application, Receiving Application, and Infor Visual cache databases — each configurable independently for enablement, schedule, retention, and destination. Emergency restore is host-only and confirmation-gated: it is never exposed over the network, and an unconfirmed request changes nothing.

#### When the backup tooling is missing

If the required backup tool is not available on the host, that condition is reported clearly and no partial or zero-length artifact is ever recorded as a success.
