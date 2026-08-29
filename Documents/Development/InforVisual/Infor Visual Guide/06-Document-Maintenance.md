# Chapter 6: Document Maintenance

This chapter includes this information:

**Topic Page**

[What is Document Maintenance? 6-2](#_bookmark489)

[Opening the Document Maintenance Window 6-4](#_bookmark492)

[Maintaining Document Folders 6-5](#_bookmark499)

[Maintaining Default Reference Types 6-6](#_bookmark500)

[Adding Documents 6-9](#_bookmark501)

[Copying and Moving Documents 6-13](#_bookmark516)

[Referencing Documents 6-15](#_bookmark520)

# What is Document Maintenance?

Sometimes you may have a single document that you associate with several items. For other documents, you may want to associate several documents to one item. Use Document Maintenance to associate your documents with records in VISUAL.

By having a single window in which you can set up all of your documents and using a reference from the appropriate areas in VISUAL, you can better manage your documentation.

## Planning Document Storage

Documents can be stored in centralized directories by reference type, site, entity, tenant, or globally. Use the Document Folder feature to designate the storage folder for documents at each level. [See](#_bookmark499) ["Maintaining Document Folders" on page 6-5.](#_bookmark499)

If you set up specific directories for sites and reference types, we recommend that you use drag-and- drop to add new documents and use the Document ID browse in the Document Reference dialog to attach existing documents to records. This prevents you from accidentally overriding the site and reference type checks that are in place when you: add new documents using drag-and-drop or attach existing documents using Document ID browse in the Document Reference dialog.

This table describes how to achieve centralized directories for each level:

**Document Storage Level**

**How to store a document at this storage level**

Reference Type

For directories by reference type, specify a directory unique for each site or tenant reference type such as, C:\\Infor\\VISUAL\\Docs\\SiteABC\\ReferenceType1 and C:\\Infor\\VISUAL\\Docs\\SiteABC\\ReferenceType2.

Site For a site-level directory not subdivided by reference type, specify the same directory for all of the site's reference types.

For an entity-level folder structure not subdivided by site, specify the same directories for all of the entity's child sites. You can still use reference types.

Entity

Tenant

For example, you could create a directory called C:\\Infor\\VISUAL\\Attachments\\EntityA\\APInvoices and assign the directory to the AP Invoice reference type in each of the entity's child sites.

For tenant-level folders, specify \*\* Tenant \*\* in the Site ID field. You can specify which sites have access to tenant-level documents on a document- by-document basis.

Global For a global document folder, create a folder and then specify it in the Global Document Path.

## Referencing Documents at the Tenant-level and the Site- level

When you attach documents in maintenance windows, the level of the maintenance window must match the level of the document. For example, Vendor Maintenance is a tenant-level application, because you cannot specify a site. If you attach an existing document to a Vendor Maintenance record, you can only attach tenant-level documents. While you can use Document Maintenance to create a site-specific vendor document, you cannot use this document in Vendor Maintenance. Site- specific documents can only be used in site-specific maintenance windows such as WIP Maintenance.

Similarly, when you create a new document attachment through a maintenance window, the level of the new document matches the level of the maintenance window. For example, if you create a new document in Purchase Order Entry, the document is created in the site that you selected in the purchase order header. If you create a new document in Customer Maintenance, the document is created at the tenant level.

Many of the document reference-enabled windows allow document creation at the site level. This table shows the windows where tenant level documents and document references are made:

**Windows Tenant-Level**

Customer Maintenance

Tenant

Vendor Maintenance Tenant

# Opening the Document Maintenance Window

Select **Admin**, **Document Maintenance**.

## Setting Up Document Categories

Use Document Categories to specify what type of document you are entering. For example, to reference Material Specification Documents, you could enter a category named Material Specs.

To set up document categories:

- Click the **Document Category** button on the toolbar.
- Click **Insert**.

The next available row appears highlighted and the cursor appears in the ID column

- Enter a suitable identification for this category.
- Press the TAB key and enter a description for this category.
- Click **Save**.
- Click **Close**.

# Maintaining Document Folders

Use document folders to manage where your documents are stored. You can set up document folders in a variety of ways. [See "Planning Document Storage" on page 6-2.](#_bookmark490)

Document file paths can be up to 255 characters long, except for the Global Document path which is up to 128 characters long.

To maintain document folders:

- In the Document Maintenance window, select **Maintain**, **Document Folders**.
- Specify the Global Document Path by either clicking on the browse folder button, or manually typing the global document path.

If paths by Site and Reference Type are not specified, the Global Document Path is the default path for storing documents.

- To set up paths for site-level documents, select the site from the Site ID drop-down list. To set up paths for tenant-level documents, select \*\* Tenant \*\* from the Site ID drop-down list. If you are licensed to use a single-site, this field is unavailable.
- Click the **Insert Row** toolbar button.
- Click in the Reference Type column of the new row and select from the **Reference Type** drop- down list.
- Click in the Path column of the new row and either manually type the reference type's document path, or double-click the Path column's directory browse button and select the path.
- Click **Save** to set the default file path for your selected Site and Reference Type combination.

# Maintaining Default Reference Types

Use default reference types to define how your documents are stored.

When a user drags-and-drops a new document onto a program's main window or a Document Reference dialog, these settings determine the default reference type. For example, if you specify Customer as the default reference type for Customer Order documents, then new documents that are drag-and-dropped onto the Customer Order Entry window have a Customer reference type. You can change the reference for new dragged-and-dropped documents in the Document Reference dialog or in the New Document dialog. The General reference type is used when no default reference type exists.

To maintain default reference types:

- In the Document Maintenance window, select **Maintain**, **Default Reference Types**.
- In the Site ID field, specify the site where you are setting up default reference types. Select **\*\* Tenant \*\*** to set up default references for tenant-level documents. If you are licensed to use a single-site, this field is unavailable.
- Click the **Insert Row** toolbar button.
- Click in the Document Type column of the new row and select the **Document Type** from the drop- down list.
- Click in the Default Reference Type column of the new row and select the **Default Reference Type** from the drop-down list.

This table lists what Reference types are available based on the selected Document Type:

**Document Type Default Reference Type**

AP Invoice

AR Invoice

Cash Application

Customer

Customer Order

Engineering Change Notice

- AP Invoice
- General
- Vendor
- AR Invoice
- Customer
- General
- AR Invoice
- General
- Customer
- General
- Customer
- Customer Order
- General
- Engineering Change Notice
- General

**Document Type Default Reference Type**

Estimating

General Journal

Operation

Part

Project (Only available when you are licensed for a project database.)

Purchase Contract

Purchase Order

Receiver (Used when adding documents in Purchase Receipt Entry and Receiving Inspection)

Requisition

RFQ

RMA

Service

- - Customer
    - Estimating
    - General
    - General
    - General Journal
    - General
    - Operation
    - Shop Resource
    - Work Order
    - General
    - Part
    - General
    - Project
    - General
    - Purchase Contract
    - General
    - Purchase Order
    - Vendor
    - General
    - Purchase Order
    - Receiver
    - Vendor
    - General
    - Requisition
    - Vendor
    - General
    - RFQ
    - Customer
    - General
    - RMA
    - General
    - Service

**Document Type Default Reference Type**

Shipper

Shop Resource

Trace

Vendor

Work Order

- Click **Save**.

- Customer
- General
- Shipper
- General
- Shop Resource
- General
- Trace
- General
- Vendor
- General
- Part
- Work Order

# Adding Documents

You can add documents by specifying a file location (for example, c:/Infor ERP VISUAL Enterprise/ Part123Schematic.doc) or a URL address (for example, file://///mycompanyserver/ Part123schematic.doc). Using a URL address can be helpful if you share documents with Microsoft Sharepoint or other similar document sharing systems.

If you add documents with a URL address, you cannot print the document using the print associated documents feature found in applications such as Customer Order Entry, Print Work Order Travellers, Customer Maintenance, Part Maintenance, and Vendor Maintenance. If you would like to be able to print the documents that you associate with records in these applications, specify a file location instead of a URL.

The system does not validate the existence of the URL you provide. Make sure you enter the URL correctly.

For physical file locations, you can specify a default Global Document Path for the file location in Document Folder Maintenance. You can also specify a file location based on Site ID and Reference Type.

## Adding a Document

To add a document:

- In the Document Maintenance window, use the Site ID field to specify where the document can be used. To use the document in a particular site, select the site from the drop-down list. To use this document at the tenant level, select \*\* Tenant \*\* from the drop-down list. When you add a document at the tenant level, the document is available to all sites by default. If you are licensed to use a single site, this field is unavailable.
- Click the **Reference Type** arrow and select the type of document from the list. You cannot remove or add Reference Types from the drop-down list. General is the default reference type.

By selecting an appropriate reference type, you can control the documents that appear in the various windows and thereby limit the documents from which your users can select. For example, if you select Part for all of your part documents and your user is referencing a document in the Part Maintenance window, that user will only be able to select a part document. You can reference General type documents from within any module.

You can select AP Invoice, AR Invoice, Customer, Customer Order, Email Attachments, Engineering Change Notice, Estimating, General, General Journal, Operation, Part, Project, Purchase Contract, Purchase Order, Receiver, RFQ, RMA, Requisition, Service, Shipper, Shop Resource, Trace, Vendor, Work Order, or Work Flow.

After you select a reference type, the Document Path field is updated. If a path is specified for the reference type and site in the Document Path Maintenance dialog, then that path is displayed in the Document Path field. If a path has not been specified for the reference type and site, then the global document path is displayed. If the global document path has not been specified, then the directory where you store your VISUAL executables is displayed.

- To add the document, perform one of these actions:
  - Drag-and-drop the document onto the Document Maintenance window. The document is copied into the directory that is displayed in the Document Path field.
  - Click the Document Path field button and navigate to the file. Click **Open**. The document is stored in the directory to which you navigated. It is not copied to any default directories that you have set up.
  - Specify a URL in the Document Path field. When you specify a URL with a valid protocol, the system selects the URL check box. The system recognizes these protocols:

http:// https://

file:// - Use this protocol to reference a file on Microsoft Sharepoint only. file:///// - Use this protocol to reference a file on a web server.

The system inserts the name of the file in the Document ID field.

- In the Description field, specify a description for this document.
- Click the **Document Category** drop-down button and select the category from the list.
- If this document is under ECN Revision Control, select the **ECN Rev Control** check box, which also deactivates the Stage and Revision fields. If the document is not under ECN Revision Control, you can specify a stage and revision level for the document.
- To allow the emailing of this document to customers or vendors, select the **Allow Emailing** check box.
- Click **Save** on the toolbar.

After you save the document, you can click the Document Link field to open the document. If you provided a URL with the http:// or https:// protocol, the system opens the document in a web browser. If you provided a URL with the file:// or file:///// protocol, the system opens the document in the application in which it was created. If you provided a physical document location, the system also opens the document in the application in which it was created.

## Setting Allowable Sites for Tenant Documents

When you create a tenant-level document, the document is made available to all sites by default. Use the Allowable Sites dialog to specify which sites can use a tenant-level document.

To set allowable sites for tenant level documents:

- In the Document Maintenance window, select **Tenant** as the Site ID.
- Click the **Document ID** browse button and select a document from the list.
- Select **Maintain**, **Allowable Sites**. Your allowable sites are displayed.
- To allow the document to be used in a site, select the check box in the **Allowable** column. To prevent the document from being used in a site, clear the Allowable check box.
- Click **Save**.

## Editing Document References

You can edit your document references without having to make the changes everywhere you have used that reference. For example, if you have widely used a material spec and you change the location on the document to which the specification refers, make a single change in the Document Maintenance window instead of changing the reference everywhere you have used it.

To edit document references:

- In the Document Maintenance window, select the site where the document is used. Select **\*\* Tenant \*\*** to edit a tenant-level reference.

If you select a site in the Site ID field, documents that belong to the site are displayed in the Documents browse. Tenant-level documents are also displayed if the site is allowed to use them.

- Make changes.

You can change the selected site only if there are no references to the document, or if the document is a tenant document that only references one site and you are selecting that site.

You can change unreferenced documents from site level to tenant level, or tenant level to site level. If you change a document from site to tenant level the document is added to all allowable sites.

- Click **Save** on the toolbar.

## Viewing Where Documents are Used

After you have used Document References in various modules, you can use the Where Used function to view a list of where you have used a particular document reference.

To view a where used list:

- In the Document Maintenance window, select the document reference to view.
- From the Info menu, select the **Where Used** option.

**Note:** If you have a large list of documents and you want to view a particular reference, click the

**_Search_** button and enter the appropriate information.

- When you have finished, click **Close**

## Deleting Document References

**Note:** After you have used document references, you cannot delete the reference from the Document Maintenance window without first removing the reference from records.

To delete document references.

- Open the Document Maintenance window.
- Click the **Document ID** button and select the reference to delete.
- Click the **Delete** button on the toolbar.

You are prompted to confirm the deletion of the reference.

- Click **Yes**.

The document reference is removed from your database.

If a reference to the document exists, you are prompted that you cannot delete the reference. Click **OK** and remove the reference before continuing.

# Copying and Moving Documents

Use the Copy/Move Documents window to copy or move documents from one folder to another. You can keep or delete the original documents.

To copy and move documents:

- In the Document Maintenance window, select **Maintain**, **Copy/Move Documents**.
- Specify the **New Path for all Copied Documents** by either clicking the browse folder button, or manually typing the path.

The selected files will be copied to the path specified.

- By default, all documents are displayed. To filter the documents in the table, specify information in these fields:

**Site ID** \- To view documents created for a particular site, select that site from the Site ID list. To view documents created at the tenant level, select \*\* Tenant \*\*. To view the documents created for all sites including the tenant, select \*\*All\*\*.

**Reference Type** \- To view documents with a particular reference type, select that type from the list. To view the documents created for all reference types, select \*\*All\*\*.

- Select these check boxes as is appropriate for each document record:

**Copy to New Path** - Select this check box to copy the selected document from the original path to the new path. To copy all documents to the new path, click the **Copy Select All** button.

**Delete from Original Path** \- Select this check box to delete the document from the original path after it has been copied. Clear this check box if you want to retain a copy of the document at its original path after a copy has been created at the new path. This check box can only be used if the Copy to New Path check box is selected. To select this check box for all documents that you are copying to a new path, click the **Delete Select All** button

- Click **Save**. For the documents that you copied, the Original Path column is updated with the file path that you specified in the New Path for all Copied Documents field.

# Printing Document Listings

The Document Listing Report lists documents on a site and reference type basis. To print a list of your documents:

- From the Document Maintenance window, click **Print** on the toolbar.
- To view documents added to a particular site, select the site from Site ID drop-down list. To view documents added at the tenant level, select \*\* Tenant \*\* from the Site ID drop-down list. To view all documents, select \*\* All \*\*.

**Note:** If you select a site, the report shows only documents specifically added to the site. It does not show tenant level documents that the are allowed to be used in the site.

- Click **Starting Doc ID** and select the document with which to start this report.
- Click **Ending Doc ID** and select the document with which to end this report.
- To limit this report based on Reference Types, select each reference type to include in the report.

**Note:** As you select each reference type, it appears highlighted. To clear a selection, click it a second time.

- In the Sequence section, select the sort order for the report. You can select **Document ID** or **Reference Type**.
- Click the **Print To** arrow and select the output for the report. Select **Print** to send this report to your printer.

Select **View** to view the report on your screen.

Select **File** to copy this report to a comma-delimited file. You are prompted for a file name.

Select **E-mail** to send a copy of this report to an email recipient. If you select to email the report, an rtf (rich text format) file is created and attached to a Microsoft Outlook email. If you selected the **PDF Format** check box, a PDF is created and attached to the email

- To print a list of only the documents you have used for references, select the **Print Linked References** check box.
- Click **OK**.

# Referencing Documents

You can make as many references to as many documents as you need. Referencing also allows you to make changes in one place instead of making many of the same changes in different places.

You can create documents or reference existing documents in applications. The application determines the type of document that you can create or reference. For example, in Customer Maintenance you can create or reference documents with the type of Customer or General. You cannot create or reference documents with the type of Part.

This table shows the applications where you can create or reference documents, the document types that you can create or reference, and whether documents can be attached to the header, line, or both:

| **Application** | **Document Types** | **Attachment Level** |
| --- | --- | --- |
|     | A/P Invoice |     |
| A/P Invoice Entry | General | Header and Line |
|     | Vendor |     |
|     | A/R Invoice |     |
| A/R Invoice Entry | Customer | Header and Line |
|     | General |     |
|     | A/R Invoice |     |
| Cash Application | Customer | Line |
|     | General |     |

Collections Window

Customer Maintenance

Customer Order Entry

Engineering Change Notices Entry

Estimating Window

General Journal Entry

Manufacturing Window - Header card

A/R Invoice General

Customer General

Customer Customer Order General

Engineering Change Notice General

Customer Estimating General

General General Journal

General Part

Work Order

Line

Header

Header and Line

Header and Line

Header and Line

Header and Line

Header

**Application Document Types Attachment Level**

Manufacturing Window - Operation Card

Manufacturing Window - Material Requirement Card

Order Management Window

Service Maintenance

Part Maintenance

General Operation Service

Shop Resource Work Order

General Part

Work Order

Customer Customer Order General

General Service

General Part

Header

Header

Header and Line

Header

Header

| Part Trace Maintenance | General Trace | Line |
| --- | --- | --- |
|     | A/P Invoice |     |
| Payment Entry | General | Line |
|     | Vendor |     |
|     | Customer |     |
| Progress Billing Entry | Customer Order | Header and Line |
|     | General |     |
|     | Customer |     |
| Project Billing Entry | Customer Order | Header and Line |
|     | General |     |

Project Maintenance

Project Window - Header Card

General Project

General Part

Work Order

Header

Header

**Application Document Types Attachment Level**

Project Window - WBS Task Card

|     | Shop Resource Work Order |     |
| --- | --- | --- |
| Project Window - Leg/Detail | General<br><br>Part | Header |
|     | Work Order |     |
| Purchase Order Entry | General<br><br>Purchase Order | Header and Line |
|     | Vendor |     |
|     | General |     |

General Operation Service

Header

Purchase Receipt Entry

Purchase Requisition Entry

Receiving Inspection

RMA Entry

Vendor Maintenance

Vendor RFQ Entry

WIP Maintenance

Purchase Order Receiver Vendor

General Requisition Vendor

General Purchase Order Receiver Vendor

General RMA

General Vendor

General RFQ

A/P Invoice General

Header and Line

Header and Line

Receiver Lines in Inspection

Header and Lines

Header

Header and Line

Header

## Referencing Existing Documents

To reference a document:

- Open the appropriate program.

For example, if you are adding document references to a part, open the Part Maintenance window.

- Select the item to which you are adding document references.

Using the part example, select the appropriate part in the Part Maintenance window.

**Note:** If the Site ID is Tenant in the Part Maintenance window, the Document Reference option is not available.

In some cases, such as the Customer Maintenance window, there may not be a Site ID value, in which case \*\* Tenant \*\* is displayed in the Document Reference dialog. If this is the case, then the document reference becomes a tenant-level document for multi-site databases, or the one available Site ID for single-site databases.

- Open the Document Reference dialog. You can open the dialog by:
  - Clicking the **Documents** toolbar button to attach a document to the record. If the record has a table, you can attach a document to an individual line by selecting the line and clicking the Line Documents toolbar button.
  - Selecting **Edit**, **Document Reference** to attach a document to the record. If the record has a table, you can attach a document to an individual line by selecting the line, then selecting **Edit**, **Line Document Reference**.
  - For Customer Maintenance, Part Maintenance, Part Trace Maintenance, and Vendor Maintenance, selecting **Maintain**, **Document Reference**.
  - For the Manufacturing Window, in the Header Card, click the **Doc Ref** button. In all other cards, select **Edit**, **Document Reference**.
  - For Customer Inquiry or Vendor Inquiry, selecting **Info**, **Document Reference** or **Info**, **Document Line Reference**.
- Click **Insert**.
- Specify a Document ID by performing one of these actions:
  - Clicking the **Document ID** browse button and select a document from the list.

A list of valid document attachments is displayed. The list is filtered to display documents that have a valid reference type for the current application. If you are working in a tenant-level application, the list is filtered to show tenant-level documents. If you are working in a site-level application, the list is filtered to show site-level documents and tenant-level documents that the site is allowed to use. Select the **All Reference Types** check box to display all of the reference types for your selected site-level or tenant-level.

- - Specifying the Document ID directly in the Document ID column. If you specify a Document ID that is not the correct type of document reference, you are notified that the document cannot be added due to its type or that it cannot find the Document ID. If you specify a Document ID that does not exist, then you are prompted to create the document. [See "Creating Document](#_bookmark526) [References in Maintenance Windows" on page 6-21.](#_bookmark526)

- Click **Save**.

## Manually Inserting a New Document in the Document Reference Dialog

Use this procedure to manually create a document reference. To create a reference by dragging-and- dropping a document onto the Document Reference dialog, see ["Using Drag-and-Drop to Associate](#_bookmark525) [Documents" on page 6-20 in this guide](#_bookmark525). To create a reference by dragging-and-dropping a document onto a maintenance window, see ["Dragging and Dropping Documents onto Maintenance Windows"](#_bookmark527) [on page 6-21 in this guide](#_bookmark527).

To create a new document reference:

- From the Document Reference dialog, click the **Insert** button.
- Type a new Document ID in the Document ID field.
- Press **Tab**.
- The system asks if you would like to create a new document. Click **Yes**.
- The Document ID that you specified in the Document ID field is displayed.

In the Site ID field, the site where you are adding the document is displayed. If you accessed the Document Reference dialog from a tenant-level application, then the document is added at the tenant level. \*\* Tenant \*\* is displayed in the Site ID field.

Specify this information:

**Description** - Specify a description of the Document ID.

**Reference Type** - Click the arrow and select the type of document. This limits the applications where you can use the document. Select General to make the document available in all applications that allow document references.

**Document Category** - Click the arrow and select the category for the document.

**URL** - If the document path is a URL, select the URL button. When you select the URL button, the system deactivates the Document Path browse button.

**Document Path** - If the document is a URL, specify the URL in the Document Path field. If the document is not a URL, click the browse button and navigate to the path of the document.

- Click **Save**. If the document meets one of these criteria, a message is displayed:
  - The document does not exist in the designated Document Path. In the message, click **Ok** and then specify the correct Document Path.
  - The Document ID that you specified does not match the document name. In the message, click **Yes** to update the Document ID that you specified with the name of the document that you are attaching. Click **No** to specify a different document path.
- Optionally, open the new Document ID in Document Maintenance to assign Stage and Revision information or to select it for ECN Revision control.

## Using Drag-and-Drop to Associate Documents

Use this functionality to create a new document or new document reference in the Document Reference table.

To use drag-and-drop:

- Access the Document Reference dialog. [See "Referencing Documents" on page 6-15.](#_bookmark520)
- Open Windows Explorer or Microsoft Outlook to locate the document you want to add as a reference.
- Select the file to add as a reference, drag the file to the Document Reference table, and release the mouse button to drop the file on the table.

When you drop a file on the table one of these messages may be displayed:

- - Message dialog indicating that links to the files you dragged will be created.To create a copy of the file in the assigned directory, click **Yes**. If you do not want to create the document reference, click **No**.If you click **Yes**, the information is added to the table.

**Note:** The copied document is referenced, and not the original file. Additionally, a copy of a dragged email or email attachment is stored in the user's local configuration directory or the **Start in** directory listed in the client icon properties.

- - Message dialog indicating that the document already exists in another site. To make the document available to other sites, click **Yes**. The site-level document becomes a tenant-level document that is located in the original site directory. If you do not want to create the document reference for other sites, click **No**.
    - Message dialog indicating that the document has an apostrophe in the name and cannot be copied. Click **Ok**.
    - Message dialog indicating that the document cannot be copied because it is locked by an active ECN (Engineering Change Notice). Click **Ok**.
    - Message dialog indicating that you must correct the Site ID or the Reference Type. You can do this by either changing the Site ID to Tenant, or by adding the Site ID to the allowed sites for the Tenant, or correcting the reference type.
    - If the document already exists in the file path specified in the Document File Path column, specify the action to take:
      - **Copy and Replace** - Replace the existing document with a copy of the document being dragged and dropped.
      - **Don't copy** - No document copy will occur and the document path will remain unchanged. The file currently in the destination folder will be attached.
      - **Copy, but keep both files** - The file currently in the destination folder will be renamed to

&lt;document_name(#)&gt;.

- - - **Copy, but rename the file and Document ID to** - The file that you are attaching and the document ID is updated to the name that you specify.
      - **Do this for all conflicts** \- Select this check box to apply your selected copy option to all similar copy issues. This check box is only displayed when multiple documents are begin dragged and dropped.

- Optionally, specify a description and change the reference type. Click **Save**.

## Creating Document References in Maintenance Windows

Dragged-and-dropped documents are stored based on this storage hierarchy. If a directory exists for tenant, site, or reference type documents, the document is stored there. If no such directory exists then the document is stored in the global document path as defined in the Document Folder Maintenance window. If folders do not exist at either of those storage hierarchy levels, then the documents are stored in the VISUAL executable directory.

You can add a document reference to a maintenance window by:

- - Dragging and dropping the document onto one of these maintenance windows: Customer Maintenance, Part Maintenance, Vendor Maintenance or WIP Maintenance.
    - Dragging and dropping the document onto a maintenance window's Document Reference dialog.
    - Specifying a new Document ID in a maintenance window's Document Reference dialog.

### Dragging and Dropping Documents onto Maintenance Windows

You can create document references to maintenance windows using the drag-and-drop method.

The document level, site-level or tenant-level, is determined by the window onto which you are dragging and dropping the document. The document's reference type is determined by the settings specified in the Document Maintenance window's Default Reference Type Maintenance dialog. The document's path is determined by the settings specified in the Document Maintenance window's Document Folder Maintenance dialog.

Dragging and dropping a new document is different than adding an existing document as a new reference for a record. New documents are created based on their default reference type. You can override the reference type default in the New Document dialog. New documents are assigned to a site or tenant; the site when it is specified in the maintenance window, or the tenant when the site is not specified in the maintenance window. Adding an existing document as a new reference for a record differs from adding a new document. An existing document must have a valid reference type for the application, which may differ from the default, and it must also be valid for the site or tenant specified in the maintenance window.

To drag-and-drop documents onto maintenance windows:

- Access the maintenance window to which you are creating the referenced document.
- Select the item. For example in Part Maintenance select the part.
- Locate the document you want referenced in the maintenance window.
- Click and hold the mouse button down to select the document, drag the mouse onto the maintenance window, and then release the mouse button to create a reference to the document. One of these dialogs is displayed:
  - If the document has not yet been created as a reference and does not exist in the file location defined for the document type, a dialog is displayed that lists the number of documents and document references that will be created. Click **Yes** to create the document and reference. **Note**: A copy of a dragged email or email attachment is stored in the user's local configuration directory or the **Start in** directory listed in the client icon properties.
  - If the document exists in the file location defined for the document type, a dialog is displayed that lists copy options. Click one of these options, and then click **Ok**:
    - **Copy and Replace** - Replace the existing document with a copy of the document being dragged and dropped.
    - **Don't copy** - No document copy will occur and the document path will remain unchanged. The file currently in the destination folder will be attached.
    - **Copy, but keep both files** - The file currently in the destination folder will be renamed to

&lt;document_name(#)&gt;.

- - - **Copy, but rename the file and Document ID to** - The file that you are attaching and the document ID is updated to the name that you specify.
      - **Do this for all conflicts** \- Select this check box to apply your selected copy option to all similar copy issues. This check box is only displayed when multiple documents are begin dragged and dropped.

- If the document is not yet a document reference, the New Document dialog is displayed. Specify this information:

**Description** - Specify a description of the Document ID.

**Reference Type** - Click the arrow and select the type of document. This limits the applications where you can use the document. Select General to make the document available in all applications that allow document references.

**Document Category** - Click the arrow and select the category for the document.

**URL** - If the document path is a URL, select the URL button. When you select the URL button, the system deactivates the Document Path browse button.

- Click **Save**.

## Viewing Attached Documents

The Documents button on the toolbar indicates whether documents are attached to the record.

- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABYAAAAWCAIAAABL1vtsAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAACfUlEQVQ4ja2UzWsTQRiH39mdyXbTVLJNTNkE+oEJRAgkzSIhPfTLhVj/BP+K6kXwrqdeFE89SE/qQelBpIKhpT3I2s22blqWyhaS0OTUGEgucTfubg9b0hrTorW/0zswzzPvOwyDypUjx/4FVw2iMHVluJtrUOBu1W63NzY2SqUSISSZTGYymX9WrK+v1+v1XC7XarUkSWq327Ozsz27P1bopgkPYlZ/RblcFkUxGo0CAMdxa2trABAIBBqNxszMjMsvfiEAMBe2+UGnC57dBcMw9XrdrScmJhYWFjRNq9Vqfr+/0+mslk755WnzPA8A9OLDR+DYAODxeCRJ8nq9oVDIbWR4eJhl2Xg8vnrEPpYIQrA8bc5F7PM8QtTZIIlEwjCMzc1NQki1Wh0ZGUkkEpZlvSt7nnwlFILXd807od/43kEAQBCEdDqdz+dt26Zp2rKsFZ1x+beimfT/3N/f/1OBe9bZbBYAgsHg+Pj4is4828EDNLwRzds3jN3db7Ist1qtqampyxSuxbKs5QNmScUDNLzPGXG/c3hY2d7ezmazkiQhhNyT+gzSzUuNWVKxj8Bqzrg1aFYqFZ7nASAQCIiiqCiKruuXdfF8D7/Ywz4CH+4ZYbYjywVFURBCHMdFIhFCSK1WKxQKsVisv+LpDn51cMqPDjmKoqqqOj8/7/P5wuEwxhgAxsbGNE3rP0jxB7XyHXOM8+m+MTrkAIAgCNFodGtri6IolweARqPBsmyXQj3/xecqnQraNwfO3p/jOPl8Xtf1yclJnuePj49lWc5kMoIgAACicK/ioqiqWiwWm82m1+tNpVLpdPq0hb9XXJTr+bWwa/ofxTV0cQLxQwBwx8pA4wAAAABJRU5ErkJggg==)No documents are attached.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABYAAAAVCAIAAADNQonCAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAACAElEQVQ4jdXUTcsxURgH8OvcxhSKxRiFvC6osaMZsZGVvDbJh/GRKKFkMbPx0oiSZKMkJTtNKIuRyLkXU+N+bsLTs3r+q6szp1/XdaZz0OFwgH8LoVUY41/fEEJ/RyiK0m63l8ulXq9nWTaRSHyo3IlWqyXLMs/zx+Ox0+koipJOpz/p6E6sVqtcLscwDEKIoqharQYANE3vdrtUKvVC+dIqg8Gw3W4xxhjjQCBQKpVms9lms1ksFpPJRF1/SujK5bJakSTZ6/VMJpPdbgcAiqJsNpskSdlsttvtEgShrj/2ch8kEomcz2dBEEiSXK/XDocjHA4Xi8V6vc7zvCiK6p5H5d4FALhcruv1OhqNzGbzaDSiaToYDNpstkaj8aKXPwgA8Pl8p9NpsVjkcrlms2m1WjWFZdlut3u5XHw+30/lN6Eqt9ut3+9ns1lNoWlaEIRkMjkYDK7Xq9fr1ZQveJZ4PM5xnCiKPM83m83VauV0OgGApulCoTAcDufz+ZPjfFQAQBRFjuOq1SoAUBTl8XhIktxsNpIkhUKhN4Sm9Pv9TCZjsVjcbjdBEAghv98/nU61bc8H+akwDCOKIkJIp9MBAMZYlmWj0fh+EC35fB5jXKlUOI5zuVzb7VaSJPUSqkEfvhfj8Xg8Hu/3e5PJFI1GY7GY9lM/JV7kzVn8P8Q33hHaBDtR0M8AAAAASUVORK5CYII=)Documents are attached.

To view an attached document, click the Documents toolbar button and double-click the document to view it.

You can view documents in any application where you can attach them.

You can view existing document attachments in these applications, but you cannot add new attachments:

- - Customer Inquiry
    - Material Planning Window
    - Vendor Inquiry

