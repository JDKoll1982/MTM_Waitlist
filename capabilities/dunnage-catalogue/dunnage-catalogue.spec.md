# Dunnage catalogue — Living Spec

## Requirements

### Both surfaces offer the same dunnage parts

#### Scenario: a part is added to the catalogue
- **WHEN** a dunnage part is added to the plant's catalogue
- **THEN** the generic request's dunnage page and the work-order substitute dialog both offer it, with the same picture

#### Scenario: a part is removed from the catalogue
- **WHEN** a dunnage part is removed from the plant's catalogue
- **THEN** neither surface offers it

### A catalogue that cannot be read is reported as unavailable, never as empty

#### Scenario: the read fails
- **WHEN** the catalogue read fails
- **THEN** the surface reports the catalogue as unavailable with the time of the attempt and a retry, and offers no shortened list

#### Scenario: the read is still in flight
- **WHEN** the catalogue is still loading
- **THEN** the surface shows progress and does not present the unfinished read as a catalogue holding nothing

### Choosing a part is what allows the dunnage step to move on

#### Scenario: nothing is chosen
- **WHEN** the requester tries to leave the dunnage step without choosing a part
- **THEN** the forward action is refused and no dunnage part is recorded

#### Scenario: a part is chosen and the step is left
- **WHEN** the requester chooses a part and moves on
- **THEN** that part and its type become the request's dunnage, and the step shows it as chosen when revisited

### One box narrows the list by part number, type or home location

#### Scenario: the requester types into the one filter
- **WHEN** the requester types text into the filter
- **THEN** the list narrows to parts whose part number, type or home location contains that text, ignoring case

### The surface remembers whether parts without pictures are shown

#### Scenario: the requester turns the pictures choice off
- **WHEN** the requester chooses to hide parts without pictures
- **THEN** those parts are hidden before the filter applies, and the same choice is in force for that person the next time the catalogue is opened

### A part that left the catalogue is still named on the raised request

#### Scenario: the chosen part is gone when the request is raised
- **WHEN** a request is raised for a part the catalogue no longer holds
- **THEN** the request keeps that part, records it as no longer offered, and the marker saying so comes from `WaitlistRequest_Dunnage_Gone.Text`
