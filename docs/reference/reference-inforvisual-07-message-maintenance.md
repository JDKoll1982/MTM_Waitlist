# Chapter 7: Message Maintenance

This chapter includes this information:

**Topic Page**

[What is Message Maintenance? 7-2](#_bookmark530)

[Setting Events 7-2](#_bookmark532)

[Setting Event Detection Preferences 7-6](#_bookmark542)

[Processing Events 7-7](#_bookmark545)

# What is Message Maintenance?

Use Message Maintenance to create and maintain records of specific events and to notify you when the event has occurred. Messages can be sent in these ways:

- - Pop up messages that appear on your computer screen
    - Email to specified people
    - Printed notification to your printer
    - SQL statements and queries This feature is helpful to:
    - Track certain events over a period of time
    - Receive immediate notification when an event has occurred.

## Setting Events

An event is a point in time at which a specified action occurs to a database record. Use Message Maintenance to specify the event to track and to formulate the message sent when the event occurs. For example, you can receive an alert when the status of a customer order changes. You can set up and change how the event is processed.

To set up events:

- Select **Admin**, **Message Maintenance**.
- Specify the event information:

**ID** - Specify a unique numeric identifier for this event.

**Class** - The class is the event that triggers the message. Click the **Class** arrow and select one of these options:

**Status change** - The status value of the record has changed. For example, a customer order has changed from Firm to Released.

**Line item change** - Information on a line item of the document has changed.

**Operation change** - Information on an operation of a work order or master has changed. **Requirement change** - Information on a requirement of a work order or master has changed. **Update** - An update of an existing record has occurred.

**Insert** - A new record has been created.

**Delete** - An existing record has been deleted.

**Query** - The event is based on a user-defined query.

**Deadline Event** - The event is based on a user-defined query executed on a specific date.

**Recurring Event** - The event is based on a user-defined query executed on a specific date and then once a day from that time onwards.

**Action** - Click the **Action** arrow and select the action taken when the event is triggered. Select one of these actions:

**E-mail** - An email is sent to the specified recipients.

**Hold** - The event is held for manual processing. You can use process these events in the Process Events dialog box.

**Print** - A message is sent to the printer specified to the printer set up as the recipient.

**Popup** - A pop-up message is displayed on the specified recipient's screen.

**Execute** - A query defined in the Message tab is executed.

**Status** - Click the **Status** arrow and select the status for the event. Select one of these statuses:

**Active** - This event is active. When the event is triggered, the action specified in the Action field occurs.

**Completed** - The event meets the conditions you specify in the Frequency section, but it has not processed.

**Suspend** - The event is temporarily suspended, preventing the event from being triggered.

**Canceled** - The event no longer occurs. Use this status as an alternative to deleting the event. If you select the Canceled status instead of deleting the event, a record of the event is retained.

- Specify Document information:

**Type** - Click the **Type** arrow and select the type of record to monitor. You can select these types:

- - All • A/R Invoice
    - Customer Order • A/P Invoice
    - Purchase Order • Quotation
    - Work Order • Part
    - Engineering Master • Unplanned Maintenance
    - Shipment • Engineering Change
    - Receipt • Requisition
    - Service Dispatch

**Base**, **Lot**, & **Split IDs** - To track changes to a specific record, specify the record ID. For engineering masters, quote masters, and work orders only, enter a Lot ID and a Split ID in the Lot ID and Split ID fields. For all other record types, specify the ID in the Base ID field.

- In the Frequency section, specify the number of times the event should be processed. Click one of these options:

**Permanent** - Click this option to process the event each time it is triggered.

**Repeat xx Times** - Click this option to process the event a specified number of times. Enter a value in the Times field.

**Do Until xx** - Select this option to process the event each time it is triggered until the specified date. Specify the date in the Date field.

**Do On** - If you selected Deadline Event or Recurring Event in the Class field, the Do On option is displayed and selected. Specify the date of the deadline or the date the recurring event occurs.

- Specify the delivery details for the message. Specify this information:

**Sender** - Specify the user who sends the message. The current user's ID is inserted by default.

**Recipient** - Specify the mail recipient who is to receive the message. You can click the Browse button to the right of the field to select a user from your mail application. To send this email to multiple addresses, separate each name with a semicolon.

If you selected Print in the Action field, click the Browse button to select the printer where the message is sent.

**Subject** - Specify the subject of the message. This appears on the pop up, eemailmail message, or printed document. This field is optional.

- Click the **Message** tab and enter a descriptive message for the triggered event.

If you are including a database query in the Query tab, use placeholders to specify where the data from the database should display in the message. For most data, use %1 for the first placeholder,

%2 for the second placeholder, and so on. For binary data, use %L1, %L2, and so on. Typically, fields that accommodate a large amount of text, such as notations and specifications, are binary data.

- Click the **Query** tab and enter a valid SQL query that you can use as a triggering test.
- Click the **Tokens** tab and enter a query that fills in the placeholder tokens that may appear in the descriptive messages.

You can use these Tokens:

**%TRIGGER_DATE** - Date event was triggered.

**%TRIGGER_DATETIME** - Date and time the event was triggered.

**% TRIGGER_TIME** - Time the event was triggered.

**%USER** - User ID that triggered the event.

**%ID** - Document ID that triggered the event.

**%TYPE** - Document type that triggered the event.

**%BASE_ID** - Document base ID that triggered the event.

**%LOT_ID** - Document lot ID that triggered the event.

**%SPLIT_ID** - Document split ID that triggered the event.

**%EVMNT** - Event ID that was triggered.

- Click the **Save** toolbar button.

### Event Samples

These samples show how to use the Message, Query, and Tokens tabs to write messages that include information from the database.

#### Creating a Message with Database Information

This example creates a message with database information when a new customer order has been created. Specify Insert in the Class field and Customer Order in the Class field. Set up the remainder of the event trigger as you see fit.

Enter this text in the Message tab:

**A new customer order %ID Customer ID %1 PO# %2 Ship Date %3 has been created. Please have it shipped.**

Enter this text in the Query tab:

**select customer_id, customer_po_ref, desired_ship_date from customer_order where ID=%ID**

Enter this text in the Tokens tab:

**%ID**

In the Message tab, the %ID indicates that the %ID token is used in the message. If you use a token in the message, you must list it in the Token tab.

The %1, %2, and %3 in the message are the placeholders for the three pieces of information you are extracting from the database with the query in the Query tab.

The results output may look like this:

A new customer order 1234 Customer ID 4567 Ship Date 06/03/2001 has been created. Please have it shipped.

#### Creating Deadline Recurring Events

This example creates an event with a deadline.

- Specify this information from the Message Maintenance window:

**Class:** Deadline event **Action:** Popup **Status:** Active

**Type:** Customer order

**Base ID:** CO-001

- Enter a deadline date in the Do On field.
- In the Message Tab enter:

**Customer order %ID Desired ship date %1 needs immediate attention as of**

**%2.**

- In the Query Tab enter:

**select id from customer_order where id=%BASE_ID and status not in('C','X')**

**and desired_ship_date<=SYSDATE INTO %BASE_ID**

- In the Tokens Tab enter:

**select desired_ship_date,SYSDATETIME from customer_order where id=%BASE_ID**

- Click **Save**.
- Create order CO-001 in Customer Order Entry with the Desired ship date set to the deadline date you entered in the Message Maintenance window.

Enter any part, quantity, vendor, and price.

- Save the customer order.
- In the Message Maintenance window, click the **Process Events** toolbar button.
- In the Process Events dialog box, Click the **Generate Deadline/Recurring Events** button.

The message is generated. If the system date is after the desired ship date, the message is generated every time you sign in.

## Setting Event Detection Preferences

Use the Preferences dialog to specify how events are detected.

- Select **Options**, **Preferences**.
- To specify which tab is displayed when you access Message Maintenance, click the **Default Tab**

arrow and select the tab.

- Click an event detection option:

**Process detected events** - Click this option to process messages when they are triggered.

**Place detected events on hold** - Click this option to hold messages when they are triggered. You can process the messages at a later time.

**Disable event processing** - Click this option to diable event processing.

- In the Minutes field, specify how often to check for new events.
- Select these options:

**Combine Messages if More Than** - Select this check box to combine messages. Messages are not sent until the specified number of messages have been generated.

**Eliminate Duplicates Created Within** - Select the check box to remove duplicate messages if they are created within a specified amount of time. Enter the number of minutes in the field.

This is helpful if you make several changes to a document and save after each change. If you select this option, only one message is sent for the changes instead of one message for each change.

**Limit Processing Time Slice to** - The system processes as many events as possible within the time limits you set in the field. The remaining events remain in the queue until the next time VISUAL checks for detected events.

- Click **Ok**.

## Processing Events

All message events are shown in the Process Events dialog box. From this dialog box, you can delete, print, and mail event messages.

### Manually Processing Events

You can manually process events that have not yet been processed.

- Click the **Process Events** toolbar button.
- Click the **Status** arrow and select the status of the events to show in the dialog box.
- Select the event to process.

The message for the event is shown in the text box.

To select all of the events in the table, click the **Select All** button.

- Process the event.

To delete the event, click the **Delete** button. To Print the event, click **Print**.

To email the event, click **Mail**.

If you click **Mail**, your mail application dialog box appears. Enter or select the recipient of the message, then send the message.

- To trigger the processing of the event, click **Ok**.

### Fixing Query Error Events

If, during the processing of events, you discover that you have errors, you can correct those errors by correcting the appropriate SQL Statement in the Message maintenance window.

After you have fixed the erroneous SQL Statement, return to the Process Events dialog box to execute the event with the error that you just corrected.

- Click the **Process Events** toolbar button. The Process Events dialog box appears.
- Click the Status arrow and select **Error** from the list.
- In the table, select the event that you just corrected.

A dialog box is displayed, asking you if you want to execute the displayed query.

- To execute the query with your recent changes, click **Yes**.
- To execute the query with your recent changes, click **Save**. The event is reprocessed with your changes.
- Click **Ok**.

To see if the message has been processed correctly, open the Process Events dialog box again and look for the event you corrected in the Completed events table.

