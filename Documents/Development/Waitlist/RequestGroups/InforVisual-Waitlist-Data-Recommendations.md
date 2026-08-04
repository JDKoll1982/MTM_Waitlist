# Waitlist Request Group Data Recommendations

This note is a practical recommendation for what the waitlist group cards and the waitlist details page should show next.

It is based on three sources:

- The current MTM Waitlist app shape, checked against the existing waitlist model and view models.
- The split Infor Visual schema files under `Documents/Development/InforVisual/DatabaseCSVFiles`.
- Microsoft list/details guidance, which recommends keeping the list card short and moving the heavier information to the details page.

## Plain-English Summary

The best version of the waitlist card is a short operational summary.

It should help someone answer these questions in a few seconds:

- What is this request?
- Who is it for?
- Where does it need to go?
- What press or resource is tied to it?
- How urgent is it?
- Is it blocked, in progress, or ready?

The details page should answer the follow-up questions:

- What order is this tied to?
- What part is involved?
- How much is needed, allocated, completed, or still open?
- What work order and operation is this tied to?
- What schedule date is at risk?
- What shipment, warehouse, or WIP location is involved?
- Who owns it internally and who is the customer contact?

## Recommended Card Fields

These are the best candidates for the main waitlist group item.

Use 6 to 8 of these, not all of them at once.

| What the user should see | Why it helps | Likely source |
| --- | --- | --- |
| Request type | Tells the user whether this is coil, FG pickup, NCM pickup, outside service, WIP pickup, or scrap. | Current app type + image category, potentially `CUST_ORDER_LINE.ORDER_TYPE`, `CUSTOMER_ORDER.ORDER_TYPE`, or local mapping logic |
| Order number | Gives the operator a strong business reference. | `CUSTOMER_ORDER.ID`, linked from `CUST_ORDER_LINE.CUST_ORDER_ID` |
| Customer name | Helps users recognize who the request belongs to. | `CUSTOMER.NAME`, linked from `CUSTOMER_ORDER.CUSTOMER_ID` |
| Part number and short description | Usually the fastest way to identify the physical item. | `CUST_ORDER_LINE.PART_ID`, `PART.DESCRIPTION` |
| Press or resource | Important for routing and prioritization on the floor. | `OPERATION.RESOURCE_ID`, `SHOP_RESOURCE.DESCRIPTION` |
| Remaining time | Good for urgency and queue decisions. | Derived from scheduled finish, promise date, desired ship date, or internal request timing logic |
| Quantity summary | Operators need to know how much is still open. | `CUST_ORDER_LINE.ORDER_QTY`, `ALLOCATED_QTY`, `FULFILLED_QTY`; sometimes `WORK_ORDER.DESIRED_QTY`, `ALLOCATED_QTY`, `FULFILLED_QTY` |
| Current status | Gives quick context without opening the record. | `CUST_ORDER_LINE.LINE_STATUS`, `CUSTOMER_ORDER.STATUS`, `WORK_ORDER.STATUS`, `OPERATION.STATUS` |

## Best First Card Layout

If the goal is clarity, the best first version of the card would be:

1. Request type
2. Order number
3. Customer name
4. Part number or part description
5. Press or resource
6. Remaining time
7. Quantity still open
8. Simple status label

## Recommended Details Page Fields

The details page can safely carry more context because the user is already focused on one request.

### Top priority details

These should be the first fields added.

| What the user should see | Why it helps | Likely source |
| --- | --- | --- |
| Order number | Core reference for the request. | `CUSTOMER_ORDER.ID` |
| Customer | Who the work is for. | `CUSTOMER.NAME` |
| Customer PO reference | Helps when the internal order number is not enough. | `CUSTOMER_ORDER.CUSTOMER_PO_REF` |
| Part number | Identifies the item. | `PART.ID`, `CUST_ORDER_LINE.PART_ID` |
| Part description | Helps non-planners recognize the item faster. | `PART.DESCRIPTION` |
| Ordered quantity | Total requested amount. | `CUST_ORDER_LINE.ORDER_QTY` |
| Allocated quantity | How much is already reserved. | `CUST_ORDER_LINE.ALLOCATED_QTY`, `WORK_ORDER.ALLOCATED_QTY` |
| Fulfilled or completed quantity | Shows progress. | `CUST_ORDER_LINE.FULFILLED_QTY`, `OPERATION.COMPLETED_QTY`, `WORK_ORDER.FULFILLED_QTY` |
| Remaining quantity | Best operational number for follow-up action. | Derived from ordered minus fulfilled or desired minus completed |
| Desired ship date | Strong external due signal. | `CUST_ORDER_LINE.DESIRED_SHIP_DATE`, `CUSTOMER_ORDER.DESIRED_SHIP_DATE` |
| Promise date | Better than desired date when customer commitments matter. | `CUST_ORDER_LINE.PROMISE_DATE`, `CUSTOMER_ORDER.PROMISE_DATE` |
| Work order | Connects the request to production. | `WORK_ORDER.TYPE`, `BASE_ID`, `LOT_ID`, `SPLIT_ID`, `SUB_ID` |
| Operation sequence | Shows the actual step in production. | `OPERATION.SEQUENCE_NO` |
| Press or resource | Tells the floor where the work belongs. | `OPERATION.RESOURCE_ID`, `SHOP_RESOURCE.DESCRIPTION` |
| Scheduled start and finish | Lets users understand timing and risk. | `WORK_ORDER.SCHED_START_DATE`, `WORK_ORDER.SCHED_FINISH_DATE`, `OPERATION.SCHED_START_DATE`, `OPERATION.SCHED_FINISH_DATE` |
| Internal owner or planner | Useful when the record needs attention. | `WORK_ORDER.PLANNER_ID`, `WORK_ORDER.ENGINEERED_BY`, `EMPLOYEE.USER_ID` |

### Good second-wave details

These are useful once the first round is working.

| What the user should see | Why it helps | Likely source |
| --- | --- | --- |
| Warehouse and ship-to | Helps physical routing. | `CUST_ORDER_LINE.WAREHOUSE_ID`, `CUSTOMER_ORDER.SHIPTO_ID`, `WORK_ORDER.WAREHOUSE_ID` |
| Shipment or packlist number | Valuable for finished goods pickup. | `SHIPPER.PACKLIST_ID` |
| Ship via and expected delivery | Useful for customer-facing pickup and loading work. | `SHIPPER.SHIP_VIA`, `SHIPPER.EXPECTED_DEL_DATE` |
| Operation run/setup hours | Helpful for press scheduling and urgency. | `OPERATION.SETUP_HRS`, `RUN_HRS`, `ACT_SETUP_HRS`, `ACT_RUN_HRS` |
| Material issued versus needed | Useful for WIP and shortage diagnosis. | `REQUIREMENT.CALC_QTY`, `ISSUED_QTY`, `ALLOCATED_QTY`, `FULFILLED_QTY` |
| Inventory available | Helps decide whether work can move now. | `PART.QTY_ON_HAND`, `QTY_AVAILABLE_ISS`, `QTY_ON_ORDER`, `QTY_IN_DEMAND` |
| WIP location | Useful if this becomes a physical chase tool. | `MT_WIP_INVENTORY.WAREHOUSE`, `LOCATION`, `QTY` |
| Cost rollups | More useful for leads and planners than floor users. | `WORKORDER_SUMMARY` and `WIP_BALANCE` cost fields |

## A Better "Requested By" Recommendation

The schema suggests there may be more than one meaning of "requested by".

That matters, because the best person to show depends on what the waitlist is meant to represent.

### Option 1: Customer-side requester

Show the person from the customer order.

Good choices:

- `CUSTOMER_ORDER.CONTACT_FIRST_NAME`
- `CUSTOMER_ORDER.CONTACT_LAST_NAME`
- `CUSTOMER.CONTACT_FIRST_NAME`
- `CUSTOMER.CONTACT_LAST_NAME`

Use this if the waitlist is mainly customer-order driven.

### Option 2: Internal owner

Show the planner, engineer, or user inside the plant.

Good choices:

- `WORK_ORDER.PLANNER_ID`
- `WORK_ORDER.ENGINEERED_BY`
- `SHIPPER.USER_ID`
- `EMPLOYEE.USER_ID` plus employee name lookup

Use this if the waitlist is mainly an internal work queue.

### Option 3: App-created requester

If the waitlist becomes a user-generated queue, the best requester field may not live in Infor Visual at all.

In that case, store the requesting app user separately and treat Infor data as the operational context behind the request.

## Type-Specific Suggestions

The same core card can work for all current types, but a few fields would be especially useful for each one.

### Coil

Best extras:

- Part number
- Part description
- Quantity open
- Inventory available

Likely sources:

- `PART.ID`
- `PART.DESCRIPTION`
- `PART.QTY_ON_HAND`
- `PART.QTY_AVAILABLE_ISS`

### Pickup FG

Best extras:

- Customer
- Packlist or shipment number
- Ship via
- Expected delivery date

Likely sources:

- `CUSTOMER.NAME`
- `SHIPPER.PACKLIST_ID`
- `SHIPPER.SHIP_VIA`
- `SHIPPER.EXPECTED_DEL_DATE`

### Pickup NCM

Best extras:

- Customer part number
- Warehouse
- Quantity open
- Promise date

Likely sources:

- `CUST_ORDER_LINE.CUSTOMER_PART_ID`
- `CUST_ORDER_LINE.WAREHOUSE_ID`
- `CUST_ORDER_LINE.ORDER_QTY`, `FULFILLED_QTY`
- `CUST_ORDER_LINE.PROMISE_DATE`

### Pickup OS

Best extras:

- Vendor or outside service
- Last dispatch date
- Last receive date
- Operation sequence

Likely sources:

- `OPERATION.VENDOR_ID`
- `OPERATION.SERVICE_ID`
- `OPERATION.LAST_DISP_DATE`
- `OPERATION.LAST_RECV_DATE`
- `OPERATION.SEQUENCE_NO`

### Pickup WIP

Best extras:

- Work order
- Operation sequence
- Press or resource
- Scheduled finish date
- WIP location

Likely sources:

- `WORK_ORDER.TYPE`, `BASE_ID`, `LOT_ID`, `SPLIT_ID`, `SUB_ID`
- `OPERATION.SEQUENCE_NO`
- `OPERATION.RESOURCE_ID`
- `OPERATION.SCHED_FINISH_DATE`
- `MT_WIP_INVENTORY.WAREHOUSE`, `LOCATION`

### Scrap

Best extras:

- Part number
- Part description
- Scrap-related operation
- Scrap or yield signal
- Quantity involved

Likely sources:

- `PART.ID`
- `PART.DESCRIPTION`
- `OPERATION.SCRAP_YIELD_PCT`
- `OPERATION.FIXED_SCRAP_UNITS`
- related work order and operation quantities

Note:

I did not find one clean, obvious, ready-made scrap reason field in the reviewed files for the waitlist use case. That probably means scrap reason may need either a different Infor table not yet reviewed in detail or a local app-side classification.

## Most Important Relationships

These are the most useful data paths for the waitlist.

- `CUST_ORDER_LINE.CUST_ORDER_ID` -> `CUSTOMER_ORDER.ID`
- `CUST_ORDER_LINE.PART_ID` -> `PART.ID`
- `CUSTOMER_ORDER.CUSTOMER_ID` -> `CUSTOMER.ID`
- `OPERATION.RESOURCE_ID` -> `SHOP_RESOURCE.ID`
- `OPERATION.WORKORDER_*` -> `WORK_ORDER.TYPE/BASE_ID/LOT_ID/SPLIT_ID/SUB_ID`

In plain language:

- The customer order line tells you what was ordered.
- The customer order tells you who it is for and when it is needed.
- The part tells you what the item actually is.
- The work order and operation tell you where it is in production.
- The shop resource tells you which press, machine, or work center is responsible.

## Which Tables Look Most Operationally Important

These tables stand out because of both row counts and indexing:

- `OPERATION` has a large row count and strong links to work orders and resources.
- `CUST_ORDER_LINE` has a large row count and is indexed by part, status, and desired ship date.
- `WORK_ORDER` is heavily indexed by its composite work order key and also by part and status.
- `CUSTOMER_ORDER` is large enough to matter and carries the customer and promise-date context.

That is a strong sign that the real operational waitlist should probably be built from some combination of:

1. customer order line
2. work order
3. operation
4. part
5. customer
6. shop resource

## Recommended First Real Version

If the goal is to keep the first production version understandable and useful, this is the best first cut.

### Group card

- Request type
- Order number
- Customer name
- Part number and short description
- Press or resource
- Remaining time
- Open quantity
- Status

### Details page

- Everything on the card
- Customer PO reference
- Desired ship date and promise date
- Work order
- Operation sequence
- Scheduled start and finish
- Warehouse or WIP location
- Planner or internal owner
- Shipment or packlist info when relevant

## Practical Decision Still Needed

Before implementation, one business choice should be made:

Should the waitlist be centered around:

- customer demand,
- production activity,
- pickup and movement work,
- or user-created internal requests?

That answer will decide what "requested by" really means and which table should act as the main source.

If the answer is not settled yet, the safest approach is:

- use customer order + line + part for the request identity,
- use work order + operation + resource for plant execution,
- and store true app-side requester information locally if the request is initiated inside MTM Waitlist.

## Files Reviewed Most Directly

- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/CUST_ORDER_LINE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/CUSTOMER_ORDER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/CUSTOMER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/WORK_ORDER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/WORKORDER_SUMMARY.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/OPERATION.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/SHOP_RESOURCE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/PART.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/SHIPPER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/WIP_BALANCE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/MT_WIP_INVENTORY.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/WIP_ISSUE_DETAIL.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ForeignKeys/CUST_ORDER_LINE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/ForeignKeys/OPERATION.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/Indexes/CUST_ORDER_LINE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/Indexes/WORK_ORDER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/TableRowCounts/CUSTOMER_ORDER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/TableRowCounts/CUST_ORDER_LINE.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/TableRowCounts/WORK_ORDER.csv`
- `Documents/Development/InforVisual/DatabaseCSVFiles/TableRowCounts/OPERATION.csv`
