# Chapter 2: Application Global Maintenance

This chapter describes these topics:

**Topics**

[What Is Application Global Maintenance? 2-2](#_bookmark4)

[Accessing Application Global Maintenance 2-3](#_bookmark5)

[Specifying General Information 2-4](#_bookmark6)

[Specifying Default Information 2-8](#_bookmark14)

[Specifying System-wide Codes 2-11](#_bookmark15)

[Specifying Other Default Information 2-25](#_bookmark57)

# What Is Application Global Maintenance?

Use Application Global Maintenance to set up information about your tenant or "corporate parent." Each database can have only one tenant. Most of the information you define in Application Global Maintenance applies to all accounting entities and sites in your database. You can override some information you define in Application Global Maintenance in Accounting Entity Maintenance or Site Maintenance.

Information defined at the tenant level includes:

- Certain application-wide information, such as purchase quote type, the default directory for documents, and interface theme
- Common codes, such as Honorifics, Packaging Types, Carriers, Workflow Codes, Commodity Codes, and Units of Measure

Accessing Application Global Maintenance

# Accessing Application Global Maintenance

Use Application Global Maintenance to define the tenant. To access Application Global Maintenance, select **Admin**, **Application Global Maintenance**.

# Specifying General Information

Use the General tab to specify this information:

- Corporate address
- Barcode transaction system time limit
- Number of recently used files to display in the File menu of certain windows
- Purchase quote type
- Synchronizer records setting

To specify information on the General tab:

- Click the **General** tab.
- In the address fields, specify your company's corporate name and address.

If you have multiple accounting entities or multiple sites, you can specify different addresses for your accounting entities in Accounting Entity Maintenance and for your sites in Site Maintenance.

If you have one accounting entity and one site, specify the company address in Application Global Maintenance. The address information in Accounting Entity Maintenance and Site Maintenance is not used in a single accounting entity, single site database.

- If you are licensed to use the Barcode Transaction System, in the Barcode Transmission System section specify the number of minutes to wait for a response from the system before the current transaction is cleared.
- To specify the number of recently accessed records to display, click the **Recent File List Limit**

arrow and select the number of records you would like to add to the File menu.

You can choose to list up to 9 records.The recently used files list is used in these applications:

- - Customer Maintenance
    - Sales Order Entry
    - Sales Order Management Window
    - Purchase Order Entry
    - Purchase Order Management Window
    - Part Maintenance
    - Vendor Maintenance

The recently used records setting you specify is applied to all of the applications that support the recently used file list. You cannot use the recently used records feature in only some of the applications. If you specify a number in the Recently Used Records field, then all of the applications that support the feature will display a list of recently used records.

Recently used records will be tracked and displayed on the File menu after you specify a setting in the Recent File List Limit field.

To de-activate the feature, specify 0 in the Recent File List Limit field.

- In the Purchase Quote Type section, specify how to enter quantities in vendor quotes. Click one of these options:

**Quantity-break tables** - To specify a default price that applies to quantities from 1 to the quantity you specify in the first quantity break, click this option.

For example, presume you specified this vendor pricing information:

| | **Quantity** | **Price** |
| --- | --- | --- |
| Default | | \$50 |
| Break 1 | 25 | \$40 |
| Break 2 | 50 | \$30 |
| Break 3 | 100 | \$20 |

If you ordered a quantity between 1 and 24, the vendor would charge \$50 per unit. If you ordered a quantity between 25 and 49, the vendor would charge you \$40 per unit. If you ordered a quantity between 50 and 99, the vendor would charge you \$30 per unit. If you ordered a quantity of 100 or more, then vendor would charge you \$20 per unit.

**Up-to-Quantity** - To specify a default price that applies to quantities greater than the largest up-to quantity you specify, click this option.

For example, presume you specified this vendor pricing information:

| | **Quantity** | **Price** |
| --- | --- | --- |
| Up to 1 | 25 | \$50 |
| Up to 2 | 50 | \$40 |
| Up to 3 | 100 | \$30 |
| Default | | \$20 |

If you ordered a quantity between 1 and 25, the vendor would charge \$50 per unit. If you ordered a quantity between 26 and 50, the vendor would charge \$40 per unit. If you ordered a quantity between 51 and 100, the vendor would charge \$30 per unit. For all quantities 101 and above, the vendor would charge the default price of \$20 per unit.

- To create Synchronizer Records for your warehouse management system, select the **Create Synchronizer Records** check box.
- To display the Financial menus, select the Financial Interface in Use check box. If you are licensed to use VISUAL Financials, you can access the applications from the financial menus. To hide Financial menus, clear the check box.
- In the Default Oldest Open Invoice section, specify a default setting for defining the oldest past due customer invoice. The system uses this information to help determine if the customer has an unpaid invoice that exceeds the receivable aging threshold that you specify in Customer Maintenance. The setting you specify in Application Global Maintenance is applied to any new customers you create. You can override the setting in Customer Maintenance. Click one of these options:

**Determined by Invoice Date** - To use the date of the invoice to identify the oldest past due invoice, click this option.

**Determined by Due Date** - To use the date that the full amount of the invoice is due to identify the oldest past due invoice, click this option.

- Click the **Save** button.

## Specifying Scheduling Information

Use the Scheduling tab to define the standard workday and shift information for your enterprise. If you have multiple sites, you can define scheduling information specific to the sites in Site

Maintenance. If you choose not to define calendars specific to your sites, the schedule you specify here is used instead.

If you are licensed to use a single site, the scheduling information you define in Application Global Maintenance is used. Any scheduling information defined in Site Maintenance is ignored.

For both single site and multi-site users, you can define schedules for your shop resources in Shop Resource Maintenance.

To define the standard shop calendar:

- Click the **Scheduling** tab.
- Enter the starting time for the first shift for each day of the week in the 1st Shift Start field. By default, the first shift is presumed to start in the morning (AM).
- For each day that has 1st Shift Start, specify the length in hours of the first, second, and third shifts. If a shift is not worked, enter a 0.

The totals of the three shifts cannot exceed 24 hours.

First Shift is the period of hours starting at the 1st Shift Start time and ending after the specified number of hours in the shift. Second shift follows immediately after the first shift and ends after the specified number of hours. Third shift is immediately after the second shift; it ends after the specified number of hours.

For example, if you specify a shift start of 7:00:00 AM, a first shift length of 8 hours, a second shift length of 8 hours, and a third shift length of 4 hours, first shift is 7:00 AM to 3:00 PM, second shift is 3:00 PM to 11:00 PM, and third shift begins at 11:00 PM and runs until 3:00 AM.

- Click **Save**.

### Deleting Calendar Selections

To clear a line on your Standard Shop Calendar:

- Select the line to clear.
- Click the **Delete Selections** button.

An X is displayed in the row header to indicate that you have marked the entry for deletion.

- Click **Save**. The information in the row is cleared.

### Specifying Calendar Exceptions

In some instances, you may need to change a normal shift day, such as for a holiday. Use Calendar Exceptions to define changes to the standard schedule.

If you have multiple sites, you can import the calendar exceptions in Application Global Maintenance into the calendar exceptions you define in Site Maintenance.

- Select **Maintain**, **Calendar Exceptions**.
- Click **Insert**.
- Specify the start date and end date for the exception. For example, if the resource will not be available on January 1, 2012, enter 1/1/12 for the start and end date.
- Specify the time that the First shift starts.
- Enter the shift duration for the exception. If the shift is not to work at all, specify zero (0) in that shift's column.

The scheduler uses the information to adjust the normal weekly calendar setting for the date of the exception, thereby giving an accurate estimation of resource availability.

- Click **Save**.

You can modify and add information to the Shop Calendar and Exception Days Table at any time. The changes take effect the next time you run the Global Scheduler.

### Deleting Calendar Exceptions

To delete an exception:

- Highlight the exception line and click **Delete**.

An X is displayed to indicate that the row is marked for deletion.

- Click **Save**.

The exception is removed from the table.

# Specifying Default Information

Use the Defaults tab to specify certain application-wide information, including:

- - Trace ID information
    - How to use customer pricing in shipping entry
    - Default Document Maintenance options
    - Password requirements in Workflow
    - PLM connection settings
    - Where to store macros

To specify information on the Defaults tab:

- Click the **Defaults** tab.
- If you use trace IDs in labor transactions and service receipts, select the **Only Trace Qty from Preceding Operation** check box to force users to enter trace IDs sequentially at every labor transaction or service receipt. Clear this check box if you do not want to force users to enter trace IDs sequentially.
- Use the Customer Price Effectivity in Shipping section to specify how the customer-specific pricing information should be used when shipping an order to the customer. In Customer Maintenance, you specify pricing information for each part the customer buys in the part pricing table. When you specify the pricing information, you specify dates that the pricing is effective. Click one of these options to specify how to use the part pricing information:

**Always Require Price in Table** - If you require that a unit price be specified in the customer's part pricing table, click this option.

**Use price on Order Line if Price is Not in Table** - To use the price specified in the customer order if no price exists in the customer's part pricing table, click this option.

**Warn if Price is Not in Table, Use Price on Order Line** - To display a warning if no price exists in the customer's part pricing table, click this option. If you continue with the shipment, the price specified in the customer order is used.

The setting you specify in Application Global Maintenance is the default setting. You can override the setting for individual parts in Part Maintenance.

- In the Documents section, specify the default email information to use in Document Maintenance:

**Allow Emailing Associated Documents (default)** - Use this check box to specify the default setting for the Allow Emailing check box in Document Maintenance. If you select the **Allow Emailing Associated Documents (default)** check box, the Allow Emailing check box in Document Maintenance is selected by default. If you clear the **Allow Emailing Associated Documents (default)** check box, the Allow Emailing check box in Document Maintenance is cleared by default. You can override the default setting in Document Maintenance.

- To require the use of passwords for secured fields when working with workflows, select the

**Passwords Required for Secured Fields** check box.

- In the Terms section, specify this information:

**Default Terms** - Select the terms to use as the default for new customer and vendor records. The terms that you select are also used as the default terms on transactions when another source of terms definitions, such as a customer or vendor record, cannot be found. If you leave this field blank, then Due on Receipt is used as the default terms.

You cannot specify terms with specific date definitions or installments. These types of terms are not displayed when you click the **Terms** browse button.

**Due on Receipt ID** - If you have set up more than one terms ID as due on receipt, use this field to specify the terms ID to use as the default Due on Receipt ID. Terms are designated as due on receipt if these conditions are met:

- - The Discount type and Net type are Age of Invoice
    - The Discount days and Net days are 0
    - The Discount % is 0
    - The freight terms are billed

When you click the browse button, only the terms that meet the criteria for due on receipt are displayed.

The terms that you select are used on transactions when default terms have not been defined on the customer or vendor record and the Default Terms field in Application Global Maintenance is blank.

If you leave this field blank, then the first terms ID in the database that meets the criteria for due on receipt is used as the default.

If you specify a default Due on Receipt terms ID, it is used on all AP and AR memos. If you do not specify a default Due on Receipt terms ID, then the first terms ID in the database that meets the criteria for due on receipt is used.

- If VISUAL is integrated to Infor PLM, you can directly access PLM from Part Maintenance. Use the PLM Integration section to specify default PLM access information for all sites in your database. You can override the information you specify in Application Global Maintenance for each of your sites in Site Maintenance. Specify this information:

**Login URL** - Specify the external launch URL for Web PLM.

**Enable** - To allow direct access to PLM for all sites by default, select this check box. To prevent direct access to PLM for all sites by default, clear this check box.

- In the Macros section, specify how to store macros and where to read macros. Macros can be stored and read from the workstation or from the database.

To store macros in and read them from the database, select the Store Macros in Database check box. After you select the check box, these conditions apply:

- - When a user runs a macro, the macro is read from the database.
    - Any macros created after the check box is selected are stored in the database.

To store macros on and read them from the workstation, clear the check box. After you clear the check box, these conditions apply:

- - When a user runs a macro, the macro is read from the workstation.
    - Any macros created after the check box is selected are stored on the workstation.

You cannot read certain macros from the database and other macros from the workstation. If you previously stored macros on the workstation but now store them in the database, you can load your existing macros from the workstation to the database. Similarly, if you previously stored macros in the database but now store them on the workstation, you can load your existing macros from the database onto the workstation. See "Storing Macros" on page 4-28 in the Concepts and Common Features guide.

- Click **Save**.

# Specifying System-wide Codes

Use the options in the Maintain menu to specify common codes used throughout your enterprise.

## Working with Table Maintenance

These codes are available from the Table Maintenance section of the Maintain menu:

**SIC Table** - Standard Industrial Classification codes classify various companies by the products they produce.

**AIC Table** - You can use AIC codes (Another Industrial Codes) as an alternative to SIC codes.

**Ship Via Table** - Ship Via codes represent the various methods you use to ship product to your customers.

**FOB Point Table** - FOB (Free On Board) codes determine at what point in the shipping process the customer must take responsibility for shipping fees for the goods your company is shipping.

**States and Provinces** - The system includes the abbreviations for the 50 United States and the Canadian provinces.

**Countries** - Countries with whom you do business.

**Territory Table** - Territory codes represent the various geographical regions in which your company conducts business.

**Commodity Table** - Commodity codes represent the class of materials used by your company.

**Honorific Table** - Honorifics precede an individual's name. Examples include Mrs., Mr., and Dr.

### Entering Information into Code Tables

To enter codes:

- Select **Maintain**, **Table Maintenance**, then select the option for the code you are entering. For example, to enter SIC Codes, select **Maintain**, **Table Maintenance**, **SIC Table**.
- Click **Insert**.
- Specify the code.
- For all tables except the Honorific table, enter a description of the code. The Honorific table does not include a description field.
- Click **Save**.

### Specifying FOB Point Codes in a Multi-site Database

In a multi-site database, one site can purchase materials from another site. When you enter the purchase order, the FOB code you use indicates which site owns the materials while they are in transit. The code you use determines when financial transactions associated with shipping occur.

To specify the in-transit owner, click the Intransit arrow and select one of these options:

**Shipping** - The site shipping the materials owns the materials while they are in transit.

**Receiving** - The site the materials are being shipped to owns the materials while they are in transit.

If you leave the Intransit field blank, then neither site owns the materials while they are in transit. The shipping site no longer owns the materials as soon as they are shipped. The receiving site assumes ownership of the parts as soon as they are received.

### Specifying User Dimensions

If you use dimensional reporting, you can attach user dimensions for commodity codes and for territory codes.

Commodity code user dimensions can be used in these transactions:

- Receivable Invoice
- Shipment
- Payable Invoice
- Purchase Receipts
- Inventory Adjustments
- Work Order Issues
- Work Order Labor
- Work Order Service
- Work Order Finished Goods

Territory code user dimensions can be used in these transactions:

- Receivable Invoice
- Shipment

You can set up different user dimensions for each code. Use the User Dimensions dialog box to specify which user dimensions to associate with a particular code. Use the User Dimensions Priorities dialog box available in the Accounting Window to determine when the warehouse user dimension IDs should be used. See "Cost Centers" on page 2-1 in the General Ledger guide.

To associate user dimensions with codes:

- From the Commodity Code table or the Territory table, click **User Dimensions...**.
- In the left pane, each user dimension group is listed. Expand the list under the user dimension group to view the transactions in which code user dimensions can be used.

To assign the same dimensions to all transaction types, click the name of the dimension group in the left pane. All Subledgers is inserted in the Subledger field.

To assign dimensions to a particular transaction type, select the appropriate transaction type. The transaction type is inserted in the Subledger field.

- Click **Insert**.
- Specify this information:

**Valid From** - Specify the date the dimension assignment becomes effective.

**Debit Dimension** - Double-click the browse button and select the dimension to use for account debits.

**Credit Dimension** - Double-click the browse button and select the dimension to use for account credits.

- Click **Save**.

### Deleting Information from Code Tables

To delete codes from tables:

- Select **Maintain**, **Table Maintenance**, then select the option for the code you are deleting.
- Select the line to delete.
- Click **Delete**.

An X is displayed in the row header indicating you have marked it for deletion.

- Click **Save**.

## Working with Reasons Maintenance

You can define these codes in the Reasons Maintenance section of the Maintain menu:

**RMA** - A Return Material Authorization (RMA) is a document that controls the return of some part or parts previously sold and shipped to a customer. Use RMA Reason Codes to specify why a customer is returning a part.

**Adjustment Reason Table** - Use Adjustment Reasons to specify why you are making an adjustment.

**Issue/Return/Transfer Reasons** - Use Issue/Return/Transfer reasons to specify why you are moving inventory.

**Deviation Reasons** - Use Deviation Reason codes to specify why the quantity used is different from the quantity completed. To set the requirement for Deviation Reason, refer to Site Maintenance.

**Inspection Reasons** - Use Inspection reasons to specify why parts and materials are inspected.

### Entering Information Into Reason Tables

To enter reason codes:

- Select **Maintain**, **Reasons Maintenance**, then select the option for the code you are entering. For example, to enter RMA Reasons Codes, select, **Maintain**, **Reasons Maintenance**, **RMA Reasons**.
- Click **Insert**.
- Specify the code.
- Specify a description of the code.
- If you are specifying Issue/Return/Transfer reason codes, click the I**ssue/Return/Transfer** arrow and select the types of transactions to which the reason code applies. You can select a single type of transaction (for example, Returns only) or any combination of transactions.
- Click **Save**.

### Deleting Information From Reason Tables

To delete codes from tables:

- Select **Maintain**, **Reasons Maintenance**, then select the option for the code you are deleting.
- Select the line to delete.
- Click **Delete**.

An X is displayed in the row header indicating you have marked it for deletion.

- Click **Save**.

## Working With Accounting Maintenance

These codes are available from the Reasons Maintenance section of the Maintain menu:

**Department Table** - Departments represent the individual areas you use in your manufacturing facility.

**Terms Table** - Terms describe the payment arrangements you use.

### Entering Information Into the Department Maintenance Table

To enter department codes:

- Select **Maintain**, **Accounting Maintenance**, **Department Table**.
- Click the **Insert** button.
- Specify this information:

**ID** - Specify the ID for the department.

**Description** - Specify the description of the department.

- Click **Save**.

### Specifying User Dimensions

If you are using user dimensions, assign a user dimension to your department IDs. To assign a user dimension:

- Select the department to which you are assigning user dimensions.
- Click **User Dimensions**.
- In the left pane, each user dimension group is listed. Expand the list under the user dimension group to view the transactions in which this record is used.

To assign the same dimensions to all transaction types, click the name of the dimension group in the left pane. All Subledgers is inserted in the Subledger field.

To assign dimensions to a particular transaction type, select the appropriate transaction type. The transaction type is inserted in the Subledger field.

- Click **Insert**.
- Specify this information:

**Valid From** - Specify the date the dimension assignment becomes effective.

**Debit Dimension** - Double-click the browse button and select the dimension to use for account debits.

**Credit Dimension** - Double-click the browse button and select the dimension to use for account credits.

- Click **Save**.

### Deleting Information From the Department Maintenance Table

To delete codes from tables:

- Select **Maintain**, **Accounting Maintenance**, **Department Maintenance**.
- Select the department to delete.
- Click **Delete**.

An X is displayed in the row header indicating you have marked it for deletion.

- Click **Save**.

**Note:** The department is not deleted from the database until you click **_Save_**.

## Entering Information into the Terms Table

**Note:** If you use Infor VISUAL Financials Global Edition, this menu selection is not available. You must define terms in VISUAL Financials Global Edition. Refer to the VISUAL Financials Global Edition online help.

Use Terms Maintenance to define the payment terms you use when you invoice customers and when vendors invoice you. Terms are used in several windows, including Customer Maintenance, Customer Order Entry, the Estimating Window, Receivable Invoice Entry, Vendor Maintenance, Purchase Order Entry, and Payable Invoice Entry.

To add terms:

- Select **Maintain**, **Accounting Maintenance**, **Terms Table**.
- Click **Insert**.
- In the Discount Terms section, specify this information:

**Type** - Click the **Type** arrow and select the type of discount term you are entering. You can select:

**Age of Invoice** - If discounts are based on a specific number of days from the invoice date, select the **Age of Invoice** option. If you select Age of Invoice, you must specify the number of days you will offer the discount and the discount percentage.

**Day of month** - If discounts apply only if payment is received before the next occurrence of the day of month you specify, select the **Day of month** option. If you select Day of Month, you must specify the day of the month to use and the discount percentage.

**Specified Date** - If discounts apply only if payment is received before a specific date, select the **Specified Date** option. If you select Specified Date, you must specify the fixed date to use and the discount percentage.

**Invoice age, day of month** - If discounts are based on a combination of a specific Invoice age and day of the month, select the **Invoice age, day of month** option. If you select Invoice age, day of month, you must specify the number of months and day of the month to use and the discount percentage.

**EOM, no of days** - If discounts are based on a specific day of the month but several months from the invoice date, select the **EOM, no. of days** option. If you select EOM, no. of days, you must specify the number of months to allow, the day of the month on which the discount will end, and the discount percentage.

For example, if you select EOM, no. of days and specify: Months = 2

Day of Month = 7 Discount % = 2.00

This discount rule is applied:

2% if paid on or before the 7th day of the month, 2 months from the invoice date. If the invoice date is May 10th, 2011, the discount is applied until July 7th, 2011.

- In the Net section, specify this information:

**Type** - Click the **Type** arrow and select the type of term you are entering. You can select:

**Age of Invoice** - If payment terms are based on a specific number of days from the invoice date, select the **Age of Invoice** option. If you select Age of Invoice, then you must also specify a number the days to use.

**Day of month** - If payment terms are based on receiving payment before the next occurrence of the day of month you specify, select the **Day of month** option. If you select Day of Month, you must specify the day of the month to use.

**Specified Date** - If payment terms are based on receiving payment before a specified date, select the **Specified Date** option. If you select Specified Date, you must specify the fixed date to use.

**Invoice age, day of month** - If payment terms are based on a combination of a specific Invoice age and day of the month, select the **Invoice age, day of month** option. If you select Invoice age, day of month, you must specify the number of months and day of the month to use.

**EOM, no of days** - If payment terms are based on a specific day of the month but several months from the invoice date, select the **EOM, no. of days** option. If you select EOM, no. of days, you must specify the number of months to allow and the day of the month on which the payment is due.

For example, if you select EOM, no. of days and specify:

Months = 3

Day of Month = 2

This payment rule is applied:

Payment is due on or before the 2nd day of the month, 3 months form the invoice date. If the invoice date is May 10th, 2011, the payment rule is applied until August 2nd, 2011. **Installments** - If payment terms are based on installments, select **Installment**.

- In the Freight Terms section, specify when freight charges are applied. Select one of these options:

**Prepaid** - If freight must be paid before shipment, select the **Prepaid** option.

**Billed** - If freight charges are to be included in the invoice, select the **Billed** option.

**Collect** - If the shipping company collects the freight charges, select the **Collect** option.

- If you selected Installments in the Net Type field, click the Installment tab to specify installment information.
  - In the Period type section, specify whether the installments will be based on days, weeks, or months. When you select an option, the period column in the installment table is changed to match your selection. For example, if you select weeks as the period type, the N.Weeks label is inserted in the installment table.
  - Click the **Insert** button and specify:

**N. Inst** - The number of the installments in sequential order is inserted.

**% Inst.** - Specify the percentage of the total due for the installment.

**N. \[Period\]** - Specify when the installment is due. For example, if you selected Days as the Period Type and specified 10 in the N.Days column, then the installment is due 10 days after the invoice date.

- 1. Repeat step b to enter additional installments. The total of all installments must equal 100%.
  - If you are working with installments and the payment is due at the end of the month, select the

**Due Date at the End of the Month** check box in the Installments section.

- 1. If you enabled VAT and you want to include the VAT amount on the first installment, select the

**VAT Amount on First Installment** check box in the Installments section.

- To generate a Term ID and Description based on the terms you specified, click **Gen ID and Desc**. Typically, the any discount percentage associated with the term is listed first followed by the net due information. For example, if you created a term that offered a 1.5% discount if paid by 10 days after the invoice date with the net amount due in 30 days, the term ID would be 1.5%10Net30.

You can also specify your own ID and description.

- Click **Save**.

### Discontinuing Use of a Term

To discontinue the use of a term without deleting the term from your database, clear the **Active** check box. The term will no longer be available for selection.

### Deleting Terms

You can delete a term from the database if it is not used on any other record. To delete terms:

- Select the term ID.
- Click **Delete**.
- The system prompts you to confirm your deletion. Click **Yes**. The Term ID you selected is deleted.
- To close the Terms Maintenance dialog box, click **Close**.

## Specifying Customer Order Acknowledgment Codes

When customers send orders to their suppliers electronically, they often require a return document verifying the receipt and acceptance of the order. In Electronic Data Interchange (EDI), this document is called the Order Acknowledgment, typically referred to as the 855 X12 Transaction or the Purchase Order Response Message in EDIFACT.

To specify Customer Order Acknowledgment codes:

- Select **Maintain**, **CO Acknowledgments Codes**.

X12 are shown in the dialog box by default. You can delete these codes.

- Click **Insert**.
- Specify this information:

**Code** - Specify a unique alpha-numeric identifier for the code.

**Description** - Specify a description of the code.

- Click **Save**.

### Deleting Customer Order Acknowledgment Codes

To delete Customer Order Acknowledgment codes:

- Select the code to delete.
- Click **Delete**.

An X is displayed in the row header, indicating that you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

## Entering Harmonization Codes

Use Harmonized Tariff Schedule (HTS) codes when shipping to and receiving from international locations.

To enter Harmonization codes:

- Select **Maintain**, **Harmonization Codes**.
- Click **Insert**.
- Specify this information:

**HTS Code** - Specify a unique identifier for the code.

**Description** - Specify a description for this code.

**Duty %** - Specify the duty percentage for this code.

- To specify duty percentages by country, click **Duty by Country**.
  - Click **Insert**.
  - Specify this information:

**Country of Origin** - Specify the country that has a specific duty percentage.

**Duty %** - Specify the duty percentage.

- 1. Click **Save**.

- Click **Save**.

### Deleting Harmonization Codes

To delete a harmonized tariff code:

- Select the code to delete.
- Click **Delete**.

An X is displayed in the row header, indicating that you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

## Entering National Motor Freight Codes

National Motor Freight Codes (NMFC) are a measure of the difficulty of transporting a product. To specify NMFC:

- Select **Maintain**, **National Motor Freight Codes**.
- Click **Insert**.
- Specify this information:

**ID** - Specify a unique ID for the code.

**Article** - Specify the NMFC article classification.

**Sub** - Specify the sub-classification of the article.

**Truck Load Class** - Specify the class for a full truck load (5 characters maximum).

**Less Than Truck Load Class** - Specify the class for a partial truck load.

**Hazardous** - To indicate that the material is hazardous, select the check box.

**Description** - A description of the types of parts to be transported under this code (250 characters maximum).

- Click **Save**.

### Deleting National Motor Freight Codes

To delete NMFCs:

- Select the code you to delete.
- Click Delete.

An X is displayed on the row header, indicating that you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

## Entering Transaction Categories

**Note:** This table is available only if you use Infor VISUAL Financials Global Edition.

Use transaction categories to extend the posting account and its segments. Transaction categories are used to generate balance transactions below the posting account level, and further classify transactions against a posting level account.

To specify transaction categories:

- Select **Maintain**, **Transaction Categories**.
- Click **Insert**.
- Specify this information:

**Code** - Specify the code to use.

**Description** - Specify a description of the code.

- Click **Save**.

### Deleting Transaction Categories

To delete Transaction Category codes:

- Select the code to delete.
- Click the **Delete** button.

An X is displayed in the row header, indicating that you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

## Specifying Workflow Codes

Use the Workflow Codes dialog box to customize the text that is displayed when you use workflow codes. You cannot add or delete codes, just change the text that appears.

To change Workflow Code text:

- Select **Maintain**, **Workflow Codes**.
- Click in the Text column for the code to change and specify new text.
- Click **Save**.

## Bid Rate Maintenance

Use the dialogs in the Bid Rate Maintenance menu to perform these tasks:

- - Create bid rate categories
    - Create bid rate groups
    - Add bid rate categories to bid rate groups

Use bid rate categories and groups in the estimating process. Usually, bid rate categories and groups are used when you develop quotes outside of VISUAL in Excel and import them into Quick Quote.

You can use categories and groups to develop high-level costs that are not necessarily the same as the cost to run a particular resource or to acquire a particular raw material. You can specify bid rate categories for resources in Shop Resource Maintenance. Specify bid rate categories for parts in Part Maintenance.

When you use Quick Quote to create a quote in the Estimating Window, the bid rate category for each part and resource is displayed.

### Creating Bid Rate Categories

To create bid rate categories:

- Select **Admin**, **Application Global Maintenance**.
- Select **Maintain**, **Bid Rate Maintenance**, **Bid Rate Categories**.
- Click **Insert**.
- Specify an ID and a description.
- Click **Save**.

### Creating Bid Rate Groups

To create bid rate groups:

- Select **Admin**, **Application Global Maintenance**.
- Select **Maintain**, **Bid Rate Maintenance**, **Bid Rate Groups**.
- Click **Insert**.
- Specify an ID and a description.
- Click **Save**.

### Adding Bid Rate Categories to Bid Rate Groups

To add bid rate categories to bid rate groups:

- Select **Admin**, **Application Global Maintenance**.
- Select **Maintain**, **Bid Rate Maintenance**, **Bid Rate Group Categories**.
- In the Bid Rate Group field, select the group to which you are adding categories.
- Click **Insert**.
- Double-click the category ID browse button and select the category.
- Click **Save**.

## Entering Packaging Type Codes

Packaging type codes define the dimensions of the packaging you use. Assign a default packaging code to a part in Part Maintenance.

To specify Packaging Type codes:

- Select **Maintain**, **Packaging Types**.
- Click **Insert**.
- In the Code column, specify the code to use for this package type.
- Specify the **Length**, **Width**, and **Height** of the package.
- Click **Save**.

### Deleting Packaging Type Codes

To delete Packaging Type codes:

- Select the code to delete.
- Click the **Delete** button.

An X is displayed in the row header, indicating you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

## Entering Carriers

Use the Carriers dialog box to specify the commercial carriers you use when shipping and receiving materials.

To specify carriers:

- Select **Maintain**, **Carriers**.
- Click **Insert**.
- Specify this information:

**ID** - Specify a unique identifier for the carrier.

**Name** - Specify the name of the carrier.

**Shipper ID** - Specify the identification of the shipper as assigned by the carrier.

**Default Route** - Specify the default routing information.

**Default Shipper Instructions** - Specify the standard instructions from the shipper to the carrier. **Default COD Fee** - Specify the standard fee the carrier charges for collecting COD payments. **Address fields** - Specify the carrier's address.

**VAT Registration** - Specify the registration number of the shipper.

**Parcel ID** - Specify a Parcel ID for this carrier. This ID is used to help identify the carrier for the UPS Connect interface.

**Web URL** - Specify the carrier's web address.

- Click **Save**.

### Deleting Carrier Codes

To delete Carrier codes:

- Select the code to delete.
- Click **Delete**.

An X is displayed in the row header, indicating that you have marked it for deletion.

- Click **Save**.

The code is removed from your database.

# Specifying Other Default Information

In addition to system-wide codes, you can specify this default information:

- - Default login profile
    - Export formats used for electronic payments
    - Address layouts
    - Revision numbering information
    - .NET configuration information

You can also monitor reporting service print jobs. The system administrator can set up this information:

- - Report format overrides
    - BOD maintenance information
    - VISUAL reporting data.

Refer to the _Infor VISUAL System Administrator's User Guide_ for the procedures a system administrator can perform.

## Specifying a Default Sign In Profile

Use the Default Sign In Profile function to set default sign in information for the work station. If you specify a default database, user ID and password, work station users will automatically be signed in when they access VISUAL. This is typically used only when executing a single program from a desktop icon, such as Barcode Labor Entry. This allows you to give access to specific applications on the shop floor and not require the personnel to sign in with a password. For system security purposes, it is highly recommended that you do not specify complete default sign in information.

You can also specify a portion of the sign in information, such as database ID and User ID. If you specify a portion of the sign in information, users are prompted to provide the remaining information when they access the system.

To set up default sign in information:

- Select **Maintain**, **Default Sign In Profile**.
- Specify this information:

**Database** - Specify the default database name. Leave this field blank to prompt users to specify a database name when they access the system.

**User ID** - Specify the default user ID. Leave this field blank to prompt users to specify a user ID when they access the system.

**Password** - Specify the password associated with the user ID you specified. For security purposes, the characters you type are replaced with asterisks. Leave this field blank to prompt users to specify a password when they access the system.

**Repeat Password** - Re-type the password. For security purposes, the characters you type are replaced with asterisks.

- Click **Ok**.

If you specified information in each of the fields, a dialog box is displayed informing you that the information you specified will take effect the next time the system is accessed. Click **Ok** to exit the dialog box.

If you specified information in only some of the fields, a dialog box is displayed informing you that users will be prompted to supply the missing information the next time the system is accessed. Click **Yes** to exit the dialog box.

## Creating Export Formats

Use the Export Formats dialog box to edit and create export format field definitions.

- Select **Maintain**, **Export Formats**.
- Click the **Type** arrow and select a format. You can select:
  - ESL
  - Intrastat
  - Payment
  - Payment/XML
  - VAT
- Specify a unique identifier for this export format in the Export ID field.
- Click the **Context** arrow and select a Context for this format. You can select:

**File Footer** - The File Footer contains batch total summary type information. Banks use this information to determine if they have successfully processed the file (i.e. have not missed any transactions).

**File Header** - The File header contains information about the company sending the file to the bank or payment processing facility.

**Header** - Header information is the "check" that has been created. Checks (cheques if international) contain the payment summary information to send to the vendor. It contains the vendor's name, the payment date, and the total amount paid. The payment total is a summary of all invoices paid to a particular vendor.

**Line Item** - Line items contain the remittance information supporting the payment total-for example, Invoice ID, Invoice Date, and Reference.

- Click **Insert** to add format definition information in the line item table. These columns are shown in the line item table:

**Start At** - This is the starting position in the data for this field. Zero is the first byte. Use this to sequence the data fields.

**Abut Previous** - Select this check box if the starting position in the data for this field is one byte after the last byte of the previous field.

**Data Expression** - You can specify any legal SQLWindows (SAL) expression. The exporter requires the data type returned to be type String.

**Supported Element** - A "Y" is displayed in this column if the SQLWindows expression you entered in the Data Expression column is a supported expression.

**Data Length** - Use this field only when the expression refers to the data variable directly without conversion. Specify the length of data in bytes not including quotes and commas.

**Data Type** - Use this field only when the expression refers to the data variable directly without conversion. Select one of these types:

**C** - Character

**N** - Number (decimal)

**D** - Date

**T** - Time

**I** - Integer

**Data Scale** - Use this type only when the expression refers to the data variable directly without conversion. Specify the scale for the decimal number type.

**Quote Data** - Specify whether quotation marks are placed around data. Select one of these values:

**D** - To place double quotation marks (") around data elements, select this option. **S** - To place single quotation marks (') around data elements, select this option. **X** - To place no quotation marks around data elements, select this option.

**Text Qualifier** - Specify the character that designates data as text.

**Comma Separate** - To separate data fields with commas, select this check box.

**Field Delimiter** - To separate data fields with a character of your choosing, specify the character in this field.

**Terminate Record** - If you select this check box, a CRLF (0x0d, 0x0a) is added after this field, if it is not the last data field in the record. Note, however, that the last data field in the record is automatically followed by CRLF. The prior rule prevents two consecutive record terminators.

- Click **Save**.

### Creating XML Payment Export Formats

You can create an AP export file format in XML. Use the Payment/XML format to export payments to Single Euro Payment Area (SEPA)-compliant file transfer systems. You can create an export format to use with release version 4.0 for SEPA Credit Transfers. For more information, refer to http:// [www.europeanpaymentscouncil.eu/content.cfm?page=sct_2010_rulebook.](http://www.europeanpaymentscouncil.eu/content.cfm?page=sct_2010_rulebook)

You can also use XML file format to create a custom export file format.

You should have an understanding of XML before setting up export file formats.

To create a simple XML payment export format:

- Click the Type arrow and select Payment/XML.
- Specify an Export ID.
- Click the Insert button
- Specify this information:

**Seq No** - Specify a sequence number for the line. The sequence number indicates the order of the lines in the XML document. The line with the lowest number is inserted in the document first. You can specify a negative number.

**Indent Level** - Specify an indent number for the line. Indent numbers identify how the lines are grouped together.

**Element** - Specify the name of the element. The element is the tag name. You do not have to specify the starting tag or ending tag.

**Value** - Specify the value to use for the element. Click the arrow to select from common database fields. You can also specify your own value.

**Is An Attribute?** - If the line in an attribute to the element, select the **Is An Attribute** check box. If you designate an attribute, you should sequence the line so that it is immediately after the element with which it is associated. You should specify the same indent level.

- Repeat steps 2 and 3 to insert additional lines.
- Click **Save**.

For example, presume you entered these lines:

| **Seq** | **Indent** | **Tag Tag Value Is an Attribute?** |
| --- | --- | --- |
| 1 | 0 | Payment N |
| 2 | 1 | Code SEPA N |
| 3 | 2 | Amount AMOUNT (selected from drop- N down menu) |
| 4 | 2 | CurrencyID CURRENCY_ID (selected from Y |

drop-down menu)

This XML code would be generated:

**&lt;Payment&gt;**

**&lt;Code&gt;SEPA&lt;/Code&gt;**

**&lt;Amount CurrencyID="USD"&gt;1234.00&lt;/Amount&gt;**

The USD currency ID and 1234.00 values are extracted from the database.

#### Building Payment/XML Formats with the Context Menu

You can build more complex Payment/XML formats using built-in macros and the Context menu. The Context menu inserts standard elements into the XML file. You can use each context once in each Export ID. The macros and contexts indicate how the information is linked together.

- Specify an Export ID in the Export ID field.
- Build the File Header. Click the **Context** arrow and select File Header.

The system inserts the doctype and root elements. The system inserts the standard xml version identification information in the Value field associated with the doctype element. Enter a value for the root element. The root element is the first tag in the XML file.

- Click the **Insert** button and specify any other elements you would like to use in the file header.
- At the point where you would like to include the Header context information, click the **Insert** button and specify %HEADER% in the Element column. This indicates that the system will look for the Header context information. If you create additional lines after the %HEADER% line, the system will insert those lines after it inserts the entire Header.
- Click **Save**.
- Build the Header context information. Specify the same Export ID as you entered in step 1, then click the **Context** arrow and select Header.
- Click the **Insert** button and specify the elements and attributes to use for the document header.
- At the point where you would like to include the LIne Item context information, click the **Insert** button and specify %LINEITEM% in the Element column. This indicates that the system will look for the Line Item information. If you create additional lines after the %LINEITEM% line, the system will insert those lines after it inserts the entire Line Item context information.
- Click **Save**.
- Build the Line Item context information. Specify the same Export ID as you entered in Step 1, then click the **Context** arrow and select Line Item.
- Click the **Insert** button and specify the elements and attributes to use for the line items.
- Click **Save**.

The system links together the information you built for the File Header, Header, and Line Item contexts.

#### Complex Expressions

You can combine simple calls for data from the database with expressions that convert the data to a different format or prompt the system to choose between two values. You can use these expressions:

**@FORMATNUMBER** - Use this expression to change the format of a number. You can specify the number of decimal places and the separator characters you use. To use the expression, specify the number you would like to convert, then specify the format. For example:

@FORMATNUMBER (1235,99, "#,##0") would convert 1235,99 to 1,235.

**@FORMATDATE** - Use this expression to change the format of a date. To use the expression, specify the date you would like to convert, then specify the format. For example:

@FORMATDATE (2010-07-14, "mm/dd/yyyy") would convert 2010-07-14 to 07/14/2010.

**@UTCDATE** - Use this expression to change the format of a date into universal time coordinate (UTC) format. To use the expression, specify the date you would like to convert, specify if you would like to show the time, and specify if you would like to show the duration. To specify whether or not to show the time or offset, use True to show the time or offset and False to not show the time or offset. For example,

@UTCDATE (2010-07-14, TRUE, FALSE) would convert 2010-07-14 to 2010-07-

14T16.25.54.18z. If you specified @UTCDATE (2010-07-14, TRUE, TRUE), the date would convert to 2010-07-14T16:25:54.18Z-04:00.

**@ROUND** - Use this expression to round a fractional number to a specific number of decimal places. To use this expression, specify the number you would like to round followed by the precision and scale. Precision is the number of digits in the number; scale is the number of digits after the decimal point. For example:

@ROUND (6543.1267, 6, 2) will convert 6543.1267 to 6543.13. If you specified @ROUND

(6543.1267, 5, 2), 6543.1267 would be converted to 6543.1 Since the number is limited to 5 digits in the definition, the system would only include one digit after the decimal point.

**@LOOKUP** - Use this expression to retrieve a cross-referenced value from the database. To use the expression, specify the key, table, keyfield, and datafield. For example,

@LOOKUP ('CURRENCY_ID', 'CURRENCY', 'ID', 'ISO_CODE') would retrieve the ISO code for the currency ID.

**@SQL** - Use this expression to execute a simple select SQL statement against the database. For example:

@SQL ('SELECT COMPANY_NAME FROM APPLICATION_GLOBAL') would retrieve the

company_name data from the application_global table.

**@IF** - Use this expression to ask the system to return one of two values based on the result of a test you specify. To use this expression, specify a test (for example, 1<2), the value the system returns if the test is true, and the value the system returns if the test is false. For example,

@IF (1>2, '1 is greater than 2', '1 is not greater than 2,') would return 1 is not greater than 2. The system would evaluate the test, 1>2, determine that the test is false (1 is less than 2), and return the value you specified for a false test, 1 is not greater than 2.

#### Concatenation

You can join simple expressions or string values and simple expressions together using a concatenation operator. The concatenation operator is a double pipe: ||. For example:

VEND_BANK_CITY || ',' || VEND_BANK_STATE || VEND_BANK_ZIPCODE would produce the vendor bank's city, state and zip code in this format: Boston, MA 02123. In this example, the comma is a string value and the references to database columns are simple expressions.

#### Other Functions

You may find these functions useful in your export file formats:

**SalDateCurrent()** - Use this function to return the current date and time. You can use this function in conjunction with a complex expression to specify a format for the data. For example:

@UTCDATE ( SalDateCurrent(), TRUE, FALSE) would return the current date expressed in UTC format without offset. For example, 2010-07-14T14:58:27.68Z.

**SalStrLeftX( string, length)** - Use this function to return a portion of the string value you specify. For example:

SalStrLeftX (DESCRIPTION, 40) would return the first 40 characters in the DESCRIPTION database column.

**SalStrTrimX (string)** - Use this function to remove all blanks from the string value. For example:

SalStrTrimX (COMPANY_NAME) would return MajesticManufacturingCompany instead of Majestic Manufacturing Company.

**SalStrScan (string 1, string2)** - Use this function to find the value you specify for string2 in string1. For example:

SalStrScan (VENDOR_NAME, 'Steel') would return vendor names that included the word 'steel' .

**SalNumberAbs (number)** - Use this function to return the absolute number.

#### Using the SQL Dialog Box

After you specify your export file format, click the SQL button. The system constructs a SQL statement that you can use to add the export file format to your database. Copy the SQL statement from the dialog box and paste it into SQLTalk or other database management tool.

#### Deleting Export Formats

To delete Export Formats:

- Click the **Type** arrow and select the type of format to delete.
- Click the **Export ID** arrow and select the format to delete.
- If you created contexts for the export ID, click the Context arrow and select the context to delete. If you created multiple contexts, you must delete each context one by one to delete the entire Export ID.
- Click **Delete**.
- Click **Yes** in the dialog box. The Export Format is deleted.

To delete lines from an Export Format:

- Click the **Type** arrow and select the type to modify.
- Click the **Export ID** arrow and select the format to modify.
- Click the row header to select the line to delete.
- Click **Delete**.

An X is displayed in the row header indicating you have marked it for deletion.

- Click **Save**.

The line you selected is removed from the Export Format.

## Entering Address Layouts

Multiple address layouts are supported for use throughout VISUAL. You define address layouts by country. When you select the country in a window such as Customer Maintenance, the Country ID, name and address layout update to the country's associated address layout. If you do not define a custom layout for a particular country, the default layout is used.

Country Address Layout IDs correspond to their country descriptions. If you use the same country description with multiple Country IDs, you can use the same layout with more than one country. If a country has multiple Country IDs with unique country descriptions, you can have multiple layouts for that country. However, it is recommended that you maintain only one address layout per country by making it flexible enough to accommodate all potential formats for the country.

**Note:** If you type in a free-form description in the Country field and that description does not exist in the Countries table, the country description is created but it is not added to the Country table. To use the layout, users must manually specify it in the country description field in a maintenance window, such as Customer Maintenance.

Address layouts can be up to five lines and are made up of labels and data fields. The labels are displayed to the left of the data fields, and you can define one label for each line. You can use up to seven data fields. You can place any of the fields on any of the lines in any order.

Users can enter alphanumeric text into the any of the data fields. Field #9 uses the codes you defined in the States and Province table.

To define an address layout:

- Select **Admin**, **Application Global Maintenance**.
- Select **Maintain**, **Address Setup**.

The default address layout is the conventional US address layout.

- To create a layout, specify an ID in the Country field. The ID must be the same as the Country for which this address layout is intended. For example, an address layout ID of **Spain** is required for an entity with a Country ID of Spain.

To modify an existing address layout, click the Country arrow and select the layout to edit.

- Specify this information:

**Field #** - The number of the field is inserted. This information is read-only.

**Type** - The type of field is inserted. An L indicates that the field is a label field. An F indicates that the field is a data field. This information is read-only.

**Title** - For label fields, specify the text to use as the label. You can specify text for a label field only. You cannot specify text for data fields.

**Line #** - Specify on which line the label field or data field is displayed. You can specify numbers 1 through 5.

**Position #** - If you are inserting more than one data field on a line, use the Position # column to indicate the order of the data fields. For example, in the default layout, fields 8, 9, and 10 are each on line 3. The Position # column indicates the order that these three fields are displayed. Field 8 is displayed first, followed by Field 9, followed by Field 10.

The position # column does not apply to field labels.

**Visible** - To display the field in the layout, select this check box. To hide the field, clear this check box.

**%** - Specify the percentage of the field to display. For example, to extend the length of a field, enter a value greater than the current value. 999 is the maximum percent value you can enter.

- Click **Save**.

The layout shows your changes.

- Click the **Close** button.

### Deleting Address Layouts

To delete Address layouts:

- Click the Country arrow and select the layout to delete.
- Click **Delete**.
- Click **Yes** in the prompt dialog box.
- Click **Save**.

The layout is removed.

### Copying Address Layouts

You can copy an existing address layout to create a new address layout. To copy an address layout:

- Click the **Country** arrow and select the layout to copy.
- Click **Copy**.
- In the Copy To field, specify the new Country ID in the Copy To field.
- Edit the layout as needed.
- Click **Save**.

### Using Custom Address Layouts

- Open a window where you enter addresses. For example, open Customer Maintenance.
- In the Country field of the address section, specify the Country ID that matches the Address Layout ID to use. For example, if this is for the UK layout, specify **UK**.

The address layout is changed

- Enter the address information. Click **Save**.

## Maintaining Revision Numbering

Use Revision Numbering Maintenance to create revision stages and numbering profiles to use with parts and other revision-controlled objects.

- Select **Maintain**, **Revision Numbering**.

By default, the DESIGN, MANUAL, PILOT, and RELEASE stages are supplied.

- To add a stage, click **Insert**.
- Specify this information:

**Stage** - Specify a name for the revision stage. The stages listed in this table are available in Part Maintenance and other applications where you can assign a stage to an object.

**Profile** - From the Profile drop-down list, select a numbering profile for this stage. You can select:

**User Numbering** - To assign each revision number based on your numbering preferences, select this option. When users specify a stage with the user numbering profile, a revision number is automatically assigned to the record.

If you specify this option, the fields in the lower half of the dialog box become available.

**Manual** - To manually specify each revision number, select this option. When users specify a stage with a manual numbering profile, they are required to specify a revision number before they can save the record.

**None** - If you do not require a revision number to be entered, select this option. When users specify a stage with None as the numbering profile, they can save the record without specifying a revision number.

**Note:** You cannot change the profile of a revision stage with a history.

**Description** - Enter a description for the revision stage into the Description field.

- If you select the User Numbering profile option, specify the revision numbers to use. You can manually enter revision numbers or specify basic information and generate the revision numbers.

To manually enter revision numbers, click **Insert** and specify the revision number to use. To specify a second number after the first revision number, click **Insert** again. To specify a second number before the first revision number, click **Insert Before**.

To generate revision IDs, use the fields in the Generate Revision ID's section. You can generate number-based IDs or letter-based IDs. To generate IDs:

**Starting Number** - To generate number-based IDs, specify the first revision number to use.

**Ending Number** - To generate number-based IDs, specify the last revision number. When you generate the revision IDs, revisions IDs between the starting number and ending number you specify are created.

**Alphanumeric Prefix/Starting Letters** - The label for this field changes depending on whether you are generating number-based revision IDs or letter-based revision IDs.

If you are generating number-based IDs, the label for this field is Alphanumeric Prefix. To attach a prefix to the revision, specify the characters in this field.

If you are generating letter-based IDs, the label for this field is Starting Letters. Specify the first letter or letters to use.

**Alphanumeric Suffix/Ending Letter(s)** - The label for this field changes depending on whether you are generating number-based revision IDs or letter-based revision IDs.

If you are generating number-based IDs, the label for this field is Alphanumeric Suffix. To attach a suffix to the revision, specify the characters in this field.

If you are generating letter-based IDs, the label for this field is Ending Letters. Specify the last letter or letters to use. When you generate the revision IDs, revisions IDs between the starting letters and ending letters you specify are created.

If you enter more than one letter in the Starting Letters and Ending Letters fields, the first letter you enter is treated like a prefix. For example, if you enter AA in the Starting Letters field and BZ in the Ending Letters field, the IDs AA, AB, AC, and so on are generated through ID BZ.

**Omit Letters** - If you are generating letter-based IDs, you can specify letters to omit from the IDS you generate. Specify the letters to omit. Use a comma to separate lists of letters.

This field is not available if you are generating numbers-based IDs.

- Click **Generate**. The number of revision IDs to be created is displayed. Click **Yes** to generate the IDs. The IDs are inserted in the table. You can edit the generated IDs as necessary.
- Click **Save**.

If you are using generated IDs, the revision IDs are entered in order when you create a revision. If you have used all of the generated revision IDs, you are warned when you try to create a revision.

## Verifying VISUAL .NET Configuration Settings

If you use VISUAL Financials Global Edition or VISUAL Time & Attendance, use the .NET Configuration Settings dialog to administer the connection to the .NET databases and executable. This configuration must be specified correctly for you to drill from the Document Lifecycle viewer to VISUAL Financials Global Edition. It is also used to write certain information to your .NET databases.

To verify these settings:

- Select **Admin**, **Application Global Maintenance**.
- Select **Maintain**, **VISUAL .NET Configuration**.
- If you have registered your VISUAL Manufacturing database and added it to your topology in the VISUAL for .NET Database Utility, then information about the instance group and instance name is displayed. Review this information to ensure that it is accurate. If no information is displayed in these fields, ensure that you have properly registered your VISUAL Manufacturing database in the .NET Database Utility.
- In the Install Path field, specify the location of LSA.exe.
- Click **Save**.

## Monitoring Reporting Service Print Jobs

If you have set up the VISUAL Reporting Service (VRPTSVC.EXE) to automatically run print jobs based on a schedule, use the Report Service Configurations dialog to monitor print jobs. You can review current Report Service print jobs, activate and deactivate them, and delete them.

The system administrator can control access to this dialog.

### Reviewing Reporting Service Print Job Configurations

To review the print job configurations:

- Select **Maintain**, **Report Service Configurations**.
- Review this information:

**Report Type** - The type of report that the service runs is displayed. For example, if a user has set up a print job for work order travellers, then Work Order Traveller is displayed.

**Site ID** - If applicable, the ID of the site where the report is run. Certain reports must be run one site at a time.

**User ID** - The ID of the user who scheduled the reporting service to run the report is displayed.

**Enabled** - If the reporting service configuration is currently active, this check box is selected. If the configuration is current inactive, this check box is cleared.

**Last Run** - The date and time that the service ran the configuration is displayed.

**Printer** - The printer where the service sent the print job is displayed.

- To reread configuration information from the database, click **Refresh**.

### Deactivating Reporting Service Print Job Configurations

To deactivate a reporting service print job configuration:

- Select **Maintain**, **Report Service Configurations**.
- Clear the **Enabled** check box.
- Click **Save**.

If you are deactivating more than one print job, you must click save after each time you clear the Enabled check box.

### Deleting Reporting Service Print Job Configurations

To delete a reporting service print job configuration:

- Select **Maintain**, **Report Service Configurations**.
- Select the configuration to delete.
- Click **Delete**. An X is displayed in the row header, indicating that the row will be deleted.
- To complete the deletion, click **Save**. To cancel the deletion and retain the configuration in your database, click Delete again. The X is removed from the row header.

## Setting Preferences

Use the Program Preferences dialog box to specify the tab that is displayed when you access Application Global Maintenance and to specify your preferred customer order entry and purchase order entry program.

You can access purchase order entry and customer order entry from several windows in the system. For example, you can open Purchase Order Entry from the Manufacturing Window. When you open Purchase Order Entry from the Manufacturing Window, the program you specify as the preferred purchase order entry program is opened.

To set program preferences:

- Select **Options**, **Preferences**.
- Specify this information:

**Default Tab** - Click the arrow and select the tab to display when you first open Application Global Maintenance.

**Preferred C/O Entry Program** - Click the arrow and select the program you prefer to use for Customer Order entry. Select either the **Customer Order Entry** program or the **Order Management** program.

**Preferred P/O Entry Program** - Click the arrow and select the program you prefer to use for Purchase Order entry. Select either the **Purchase Order Entry** program or the **Purchase Management Window**.

- Click **Done**.
