# Chapter 4: Site Maintenance

This chapter describes these topics:

**Topics**

[What are Sites? 4-2](#_bookmark163)

[Accessing Site Maintenance 4-3](#_bookmark164)

[Creating a Site 4-4](#_bookmark165)

[Assigning Existing Parts, Resources, Services, and Employees to Sites 4-20](#_bookmark182)

[Setting Service Charge Defaults 4-22](#_bookmark187)

# What are Sites?

Sites are specific physical locations that have their own parts, services, shop resources, and warehouses. Database users and employees are assigned to specific sites. Most of the transactions you enter are at the site level.

If you are licensed to use multiple sites, you can set up different information for each site. If you are licensed to use a single site, then you can set up information for your single site in Site Maintenance.

To set up basic information about sites, use Site Maintenance. You can:

- Create sites
- Set up general information, such as the site address, default packlist status, repetitive manufacturing information, part location on the fly information, part warehouse on the fly information, and issue negative information
- Set up scheduling information
- Set up shipment tracking information, such as shipment reason codes, default shipping label information, and the partial shipments with pre-invoiced orders setting
- Set up ECN information
- Set up inventory and labor defaults, inspections defaults, and default project warehouse information
- Assign existing parts, services, and shop resources to the site
- Maintain calendar exceptions

If you are licensed to use multiple sites, the system administrator can assign the site to database users. Assigned sites are referred to as _allowable sites_. Database users can choose which of their allowable sites they want to use. The sites database users choose to use are referred to as _viewable sites._ If you are licensed to use a single site, all users should be assigned to the single site.

If you are licensed to use multiple sites, you also assign sites to employees in Employee Maintenance. Employees can enter labor tickets for any site they are allowed to use. If you are licensed to use a single site, all employees should be assigned to the single site.

Accessing Site Maintenance

# Accessing Site Maintenance

To access Site Maintenance, select **Admin**, **Site Maintenance**.

# Creating a Site

Each site belongs to an accounting entity. Accounting entities can have multiple sites, but each site can belong to only one entity.

If you upgraded from a previous version, you defined one accounting entity ID for each of your financial entities during the upgrade process. Each financial entity ID becomes a site ID in the upgraded database.

If you have not exceeded the maximum number of sites allowed by your license, you can create additional sites.

Before you can create a site, you must define its parent accounting entity. The accounting entity must have an entity currency assigned to it.

To create a site:

- Click **New**.
- Click the **Entity ID** arrow and select the parent accounting entity for the site you are creating.
- In the Site ID field, specify a Site ID. We recommend that you **do not** use single quotations ('), forward slashes (/), or commas (,) in your site ID. Using these characters may result in reduced site selection functionality in reports and cause issues with integrations to other products through Infor10 Ion.
- Click **Save**.

## Specifying General Site Information

Use the General tab to define basic information about your site.

- Click the **General** tab.
- Specify the site's address in the address fields.

If you are licensed to use a single site, the address you define in Application Global Maintenance is used in the system. You do not have to specify a site address.

- In the Packlists section, specify the default status for this site's packlists. Specify one of these values:

**S** - (Shipped) Packlists are not invoiced or shipped.

**1, 2, or 3 Review** - Packlists must be reviewed.

**A** - Approved for Invoicing.

- In the Repetitive section, specify information to use in the Material Planning Window. Specify this information:

**Demand Fence 1 and 2** - Demand fences are used in conjunction with MRP. The values you specify in this section are used to order parts if the part has an order policy of Master Schedule, and the demand fence is not defined on the part's record or on the product code for the part. The value is in days.

**Use Fence 1 as MRP Frozen Period** - To prevent planned orders from being created during the demand fence period, select this check box. When you select this check box, planned orders cannot be created within the demand fence time period. Planned orders are generated after the time specified in demand fence 1 elapses. By using a frozen demand fence, you can prevent planned order from being generated in a time frame that is too short to actually supply the product.

Clear this check box to allow planned orders to be generated at any time.

- In the Part Location on the Fly section, select one of these options:

**Not Allowed** - Select this option to prevent part locations from being assigned on the fly. You can only use existing part/location associations. You cannot create new part/location associations or unique warehouse locations at the time of transaction. With this setting, you must establish part/ location associations using the Part Maintenance window or the Part Location Creator.

**Assign Existing Location to Part** - Select this option to allow users to assign an existing warehouse location to a part (an association that previously did not exist) at the time of a transaction. The warehouse location and part must already exist.

**Create New Location and Assign to Part** - Select this option to allow users to create a new warehouse location and assign it to a part, thereby creating a valid part/location association at the time of the transaction. The warehouse must already exist.

The part location on the fly setting applies to transactions created using:

- - Inventory Transaction Entry (VMINVMNT.EXE)

This applies when you receive, issue, adjust, and transfer (only in To Warehouse Locations) part quantities and must specify a warehouse location into which you are receiving, transferring, issuing, or adjusting.

- - Purchase Receipt Entry (VMRCVMNT.EXE)

This applies when you are receiving a purchase order quantity at a warehouse and can specify a location within the warehouse at the time of receipt.

- - Shipping Entry (VMSHPENT.EXE) (returns only)

This applies when you are receiving a returned customer shipment at a warehouse and can specify a location within the warehouse at the time of return receipt.

- - Physical Inventory Count (VMPHYINV.EXE)

This applies when you are conducting part counts and recounts within warehouses and can specify a location.

- - Interbranch Transfer Shipping Entry (VMIBTSHP.EXE)

This applies when you are shipping an IBT from one warehouse (From Whse ID) location to another (To Whse ID) and can specify from which location to ship the parts.

- - Interbranch Transfer Receipts Entry (VMIBTRCV.EXE)

This applies when you are receiving an IBT into a warehouse location (To Whse ID) and can specify into which location to receive the transfer.

When you create a new part/location association, you are prompted to specify a Hold Reason ID, Description, and a Status for the part in the new Warehouse Location.

- In the Part Warehouse on the Fly section, select the Prevent Warning Message to prevent a warning message from displaying when you create a new Part Location on the fly. This check box is not available if you selected the Not Allowed option in the Part/Location on the Fly section.
- In the Issue Negative section, select from these combinations:

**Warehouse cleared, Location cleared** - Use this combination to prevent both locations and warehouses from reaching a negative quantity.

**Warehouse selected, Location selected** - Select both check boxes to allow individual warehouse locations to reach a negative quantity and to allow the warehouse to reach a negative quantity as well. The system behaves in the same manner as it does if you select only the Warehouse check box.

**Warehouse cleared, Location selected** - Use this combination to allow warehouse locations to reach a negative quantity, but to prevent the warehouse from reaching a negative quantity. If you select this combination, a particular warehouse location can reach a negative quantity provided that the material is available in another location in the warehouse.

**Warehouse selected, Location cleared** - If you select this combination, neither warehouses nor locations are allowed to reach a negative quantity.

Auto Issue locations are exempt and are allowed to reach negative quantities regardless of the setting.

Allocations are not considered. The system only considers quantity on hand when determining whether the location reaches a negative quantity.

- Select the **Allocate Negative** check box to allocate demand even though it may cause a negative balance.
- Use the Prevent Negative Backdating check box to determine how to examine current inventory when a backdated inventory transaction is generated in Inventory Transaction Entry, Shipping Entry, Receiving Entry, or IBT Shipping Entry. If you select this check box, then the quantity you had on hand on the date of the transaction is used to determine inventory levels. If you did not have sufficient quantity on the date of the transaction, then the transaction cannot be completed. For example, say you enter an adjust out inventory transaction on January 5 for 10 units, but you specify January 3 as the transaction date. If you only had 8 units in your inventory on January 3, then you are prevented from completing the backdated transaction if you select the Prevent Negative Backdating check box.

If you clear the Prevent Negative Backdating check box, then the quantity you have on hand on the date you enter the transaction is used to determine inventory levels. For example, say you enter an adjust out inventory transaction on January 5 for 10 units, but you specify January 3 as the transaction date. If you have 10 units on hand on January 5, then you can complete the transaction, even if you did not have 10 units on hand on January 3. When you clear the Prevent Negative Backdating check box, you can generate negative inventory balances for past dates, even though you do not allow negative inventory balances for current dates.

- If VISUAL is integrated to Infor Quality Management®, select the Enable check box in the Infor Quality Management Interface section. Specify this information:

**Always Use/Query Use** - After an ECN is ready to be implemented, you click the Start button in ECN Entry. These options determine the result of clicking the Start button. If you click Always Use, then IQM is always launched when you click the Start button in ECN Entry. If you click Query

Use, then you are prompted to choose to open the appropriate IQM maintenance window or to open the appropriate VISUAL maintenance window. For example, if the ECN is for a document, you can choose to open IQM Document Maintenance or VISUAL Document Maintenance.

**Application Path** - Specify the default URL for IQM. This is the URL you access to sign into IQM.

**Configuration** - Specify the IQM configuration to sign into.

**Use VE User** - To pass the currently signed in VISUAL user ID to the IQM sign in window, select this check box. If the user is already signed into IQM, then IQM can be accessed directly from VISUAL. The user does not have to sign into IQM again. If the user is not currently signed into IQM, then the user ID is passed to the IQM sign in window. The user must supply a password. To require users to always sign into IQM, clear this check box.

- Click **Save**.

## Specifying Site Scheduling Information

Use the Scheduling tab to define the default shop calendar for the site. The weekly shop calendar defines standard workday and shift information for your plant. The Global Scheduler uses this information when producing a shop schedule. You can define calendars specific to a particular schedule or individual shop resource. Where neither of these are defined, the calendar specified here is used. This calendar should therefore define the default work week. Exceptions for holidays and different work center schedules are handled elsewhere.

If you are licensed to use a single site, set up scheduling information in Application Global Maintenance.

- Click the **Scheduling** tab.
- In the Production Schedule section, click Define Production Schedule and specify this information:

**ID** - Specify an ID for the production schedule.

**Description** - Specify a description for the production schedule.

**Note:** You can also specify a default production schedule ID for the site when you access the Global Scheduler. If you have not yet defined the default production schedule ID for a site, when you access the Global Scheduler you will be prompted to define the ID and description for each site that does not have a default production schedule ID.

- In the table, specify this information:

**1st Shift Start** - Enter the starting time for the first shift for each day of the week. By default, the first shift starts in the morning (AM).

**Shift 1, Shift 2, Shift 3** - For each day that has at least one shift, specify the length in hours of each shift. If a shift is not worked, enter a 0.

The totals of the three shifts cannot exceed 24 hours.

First Shift is the period of hours starting at the 1st Shift Start time and ending after the specified number of hours in the shift. Second shift follows immediately after the first shift and ends after the specified number of hours. Third shift is immediately after the second shift; it ends after the specified number of hours.

For example, if you specify a shift start of 7:00:00 AM, a first shift length of 8 hours, a second shift length of 8 hours, and a third shift length of 4 hours, first shift is 7:00 AM to 3:00 PM, second shift is 3:00 PM to 11:00 PM, and third shift begins at 11:00 PM and runs until 3:00 AM.

- Use the check boxes to define scheduling parameters. The selections you make apply to all schedules for the site. Select one or more of these options:

**Treat All Release Dates as Hard in All Schedules** - To schedule all work orders based on their hard release dates, select this check box. If you select this check box, the Concurrent Scheduler schedules no activity on any work orders in any schedule before the work order's hard release date, which you specify when you create the work order. If you select this check box, the Treat release date as hard check box on individual work orders is not available.

To decide whether to treat the release date as hard on a work order by work order basis, clear this check box. When you clear this check box, the Treat release date as hard check box becomes available on individual work orders. If you clear the Treat release date as hard check box on an individual work order, the release date is disregarded, and the work order is scheduled as time and materials allow.

**Use All Supply Before Applying Lead-time in Material Checks** - To consider material supply beyond work orders' required dates, select this check box. When you select this check box, the scheduler ignores a work order's required date when locating material supply. When sufficient supply is located, the work order is scheduled. If sufficient supply cannot be located, then the material's lead time is used to determine when the work order can be scheduled.

If you select this check box, then all parts in the site will use all supply before applying lead time. You cannot override the setting on the part record.

To consider material supply only up to the work order's required date, clear this check box. When you clear this check box, the scheduler does not look beyond the required date for supply when there is insufficient supply at the required date. In this case, it applies the part's or requirement's lead time to determine if sufficient supply can be obtained by the required date, or to determine when it can obtain sufficient supply.

If you clear the check box in Site Maintenance, you can specify on a part-by-part basis which setting to use. In Part Maintenance, use the Supply Before Leadtime check box specify whether or not to use the work order's required date when assessing supply.

The Concurrent Scheduler considers purchase orders, purchase order delivery schedules, Coproducts, work orders, and, if netting planned orders, planned orders, as supply.

**Use Global Calendar and Exceptions for Fabricated Parts** - To take calendar exceptions into account when calculating availability dates for your fabricated parts, select this check box. By using this setting you can specify the dates you do not want to use in calculations. Clear the check box to ignore calendar exceptions.

**Use Global Calendar and Exceptions for Release Dates** - To take calendar exceptions into account when calculating release dates, select this check box. By using this setting you can specify the dates you do not want to use in calculations. Clear the check box to ignore calendar exceptions.

- In the Scheduling Notch size section, specify the notch size for the schedule. To set the notch size to one second, select the One second check box. When you select this option, the In Minutes field is unavailable. To set the notch size in minutes, clear the One second check box, then specify a notch size in the In minutes field. You can specify any whole number from 1 to 6; for example, specify 2 for a notch size of two minutes. You can also specify 10 for a notch size of 10 minutes.
- Click **Save**.

### Specifying Calendar Exceptions

In some instances, you may need to change a normal shift day. In these cases, you can define an exception for the calendar. For example, use the exception table to define holidays.

If you have multiple sites, you can import the calendar exceptions in Application Global Maintenance into the calendar exceptions you define in Site Maintenance.

- Select **Maintain**, **Calendar Exceptions**.
- In the Site ID field, click the arrow and select the site for which you are defining calendar exceptions.
- Click **Insert**.
- Specify the start date and end date for the exception. For example, if the resource will not be available on January 1, 2012, enter 1/1/12 for the start and end date.
- Specify the time that the First shift starts.
- Enter the shift duration for the exception. If the shift is not to work at all, specify zero (0) in that shift's column.

The scheduler uses the information you enter here to adjust the normal weekly calendar setting for the date of the exception, thereby giving an accurate estimation of resource availability.

- Click **Save**.

You can modify and add information to the Shop Calendar and Exception Days Table at any time. The changes take effect the next time you run the Global Scheduler.

#### Copying Global Exceptions

You can copy the calendar exceptions set up in Application Global Maintenance to the site. To copy calendar exceptions:

- In the Site ID field, click the arrow and select the site for which you are defining calendar exceptions.
- Click **Copy from global exceptions**.
- Click **Save**.

## Specifying Shipment Tracking Information

Shipment Tracking is primarily for European users who need to be able to produce appropriate shipping documentation for materials that they may be holding off-site, are in transit to some other location-a customer or another warehouse-or that they are returning to a vendor.

To specify shipment tracking information:

- Click the **Shipment Trk** tab.
- To enable the shipment tracking function, select the **Shipment tracking enabled** check box. When you select this check box, you can print transportation document in these applications:
  - Shipping Entry
  - Service Dispatch Entry
  - Interbranch Transfer Entry
  - Interbranch Shipping Entry

When you select this check box, the Shipment Reason Codes section Maintenance section become available.

Clear the check box if you do not use shipment tracking.

- If you selected the Shipment tracking enabled check box, specify this information:

**Shipment Reason Codes** - This section is available only if you select the Shipment tracking enabled check box. Click one option:

**Required** - Select this option if users must supply a reason code to print a transportation document.

**Optional** - Select this option if users can print a transportation document without supplying a reason code.

**Maintenance** - Click Ship Reason Codes... to define reason codes for your shipping documents. Specify this information:

**Shipment Type** - Click the arrow and select one of these types:

**Shipment** - Select this option to classify the shipment as a shipment of goods to an outside location.

**Inventory Transfer** - Select this option to classify the shipment as a transfer of inventory between locations.

**Service Dispatch** - Select this option to classify the shipment as a service dispatch. **Purchase Return** - Select this option to classify the shipment as a purchase return. **Reason Code** - Specify a unique reason code ID.

**GL Account ID** - Specify an account ledger to assign to this Ship Reason Code.

**Default Warehouse ID** - Specify the ID for the warehouse to use for this code.

- In the Default Shipping Label field, click the Default Shipping Label browse button and select the default label format.
- To allow the deletion of packlists with ship dates in closed or locked periods, select the **Allow Deletion of Packlists in Closed or Locked Periods** check box. When you select this check box, any packlist can be deleted provided that no invoices have been created for the packlist. Clear the check box to allow packlists to be deleted only if the ship date is in an open period.
- In the Partial Shipments with Pre-invoiced Orders section, select one option:

**Create memo with excess balance** - Select this option if you would like to create a memo for any excess amount from pre-invoices or progress billings when customer orders are partially shipped. For example, if the customer has been pre-invoiced for \$300 for a certain quantity of a part, and you have shipped \$200 worth, then the system generates a memo worth \$100 for the customer's account.

**Retain excess balance for future shipments** - Select this option if you would like to retain the excess amount that a customer has been pre-invoiced or has been progress billed. The system behaves differently depending on if you pre-invoice the customer or progress bill the customer.

If you progress bill the customer, the system considers the amount to apply on a line-by-line basis. For example, if the customer has a progress billing invoice for \$300 for a certain quantity of a part on line 1 of the customer order, and you have shipped \$200 worth, then the system retains the \$100 to apply to a future shipment of the same part on line 1 of the customer order.

If you create the pre-invoice in Accounts Receivable Invoice Entry, then the grand total of the customer order is considered when the system determines the amount to refund or apply to a future shipment. The individual lines are not considered.

If you close the customer order or a line short prior to consuming all of a pre-invoice amount, the user must manually generate a memo to offset the remaining balance of the pre-invoice.

If you select this option, certain options may be disabled in Invoice Forms. If at least one customer order has been partially shipped and has a pre-invoice applied to it, the system disables the Create A/R Invoices, Combine All Packlists for an Order on One Invoice, and Combine All Packlists for a Customer on One Invoice options. You can re-activate these options by generating invoices for pre-invoiced orders with partial shipments. To identify these orders, print the List of Pre-Invoiced Orders with Partial Shipments report. Then, use the Generate/Print One Invoice option available on the File menu to generate an invoice for each order in the List of Pre-Invoiced Orders with Partial Shipments report. After you generate each invoice, the system reactivates the Create A/R Invoices, Combine All Packlists for an Order on One Invoice, and Combine All Packlists for a Customer on One Invoice options.

- Click **Save**.

## Specifying ECN Information

If you use Engineering Change Notices (ECNs) to control changes in your manufacturing processes, use the ECN tab to specify settings for ECN control.

- Click the **ECN** tab.
- Specify these settings:

**Modify ECNs with an on-hold status** - To allow users to modify ECNs with an on-hold status, select this check box. Clear this check box to prevent users from modifying ECNs with an on-hold status.

**Passwords required for secured fields** - To require users to enter a password when they enter data or change the status of secured fields, select this check box. This functionality provides you with electronic signature capability. To allow users to modify secured fields without entering a password, clear this check box.

**Generate all tasks simultaneously** - To generate tasks for members of all of your ECN teams at the same time when you change the ECN's status from Undefined, select this check box. Clear this check box to generate tasks for only the first non-completed team. For example, Implementation team members will not receive tasks until Authorization team members have all signed off on their tasks.

**Lock new engineering change notifications** - To automatically lock all new ECNs, select this check box.

**Allow application of ECN to unreported ops/reqs to in-process work orders** - To apply ECNs to work order that are already in process, select this check box. ECNs will be applied provided that labor has not already been reported or material issued to the operations and requirements affected by the ECN. For example: you have an engineering master with operations A, B, and C. You enter an ECN that affects operation B.

In one work order based on the engineering master, a labor ticket has already been entered against operation B. In this case, the ECN would not be applied to the work order. In another work order based on the engineering master, labor had been reported to operation A but not to operation B. In this case, the ECN would be applied.

If the ECN update has any impact on a requirement or operation that has actual labor or material issues, the work order will not be updated. For example, if an ECN changes a quantity per on a leg header card, the work order will not be updated If labor or materials have been applied under the leg.

If you select this check box, ECNs will not be applied to split work orders or work orders with active demand supply links.

Clear the check box if you do not want the system to apply ECNs to any in-process work order.

**One active ECN per part** - Select this check box to allow only one active ECN per part master. Clear this check box to allow multiple ECNs to be applied to a single part master.

**One active ECN per document** - Select this check box to allow only one active ECN per document. Clear this check box to allow multiple ECNs to be applied to a document.

**One active ECN per work order** - Select this check box to allow only one active ECN per work order. Clear this check box to allow multiple ECNs to be applied to a work order.

**One active ECN per engineering master** - Select this check box to allow only one active ECN per engineering master. Clear this check box to allow multiple ECNs to be applied to an engineering master.

**One Active ECN per project** - Select this check box to allow only one active ECN per project. Clear this check box to allow multiple ECNs to be applied to a project.

- Click **Save**.

### Entering and Deleting Maintenance Codes

Use the buttons in the Maintenance section to access these ECN codes:

- - Types
    - Dispositions
    - Reasons
    - Rejection Codes

#### Entering Maintenance Codes

**Note:** Use the same method to enter and delete information for all ECN maintenance dialog boxes. To enter ECN maintenance codes:

- Click the button for the maintenance code to enter. For example, to enter Disposition codes, click the **Dispositions** button.
- Click **Insert**.
- Enter the code and a description for that code.
- Click **Save**.

#### Deleting Maintenance Codes

To delete maintenance codes:

- Open the maintenance table that contains the code to delete.
- Select the line item to delete.
- Click **Delete**.

An X is displayed in the row header indicating you have marked it for deletion.

- Click **Save**.

## Specifying Defaults

The settings you specify on this tab apply to several areas of your site.

- Click the **Defaults** tab.
- In the Inventory/Labor section, select these options:

**Autogen Labor During Receipt** - Select this check box to backflush labor when you receive a work order into inventory or when you ship a customer order that is linked to a work order.

If you clear this check box, then backflushing is triggered in labor entry only. If you clear this check box, then the last operation in a work order must not use an auto-reporting resource. You cannot manually report labor to an auto-reporting resource.

**Default Employee** - If you selected the Autogen Labor During Receipt check box, specify the ID of the employee to use on labor tickets that are generated when you ship a customer order that is linked to a work order.

**Backflush Subordinate Legs** - Select this option to backflush labor to subordinate legs. In addition to selecting this check box, you must also specify an auto-reporting resource on the last operation in the leg to initiate the backflush of legs.

**Generate Labor Tickets During Backflush** - Select this check box to generate labor tickets for operations that have been backflushed. The costs that are specified on the Costs tab of the operation are used to determine the labor costs.

Clear this check box if you do not want to generate labor tickets for backflushed operations. If you clear this check box, then backflushed operations do not have any labor costs.

**Require Issue Reason for All Issues** - To require reason codes for every material issue you enter, select this check box.

**Require Issue Reason for All Issue Returns** - To require reason codes for every material return you enter, select this check box.

**Percent complete** - To report quantities based on percentage complete instead of quantity complete, select this check box. If you select this check box, then quantities for all run operations that you create for this site are reported as a percentage. These changes are made:

- In Labor Ticket Entry, Qty Remaining and Qty Completed labels are replaced with Percent Remaining and Percent Completed labels.
- In Wedge Barcode Labor Entry, Op Qty Completed, Op Qty Remaining, and Op Qty Remaining labels are replaced with Op Prcnt Complete, Op Prcnt Remaining, and Percent Completed prompts.
- In ALTS, the Quantity Completed prompt is replaced by a Percent Completed prompt.

If you clear this check box, you can set up whether to report labor quantities as percentages on individual work orders or operations.

**Require Deviation Reason for Deviated Quantities** - To require reason codes for every deviation quantity you enter during labor ticket entry, select this check box.

**Require Adjustment Reason for All Adjust Ins** - To require reason codes for all of your inventory adjust ins, select this check box.

**Require Adjustment Reason for All Adjust Outs** - To require reason codes for all of your inventory adjust outs, select this check box.

**Require Transfer Reason for All Transfers** - To require reason codes for all inventory transfer transactions, select this check box.

**Quantity Complete by Hours** - To automatically calculate quantity complete or percentage of completion based on the hours reported on the labor ticket, select this check box.

If you report labor based on quantity complete, then this calculation is made to determine the quantity completed during the labor ticket:

(hours reported on ticket/total estimated hours for operation) \* operation quantity

If you report labor based on percentage complete, then this calculation is made to determine the percentage:

(hours reported on ticket/estimated hours for operation) \* 100

The operation is automatically closed when the quantity or percentage complete equals or exceeds the operation quantity.

If you select the Quantity Complete by Hours check box in Site Maintenance, then all run type labor transactions in the site are automatically calculated.

**Max Percent Completed** - If you selected the Quantity Complete by Hours check box, use this field to specify the maximum percentage that can be calculated automatically. When the percentage complete meets the threshold that you specify, automatic calculation of quantity complete is stopped. The operation remains on your schedule until the operation is manually closed. This formula is used to calculate the number of hours for the operation that remain on your schedule:

((100 - value specified in Max Percent Completed field)/100) \* total hours required for the operation

If you specify a value in this field, then automatic calculation of labor on all operations in the site will stop at the value that you specify.

- If you are integrated with Infor Quality Management, specify settings in the Inspections section. Select these options:

**Require Completed Inspections on Labor Tickets** - To require users to select the Quality Data Collection Complete check box in the Labor Ticket Entry window before they can save the labor ticket, select the Require Complete Inspections on Labor Tickets check box.

**Require Completed Inspections for Return/Release** - To require users to select the Quality Data Collection Complete check box in the Receiving Inspection dialog box of the Purchase Receipt Entry window, select the Require Completed Inspections for Return/Release check box.

- If you are licensed to use Projects functionality, in the Projects section specify the default warehouse to use as the basis for your project warehouses.
- You can specify a default internal customer ID to use when this site buys items from another site. Click the **Default Internal Customer ID** browse button and select the customer ID to use.

You can override the default setting on the purchase order.

- In Vendor Maintenance, you can set up a list of buyers who are not allowed to purchase materials from a particular vendor. These buyers are excluded buyers. If you create a purchasing document for that vendor, you cannot specify an excluded buyer in the Buyer ID field. In the Vendor Exclusion Mode section, specify the purchasing information an excluded buyer can access. Click one of these options:

**Exclude buyers from using vendor** - Click this option to prevent the use of an excluded buyer on a purchasing document. If the excluded buyer is a user, the buyer can still create and edit purchasing documents, but cannot be the buyer specified on the document.

**Exclude users and buyers from using vendor** - If an excluded buyer is also a database user, click this option to prevent the buyer from creating any purchasing documents for vendors that the buyers are not allowed to use. Users can still view purchasing documents from any vendor. To prevent excluded buyers from editing purchasing documents for vendors they are not allowed to use, select the **Read only mode for excluded vendor documents**.

- In the Auto Issue Method section, click one of these options:

**Based on Operation Qty Complete** - Click this option to auto-issue material requirements incrementally based on the quantity or percent completed on each labor ticket. For example, presume that the operation is for a quantity of 5 and the Qty Per for the material requirement is 1. If a quantity of 2 is completed on a labor ticket, then 2 units of the material requirement are issued (presuming that there is no fixed scrap or deviated quantity). If an operation is closed before all quantities are completed, then the material requirement is also closed short.

**Based on the Full Requirement Qty on First Labor Ticket** - Click this option to auto-issue the full material requirement quantity after the first labor ticket is reported for the operation. If you backflush labor without creating labor tickets, then the full material requirement is issued after the first quantity is backflushed.

Depending on how you set up Preferences Maintenance, the issued quantity can include fixed scrap and extra materials for deviated quantities that are reported on the first labor ticket.

If a labor ticket has already been created for the operation or a quantity has already been backflushed, then additional material is not issued. For example, if you created a labor ticket for an operation and then increased the quantity of the material requirement, the additional requirement will not be issued.

**Note:** If operations are in process when you select this auto-issue method, additional materials are not issued to any in-process operation. To complete material issues for in-process operations, you must manually issue the materials.

**Based on Full Remaining Req Qty on Run Complete** - When you use this option, materials are issued in proportion to the quantity or percent complete on the operation until the Run Complete labor ticket is saved to the operation. When the Run Complete labor ticket is saved, all remaining material requirements are issued to the operation, even if the operation is closed short. If you backflush labor during the shipment of customer orders, then the full remaining quantity of a material requirement is issued when the full quantity of the order line has been shipped or when the line is closed short.

Depending on how you set up Preferences Maintenance, this quantity can include fixed scrap and extra materials for deviated quantities.

For more information on setting up auto-issue parts, see "Auto-issue Parts" on page 3-21 in the Inventory guide.

- If you are integrated to Infor PLM, you can directly access PLM from Part Maintenance. Use the PLM Integration section to specify PLM access information for the site. You can use the default connection information specified in Application Global Maintenance, or you can specify site- specific connection information in Site Maintenance. Specify this information:

**Login URL** - If this site uses a unique URL to access PLM, specify the external launch URL for Web PLM in this field. If this site uses the default URL specified in Application Global Maintenance, leave this field blank.

**Enable** - Specify whether you can directly access PLM from Part Maintenance when viewing a part for this site. Specify one of these options:

**Default** - Specify Default to use the enable setting specified in Application Global Maintenance. If you specify Default and the Enable check box is selected in Application Global Maintenance, then you can access PLM from Part Maintenance when viewing parts for this site. If you specified a URL in Site Maintenance, then that URL is used to access PLM. If you left the Login URL in Site Maintenance blank, then the URL specified in Application Global Maintenance is used to access PLM. If you specify Default and the Enable check box is cleared in Application Global Maintenance, then you cannot access PLM when viewing parts for this site, regardless if you have specified a URL in either Site Maintenance or Application Global Maintenance.

**No** - Specify No if you cannot access PLM in Part Maintenance when viewing parts for this site. If you specify this option, you cannot access PLM when viewing parts for this site even if you specify a Login URL in Site Maintenance or Application Global Maintenance.

**Yes** - Specify Yes if you can access PLM in Part Maintenance when viewing parts for this site. If you specify this option and you specify a login URL in Site Maintenance, then the URL you specify in Site Maintenance is used to access PLM. If you specify this option and you leave the Login URL field blank in Site Maintenance, then the login URL specified in Application Global Maintenance is used to access PLM.

- Click **Save**.

## Using the APS Tab

This tab is displayed only if you are licensed to use VISUAL APS.

Use the APS tab to specify default file information to use with the information that you import with the APS Import Utility and export with the APS Export Utility.

### Assigning Import File Paths

Set these Import File Paths:

- Customer Order
- Inventory
- Work Order
- Labor
- Purchase Order
- Master Schedule
- Part
- Resource

To assign import file paths:

- Click the **APS** tab.
- Click a **Path / File** button. For example, to assign the Customer Order file path, click the

**Customer Order Path / File** button.

- Navigate to the folder where you keep the import file, select it and click **Open**. The path and name you selected appears in the path field.
- Click the **Save** toolbar button.

### Assigning Export File Paths

To assign export file paths:

- Click the **APS** tab.
- Click the **Export Paths / Files** button.
- Click the appropriate button for the export path to assign.
- Navigate to the folder where you keep the import file, select it and click **Open**. The path and name you selected appears in the path field.
- Click the **Save** toolbar button.

### Assigning APS Import Default IDs

To assign import IDs:

- Click the **APS** tab.
- Click the **APS Import Defaults** button. The APS Defaults dialog box appears.
- Click the appropriate browse button for the ID to set.
- Select the ID to use and click the **OK** button.
- Cick **OK** in the APS Defaults dialog box and click the **Save** toolbar button.

### Setting File Styles

To set file styles:

- Click the **APS** tab.
- To surround strings with quotation marks, select the **Quoted Strings** check box.
- To trim the leading and trailing spaces from the text strings it imports, select the **Trim Trailing and Leading Spaces From Strings** check box.
- To assume that periods in numbers are decimal points, select the **Implied Decimal Points On Numbers** check box.
- Click in the **Field Delimiter** arrow and select the identifier to use between fields. You can select:

**Asterisk** - \* **Tilde** - ~ **Comma** - **_,_**

**Tab** - An invisible tab character.

**Null (Fixed Length)** - Fields in the import file must be the same length as specified by the database.

- Click the **Record Delimiter** arrow and select the identifier to use between records. You can select:

**Newline** - To display each record on a new line, select the **Newline** option.

**Null (Fixed Length)** - Records in the import file must be the same length as specified by the database.

- Click the **Save** toolbar button.

# Assigning Existing Parts, Resources, Services, and Employees to Sites

If you are licensed to use multiple sites, you can assign existing parts, shop resources, services, and employees to your sites in the Site Maintenance window. Parts, resources, and services must be assigned to a site before they can be used in transactions for that site. Employees must be assigned to a site before they can be used in a labor ticket.

If you are licensed to use a single site, all parts, shop resources, and services automatically exist in your single site.

The SYSADM user can also assign database users to sites. For more information, refer to the "User Management" chapter in the System Administrator guide.

## Adding Parts to a Site

To add existing parts to a site:

- Select **Maintain**, **Site Parts**.
- Click the **Site ID** arrow and select the site to which you are adding parts.
- Click the **Add to Site** check box for the parts to add to the site. To add all parts to the site, click

**Select All for Add**. To clear all selections in the Add to Site column, click **Unselect All for Add**.

- Click **Save** to add the parts to the site. After you click save, the Exists in Site check box is selected for the parts you added.

After you add parts to the site, you can modify certain part information in Part Maintenance at the site level.

## Adding Services to a Site

To add existing parts to a site:

- Select **Maintain**, **Site Services**.
- Click the **Site ID** arrow and select the site to which you are adding services.
- Click the **Add to Site** check box for the services to add to the site. To add all services to the site, click **Select All for Add**. To clear all selections in the Add to Site column, click **Unselect All for Add**.
- Click **Save** to add the services to the site. After you click save, the **Exists in Site** check box is selected for the services you added.

After you add services to the site, you can modify certain service information in Service Maintenance.

## Adding Shop Resources to a Site

To add existing shop resources to a site:

- Select **Maintain**, **Site Resources**.
- Click the **Site ID** arrow and select the site to which you are adding shop resources.
- Click the **Add to Site** check box for the shop resources to add to the site. If you choose to add a shop resource group ID, you are asked if you want to add the resources that are members of the group to the site. Click **Yes** to add the group members.

To add all shop resources to the site, click **Select All for Add**.

To clear all selections in the Add to Site column, click **Unselect All for Add**.

- Click **Save** to add the shop resources to the site. After you click save, the Exists in Site check box is selected for the shop resources you added.

After you add shop resources to the site, you can modify certain shop resource information in Shop Resource Maintenance.

## Adding Employees to a Site

To add existing employees to a site:

- Select **Maintain**, **Site Employees**.
- Click the **Site ID** arrow and select the site to which you are adding employees.
- To add employees to the site, click **Add to Site**. To add all employees, click **Select All for Add**.

To clear all selections in the Add to Site column, click **Unselect All for Add**.

- Click **Save**. After you click save, the **Exists in Site** check box is selected for the employees you added.

If you are licensed to use multiple sites, then each employee's pay rate is maintained at the site level. After you add employees to your sites in Site Maintenance, access Employee Maintenance to define the pay rate for each employee at each site. [For more information, refer to "Assigning Employees to](#_bookmark586) [Sites" on page 8-11 in this guide.](#_bookmark586)

You can also add employees to sites in Employee Maintenance.

# Setting Service Charge Defaults

Use the Service Charge Defaults dialog box to set up the service charges you use. If you are licensed to use multiple sites, maintain this information by site.

To set up service charge defaults:

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site for which you are setting up service charge defaults. If you are licensed to use a single site, this field is unavailable.
- Select **Maintain**, **Service Charge Defaults**.
- Specify this information:

**ID** - Specify a unique identifier for the service charge in the ID field.

**Description** - Specify a description of this service charge in the Description field.

**Unit Price** - Specify the service charge per unit.

**Trade Discount %** - Specify the discount available for this service.

**Commission %** - Specify the commission rate paid to the sales representative for this service.

**Product Code** - Specify the product code to which this service charge applies. **Commodity Code** - Specify the commodity code to which this service applies. **Sales Tax Grp ID** - Specify the sales tax group to apply to this service charge. **Revenue Account ID** - Specify the account where the service charge is posted.

- To include this service charge for Intrastat calculations, select the **Include for Intrastat** check box.
- To include any comments or specifications for this service charge, click in the Specifications text box and enter the comments.
- Click **Save**.

### Deleting Service Charge Codes

To delete service charge codes:

- Click the browse button and select the ID to delete.
- Click **Delete**.
- In the confirmation dialog box, click **Yes**.

The service charge is deleted from your database.
