# Unified Item Picker — Item Workflow Mermaid Diagrams

Based on the unified Item-picker design and the configuration table, there are **19 in-scope Items**: 7 Pickup, 8 Deliver, 3 Assist, and 1 Other. The four Items without a current value source—Finished Goods, NCM, WIP, and Outside Service—are intentionally excluded until their own resolution work lands.

The common navigation is **Work Center → Category → Item → Details → Preview → Summary → Result**, with the Item list filtered to what the selected job actually supports.

**Every workflow decides whether an Item is offered before the user can select it.** The availability check sits
*above* the Item step, because it is the step that builds the Item list — a job that cannot support an Item never
shows that Item to click. Every diagram below is drawn in that order:
Work Center → Category → availability check → Item.

## 1. Pickup workflows

### Pickup — Coil or Flatstock

```mermaid
flowchart TD
    A["Start New Request"] --> B["Select Work Center"]
    B --> C["Select Category: Pickup"]
    C --> E{"Job has MMC/MMF subordinate?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Select Item: Coil or Flatstock"]
    D --> F["Read subordinate parts"]
    F --> G["Filter category: Coil or Flatstock"]
    G --> H["System identifies part number"]
    H --> I["Show Details"]
    I --> J["Preview request"]
    J --> K["Confirm"]
    K --> L["Create Pickup request"]
```

**Rule:** no user selection is required for the material itself; visibility depends on the requesting job having MMC/MMF subordinate material.

---

### Pickup — Die

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E{"Job has FGT die?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Die"]
    D --> F["Load dies from job"]
    F --> G["User selects Die"]
    G --> H["User selects destination"]
    H --> I{"Destination"}
    I -- "Home Location" --> J["Card identifier = Die location"]
    I -- "Die Shop" --> K["Card identifier = Die number"]
    I -- "Other" --> K
    J --> L["Preview"]
    K --> L
    L --> M["Confirm"]
    M --> N["Create Pickup request"]
```

The user selects both the die and destination; the displayed identifier changes when the destination is Home Location.

---

### Pickup — Component

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E{"Job has non-coil/flatstock components?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Component"]
    D --> F["Load job components"]
    F --> G["Exclude MMC/MMF"]
    G --> H["User selects component"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Pickup request"]
```

---

### Pickup — Riser Table

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E["Always available"]
    E --> D["Item: Riser Table"]
    D --> F["No user entry"]
    F --> G["Preview: Pickup / Riser Table"]
    G --> H["Confirm"]
    H --> I["Create Pickup request"]
```

Riser Table is equipment rather than a job part, so it is always available.

---

### Pickup — Dunnage

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E{"Job has saved dunnage assignment?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Dunnage"]
    D --> F["Read selected_dunnage_parts_json"]
    F --> G["Resolve dunnage part"]
    G --> H["Preview"]
    H --> I["Confirm"]
    I --> J["Create Pickup request"]
```

---

### Pickup — Scrap removal

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E{"Scrap decision recorded on the job?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> F{"Type is 'No Scrap'?"}
    F -- "Yes" --> X
    F -- "No" --> D["Item: Scrap removal"]
    D --> G["Read the job's selected scrap type"]
    G --> H["No user entry"]
    H --> I["Preview: Pickup / {ScrapType}"]
    I --> J["Confirm"]
    J --> K["Create Pickup request"]
```

**The scrap type is already set on the job** during Work Center Setup, so the user never types it.

A scrap decision counts as **recorded** only when the stored value is a real type: set, not `No Scrap`, and not the
`Scrap Type Required` placeholder. That placeholder is the workflow's fallback whenever nothing has been saved or a
suggestion finds no match, so a stored placeholder means **no decision was made** — not "no scrap". The codebase's
canonical test is `HasScrapDecision` in `SetupDunnageTypeViewModel`; it excludes the placeholder and treats
`No Scrap` as an explicit, valid decision.

`No Scrap` is therefore a real answer rather than an absence: a job that chose it has no scrap to collect, so the
Item is not offered. Otherwise Line 2 carries the job's scrap type.

The picker's options come from `s_defaultScrapTypes` in `SetupWorkflowService` — `3003 Aluminum`, `5052 aluminum`,
`Galvanized Steel`, `Steel`, `Skeleton`, `Gaylord`, plus `No Scrap`. Spell them exactly: `5052 aluminum` is
lowercase, and `Galvanized` is `Galvanized Steel`.

---

### Pickup — Hopper

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Pickup"]
    C --> E["Always available"]
    E --> D["Item: Pickup Hopper"]
    D --> F["No user entry"]
    F --> G["Preview: Pickup / Hopper"]
    G --> H["Confirm"]
    H --> I["Create Pickup request"]
```

The hopper is treated as a manual consumable/equipment item with no user entry.

---

## 2. Deliver workflows

### Deliver — Coil

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has coil?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Coil"]
    D --> F["Read coil from job"]
    F --> G["Destination = requesting Work Center"]
    G --> H["Preview"]
    H --> I["Confirm"]
    I --> J["Create Deliver request"]
```

Destination is always the requesting work center and is not user-entered.

---

### Deliver — Riser Table

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E["Always available"]
    E --> D["Item: Riser Table"]
    D --> F["Destination = requesting Work Center"]
    F --> G["No user entry"]
    G --> H["Preview"]
    H --> I["Confirm"]
    I --> J["Create Deliver request"]
```

---

### Deliver — Hopper

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E["Always available"]
    E --> D["Item: Hopper"]
    D --> F["Destination = requesting Work Center"]
    F --> G["No user entry"]
    G --> H["Preview"]
    H --> I["Confirm"]
    I --> J["Create Deliver request"]
```

---

### Deliver — Flatstock

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has Flatstock?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Flatstock"]
    D --> F["Read Flatstock part"]
    F --> G["Destination = requesting Work Center"]
    G --> H["Preview"]
    H --> I["Confirm"]
    I --> J["Create Deliver request"]
```

An important rule here is that **if no Flatstock is found, the Item is not shown at all**.

---

### Deliver — Die

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has die?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Die"]
    D --> F["Read die from job"]
    F --> G["Resolve Die Number + Location"]
    G --> H["Destination = requesting Work Center"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Deliver request"]
```

---

### Deliver — Dunnage

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has dunnage?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Dunnage"]
    D --> F["Read selected dunnage assignment"]
    F --> G["Resolve dunnage part"]
    G --> H["Destination = requesting Work Center"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Deliver request"]
```

---

### Deliver — Wrong Coil

**One Deliver request, not a compound operation.** The handler swaps the wrong coil for the correct one as part of
the deliver, so the request names itself as a wrong-material bring.

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has coil?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Wrong Coil"]
    D --> F["Identify the correct coil"]
    F --> G["User provides wrong-coil explanation"]
    G --> H["Validate explanation"]
    H --> I["Destination = requesting Work Center"]
    I --> J["Preview - Line 1: Wrong Coil Bring:"]
    J --> K["Line 2 = the correct coil number"]
    K --> L["Confirm"]
    L --> M["Create Deliver request"]
```

**Line 1 reads `Wrong Coil Bring:`** and **Line 2 carries the correct coil's number** — the coil being brought,
not the wrong one being taken away. The Category stays **Deliver**, and there is no second request.

---

### Deliver — Wrong Flatstock

**One Deliver request, not a compound operation** — the same shape as Wrong Coil.

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Deliver"]
    C --> E{"Job has Flatstock?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Wrong Flatstock"]
    D --> F["Identify the correct Flatstock"]
    F --> G["User provides wrong-flatstock explanation"]
    G --> H["Validate explanation"]
    H --> I["Destination = requesting Work Center"]
    I --> J["Preview - Line 1: Wrong Flatstock Bring:"]
    J --> K["Line 2 = the correct flatstock part number"]
    K --> L["Confirm"]
    L --> M["Create Deliver request"]
```

**Line 1 reads `Wrong Flatstock Bring:`** and **Line 2 carries the correct flatstock's part number**. The handler
swaps the wrong flatstock for the correct one as part of the deliver.

---

## 3. Assist workflows

### Assist — Coil / Turn Coil

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Assist"]
    C --> E{"Job has coil?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Coil"]
    D --> F["Identify job coil"]
    F --> G["Action = Turn coil"]
    G --> H["No additional user entry"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Assist request"]
```

Here the **action itself is the request**; there is no separate value-entry step.

---

### Assist — Place Parts on Table

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Assist"]
    C --> E{"Job has subordinate parts?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Place Parts on Table"]
    D --> F["Read parts on job"]
    F --> G["Action = Place parts"]
    G --> H["No additional user entry"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Assist request"]
```

---

### Assist — Remove Parts from Table

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Assist"]
    C --> E{"Job has subordinate parts?"}
    E -- "No" --> X["Item not offered"]
    E -- "Yes" --> D["Item: Remove Parts from Table"]
    D --> F["Read parts on job"]
    F --> G["Action = Remove parts"]
    G --> H["No additional user entry"]
    H --> I["Preview"]
    I --> J["Confirm"]
    J --> K["Create Assist request"]
```

The two table-assist Items are explicitly visibility-filtered by whether the job has subordinate parts.

---

## 4. Other workflow

### Other — Free-text request

```mermaid
flowchart TD
    A["Start"] --> B["Work Center"]
    B --> C["Category: Other"]
    C --> E["Always available"]
    E --> D["Item: Other"]
    D --> F["Show request description field"]
    F --> G["User describes request"]
    G --> H["Validate required text"]
    H --> I["Preview"]
    I --> J["Card Line 1 = Other"]
    J --> K["Card Line 2 = User Message"]
    K --> L["Confirm"]
    L --> M["Create Other request"]
```

Other is the catch-all free-text Item and replaces the former Pickup Other and Assist Forklift paths.

---

## 5. The common Item-picker shell

All 19 workflows should sit inside the same picker architecture rather than implementing separate navigation flows:

```mermaid
flowchart LR
    A["New Request"] --> B["Work Center"]
    B --> C["Category"]
    C --> E{"Is Item supported by job?"}

    E -- "No" --> F["Do not offer Item"]
    E -- "Yes" --> D["Offer Item in the list"]

    D --> G["Load Item configuration"]

    G --> H{"Needs user entry?"}
    H -- "No" --> I["Build request automatically"]
    H -- "Yes" --> J["Collect Item-specific details"]

    I --> K["Preview"]
    J --> K

    K --> L["Summary"]
    L --> M["Confirm"]
    M --> N["Stored request"]
```

The important architectural point is that **Item configuration determines the detail behavior**. The application should not have a separate hardcoded workflow for each legacy subtype.

---

## 6. Unified request lifecycle after creation

Once any of the 19 Items creates a request, the downstream lifecycle should be identical:

```mermaid
stateDiagram-v2
    [*] --> Waiting

    Waiting --> InProgress: Handler accepts
    Waiting --> Cancelled: Requester cancels

    InProgress --> Done: Assigned handler completes
    InProgress --> Waiting: Assigned handler releases

    Done --> [*]
    Cancelled --> [*]
```

This is important to the unified-card design: **the Item changes what the request means and what data it carries, but it does not create a different lifecycle model.** The existing handler workflow covers accepting, completing, releasing, requester cancellation, authorization, and persistence.

---

## 7. Unified card representation

Every successful path should converge on the same conceptual card:

```mermaid
flowchart TD
    A["Created Request"] --> B["Category / Umbrella Verb"]
    B --> C["Item-specific identifier"]
    C --> D["Common status / urgency / ownership"]
    D --> E["Common action area"]

    B --> F["Line 1"]
    C --> G["Line 2"]

    F --> H["Uniform 2-line card"]
    G --> H
```

The design specifically calls for **one uniform card across the in-scope Items**, with Line 1 representing the umbrella verb and Line 2 carrying the Item identifier; type-specific detail belongs on the detail page rather than creating another card layout.

### Deliberate exclusions

I would **not** create workflows for:

- `Pickup → Finished Goods`
- `Pickup → Non-Conforming (NCM)`
- `Pickup → WIP`
- `Pickup → Outside Service`

Those four are explicitly outside the current implementation because they do not yet have a persisted value source/resolution path. The current specification says they remain hidden until that work lands.

This gives you a clean implementation model:

**19 Item-specific detail flows → one shared preview/summary/create path → one shared request lifecycle → one shared card representation.**
