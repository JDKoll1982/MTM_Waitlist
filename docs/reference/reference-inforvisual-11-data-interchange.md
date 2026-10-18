# Chapter 11: Data Interchange

This chapter includes this information:

**Topic Page**

[What is Data Interchange? 11-2](#_bookmark819)

[What is EDI? 11-2](#_bookmark821)

[What is Electronic Commerce? 11-2](#_bookmark824)

[What is Integration? 11-3](#_bookmark827)

[EDI Requirements 11-3](#_bookmark833)

[Starting the Generate the Data Import Layouts Window 11-5](#_bookmark835)

[Inbound & Outbound Layout Process Overview 11-5](#_bookmark838)

[Considerations Before Creating Data Imports Layouts 11-5](#_bookmark847)

[Using the Generate Data Import Layouts Window 11-8](#_bookmark881)

[Starting the Data Import Exchange Window 11-24](#_bookmark923)

[Importing and Exporting Data In the Data Import Exchange Window 11-24](#_bookmark927)

[Running the Data Interchange Utility in Command Line Mode 11-33](#_bookmark944)

[Integration Requirements 11-35](#_bookmark949)

[EDI Transaction 850 - Purchase Order 11-36](#_bookmark954)

[A Glossary of EDI Terms 11-38](#_bookmark975)

[ANSI X12 EDI Transaction Sets 11-38](#_bookmark977)

[Tips for Successful Importing and Exporting 11-44](#_bookmark981)

# What is Data Interchange?

Electronic Data Interchange (EDI) is an optional module that imports and exports data into and out of VISUAL. The import and export occurs using VISUAL's Data Interchange (.VDI) files. There are two executables that perform this function. VMDIGEN.exe, the Generate Data Import Layouts window, defines the .VDI file layouts. VMDIXCHG.exe, the Data Import Utility Exchange window, performs the import or export of data.

If you have the line **EDI Menus=Yes ("Y" or "1" also apply)** in Preferences Maintenance, the system displays an EDI menu in the main toolbar. You can open all three EDI applications from this menu.

## What is EDI?

EDI is the electronic exchange of business documents in a predefined standard format. As a key component of Electronic Commerce, EDI controls costs, improves quality, increases efficiency, and gains strategic advantage.

The American National Standards Institute (ANSI) sets the standards for EDI in the United States. The United Nations standards body, UN/EDIFACT, sets the international standards for EDI. The ANSI accredited standards committee, X12, is the U.S. representative to the United Nations standards body. The ASC X12 standards specify the format and data content of electronic business transactions. Through the use of the standard, all organizations can enjoy the efficiencies of a common interchange language.

## What is Electronic Commerce?

Electronic commerce is a general term that refers to the use of computer and telecommunications technologies to support trading in goods and services. Some technologies such as EDI, electronic mail, and electronic funds transfer are already widely used. Some of them (including EDI) require agreements between trading partners (buyers and suppliers) in order to govern the electronic trading relationship.

Electronic commerce technologies can be used in any environment where documents are exchanged between organizations, including procurement/purchasing, finance, trade and transport, health, law and revenue/tax collection.

The potential benefits for both suppliers and buyers include reduced paperwork and administrative lead times, more timely business transactions, quicker and easier access to information, and reduced need to re-key information into computers. These benefits allow agencies to adopt more efficient purchasing practices such as Just-In-Time, Quick Response, and Direct Store Delivery.

## What is Integration?

An ideal extension of EDI-capability is to provide a seamless solution between the EDI translation software and another major application that can utilize incoming data and/or produce outgoing data, such as VISUAL. The "sharing" of data between applications is called Integration. The obvious benefit of integration is the elimination of redundant data entry. Other benefits include the reduction of errors due to re-keying, and the reduction of personnel that are necessary for data entry.

The Data Import Utility provides integration capability between VISUAL and your EDI software. You can directly integrate this information through the Data Import Utility:

- Actual Demand Information (Inbound Customer Orders (CPO))
- Planning or Forecasting Information (Inbound Master Scheduler/Customer Forecast (PLN))
- Actual Cash Receipt information (Inbound Cash Receipts for Invoices (CSH))
- Purchase Receipt Acknowledgement (RCA)
- Outbound Customer Order and Customer Order Change Acknowledgments (ACK)
- Actual Shipment Information (Outbound Advance Ship Notice (ASN))
- Actual Accounts Receivable Information (Outbound Invoices (INV))
- Vendor Purchase Order Information (Outbound Purchase Orders (VPO))
- Warehouse Shipment Advice (WSA)

### Integration Requirements

Integration between an EDI translator and VISUAL usually requires the use of integration "maps." Both the translation software and VISUAL often require maps that format the data that is brought into or extracted from the database.

There are many commercially available EDI communication software products that will allow you to build maps in that software to produce the file formats readable by VISUAL's data integration module. Integration with those products is available from our Professional Services Organization as well as from some of our channel partners.

In order to integrate your EDI data with VISUAL, you must determine which fields VMDIXCHG populates (inbound data) and from which it extracts (outbound data). To do this, use the the Data Import Utility Generate Layouts window to define the file layout and select the fields for each record type.

## EDI Requirements

Translation software is required, which has its own hardware requirements. The Data Import Utility module from Infor Global Solutions is required for integration.

Communication requirements include an Async/Bysync Hayes® compatible modem as well as a method of transferring data (not provided by Infor Global Solutions), either a Value Added Network (VAN) account, direct connection to a trading partner, or Internet access.

In the interest of a timely and cost-effective implementation, Infor Global Solutions will help determine your EDI implementation requirements by conducting a needs assessment, assisting with an EDI software purchase, and providing services and support.

# Starting the Generate the Data Import Layouts Window

**Caution:** The data you are exporting may be sensitive; therefore, you may want to limit accessibility to VMDIGEN.exe. Use Security Maintenance to control user access.

You need to define the .VDI file formats using the Generate Data Import Layouts window before the Data Import Utility can support any import or export activity. Because the necessary data in any given application depends on the destination or source, the Data Import Utility requires you to indicate exactly which data elements it will use. While you are responsible for specifying the elements of each table, the Data Import Utility controls the sequence of the elements in the file layout.

To start the Generate Data Import Layouts window:

Select **Data Import Generate** from the EDI menu.

## Inbound & Outbound Layout Process Overview

### Inbound Layout

Once you know the data fields you are importing, use the Generate Data Import window to define a

.VDI file layout to match the data with the proper VE fields. After you complete this, the Data Import Exchange window expects to import a file that matches the layout.

### Outbound Layout

Use the EDI implementation guide from your trading partner or sample EDI data to determine which data fields you need to extract from your database in order to create the necessary EDI file. Then use the Data Import Generate Layouts window to select the fields, design the exported file layout, and record definitions. Finally, use the EDI mapping software to translate the data into the EDI software for transmission to your trading partner.

## Considerations Before Creating Data Imports Layouts

This section contains some basic information about creating Data Import layouts. After this section, specific instructions for creating layouts of each type appear.

### Accessing Layouts

The Key/Version and Description buttons allow you to select a layout from the list of existing layouts. Once you have created and saved a layout, you can access it using either of these buttons.

### File Types

Outbound files can have a file type of either Single or Multiple; Inbound files may only have a File Type of Single. For this type, the Multiple radio button is unavailable.

### Record and File Naming

The Multiple file type writes each record type to a separate file. VISUAL imports and exports Header tab fields as records beginning with the letters HDR.; the .VDI file name ends in the letter H. For example, ACK0001H.VDI contains the HDR records exported for layout 0001. Header Special Detail tab fields are records beginning with the letters SDL; the .VDI file name ends in the letter D. The Line Item tab fields are records beginning with the letters LIN; .VDI file name ends in the letter L. The Sub Line Item tab fields are records beginning with the letters SUB; .VDI file name ends in the letter S. The Additional Line Item tab fields are records beginning with the letters ASL; .VDI file name ends in the letter A.

### Tab Access

All 5 tabs described above are available to the **ASN** and **WSA** keys. The **ACK**, **CPO**, and **VPO** keys allow you access to the Header, Line Item, and Sub Line Item tabs. The **CSH**, **INV**, and **RCA** keys allow you access to the Header and Line Item tabs. The **PLN** key allows you access to the Header tab.

### File Styles

#### Fixed File Style

A fixed file indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record. For example, if you have selected the Part ID in the Line Item tab of your VMDIGEN layout, and the layout shows the Part ID as being 30 characters long at position 25, then each time you export the Part ID for the LIN records of this layout, it will begin at position 25 and will take up 30 character positions. Typically, the Data Import Exchange module blank fills the unused portion of any alphanumeric fields and zero fills the unused portion of any decimal or number fields.

#### Fixed Style and Newline

If you select Fixed as the File Style, you can also choose from the Record drop-down list. For example, if you want fields in the records to appear in fixed positions (per layout) and each record to appear on its own line, you would choose Fixed as the file type and then Newline from the record drop-down list. For Fixed layouts for which you have selected nothing from the record drop-down, VISUAL normally imports or exports all records on the same line.

#### Delimited File Style

If you select Delimited as the style, you also need to select a delimiter for the end of each record, and the character VISUAL should use to separate each field in the record. For example, if you have selected the Part ID in the Line Item tab of your Delimited VMDIGEN layout, and Newline is your record type, Comma your field delimiter, your data would be:

LIN,02001,1,PRODUCT_P,55 LIN,02001,2,PRODUCT_Q,18

Immediately after the last bit of data in the Part ID field would be a comma, followed by the start of the next field. At the end of each record, VISUAL starts a new line for the next record. In the example above, 02001 is the Customer Order ID, followed by the Line Number, the Part ID, and the Quantity. The example uses Newline as the record delimiter, so each LIN record appears on its own line. The Delimited style does not blank-fill or zero-fill the unused portion of any exported fields - only the actual data is exported (and VISUAL only expects to find the actual data in the VDI file you are importing).

### Accessibility to Buttons and Boxes

For keys ASN, ACK, INV, RCA, VPO, and WSA the Inbound radio button is unavailable; for keys CPO, PLN, and CSH, the Outbound radio button is unavailable.

Several buttons and boxes are accessible to you only for CPO layouts. As a result, those buttons and boxes are unavailable for all other layouts. The fields you can use for CPO layouts are the Inbound Duplicates Rule button (listed to the right of the fields on the Header tab, these buttons handle the Update, Reject and Replace functions); the Delete Unshipped Lines checkbox (listed to the right of fields on the Line Item tab); and the Delete Unshipped Schedules checkbox (listed to the right of fields on the Sub Line Item tab).

### Accessibility to Options Menu Choices

Keys CSH, CPO, and ACK are the only keys for which you can select special settings. For keys ASN, INV, PLN, and VPO, the special settings options are unavailable.

The Options menu option Flush Temp. Cash Tables is only available for key CSH. The Options menu option Print Length Discrepancies is available for all keys.

### Mandatory Fields

When selecting data fields for any layout key, you may notice that certain fields under each tab are selected by default and some are simply unavailable. Fields selected by default are Mandatory fields for the key with which you are currently working. Any inbound CPO, PLN, or CSH Data Import layouts and .VDI files must contain the Mandatory fields in order to import properly. Also, any outbound ASN, ACK, INV, RCA, VPO, and WSA layout automatically exports these Mandatory fields as part of the tabs you use in the layout.

### Hidden Fields

When selecting data fields for the CPO layout key, you may notice that certain fields under each tab are not visible among the Data Import layout fields. VISUAL addresses these fields; they are classified as Hidden for the CPO. As a result, you can't import data to those fields.

The database fields still exist (as you can easily verify - see your System Administrator for more information). The only field hidden in the VPO key is the Purc_Line_Del.Del_Sched_Line_No (in the Sub Line Item tab). There are no hidden fields for keys ACK, ASN, CSH, INV, or PLN. If interested, please see the available Hidden field listing.

### Default Layouts

When you start VMDIGEN for the first time, the program creates pre-defined Default layouts for the seven available layout keys and assigns each a version number of 0000. You can use these default layouts as you would any manually created layout, meaning you can modify or delete them. If you do delete a layout, the Data Import Utility recreates the layout automatically the next time you start the program. If interested, please see the available Default field layout listings.

## Using the Generate Data Import Layouts Window

### Creating ACK Layouts (Customer Acknowledgements)

- From the Key/Version list box, select **ACK** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for ACK are HDR, LIN, and SUB.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need to select a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- From the Options menu, select **Special Settings** or click the **Special Settings** button on the main toolbar.

The Special Settings dialog box appears. The ACK tab has the default position. You cannot select the other tabs.

Select the appropriate option:

**Original Orders Only (855)** \- Select this option button to acknowledge original customer orders only. Original orders are orders with the EDI Release checkbox selected but not the Order Changed checkbox. These check boxes appear in the EDI tab of the Customer Order Entry window.

**Change Orders Only (865)** \- Select this option button to acknowledge changed customer orders only. Changed orders are orders with both the EDI Release checkbox selected **and** Order Changed checkbox selected. These check boxes appear in the EDI tab of the Customer Order Entry window.

When you update an existing order using Data Import Exchange, VISUAL selects the Order Changed checkbox. When you manually make changes to new orders in Customer Order Entry, you have to manually mark the Order Changed checkbox. You can clear the checkbox if you mistakenly change an order prior to exporting your 855 ACK documents.

If multiple 865 layouts exist for the same customer, the 865 ACK is only exported one time per change. That is, if you have orders with Order changed checkbox marked and you export 865 ACK, you cannot export the same orders with another layout -- unless you first re-select the checkbox.

**Both Original and Change (ORDRSP)** \- Select this option button to acknowledge both original and changed customer orders.

When you update an existing order using Data Import Exchange, VISUAL selects the Order Changed checkbox. When you manually make changes to new orders in Customer Order Entry, you have to manually mark the Order Changed checkbox. You can clear the checkbox if you mistakenly change an order prior to exporting your 855 ACK documents.

If multiple 865 layouts exist for the same customer, the 865 ACK is only exported one time per change. That is, if you have orders with Order changed checkbox marked and you export 865 ACK, you cannot export the same orders with another layout -- unless you first re-select the checkbox.

The Data Import Utility includes Customer Order Acknowledgment Codes on every line item (LIN record) of your ACK (this applies to 855 as well as 865 documents). You can enter and maintain common Customer Order Ack Codes using in Application Global Maintenance. If the codes your trading partner requires you to use are not in the default list, you may add them. For more information, refer to the "Application Global Maintenance" chapter.

When your EDI order is saved or received via VMDIXCHG, all customer order line items have your default code on them. For example, some clients use the IA code (Item Accepted), some use AC (Item Accepted and Shipped), and others might use some mutually-agreed upon code. You can add as many codes as you need. Do this in Customer Maintenance. For more information, refer to the "Customer Order Entry" chapter.

Click **Ok** to commit these settings.

- Begin to define a record layout. See the section Defining Record Layout later in this chapter for more information.
- In Customer Order Entry, customer order line items receive the default code you chose for the customer. Before exporting your ACK documents, you may find one of your line items requires a code other than the default. For example, you may have IA as your default, but need to send IH (Item on Hold) for this one line item.

In that case,

- - Open Customer Order Entry and find the order.
    - Highlight the line item and scroll over to the far right of the line item table.
    - Select the Ack Code field and choose the code.
    - Save the order.

- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating ASN Layouts

- From the Key/Version list box, select **ASN** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for ASN are HDR, SDL, LIN, SUB, and ASL.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing VISUAL that the data in the .VDI file varies in size. You also need toselect a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter

### Creating CPO Layouts in VMDIGEN

If you are licensed to use multiple sites, use caution when defining and using a CPO layout. Since you specify the Allocations Require Warehouse/Location setting (DDP) on the accounting entity record, some of your entities may require you to specify warehouse IDs/locations in allocations (DDP enabled) while others do not. Sites assigned to an accounting entity inherit the accounting entity's setting. As a result, the warehouse ID in all CPO layouts is optional.

If you plan to use a CPO layout in conjunction with a site that is DDP enabled, you must select a CPO layout that contains a Warehouse ID. If you import a .vdi without a warehouse ID to a site that requires the warehouse, the file import will fail.

If your site is DDP enabled, all customer order lines in the .vdi file must include the warehouse ID. Otherwise, the file import will fail.

If your site is not DDP enabled, you can include a warehouse ID in a CPO layout even though the site does not require a warehouse. If a warehouse is specified in the .vdi file, then the warehouse ID will be inserted in the customer order line. If a warehouse is not included in the .vdi file, the file import will still be successful because the site does not require the warehouse.

- From the Key/Version list box, select **CPO** and then enter a version for the layout in the adjacent field_._
- In the Description field, enter a description for the layout key. Click **Description** to choose a predefined layout key description.
- From the File Style section, select the appropriate file style.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- Choose this option if you want the data in the .VDI file(s) to always occupy the same "fixed" space for each field and record.

**Delimited** \- Choose this option if the data in the .VDI file varies in size.

- Select the appropriate delimiter for the end of each record, and a character to separate each field in the record.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Click the **Header** tab and choose the appropriate Inbound Duplicates Rule for headers, line items, and delivery schedules.

**Note:** The Update check box must be selected in the Header Inbound Rule section to use Line Item Duplicate and Delivery Schedule Duplicate features.

During the import of data, the Data Import Utility checks the import file records to see if duplicate data exists. Duplicate data has the same primary key in the targeted VISUAL table.

For CPO imports, a duplicate customer order has the same Customer Order ID as an order that already exists in the database.

If a quantity exists in the SUB record, VISUAL compares the quantity of the first delivery schedule to it, and then updates it with the quantity with the difference between the two. If the first delivery schedule is less than the fab build qty, and you are using the Fab Build Quantity as your base, VISUAL reduces the first delivery schedule qty to 0. If there is no fab build qty at the time of import, VISUAL imports the first delivery schedule qty in the VDI file (as if you had chosen to import as discrete).

- Select Header, Line Item Duplicate, and Delivery Schedule Duplicate options: Header

**Insert** \- Select the **Insert** check box to allow the insert of new customer orders **Update** \- Select the **Update** check box to allow the update of existing customer orders **Line Item Duplicate**

**Insert** \- Select the **Insert** check box to allow the insert of new line items. **Update** \- Select the **Update** check box to allow the update of existing line items. **Delivery Schedule Duplicate**

**Insert** \- Select the **Insert** check box to allow the insert of new delivery schedules.

**Update** \- Select the **Update** check box to allow the update of existing delivery schedules.

- Choose a method of calculating quantity:

**Cumulative** \- Select the **Cumulative** option button to use the difference between prior values and the delivery schedule value.

**Discrete** \- Select the **Discrete** option button to import the actual delivery schedule quantities included in the SUB (delivery schedule) records of the CPO VDI file. VISUAL performs no calculations or computations.

- Choose a Cumulative Base:

**Shipped Quantity** \- Select the **Shipped Quantity** option button to use the actual ship quantity plus any adjustment as the starting point.

**Fab Build** \- Select the **Fab Build** option button to use the Fab Build Quantity as the starting point.

- Select these check boxes as they apply to the CPO layout you are creating:

**Mark EDI Blanket Flag** \- To mark the imported customer order as a Blanket order when you run VMDI Exchange for a CPO layout, select the **Mark EDI Blanket Flag** check box.

**Update Dispatched Orders** \- If you have DCMS installed and you want VMDI Exchange to update the customer orders with information from the .VDI file, select the **Update Dispatched Orders** check box.

- Click the **Line Item** tab and select the **Delete Unshipped Lines** check box to delete any unshipped lines on an order before updating with updated data from a new .VDI file. This feature is useful for customers whose trading partners insist that they delete any customer order line items not shipped on an order prior to receiving updates for that order.

If you select to Delete Unshipped Lines, this warning message is shown:

"This will delete ALL unshipped lines on matching imported customer order headers regardless of which lines are imported. Processing will also delete ALL unshipped delivery schedule lines for those customer order lines containing delivery schedules."

- Click the **Sub Line Item** tab and select the **Delete Unshipped Schedules** check box to delete any unshipped delivery schedules on an order before updating with updated data from a new .VDI file. This feature is useful for customers whose trading partner insists that they delete any customer order delivery schedule lines not shipped on an order prior to receiving in updates for that order.

If you select to Delete Unshipped Schedules, this warning message is shown:

"This will delete ALL unshipped delivery schedule lines of those matching customer order line(s) being imported."

- Click the **Line Item** tab and begin to establish Match Fields.

If you have chosen Update (see step 6) as your inbound rule, you can choose how to determine line item matches for your CPO layout. If you do not choose any line item match fields, VISUAL updates line items based on a match of the Part ID. If you have multiple line items on an order for the same Part ID, it will only update the first occurrence of the part. You may need to choose additional match fields to avoid that problem.

It is possible to use the line item number as a match field, but many EDI trading partners don't use the line item number in the incoming data. You may need to use other fields to determine the correct line item for update.

To solve the problem of matching on the line item number, choose other fields for VISUAL to send in the line item. You can choose a combination of fields that allow you to create a unique line item in the order. If the combination you have selected does not determine unique line items, you will

need to add more fields to your match criteria until you find the right combination. For example, Part ID and EDI Release Number may determine a unique line item for your order. If that's not unique, try adding the Shipto ID field.

**Note:** If you use the line item number field as part of your match criteria, VISUAL ignores all other match fields and the line item number determines the match.

Also, if you have delivery schedules on your line items, do _not_ use the Desired Ship Date field as part of your match criteria. VISUAL uses the Desired Ship Dates in all of the delivery schedules for the line item and finds the earliest date, and then updates the Desired Ship Date field on the line item with that date. If you use delivery schedules, make an effort to avoid using the Desired Ship Date field as part of your match criteria.

If you are not using delivery schedules, the Desired Ship Date is fine to use as part of your match criteria.

In the Available Match Fields data field, highlight the field to add.

Click the **Right** Arrow to move the field into the Selected Match Fields section date field. Repeat this procedure for each line.

- Select **Special Settings** from the Options menu or click the **Special Settings** button on the main toolbar.

The Special Settings dialog box appears. The CPO tab has the default position. You cannot select the other tabs.

- Select the appropriate options:

Select the **Auto Generate CO ID** check box to generate the Customer Order ID for you. After selecting this check box, you must enter a Prefix Token for use in your VDI file.

During the exchange of EDI data, VISUAL replaces the token with the actual generated Customer Order ID.

With the Autogenerate CO ID check box selected, VISUAL generates unique Customer Order IDs for incoming EDI customer orders. You cannot, however, use this feature if order details may change, because VISUAL has no way of matching the original order with the incoming changes to the order. Use this feature for unique, discrete customer orders that you are sure will not change.

- Enter a prefix token in the Prefix Token field.

During the exchange of EDI data, VISUAL looks for this token in the 4th column on each header, line, and subline in your VDI import file.

- Click **Ok** to commit the special settings.

### Creating CSH Layouts

- From the Key/Version list box, select **CSH** and then enter a version for the layout in the adjacent field.
- From the Options men, select **Special Settings** or click the Special Settings button on the main toolbar.

The Special Settings dialog box appears. The CSH tab has the default position. You cannot select the other tabs.

Select the appropriate search option:

Search Invoices Based on:

**Invoice ID** \- VISUAL searches invoices based on Invoice ID **Packlist ID** \- VISUAL searches invoices based on Packlist ID **BOL ID** \- VISUAL searches invoices based on BOL ID

**ASN ID** \- VISUAL searches invoices based on ASN ID.

- Click **Ok** to commit these settings.
- To flush out the temporary cash tables, select **Flush Temp. Cash Tables** from the Options menu.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating INV Layouts

- From the Key/Version list box, select **INV** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for INV are HDR and LIN.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need to select a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Click **Ok** to commit these settings.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating PLN Layouts

- From the Key/Version list box, select **PLN** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file. The only available record type for used for PLN is HDR. PLN documents are imported from single files only. No multiple file option is available.

**Multiple file** \- Not available for layout PLN

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need to select a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- From the Header Tables tab, select the appropriate inbound duplicates rule.

During the import of data, the Data Import Utility checks the import file records to see if duplicate data exists. Duplicate data has the same primary keys in the targeted VISUAL table.

In the case of PLN imports, a duplicate forecast has the same Customer ID, Forecast ID and Part ID as a forecast that already exists in the database.

**Update** \- Select this option to examine the 3-field key above and update the forecast the data from the .VDI file if it finds a match. If VISUAL finds the same dates in the forecast in both the .VDI file and in VISUAL, it updates the records; if it finds new dates in the .VDI file, it adds the records.

**Reject** \- Select this option to ignore data in the .VDI file that has the same primary keys as data in the target VISUAL table. In the case of PLN imports, if VISUAL finds a match on the 3-field key it will throw out the .VDI file forecast (leaving the forecast in VISUAL untouched) and write an entry to the log file. No information is written to the database for the forecast and no dates are examined.

**Replace** \- Select this inbound rule and VISUAL, if it finds a match on the 3-field key, eliminates all of the records for that forecast from VISUAL and import them instead from the .VDI file. As with the Reject Duplicate inbound rule, VISUAL does not examine any dates. The only option that addressed the required date field is the Update inbound rule.

- Click **Ok** to commit these settings.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating RCA Layouts

- From the Key/Version list box, select **RCA** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for RCA are HDR, LIN, and SUB.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need to select a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Click **Ok** to commit these settings.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating VPO Layouts

- From the Key/Version list box, select **VPO** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for VPO are HDR, LIN, and SUB.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need toselect a delimiter for the end of each record and the character VISUAL should use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma, Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Click **Ok** to commit these settings.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Creating WSA Layouts

- From the Key/Version list box, select **WSA** and then enter a version for the layout in the adjacent field_._
- Select a File Type, Single or Multiple.

**Single file** \- The single file type writes all records to one file.

**Multiple file** \- The multiple file type writes each record type to a separate file. Available record types for WSA are HDR, LIN, and SUB.

- Select the appropriate file style from the File Style section.

The file style refers to the format of the data contained in the .VDI file. You can select:

**Fixed** \- This style indicates that the data in the .VDI file(s) will always occupy the same "fixed" space for each field and record.

**Delimited** \- By selecting this option, you are informing the Data Import Utility that the data in the

.VDI file varies in size. You also need to select a delimiter for the end of each record and the character to use to separate each field in the record. Please see the detailed description of Delimited styles in the previous section.

For Record, select from **Blank**, **Newline**, and **Null (Fixed Length)**.

For Field, select from **Blank**, **Asterisk**, **Comma**, **Tab**, or **Null (Fixed Length)** as a delimiter.

**Note:** If you are creating a Null (fixed length) layout (i.e. with each record contained on its own line), choose these settings:

**File Style:** Fixed

**Record:** Newline

**Field:** Null (Fixed Length)

- Click **Ok** to commit these settings.
- Begin to define a record layout. See the section "Defining Record Layouts" later in this chapter for more information.

### Defining Record Layouts

On the bottom half of the Generate Data Import Layouts window there are five tabs: Header Tables, Header Special Details, Line Item Tables, Sub Line Item Tables, and Adt Line Item Tables.

Each tab represents a record type in the layout. Not all tabs are used in each layout. If a tab or record type is not relevant to a key type, no tables appear when you select the tab.

Each tab also displays a list of default tables that contain the most appropriate fields for the selected key. Usually, all of the fields you want to access are available by default. If not, you can "join" additional tables to the ones displayed.

- Select a tab to start defining a record layout.
- Click the **Data** button to select data elements for the record type you are defining. (There is a

**Data** button below each list of tables.)

The dialog box shows all of the available columns of the tables for that record type. The table displays the table name (or view), the column or field name, the data type, the position in the .VDI file record, the length of the field, the scale (number of decimal places) and whether the inbound data may be null for this column.

- Select **Print**, **View**, **File**, or **E-mail** and then click the **Print** button to print, view, or send the data elements you have selected thus far. For more information on sending reports, refer to the "Concepts and Common Features" chapter.

Because this information is taken directly from the database catalog, the data fields must already be defined in the schema. If you intend to import user-defined columns and/or tables, these columns and tables must be added to the database prior to using the Generate Data Import Layouts window.

See "Joining Tables" for more information

When you first define a new layout, the default tables appear. These tables are mandatory. You can remove the columns from a layout, but the table itself is not removable.

If one or more data elements that you require is not displayed in the default tables, you can add the tables that store those fields to the end of an existing table list. Any user-defined or custom tables should have primary keys defined so that the join process can find the primary key automatically.

If you join additional tables to a tab, the mandatory tables appear first in the table lists, followed by each table you added. The columns appear alphabetically within each table.

- To include a column as part of the .VDI layout, highlight the appropriate column and click on the

**Select** button.

The x in the row header disappears and the position of the data element in the record appears in the Pos column. This position is only accurate if the File Style of the layout is Fixed; VISUAL updates the position any time you select or deselect an element.

The sequence in which the columns appear in the table window is the same sequence they will appear in the .VDI file layouts for each record. This sequence is critical because the .VDI file(s) will be created or read by an external application, most likely an EDI mapping tool. The use of an external application needs to be designed around the .VDI layout.

- Once you define the record layouts for each tab, click **Ok**. You return to the Generate Data Import Layouts window.
- Click **Save**.

The Data Import Utility import and export data is placed in files having predefined names. The only user-defined portion of the file-naming is the directory in which they reside. Thus, when importing or exporting is done, the program always looks for or creates the same files in a user-defined place.

To keep multiple versions of the files (i.e., archive each imported or exported file) then you must do this outside of the Data Import mechanism.

.VDI files are named for their layout key target and layout version.

For multiple files format, there may be up to five files, for each tab used in the Generate Layout window. Each file has a suffix indicating its contents:

ASN9999**H**.VDIHeader (HDR) records ASN9999**D**.VDIHeader Special Detail (SDL) records ASN9999**L**.VDILine Item (LIN) records ASN9999**S**.VDISubline records (SUB) records

ASN9999**A**.VDIAdditional Line Item records (ASL) records

**Note:** 9999 is the layout version. Single file formats:

XXX9999.VDIContains HDR and LIN records

**Note:** XXX is the layout key and 9999 is the layout version.

### Joining Tables

If you require additional tables, you can add them to the end of the item list in the appropriate tab. Any user-defined or custom tables should have primary keys defined so that the join process can find the primary key automatically.

VISUAL sets up joins automatically for mandatory tables, but does not permit access to the join for a mandatory table. You must define the joins for any non-mandatory tables that you add.

To join tables:

- First select the tab to which to add the tables. From the table selection drop down box, select the table to join, then click on the **Join** button.

The Join Clause dialog box appears.

You can only join a table that is relative to the one that already exists.

You must always select RECTYPE. It is fixed depending on the tab your are in (HDR, SDL, LIN, SUB, ASL)

- Select one of the primary key columns on the right table, then select the corresponding data element on the left column.

For example, if the Part table is on the right, and its Primary Key, ID, is selected, you should find a column containing the Part ID on the left, such as CUST_ORDER_LINE.PART_ID.

- Click on the appropriate number button to number the column, then click **Set**. The join clause must include all the fields that make up the primary key.
- Click **Ok**.

The table is now joined.

The user-defined table is an advanced feature of the Data Import Utility. In most cases, you will not need this feature.

Please be careful when selecting the join keys. An incorrect join can cause data inconsistencies in your .VDI files.

### Associating Customers to Outbound Layouts

You need to associate each outbound layout with one or more VISUAL Customer IDs so that when you run the layout through exchange, it knows whose data (invoices, ship notices) to export. To do this:

- If you haven't done so already, save your outbound layout by clicking on **Save**.
- Select **Associate** from the File menu or click the **Associate** button on the main toolbar. The Associate Outbound Format dialog box appears.
- Select which customers will use this layout when exporting data.

You can click on the **Select All** button to select all of the customers listed and the **Unselect All**

button to deselect all of the customers you have selected.

- Click on **Save**, and the click **Close**.

The customers you selected are now associated with this layout. When this layout is selected in the Data Import Exchange window, data for all customers who have been associated with this layout will be exported.

### Printing Length Discrepancies

Use the print length discrepancies function to print a report of the fields that have changed beginning with version 6.2x. Rather than the Data Import Utility automatically changing the lengths, you can choose to keep the older field lengths. This may help you keep some of your older EDI maps functionally accurate.

- From the Options menu, select **Print Length Discrepancies** or click the **Print Length Discrepancies** button on the main toolbar.
- Select an output for the length discrepancies report:

**Print** \- VISUAL prints the report after you select standard print options.

**View** \- VISUAL displays the report on your screen before printing.

**File** \- VISUAL presents you with a Print To File dialog box in which you can specify where to place the report.

**E-mail** \- Select this option to send the report in a Rich Text Format (.RTF) through electronic mail. When you generate the report the system attaches the file to a Microsoft Outlook email. For more information on addressing and sending the email, refer to your Microsoft Outlook user documentation.

- Click **Print** to output the report to tone of the above four destinations.
- Click **Apply to DB** to apply field changes to the layout.

### Flushing Temporary Cash Tables

For layout key CSH, you have the option of flushing temporary cash tables. You may be using maps to receive cash receipts, which the Data Import Utility uses to populate temporary cash tables. Unless you flush them out, those temporary cash tables remain in the database. Allowing those tables to remain may result in inconsistent performance and unexpected error messages. It's a good idea to flush your temporary cash tables from time to time.

- From the Options menu, select **Flush Temporary Cash Tables** or click the **Flush Temporary Cash Tables** button on the main toolbar

A dialog box appears, asking you if you are sure.

- Click **Yes** to continue.

VISUAL completes the flushing and returns a dialog box telling you the tables are now clean.

# Starting the Data Import Exchange Window

The Data Import Exchange window is the application that does the actual importing or exporting of data. Data Import Exchange imports/exports the data to and from the .VDI files.

To start the Data Import Exchange window:

Select **VMDI Exchange** from the EDI menu.

## Importing and Exporting Data In the Data Import Exchange Window

Use the Data Import Exchange window to import or export data to and from the VISUAL database.

If VISUAL encounters closed or cancelled orders during the import process, the import utility adds the order using the next available number and notifies you that it found a duplicate. You can view the details of the transaction by viewing the log.

- In the Key/Version fields, specify which key (ACK, ASN, CPO, CSH, INV, PLN, RCA, VPO, or WSA) and layout version number to use (defined using VMDIGEN.EXE).

When you specify the key and version, VISUAL selects the appropriate radio button from the Direction section. The data is either incoming or outgoing. VISUAL also determines the file type, the inbound or outbound rule, and the file style for the data (previously defined using VMDIGEN.EXE).

**Note:** If you select CPO, you can optionally select the **Close Partial Shipped Schedules**, and **Include Today in Proration** check boxes. You cannot manually override the **Delete Unshipped Lines** or **Delete Unshipped Schedules** options using VMDIXCHG.EXE. You must make changes using VMDIGEN.EXE.

If you do not want to delete lines shipped at zero quantity, add SuppressZeroLine to the \[Shipping\] section of Preference Maintenance and specify a value of N.

If you select PLN, you can optionally change the Include Today in Proration option.

- In the Directory field, specify the target directory for the .VDI file.

For an inbound file, this is the location in which VISUAL will find the .VDI file; for an outbound file, this is the location in which VISUAL will create the .VDI file(s). VISUAL exchanges each key and version separately.

VISUAL updates the run date for the selected .VDI layout after each exchange; it appears in the Run Date field. Normally, data is only exported once. The Data Import Exchange window only exports data (A/R Invoices and ASN's) with a date equal to or later than the Run date of the .VDI layout. You can enter an earlier date/time stamp to re-export data, but use caution. Using an earlier run date will resend all data again from the specified date and later, which could mean resending duplicates.

- Click on the **Exchange** button on the main toolbar or select **Exchange** from the File menu.

If there are any critical errors while VISUAL is importing data, a message box that describes the error appears. VISUAL also generates a log file that contains more information about critical errors and information about less critical errors.

The log file lists each record that has an error and the error message; and shows each column with the data that VMDIXCHG was trying to write. The log file is located in the same user specified directory and has the same name as the .VDI file with a .log extension.

You should check the log file after each exchange.

### Examining Log Files After Importing and Exporting

Four different logs are generated by VMDIXCHG for various keys.

#### Error Logs

This log is generated when importing CPO, CSH, and PLN documents and is very useful when debugging initial import problems. This log will only contain errors (CPO documents rejected due to match criteria will not appear in this log). The log is named for the key and layout (for example, the error log for CPO layout 0001 would be called CPO0001.log). The log is overwritten each time this layout is run.

An example log follows.

&lt;LogFile&gt;

&lt;LogFileHeader&gt;

&lt;LogFileType&gt; Error Log

&lt;/LogFileType&gt;

&lt;LogFileDate&gt;

5/1/00 9:39:21:000000 AM

&lt;/LogFileDate&gt;

&lt;LayoutKey&gt; CPO

&lt;/LayoutKey&gt;

&lt;LayoutVersion&gt; 0001

&lt;/LayoutVersion&gt;

&lt;Version&gt; 0001

&lt;/Version&gt;

&lt;/LogFileHeader&gt;

&lt;VMDIErrorLog3&gt;

\====================================

Data error encountered in column CUSTOMER_ORDER.SHIP_TO_ADDR_NO during import

LCUSTOMER_ORDER.ID=GE90C12064001 CUSTOMER_ORDER.CUSTOMER_ID=20000818 CUSTOMER_ORDER.CUSTOMER_PO_REF=000000004200000 CUSTOMER_ORDER.SHIP_TO_ADDR_NO=?

CUSTOMER_ORDER.ORDER_DATE= CUSTOMER_ORDER.DESIRED_SHIP_DATE= CUSTOMER_ORDER.STATUS= CUSTOMER_ORDER.EDI_FLAG= CUSTOMER_ORDER.ENTITY_ID= CUSTOMER_ORDER.BACK_ORDER= CUSTOMER_ORDER.POSTING_CANDIDATE= CUSTOMER_ORDER.MARKED_FOR_PURGE= CUSTOMER_ORDER.SELL_RATE= CUSTOMER_ORDER.BUY_RATE= CUSTOMER_ORDER.CONTACT_HONORIFIC= CUSTOMER_ORDER.CONTACT_FIRST_NAME= CUSTOMER_ORDER.CONTACT_INITIAL= CUSTOMER_ORDER.CONTACT_LAST_NAME= CUSTOMER_ORDER.CONTACT_SALUTATION= CUSTOMER_ORDER.CONTACT_PHONE= CUSTOMER_ORDER.CONTACT_FAX= CUSTOMER_ORDER.CONTACT_POSITION= CUSTOMER_ORDER.FREE_ON_BOARD= CUSTOMER_ORDER.SHIP_VIA= CUSTOMER_ORDER.SALESREP_ID= CUSTOMER_ORDER.TERRITORY= CUSTOMER_ORDER.TERMS_NET_TYPE= CUSTOMER_ORDER.TERMS_NET_DAYS=

CUSTOMER_ORDER.TERMS_NET_DATE= CUSTOMER_ORDER.TERMS_DISC_TYPE= CUSTOMER_ORDER.TERMS_DISC_DAYS= CUSTOMER_ORDER.TERMS_DISC_DATE= CUSTOMER_ORDER.TERMS_DISC_PERCENT= CUSTOMER_ORDER.TERMS_DESCRIPTION= CUSTOMER_ORDER.DISCOUNT_CODE= CUSTOMER_ORDER.SALES_TAX_GROUP_ID= CUSTOMER_ORDER.FREIGHT_TERMS= CUSTOMER_ORDER.CURRENCY_ID= CUSTOMER_ORDER.CONTACT_MOBILE= CUSTOMER_ORDER.CONTACT_EMAIL= CUSTOMER_ORDER.EDI_BLANKET_FLAG= CUSTOMER_ORDER.EXCH_RATE_FIXED=

&lt;/VMDIErrorLog3&gt;

&lt;VMDIErrorLog3&gt;

\====================================

#### Successful Transactions (Good) Logs

This log is generated when importing CPO, CSH, and PLN documents and lists every record imported for the run. This log will not contain errors (they are in the error log). The log is named for the key and layout (for example, the good log for CPO layout 0001 would be called CPO0001G.log). The log is overwritten each time this layout is run.

An example log follows:

&lt;LogFile&gt;

&lt;LogFileHeader&gt;

&lt;LogFileType&gt;

Successful Transactions Log

&lt;/LogFileType&gt;

&lt;LogFileDate&gt;

5/1/00 9:39:21:000000 AM

&lt;/LogFileDate&gt;

&lt;LayoutKey&gt; CPO

&lt;/LayoutKey&gt;

&lt;LayoutVersion&gt; 0001

&lt;/LayoutVersion&gt;

&lt;Version&gt; 0001

&lt;/Version&gt;

&lt;/LogFileHeader&gt;

&lt;VMDIGoodLog&gt;

++++++++++++++++++++++++++++++++++++++++

The following record was updated. Record number: 1

CUSTOMER_ORDER.ID=R 0702210 CUSTOMER_ORDER.CUSTOMER_ID=ABLMAN CUSTOMER_ORDER.CUSTOMER_PO_REF=UMWX73801 CUSTOMER_ORDER.SHIP_TO_ADDR_NO=1 CUSTOMER_ORDER.FREE_ON_BOARD=NASHUA CUSTOMER_ORDER.SHIP_VIA=FEDEX CUSTOMER_ORDER.ORDER_DATE=12/15/00 12:00:00:000000 AM

CUSTOMER_ORDER.DESIRED_SHIP_DATE=12/15/00 12:00:00:000000 AM CUSTOMER_ORDER.STATUS=R

CUSTOMER_ORDER.EDI_FLAG=Y CUSTOMER_ORDER.SHIPTO_ID=CPO1 CUSTOMER_ORDER.ENTITY_ID=MMC CUSTOMER_ORDER.BACK_ORDER=N CUSTOMER_ORDER.POSTING_CANDIDATE=N CUSTOMER_ORDER.MARKED_FOR_PURGE=N CUSTOMER_ORDER.SELL_RATE=1.0 CUSTOMER_ORDER.BUY_RATE=1.0 CUSTOMER_ORDER.CONTACT_HONORIFIC=Mr. CUSTOMER_ORDER.CONTACT_FIRST_NAME=David CUSTOMER_ORDER.CONTACT_INITIAL=J. CUSTOMER_ORDER.CONTACT_LAST_NAME=Brown

CUSTOMER_ORDER.CONTACT_SALUTATION=Dear Mr. Brown: CUSTOMER_ORDER.CONTACT_PHONE=\[617\] 444-7000

CUSTOMER_ORDER.CONTACT_FAX=\[617\] 444-7011

CUSTOMER_ORDER.CONTACT_POSITION=General Manager CUSTOMER_ORDER.SALESREP_ID=MARK CUSTOMER_ORDER.TERRITORY=NEW ENGLAND CUSTOMER_ORDER.TERMS_NET_TYPE=A CUSTOMER_ORDER.TERMS_NET_DAYS=30 CUSTOMER_ORDER.TERMS_NET_DATE= CUSTOMER_ORDER.TERMS_DISC_TYPE=A CUSTOMER_ORDER.TERMS_DISC_DAYS=10 CUSTOMER_ORDER.TERMS_DISC_DATE= CUSTOMER_ORDER.TERMS_DISC_PERCENT=2.0 CUSTOMER_ORDER.TERMS_DESCRIPTION= CUSTOMER_ORDER.DISCOUNT_CODE=WHOLESALE CUSTOMER_ORDER.SALES_TAX_GROUP_ID=MA CUSTOMER_ORDER.FREIGHT_TERMS=P CUSTOMER_ORDER.CURRENCY_ID=(USD) \$ CUSTOMER_ORDER.CONTACT_MOBILE= CUSTOMER_ORDER.CONTACT_EMAIL= CUSTOMER_ORDER.EDI_BLANKET_FLAG=N CUSTOMER_ORDER.EXCH_RATE_FIXED=N CUSTOMER_ORDER.SEND_ACK=Y

&lt;/VMDIGoodLog&gt;

&lt;VMDIGoodLog&gt;

++++++++++++++++++++++++++++++++++++++++

The following record was updated. Record number: 1

CUST_ORDER_BINARY.BITS=R 0701743 00000701743 NEW CRIB #: 40CR93 CUST_ORDER_BINARY.BITS_LENGTH=41

&lt;/VMDIGoodLog&gt;

&lt;VMDIGoodLog&gt;

++++++++++++++++++++++++++++++++++++++++

The following record was updated. Record number: 1

Cust Order Line No 1 for Cust Order ID R 0702210 was updated. CUST_ORDER_LINE.LINE_NO=1 CUST_ORDER_LINE.PART_ID=0038860967 CUST_ORDER_LINE.UNIT_PRICE=2.5 CUST_ORDER_LINE.USER_ORDER_QTY=190.0 CUST_ORDER_LINE.SELLING_UM=10X10_SHT CUST_ORDER_LINE.DESIRED_SHIP_DATE=11/22/99 12:00:00:000000 AM CUST_ORDER_LINE.MISC_REFERENCE=0038860967 CUST_ORDER_LINE.SHIPTO_ID=

CUST_ORDER_LINE.LINE_STATUS=A CUST_ORDER_LINE.ORDER_QTY=190.0 CUST_ORDER_LINE.PRODUCT_CODE= CUST_ORDER_LINE.COMMODITY_CODE=STEEL CUST_ORDER_LINE.TRADE_DISC_PERCENT=15.0 CUST_ORDER_LINE.GL_REVENUE_ACCT_ID= CUST_ORDER_LINE.COMMISSION_PCT=15.0 CUST_ORDER_LINE.ACK_ID= CUST_ORDER_LINE.SEND_ACK=

&lt;/VMDIGoodLog&gt;

&lt;VMDIGoodLog&gt;

++++++++++++++++++++++++++++++++++++++++

The following record was updated. Record number: 1 CUST_LINE_BINARY.BITS=D CUST_LINE_BINARY.BITS_LENGTH=2

&lt;/VMDIGoodLog&gt;

&lt;VMDIGoodLog&gt;

++++++++++++++++++++++++++++++++++++++++

#### Insert/Update Logs

This log is generated when importing CPO, CSH, and PLN documents and lists every imported or updated document from the run. This log will not contain errors (they are in the error log). The log also shows any records deleted by the user having marked either the "Delete unshipped lines" or "Delete unshipped schedules" check boxes. The log is named for the key and layout (for example, the insert/ update log for CPO layout 0555 would be called CPO0555IN.log). The log is overwritten each time this layout is run.

An example log follows.

&lt;LogFile&gt;

&lt;LogFileHeader&gt;

&lt;LogFileType&gt; Insert Update Log

&lt;/LogFileType&gt;

&lt;LogFileDate&gt;

4/12/00 2:30:18:000000 PM

&lt;/LogFileDate&gt;

&lt;LayoutKey&gt; CPO

&lt;/LayoutKey&gt;

&lt;LayoutVersion&gt; 0555

&lt;/LayoutVersion&gt;

&lt;Version&gt; 0555

&lt;/Version&gt;

&lt;/LogFileHeader&gt;

&lt;VMDIInsertLog&gt;

\====================================

Customer Order Z05550010 inserted

&lt;/VMDIInsertLog&gt;

&lt;VMDIInsertLog&gt;

\====================================

Customer Order Z05550011 inserted

&lt;/VMDIInsertLog&gt;

&lt;VMDIInsertLog&gt;

\====================================

Customer Order Z05550012 inserted

&lt;/VMDIInsertLog&gt;

&lt;VMDIInsertLog&gt;

\====================================

Customer Order Z05550013 inserted

&lt;/VMDIInsertLog&gt;

&lt;VMDIInsertLog&gt;

\====================================

Customer Order Z05550014 inserted

&lt;/VMDIInsertLog&gt;

&lt;/LogFile&gt;

#### Autogenerate Logs

VMDI generates this log when importing CPO documents with the Autogenerate feature. This log is a copy of the actual CPO VDI file, except the Autogenerate Tokens have been replaced with the actual customer order Ids created in the system. The log is named CPO and the layout (for example, the Autogenerate log for CPO layout 0025 would be called CPO0025.AUTO). The log is overwritten each time this layout is run. An example VDI file and log are shown below. Notice how the token AUTO is replaced with the actual customer order ID and that the number is incremented for second order (02006 for first order and 02007 for second):

![](data:image/png;base64,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)![](data:image/png;base64,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)

### Setting Session ID Preferences

You can establish exchange log file naming preferences by using the Session ID option.

- From the Options menu, select **Session ID**, or click the **Session ID** button on the main toolbar.
- The Session ID Settings dialog box appears.
- Select the **Use Session ID** check box to specify a Session ID for the Data Import Utility exchange logs.

The Next Session and Prefix data fields are now active.

- Enter the number for the next session log. VISUAL automatically increments the number as you conduct exchanges. To use a different number, enter it in the data field.

Enter a suffix if you want the exchange log file to have a special ending. For example, a next session log of 6 and a suffix or VE, produces a log file (Key)6VM.log.

- Select the **prefix KEY** check box to attach the Key/Version (one of seven) of the exchange to the front of the log file.

Selecting this makes the Prefix data field unavailable. For example, setting preferences to a next session log of 7, a suffix of VE, and a KEY of ACK, leads to VISUAL producing the log ACK7VM.log.

## Running the Data Interchange Utility in Command Line Mode

There are two modes of operation for VMDIXCHG: Standard and Command Line mode. Standard mode requires input from the user; Command line mode does not. If VMDIXCHG finds a valid command line, it executes in command line mode. Command line mode allows you to specify the run options currently available through the main screen.

When you run the program in command line mode, you use a cumulative log file, VMDIXCHG.LOG. Each time you execute the program, VMDIXCHG appends time stamped status information to the end of the file.

The VMDIXCHG.LOG file resides in the same directory as all other \*.VDI and \*.LOG files. When you run the program in batch mode, you can exchange multiple types of data (i.e. PLN, VPO, etc.) in the same run.

VMDIXCHG.APP

Command Line:

The command line must conform to the this format:

The first value is always the EXE name, VMDIXCHG.

The next three values are the database connection strings. The data base connection strings follow standard VISUAL format:

\-D &lt;database name&gt;

\-U &lt;User ID&gt;

\-P &lt;password&gt;

All command line arguments up to this point are mandatory. The remaining arguments are optional. If you do not specify a particular argument, VMDIXCHG applies a default value where applicable.

Exchange File Path:

\-F &lt;file path&gt;

If you omit the -F argument, VMDIXCHG uses the file path specified in the DataFilePath row in the EDI EXCHANGE section of Preferences Maintenance. You should only use the -F option if you need to temporarily override the default directory path.

Next, list the types of values to exchange. You may list more than one type. Keep in mind that you must separate additional values with a space.

The valid types are:

- ACK
- ASN
- CPO
- CSH
- INV
- PLN
- RCA
- VPO
- WSA

You must specify at least one valid data type, each of which must have a 4 digit unique identifier after it. The data type / numeric identifier combination equates to the KEY and VERSION columns of the VMDI_LAYOUT table.

Inbound Rule:

VMDIGEN establishes the inbound rule. You cannot change it from within VMDIXCHG. Therefore, you must specify inbound types by their KEY/VERSION (i.e. CPO8888).

**Note:** Inbound rule parameters are only applicable for inbound types such as CPO, CSH, and PLN. Outbound Rule:

The next argument specifies whether you want VMDIXCHG to either append the outbound data to existing output files, or create a new file each time. Specify -A to append the file, or -O to overlay the file. If you omit this argument, the default is set to overlay the file.

**Note:** Outbound rule parameters are only applicable for outbound types such as ACK, ASN, INV, RCA, VPO, and WSA.

This example illustrates a command line that executes VMDIXCHG connected to database VMFGDEMO as user MIKE and password OCEAN, and executes the exchanges for CPO0000, INV9999 and ASN3333. VMDIXCHG overrides the EDI file directory for this run with C:\\Infor\\EDI\\TEMP. In addition, the INV output file is overlaid and the ASN output file is appended onto it.

**VMDIXCHG -D VMFGDEMO -U MIKE -P OCEAN -F C:\\Infor\\EDI\\TEMP CPO0000 INV9999 -O ASN333 -A**

Because the program is running without user intervention, you cannot change the run date parameter. The run "as of date" for each data type that you are exchanging is from the LAST_RUN_DATE column of the VMDI_LAYOUT table for each layout key.

VMDIXCHG.LOG

This example illustrates the log file created when you run the program in batch mode: 8/21/02 11:07:13: \*\*\*\*\*\*\*\*\*\* Session Started \*\*\*\*\*\*\*\*\*\*

8/21/02 11:07:13: Begin program execution in command line mode. ExtCommandLine=-DVMFG52 - USYSADM -PSYSADM PLN0000

8/21/02 11:34:20: Import of PLN0000 data to c:\\Infor\\bak\\edi\\pln0000.vdi has begun 8/21/02 11:34:23: 60 PLN records imported

8/21/02 11:34:24: ----- Session Ended -----

## Integration Requirements

This section relates to the integration of EDI data into and out of the VISUAL database. For Harbinger and Sterling Commerce, you may choose to have Infor Global Solutions design and create your integration maps or you may choose to develop your own maps. For other EDI translators, you need to develop any necessary "maps" to and from that software in the format that the Data Interchange module produces and reads.

These sections describe the modules and EDI transactions that can currently be integrated. Additional EDI transactions are handled directly through the EDI translation software in standalone mode.

### Customer Order Information (Inbound)

This is typically the most complex transaction to integrate because of the vast differences in how you and your trading partners may use the transaction data.

You need to consider several factors:

- Which inbound transaction(s) contain customer order information? (850, 860, 830, 862, etc.)
- Is the trading partner's VISUAL Customer ID present in the EDI data?
- Is there a Ship to location that applies to the entire order or does each line item have a separate Ship to location?
- Are the VISUAL part numbers present in the EDI data?
- Do you want to accept the pricing as it comes in should VISUAL override it?

## EDI Transaction 850 - Purchase Order

The purchase order is the most widely used commercial EDI transaction. While the information contained in a purchase order may vary in content and format from company to company, there are pieces of information that are common to every PO:

- Order number and date
- Buyer's Company ID
- Ship-to location
- Requested delivery date
- One of more line items containing quantity ordered, unit of measure, unit price, and a part number or description

Other transactions may contain customer order information, such as the PO Change (860) or Material Release (830). This information can also be brought into VISUAL through integration.

### Planning Information (Inbound)

This is typically the simplest transaction to implement. When planning information is imported, the CUSTOMER_ FORECAST table is populated in VISUAL database. These records contain a CUSTOMER ID, FORECAST ID, FORECAST DATE, PART NUMBER, and QUANTITY.

### EDI Transaction 830 - Planning Schedule

The planning schedule is most typically used to transfer forecasting/material release information. You can use this transaction in a variety of ways, such as:

- a simple forecast
- a forecast with the buyer's authorization for the seller to commit to resources, such as labor or material
- an order release mechanism, containing such elements as resource authorizations, period-to- date cumulative quantities, and specific ship/delivery patterns. The order release forecast may also contain all data related to purchase orders, eliminating the need for discrete generation of purchase orders.

For inbound planning and customer order information, successful integration often depends on having raw EDI data (sample or production) from the trading partner. This data is necessary in order to determine the absolute segment and element location of each required piece of data.

The EDI requirements or "mappings" that a trading partner are sometimes inaccurate or outdated. Because the integration maps are dependent on the format of the data, changes or discrepancies may cause implementation delays. This is why sample data is so critical to a smooth and timely integration process. Accurate data not only eliminates the need for modifications to the maps, but is also essential for testing the maps prior to delivery to the customer.

### Shipping Information (Outbound)

Shipping data is extracted from VISUAL to one or more fixed length files. The files contain the BOL information, the order or packlist information, and the line item information for each order. The details of each file are determined by the requirements of your trading partner.

### EDI Transaction 856 - Advance Shipment Notice

The ship notice lists the contents of a shipment and its configuration within the shipment container at various levels of detail. ASN's are used extensively in the retail and automotive industries, where Just-In-Time (JIT) and Efficient Consumer Response (ECR) are an integral part of the management philosophy.

A typical ASN has this information:

- ASN number and date
- Shipment identification number (SID)
- Date and time of the shipment
- Bill of lading (BOL) number
- Number of packages
- Total shipment weight
- Product number and quantity of each item shipped (SKU)

### Invoice Information (Outbound)

Invoice data is extracted from VISUAL to one or more fixed length files. The files contain the Invoice header information, the line item information, and (when applicable) subline item information. The details of each file are determined by the requirements of your trading partner.

### EDI Transaction 810 - Invoice

The invoice is the second most widely used EDI transaction set. It is a natural extension of the purchase order and can generally be implemented at the same time. In most cases, an invoice simply repeats much of the information contained in the purchase order, while adding invoice related information such as:

- Invoice number and date
- Quantity shipped and invoiced amount for each item
- Total invoice amount
- Payment terms

## A Glossary of EDI Terms

**Accredited Standards Committee X12 (ASC X12)** \- The committee that defines the structure of EDI Transaction Sets in the United States.

**Data Element** \- The smallest unit of information in the standard, representing a single piece of information. According to the standards, each data element is assigned a reference number, a name, description, data type and the min/max length.

**EDI** \- Electronic Data Interchange. The exchange of commonly used business transactions in a formal, structured manner.

**EDIFACT** \- An EDI Standard developed in Europe and typically used outside the United States.

**Forms Overlays** \- Forms that are used in STX for Windows to perform data entry or printing of EDI data in a "user-friendly" format.

**Functional Group** \- As part of an Interchange, a Functional Group consists of a GS Header Segment, one or more Transaction Sets of the same type, and a GE Trailer Segment.

**Interchange** \- A group of one or more types of documents that are bound for transmission to a single trading partner.

**Log-ons** \- A communications script that enables the STX software to connect to a VAN, Internet, or directly to a TP (if available) in order to transfer and receive EDI documents.

**Maps or File Overlays** \- Maps link the STX software to the Data Import Utility module that connects to the VISUAL database. In order to integrate STX and VISUAL, one map must be created for each trading partner/transaction set combination.

**Integration** \- The "sharing" of data between applications through application files to eliminate the need for repetitive data-entry and reduce errors.

**Segment** \- A segment consists of a segment identifier, one or more related data elements in a defined sequence, and a segment terminator.

**Trading Partner** \- Someone with whom you trade electronic documents.

**Transaction Set** \- An electronic document whose format is defined by ASC X12. A Transaction Set consists of an ST Header Segment, one or more data segments in a specific order, and an SE Trailer Segment. Each set is assigned a numeric identifier. (e.g. 810 = an invoice)

**Value Added Network (VAN)** \- An "electronic post office" whose responsibilities include providing a "mailbox" to which your partners send your documents, and to collect and distribute the documents that you send.

## ANSI X12 EDI Transaction Sets

The Data Interchange Standards Association (DISA) and UN/EDIFACT have defined several hundred EDI transaction sets. Below is a comprehensive list of the ANSI/X12 Transaction Sets.

104 - Air Shipment Information

110 - Air Freight Details and Invoice

125 - Multilevel Railcar Load Details

126 - Vehicle Application Advice

127 - Vehicle Baying Order

128 - Dealer Information

129 - Vehicle Carrier Rate Update

130 - Student Educational Record (Transcript)

131 - Student Educational Record (Transcript) Acknowledgment 135 - Student Loan Application

139 - Student Loan Guarantee Result

140 - Product Registration

141 - Product Service Claim Response

142 - Product Service Claim

143 - Product Service Notification

144 - Student Loan Transfer and Status Verification

146 - Request for Student Educational Record (Transcript)

147 - Response to Request for Student Educational Record (Transcript)

148 - Report of Injury or Illness

151 - Electronic Filing of Tax Return Data Acknowledgment

152 - Statistical Government Information 154 - Uniform Commercial Code Filing 161 - Train Sheet

170 - Revenue Receipts Statement

180 - Return Merchandise Authorization and Notification 186 - Laboratory Reporting

190 - Student Enrollment Verification 196 - Contractor Cost Data Reporting

204 - Motor Carrier Shipment Information

210 - Motor Carrier Freight Details and Invoice

213 - Motor Carrier Shipment Status Inquiry

214 - Transportation Carrier Shipment Status Message

217 - Motor Carrier Loading and Route Guide

218 - Motor Carrier Tariff Information

250 - Purchase Order Shipment Management Document

251 - Pricing Support

260 - Application for Mortgage Insurance Benefits

263 - Residential Mortgage Insurance Application Response

264 - Mortgage Loan Default Status

270 - Health Care Eligibility/Benefit Inquiry

271 - Health Care Eligibility/Benefit Information

272 - Property and Casualty Loss Notification

276 - Health Care Claim Status Request

277 - Health Care Claim Status Notification 290 - Cooperative Advertising Agreements

300 - Reservation (Booking Request) (Ocean)

301 - Confirmation (Ocean)

303 - Booking Cancellation (Ocean)

304 - Shipping Instructions

309 - U.S. Customs Manifest

310 - Freight Receipt and Invoice (Ocean)

311 - Canadian Customs Information

312 - Arrival Notice (Ocean)

313 - Shipment Status Inquiry (Ocean) 315 - Status Details (Ocean)

317 - Delivery/Pickup Order 319 - Terminal Information

322 - Terminal Operations Activity (Ocean)

323 - Vessel Schedule and Itinerary (Ocean)

324 - Vessel Stow Plan (Ocean)

325 - Consolidation of Goods in Container

326 - Consignment Summary List

350 - U.S. Customs Release Information

352 - U.S. Customs Carrier General Order Status

353 - U.S. Customs Events Advisory Details

354 - U.S. Customs Automated Manifest Archive Status

355 - U.S. Customs Manifest Acceptance/Rejection

356 - Permit To Transfer Request

361 - Carrier Interchange Agreement (Ocean) 404 - Rail Carrier Shipment Information

410 - Rail Carrier Freight Details and Invoice 414 - Rail Carrier Settlements

417 - Rail Carrier Waybill Interchange

418 - Rail Advance Interchange Consist

419 - Advance Car Disposition

420 - Car Handling Information

421 - Estimated Time of Arrival and Car Scheduling

422 - Shipper's Car Order

425 - Rail Waybill Request

426 - Rail Revenue Waybill

429 - Railroad Retirement Activity 431 - Railroad Station Master File 440 - Shipment Weights

466 - Rate Request

468 - Rate Docket Journal Log 485 - Ratemaking Action

490 - Rate Group Definition 492 - Miscellaneous Rates 494 - Scale Rate Table

511 - Requisition

517 - Material Obligation Validation 527 - Material Due-In and Receipt 536 - Logistics Reassignment

561 - Contract Abstract

567 - Contract Completion Status

568 - Contract Payment Management Report

601 - Shipper's Export Declaration

602 - Transportation Services Tender 622 - Intermodal Ramp Activity

805 - Contract Pricing Proposal

806 - Project Schedule Reporting

810 - Invoice

811 - Consolidated Service Invoice/Statement

812 - Credit/Debit Adjustment

813 - Electronic Filing of Tax Return Data

815 - Cryptographic Service Message

816 - Organizational Relationships

818 - Commission Sales Report

819 - Operating Expense Statement

820 - Payment Order/Remittance Advice

821 - Financial Information Reporting

822 - Customer Account Analysis

823 - Lockbox

824 - Application Advice

- \- Tax Information Reporting
- \- Financial Return Notice
- \- Debit Authorization
- \- Payment Cancellation Request
- \- Planning Schedule with Release Capability
- \- Application Control Totals
- \- Price/Sales Catalog
- \- Residential Mortgage Credit Report Order
- \- Benefit Enrollment and Maintenance
- \- Health Care Claim Payment/Advice
- \- Contract Award
- \- Health Care Claim
- \- Trading Partner Profile
- \- Project Cost Reporting
- \- Request for Quotation
- \- Specifications/Technical Information
- \- Nonconformance Report
- \- Response to Request for Quotation
- \- Product Transfer Account Adjustment
- \- Price Authorization Acknowledgment/Status
- \- Inventory Inquiry/Advice
- \- Material Claim
- \- Material Safety Data Sheet
- \- Response to Product Transfer Account Adjustment
- \- Purchase Order
- \- Asset Schedule
- \- Product Activity Data
- \- Routing and Carrier Instruction
- \- Shipment Delivery Discrepancy Information
- \- Purchase Order Acknowledgment
- \- Ship Notice/Manifest
- \- Shipment and Billing Notice
- \- Shipment Information
- \- Freight Invoice
- \- Purchase Order Change Request - Buyer Initiated
- \- Receiving Advice/Acceptance Certificate
- \- Shipping Schedule
- \- Report of Test Results
- \- Text Message
- \- Purchase Order Change Acknowledgment/Request - Seller Initiated
- \- Production Sequence
- \- Product Transfer and Resale Report
- \- Electronic Form Structure
- \- Order Status Inquiry
- \- Order Status Report

872 - Residential Mortgage Insurance Application

875 - Grocery Products Purchase Order

876 - Grocery Products Purchase Order Change

878 - Product Authorization/Deauthorization

879 - Price Change

880 - Grocery Products Invoice

882 - Direct Store Delivery Summary Information 888 - Item Maintenance

889 - Promotion Announcement

893 - Item Information Request

894 - Delivery/Return Base Record

895 - Delivery/Return Acknowledgment or Adjustment

896 - Product Dimension Maintenance

920 - Loss or Damage Claim - General Commodities

924 - Loss or Damage Claim - Motor Vehicle

925 - Claim Tracer

926 - Claim Status Report and Tracer Reply 928 - Automotive Inspection Detail

940 - Warehouse Shipping Order

943 - Warehouse Stock Transfer Shipment Advice

944 - Warehouse Stock Transfer Receipt Advice

945 - Warehouse Shipping Advice

947 - Warehouse Inventory Adjustment Advice 980 - Functional Group Totals

990 - Response to a Load Tender

996 - File Transfer

997 - Functional Acknowledgment

998 - Set Cancellation EDI Requirements Worksheet

## Tips for Successful Importing and Exporting

Use these tips and hints when importing or exporting EDI information.

### Customer Orders (CPO)

The EDI Release Flag, located under the EDI tab in the Customer Order Entry window, is key to the successful export of invoices (INV) and Advance Ship Notices (ASN). You can activate the flag using several methods:

You can manually mark the flag on the customer order at some point prior to exporting the ASN or INV.

You can use Customer Maintenance (under the E-Commerce tab) to specify the customer as an EDI Trading Partner. Doing this ensures that all future orders will have the EDI flag selected.

You can establish your CPO import file so the EDI Release Flag field is "Y" for all incoming orders.

### Advance Ship Notices (ASN)

Before you can export ship notices using the Data Import Utility, the EDI Release Flag must be set for customer orders. The EDI Release check box is located in the EDI tab of the Customer Order Entry window. You can select the check box any time prior to exporting data using VMDIXCHG. After you have entered the customer orders in VISUAL, ship them using Shipping Entry.

After Shipping the customer order and creating packlists, select **Print Bill of Lading** from the Shipping Entry File menu to create a BOL. Saving the Bill of Lading prompts VISUAL to generate a BOL number. At this point, you can launch an ASN layout in VMDIXCHG for this customer and you should be able to successfully export the ASN document. Failure to either mark the customer order's EDI Release flag in Customer Order Entry or create a BOL in Shipping Entry, prevents the ASN documents from exporting.

**Note:** VMDIXCHG ignores any shipping notices you may create from orders that don't have the EDI Release flag set. You can set the flag after you have shipped the orders and VMDIXCHG will then examine the documents runtime, assuming the date/time and Association requirements are also correct.

For example, if you are sending out Ship Notices to Able Manufacturing, VMDIXCHG searches for documents with a Customer ID of ABLMAN, the EDI Release Flag set, have a BOL, and meet the date/time requirement.

Launch VMDIXCHG for the proper layout (for example, ASN0012 for Able Manufacturing 856 documents) and verify the date on which to start exporting data. For example, if today is 5/12/99 and you enter 5/10/99 on the Run Date line, then VMDIXCHG will export all valid documents from 5/10 through today. Valid documents are those that have the EDI Release Flag set on them and for which you have created BOLs.

### Invoices (INV)

Before you can export ship notices using the Data Import Utility, the EDI Release Flag must be set for customer orders. The EDI Release checkbox is located in the EDI tab of the Customer Order Entry window. You can select the checkbox any time prior to exporting data using VMDIXCHG. After you have entered the customer orders in VISUAL, ship them using Shipping Entry, and invoice them using the Invoice Forms option available from the Reports menu. At this point, the orders have been entered, shipped, and invoiced in VISUAL. Now you need to export the invoices using VMDIXCHG.

**Note:** VMDIXCHG ignores any shipping notices you may create from orders that don't have the EDI Release flag set. You can set the flag after you have shipped the orders and VMDIXCHG will then examine the documents runtime, assuming the date/time and Association requirements are also correct.

For example, if you are sending out Invoices to Able Manufacturing, VMDIXCHG searches for documents with a Customer ID of ABLMAN with the EDI Release Flag set, that you have shipped using Shipping Entry, invoiced using Invoice Forms, and meet the date/time requirement (see below).

Launch VMDIXCHG for the proper layout (for example, INV0012 for Able Manufacturing 810 documents) and verify the date on which to start exporting data. For example, if today is 5/12/02 and you enter 5/10/02 on the Run Date line, then VMDIXCHG will export all valid documents from 5/10 through today. Valid documents are those that have the EDI Release Flag set on them, and you have shipped using Shipping Entry, and invoiced using Invoice Forms.

### Forecasts (PLN)

Follow this procedure to transfer customer forecast (planned data) information that you have imported from an EDI 830 transaction to the Master Production Schedule as a part forecast.

- From the main menu, select **Tools**, then **Material Planning Window**. The Material Planning Window appears.
- From the File menu, select **Master Production Schedule**.
- Click **Customer Forecasts**.

The Transfer Customer Forecasts to Part Forecasts dialog box appears.

- Select the **Import to Both** option button.
- Enter a Forecast ID and select the **Purge prior data** and **Bucketless import** check boxes. Results appear in the Master Production Schedule. Enter the Part ID to view.
- To edit the forecast, either make revisions to the Master Production Schedule or click the **Edit Customer Forecasts** button at the bottom of the dialog box and do the editing there.

For more information on customer forecasts, refer to the "Material Planning Window" chapter.

### Special Notes

If you import an EDI 830 document into VISUAL as a customer forecast, VISUAL Quality only imports the 8 fields on the Customer Forecast screen.

The Customer Forecasts screen shows the 8 fields VISUAL Quality imports. They are: Part ID or Customer Part IDCustomer ID

Forecast IDRequired Date (s) Required Quantity(s)PO Reference Forecast DateWarehouse ID

For example, you can create the Forecast ID in the EDI map to be a combination of PO# and Shipto DUNS # (N104). The only two EDI segments VISUAL examines when creating a customer forecast are the LIN and FST.

One approach that works well involves making the Forecast ID the same as the Part ID. By using the Part ID as the Forecast ID, anytime a date or quantity changes for that part, VISUAL updates the forecast rather than adding to it. The only time VISUAL adds to the forecast is when the trading

partner for that part sends a new date. In this map example, the customer originally wanted to use a combination of the EDI 830 Run Date and the Customer ID. However, if the trading partner had changed quantities for previously received dates, they would not send the same run date again. The run date is essentially the date the trading partner sent the EDI 830. Therefore, the entire forecast for that part - and all of the dates included in it - would be inserted into the Customer Forecast table. As a result, each time they sent an 830, VISUAL adds it.

One disadvantage of using the Part ID as the Forecast ID is that if the client decides to use reject duplicates (with the Forecast ID set up as above), then anytime you received another line item for the same part and Customer ID, VISUAL would reject the line's data.

When you run VMDIXCHG on a PLN VDI file, VISUAL enters the data into the Customer_Forecast table. In order to view the forecast data from the Edit Customer Forecasts screen you must first Transfer Customer Forecasts to Part Forecasts. To do that, select the **Import to Both** option button (to import the customer forecast to the forecast and master schedule tables), select the **Purge Prior** and the **Bucket-less Import** check boxes, enter a Forecast ID (appears on the Master Schedule screen), enter EDI-FORECASTS, and click **Begin**.

The Forecast ID displaying on the Master Schedule you imported using the Transfer feature (above) is not the same Forecast ID you imported using the Data Import Utility (shows on the Customer Forecast screen). For example, the Forecast ID you entered on the transfer screen is EDI- FORECASTS, but the actual forecast IDs imported from the Customer_Forecast table might be named EDITESTPO1, EDITESTPO2, and EDITESTPO3.

### Purging Prior: A Few Hints

Choose the same ID every time you perform a transfer (EDI-FORECASTS, for example). When you perform the transfer, VISUAL updates the new Customer_Forecast data. For example, if the qty for 5/ 1/02 for forecast ID EDITESTPO1 is 104 and it is actually 50 in the new data, VISUAL changes it to 50 when you perform the transfer and the master schedule will change to show the proper amounts. If you entered a new Forecast ID (EDI-FORECASTS2, for example) when running the transfer, then VISUAL imports all the contents of the customer_forecast table to the master schedule table.

Essentially, the same data would appear twice in the master schedule (once for the EDI- FORECASTS and once for EDI-FORECASTS2).

### Adding a Trading Partner Name to an ASN (EDI 856)

**Caution:** If you do not perform these steps in the order listed, your joins may be inaccurate and VISUAL could consequently export incorrect data. For example, when performing joins in the Additional Line Item file of an ASN, you could mistakenly export data for ALL packlists in the system if you perform the steps out of order or miss some.

The Trading Partner ID MUST be in the First Record of the First data file (BOL table of HDR ). To do that, follow the steps below.

- From your Windows Start menu, select **Run**.
- Type **SQLTALK**.

SQLTalk Opens.

- Type **Connect VMFGEDI;** &lt;CTRL+ENTER at end of each line&gt;
- Type **Create View View_Shipper As Select \* From Shipper;**
- Type **Create View View_Cust_Order As Select \* From Customer_Order;**
- Type **Create View View_Customer As Select \* From Customer;**
- Type **GRANT ALL ON VIEW_SHIPPER TO PUBLIC;**
- Type **CREATE PUBLIC SYNONYM VIEW_SHIPPER FOR SYSADM.VIEW_SHIPPER;**
- Type **GRANT ALL ON VIEW_CUST_ORDER TO PUBLIC;**
- Type **CREATE PUBLIC SYNONYM VIEW_CUST_ORDER FOR SYSADM.VIEW_CUST_ORDER;**
- Type **GRANT ALL ON VIEW_CUSTOMER TO PUBLIC;**
- Type **CREATE PUBLIC SYNONYM VIEW_CUSTOMER FOR SYSADM.VIEW_CUSTOMER;**
- Type **Commit;**
- Launch VMDIGEN
- Create a new layout.
- Choose fields from default tables listed and save the layout
- Add the View_Shipper table, View_Cust_Order and then the View_Customer table.
- Choose fields from the three new tables.
- Join the BOL table to the View_Shipper table via the BOL ID field.
- Join the View_Shipper to the View_Cust_Order table by linking the Cust_Order_ID field in the View_Shipper table to the ID field in the View_Cust_Order table.
- Join the View_Customer_Order to the View_cust table by linking the Customer_ID field in the View_Cust_Order table to the ID field in the View_Customer Table.
- Save the layout.
- Add additional tables as necessary and repeat steps 16-21 above.

### Adding a Customer as an EDI Customer

This procedure is a shortened version of what appears in the Customer Maintenance chapter. For more information, refer to the "Customer Maintenance" chapter.

- From the Sales menu, select **Customer Maintenance**. The Customer Maintenance window appears.
- Call up the appropriate customer.
- Click the **E-Commerce** tab.
- Select the **EDI Trading Partner** checkbox.
- Click the **Save** button to commit the setting.

With this preference set, VISUAL activates the EDI Release flag for all new orders (both manual and imported via VMDIXCHG) for the customer.

