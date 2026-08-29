# Chapter 3: Accounting Entity Maintenance

This chapter describes these topics:

**Topics**

[What is Accounting Entity Maintenance? 3-2](#_bookmark85)

[Accessing Accounting Entity Maintenance 3-3](#_bookmark86)

[Defining Accounting Entities 3-4](#_bookmark87)

# What is Accounting Entity Maintenance?

Use Accounting Entity Maintenance to define information about your accounting entities. An accounting entity is an independent financial entity within your company. The accounting entity is the "middle level" of the organizational structure. Each accounting entity is summarized to the Tenant level. Information defined at the accounting entity level includes:

- Costing information, such as the costing method, POC revenue recognition method, and burden basis
- Default VAT information
- Default Intrastat information
- Payment information, such as Next Payment Batch Sequence # and withholding information
- Default Order Management information, such as whether WIP/VAS is enabled, how the date of the oldest outstanding invoice is determined, and allocation information

Information you define at the accounting entity level applies to each of the sites that belong to the entity.

Accessing Accounting Entity Maintenance

# Accessing Accounting Entity Maintenance

To access Accounting Entity Maintenance, select **Admin**, **Accounting Entity Maintenance**.

# Defining Accounting Entities

If you upgraded from a previous version, you defined one accounting entity ID during the upgrade process.

If you have not exceeded the maximum number of accounting entities allowed by your license, you can create additional accounting entities.

- To create an accounting entity ID:
- Click the New button.
- In the Entity ID field, specify the accounting entity ID. We recommend that you **do not** use single quotations ('), forward slashes (/), or commas (,) in your entity ID. Using these characters may cause issues with integrations to other products through Infor ION.
- Click **Save**.

Use the tabs to define additional information about the accounting entity.

## Specifying Information on the General Tab

Specify this information on the General tab:

- The entity's address
- The entity's tax ID number
- The default language ID for menus
- The effective exchange rate date

To specify information on the General tab:

- Click the **General** tab.
- In the address section, specify the entity's address.

If you are licensed to use one entity and one site, define the address in Application Global Maintenance. The address information specified in Accounting Entity Maintenance is not used in a single entity, single site database.

- In the Tax ID field, specify the entity's tax ID number.
- In the Language section, click the arrow and select USA. This field enables menus when you build a baseline database. The field does not control the translations used in the interface. See "Translations" on page 6-1 in the System Administration guide.
- In the Effective Exchange Rate Date section, specify which date should be used to determine the exchange rate to apply to a transaction. Select one of these options:

**Use System Date** - Select this option to use the date that you entered the transaction to determine the exchange rate.

**Use Transaction Date** - Select this option to use the date of the transaction to determine the exchange rate. Because you can specify a date on a transaction, the date of the transaction is not necessarily the same as the current date. This is the default setting.

After you enter a transaction for this entity, these options are not available.

- Click **Save**.

## Specifying information on the Costing tab

See ["Manufacturing Costing" on page 5-1 in this guide](#_bookmark191).

## Using the VAT Tab

Value-Added Tax (VAT) is a tax on consumed goods. While the total tax amount is ultimately paid by the customer who purchases the final product, a portion of the tax is collected any time goods or services are purchased.

For example, if a company buys two raw materials for the manufacture of a product, the company would pay VAT on the raw materials. When the final product is sold, the company is reimbursed for the taxes paid on the raw materials.

VAT is not typically paid on exports.

To activate the fields on the VAT tab, click the Value Added Tax Support Enabled check box.

### Specifying General VAT Information

Specify this information on the VAT tab:

**Use VAT Books to generate invoice numbers** - To use the invoice and memo numbering information set up in VAT Books, select this check box. To use the auto-numbering scheme set up in Accounts Payable Invoices and Accounts Payable Receivables, clear this check box.

**Calculate VAT on Freight** - To apply VAT to freight charges, select this check box. To omit freight charges from VAT calculations, clear this check box.

**VAT Categories Required** - To require users to specify a VAT category when VAT is applied to a transaction, select this check box. To allow users to apply VAT to transactions without specifying a VAT category, clear this check box. VAT categories identify the type of item being taxed: material, service, or other.

**VAT Registration** - Specify the VAT number of your company.

**Next VAT Sequence #** - Specify the next number to use for VAT reporting.

**VAT Receivables Export ID** - Specify the Receivables Export ID if you are generating reports to file.

**VAT Payables Export ID** - Specify the Payables Export ID if you are generating reports to file.

### Setting Up VAT Books

VAT Books control the numbering sequence of all invoices and memos created when VAT is enabled. In the European Community, there may be more than one "VAT book" or numbering sequence in use at any one time. With VAT enabled, more than one automatic number sequence can exist for invoices and memos.

Use VAT Book Setup to specify numbering sequences. You can set up numbering sequences for Accounts Payable Invoices, Accounts Payable Memos, Accounts Receivable Invoices, and Accounts Receivable Memos. After you set up numbering sequences in VAT Book Setup, you can assign the accounts receivable sequences to customers and accounts payable sequences to vendors.

When you generate invoices for your customers, the invoice number is determined by the VAT Book code you assigned to the customer for invoices. If you issue a memo to a customer, the memo number is determined by the VAT book code you assigned to the customer for memos.

If you create an invoice or memo manually, you must specify a VAT book code. If you create an invoice or memo manually, the VAT book code you specify does not need to match the VAT book code you specified on the customer or vendor record.

VAT codes are also used in VAT reports and Intrastat reports. To set up a VAT Book:

- Click **VAT Book Setup**.
- Click **Insert**.
- In the table, specify this information:

**Code** - Specify an identifying code for the VAT Book.

**Year** - Specify the year this VAT Book code is active. The current year is inserted when you create a line in the table, but you can specify a different year.

**Description** - Specify a description of the VAT Book.

- In the Current Posting Date field, specify the date that transactions using the VAT Book code are posted. This field is optional.

Posting dates used on VAT transactions must be sequential. For example, if you generated invoice 101 and 102, the posting date on invoice 102 must be equal to or later than the posting date on invoice 101.

If multiple users enter VAT transactions, you can specify a date in the Current Posting Date field to ensure that posting dates on transactions are entered correctly.

- In the VAT Code Type section, specify the types of transactions to associate with the VAT book code. Select one or more of these check boxes:
  - A/R Invoices
  - A/R Memos
  - A/P Invoices
  - A/P Memos
- In the Sequence Numbers section, specify this information:

**Start Number** - Specify the first number to use.

**Next Sequential Number** - Specify the second number to use.

**Alphanumeric Prefix** - Specify the alphanumeric characters to use before the sequence number. This field is optional.

**Alphanumeric Suffix** - Specify the alphanumeric characters to use after the sequence number. This field is optional.

**Number of Decimal Places** - Specify the maximum number of digits to use in the ID.

**Show Leading Zeros** - To display placeholder zeroes in the ID, select this check box. For example, if you specified 4 in the Number of Decimal Places field and selected this check box, 0001 is used as the ID instead of 1.

- In the Report Type field, click the arrow and specify the type of report to associate with the VAT book code. Specify EC (European Community) or IT (Italy).
- Click **Save**.

### Setting Up VAT Percentages and G/L Accounts

You can set up VAT tax percentages and associated G/L accounts in the VAT section.

- Click **VAT Setup**.
- Click **Insert**.
- In the table, specify this information:

**Code** - Specify the VAT code.

**Description** - Specify a description of the code.

- In the Effective Date field, specify the date that the VAT code becomes effective.
- In the VAT Type field, specify the type of VAT code. Select one of these options:

**Default VAT** - Use this option to set up VAT codes to use between countries that are not both in the European Commission.

**Intra-Euro VAT** - Use this option to set up VAT codes between two countries in the European Commission. Use this type to create VAT codes if vendors are not required to apply VAT for transactions. You can specify Intra-Euro VAT codes on Vendor records and purchase orders, and accounts payable invoices.

- If you selected Default VAT in the VAT Type field, specify this information:

**Tax Percent** - Specify the VAT percentage.

**Tax G/L Account ID** - Specify the G/L account to be posted for this tax.

**Recoverable Tax Percent** - Specify the percent of recoverable tax. This pertains only to payable taxes.

**Tax Rcv G/L Account ID** - Specify the G/L account to be posted for this tax. This account receives a debit entry for recoverable tax amounts.

- If you selected Euro VAT in the Vat Type field, specify this information:

**Recoverable Tax Percent** - Specify the percent of recoverable tax. This pertains only to payable taxes.

**Tax Rcv G/L Account ID** - Specify the G/L account to be posted for this tax. When a payable transaction is processed, this account receives a debit entry for recoverable tax amounts.

**Intra-Euro Tax Percent** - Specify the Intra-Euro VAT percentage. For Intra-Euro VAT codes, this value typically equals the value that you specified in the Recoverable Tax Percent field.

**Intra-Euro Debit Tax G/L Acct ID** - Specify the account to use to process any differences between the recoverable tax percentage and the Intra-Euro tax percentage. Since these two percentages are typically equal, transactions against this account should be minimal.

**Intra-Euro Credit Tax G/L Acct** - Specify the account to credit when a payable transaction is processed. If the recoverable tax percentage is equal to the Intra-Euro tax percentage, you can enter the same account in this field as you entered in the Tax Rcv G/L Account ID.

- Click **Save**.

### Specifying VAT Categories

VAT categories identify the type of item being taxed: material, service, or other. If you have selected the VAT Categories Required check box, users are required to select a VAT category for all VAT- related transactions.

To specify a VAT category:

- Click **VAT Category Setup**.
- Click **Insert**.
- Specify this information:

**ID** - Specify the category ID.

**Type** - Specify the type of item associated with this category. Click the arrow and specify one of these options:

**M** - Select **M** if this category is for a material.

**S** - Select **S** if this category is for a service.

**O** - Select **O** if this category is for other items.

**Description** - Specify a description of the VAT category.

- Click **Save**.

## Using the Intrastat Tab

Intrastat is an optional feature. You must have the correct serial number to enable Intrastat. If you have questions, please contact Infor Customer Support.

Intrastat is a European Community term that refers to the tracking statistics of goods manufactured and distributed to and from EC member countries. Intrastat is intended to eliminate customer declarations for distributors of goods who import and export within the EC.

Intrastat applies to the importation and exportation of goods. It does not apply to goods shipped within a given member country. Only VAT registered companies are required to provide Intrastat information.

Intrastat requires that goods sold and manufactured in the EC be tracked so that their origin and destination is recorded in a permanent way. Periodically, each manufacturer and distributor of goods must report the items they sold and distributed, where they were manufactured or obtained, and where they were sent, including intermediate distribution points.

Use the VISUAL Intrastat feature to report the appropriate information to the country in which the manufacturer operates. You can also record supporting data on which the reports are based. The exact form and format of these reports varies from country to country. You may need edit these reports.

### Specifying Intrastat Settings

To specify Intrastat settings:

- Click the **Intrastat** tab.
- In the Intrastat section, specify this information:

**Intrastat Enabled** - Select this check box to enable the Intrastat features. When you select this check box, the remaining fields on the tab become available.

**Review Intrastat before Saving Transaction** - To be prompted to view Intrastat information prior to saving any transaction, select this check box.

**Country ID** - Specify the Country ID of your company.

**Branch ID** - Specify Branch ID of your company.

**Next Sequence #** - Specify the next number for Intrastat report.

- In the Report section, specify the formats of the Arrival report and the Dispatch report. The options for the reports are the same. To specify the Arrival report format, click **Arrival Format**. To specify the Dispatch report format, click **Dispatch Format**.
- Specify this information:

**Show** - Select the data to show on the report. You can select **Tariff Code**, **Excise Price**, **Region, Country**, **Port of Arrival**, **Port of Transshipment**, and **Siret Number**.

You cannot generate a report until all of the Intrastat line items you have selected are present.

**Export ID** - Specify an Export ID to generate reports to file.

- Click **Ok** to exit the dialog box.
- In the Frequency section, specify how often the reports are generated. Click **Monthly**, **Quarterly**

or **Yearly**.

- Click the **Save** button.

### Specifying Additional Intrastat Report Information

Use the Additional Intrastat Maintenance window to specify additional Intrastat information to show on your reports.

**Note:** You must select the **_Intrastat Enabled_** check box on the Intrastat tab to display Additional Intrastat Maintenance window.

To set up additional Intrastat information:

- Select **Inventory**, **Intrastat Maintenance**.
- In the header, specify the filters for the table. Specify these filters:

**Site ID** - If you are licensed to use multiple sites, click the Site ID arrow and select the site ID to view in the table. If you are licensed to use a single site, this field is unavailable.

**Movement Type** - Select the movement to view in the table. Click Dispatch or Arrival.

**Date Range** - Use the calendar buttons to specify the time period to view in the table.

- To add a new transaction using the criteria you specified in the header, click the **Insert Row**

button.

- Specify this information:

**Reference** - Specify a short description or reference code for the transaction.

**Movement Da****te** - Specify the date the movement takes place.

**Tariff Code / Commodity Code** - Double-click the browse button and select the Tariff or Commodity code.

**Excise Price** - Specify the excise charge.

**Delivery Terms** - Click the arrow and select the delivery terms for this transaction.

**Nature of Trans** - Double-click the browse button and select the type of transaction.

**Mode of Transport** - Double-click the browse button and select the type of transportation used.

**Net Mass** - Specify the weight of the shipment.

**Supplement Unit** - Specify the number of units.

**To Country** - Click the browse button and select the country to which the shipment is going.

**Orig Country** - Click the browse button and select the country from where the goods originally came.

**From Country** - Click the browse button and select the country from which this shipment is shipped.

**Region** - Click the browse button and select the region of the country from which this shipment is shipped.

**Port of Arrival** - Click the browse button and select the port in which this shipment will first arrive in the country.

**Port of Transshipment** - Click the browse button and select the intermediate country used between shipping and receiving.

**Number of consignments** - Specify the number of agents selling items in the shipment for the shipper.

- Click the **Save** toolbar button.

### Specifying ESL Information

A European Sales List (ESL) is a VAT report that is based on goods reported under Intrastat. Specify this ESL information:

**ESL Export ID** - Specify an ESL Export ID to generate reports to file.

**Next Sequence #** - Specify the next number for the ESL report.

**Frequency** - Specify how often to generate the report. Click one of these options: **Monthly**, **Quarterly**, or **Yearly**.

Click **Save**.

### Entering Intrastat Codes

Use the buttons at the bottom of the Intrastat tab to specify codes used in Intrastat transactions. You can specify these codes:

**Port of Arrival** - This identifies the place where the goods are received.

**Nature of Transaction** - This identifies the type of transaction, such as transfer of ownership or return of goods.

**Tariff** - This identifies the tariff applied to the transaction. This code is for identification purposes only. It does not actually apply the tariff rate.

**Country** - This identifies the countries used in Intrastat transactions.

**Port of Transshipment** - This identifies the place where goods are shipped.

**Mode of Transport** - This identifies the methods used to transport goods.

**Region** - This identifies regions where your warehouses are located.

For all code types except Country, you specify a code and a description. For the Country code type, you specify an ID, description, whether Intrastat is required for the country, and the Intrastat country ID.

To specify Port of Arrival, Nature of Transaction, Tariff, Port of Transshipment, Mode of Transport, and Region codes:

- Click the button that corresponds to the code you are entering. For example, click **Port of Arrival**

to enter Port of Arrival codes.

- Click **Insert**.
- Specify this information:

**Code** - Specify a code.

**Description** - Specify a description for the code.

- Click **Save**.

#### Specifying Country Codes

To specify a country code:

- Click **Countries...**.
- Click **Insert**.
- Specify this information:

**ID** - Specify a code.

**Description** - Specify a description for the code.

**Intrastat Required** - Select this check box to require Intrastat to be used with the country ID.

**Intrastat Country** - Specify the country ID to use with Intrastat.

- Click **Save**.

## Using the Payment Tab

Use the Payment tab to specify default information for electronic payments you make to vendors. electronic payments made to vendors. You can specify this information on the Payment tab:

- - Batch number information
    - Withholding information
    - Cash variance information
    - Source of commission payments To specify electronic payment settings:

- Click the Payment tab.
- In the Next Payment Batch Sequence # field, specify the next batch number to use for payments.
- If you withhold tax from your vendor payments, select the Withholding Enabled check box. Then, specify this information:

**Next Sequence #** - Enter the next sequential number to use for reporting.

**Social Security No.** - Enter the company's or individual's social security number.

**Summary Export ID** - Enter an Export ID for monthly summary reports.

**Certificate Export ID** - Enter an Export ID for tax certificates.

- In the Cash Variance section, specify this information:

**Number** / **Percent** - Select whether to calculate cash variances by numbers or percentages.

**Minimum** - Enter the smallest number or percent to report.

**Maximum** - Enter the largest number or percent to report.

- In the Pay Commission By section, specify the basis for commissions you pay to your sales force. Click one of these options:

**Cash Receipt** - Click this option to generate commissions when you receive payment from the customer.

**Invoice/Invoice Date** - Click this option to generate commissions based on the accounts receivable invoice.

- Click **Save** to save the information.

## Using the Order Management Tab

Use the Order Mgt tab to specify default information for your customer order management. In the Order Mgt tab, you can:

- - Enable WIP/VAS. WIP/VAS (work in process/value-added service) is typically used in conjunction with VISUAL DCMS.
    - Specify allocation behavior
    - Specify the default shipping weight unit of measure
    - Specify the default back order fill rate
    - Specify order management codes To specify this information:

- Click the **Order Mgt** tab.
- Select the **WIP/VAS Enabled** check box to enable WIP/VAS functions.

Parts that make up an order may require extra services before they are shipped to a customer or transferred to another warehouse. For example, parts in an order may require price tag printing and application. These activities are work in process (WIP) activities.

In certain cases, employees in the distribution center may need to provide additional services before a product is shipped. These activities are value-added services (VAS).

**Note:** You cannot use WIP/VAS if you do not have an Order Management enabled serial number.

- Specify allocation settings. Depending on your selections, certain options become available or unavailable. Select from these options:

**Allocations Require Warehouse/Location** - To allow users to allocate supply and assign demand without specifying a warehouse or a location, clear this check box. To require supply allocations and demand assignments to have a warehouse ID and location ID before they can be linked to transactions, select this check box. When you select this check box:

- - You must supply a Warehouse ID for purchase and customer orders before you save the order.
    - You can enter a new work order and add material requirements and operations, but you cannot allocate work order quantities to demand or allocate supply to material requirements.

This setting does not apply to interbranch transfers (IBTs).

When you select the Allocations Require Warehouse/Location check box, the Auto Allocate check box and the Requirement Allocation Level options become available.

**Requirement Allocation Level** - These options become active when you select the Allocations Require Warehouse/Location check box. Use the requirement allocation level to determine if materials must be allocated to work orders before the work order can be firmed or released. Click one of these options:

**Full** - Click this option to require all materials to be allocated to the work order before the work order can be firmed or released.

**Partial** - Click this option to optionally allow required materials to be allocated to a work order before the work order can be firmed or released. When you click this option, a work order can be released without materials allocated.

**None** - Click this option if you do not require materials to be allocated to a work order before it can be released.

**Auto Allocate** - To automatically allocate supply to demand, select this check box. You cannot select the Auto Allocate option unless you have selected the Allocations Require Warehouse/ Location option.

**Customer Order Allocation** - When you select the Auto Allocate option, the Customer Order Allocation Level section becomes active. Use the customer order allocation level to determine if materials must be allocated to customer orders before the orders can be placed. Click one of these options:

**Full** - Click this option to require all materials to be allocated to customer orders before the orders can be placed.

**Partial** - Click this option to optionally allow required materials to be allocated to customer orders before the orders can be placed. When you click this option, a customer order can be placed without materials allocated.

**None** - Click this option if you do not require materials to be allocated to a customer order before it can be placed.

- In the Default Shipping Weight UM field, click the browse button and specify the unit of measure to use for weight shipment in the Order Management Window.
- In the Default Back Order Fill Rate field, specify the default fill rate to use for customer back orders.

The back order fill rate is a number between 0 and 100 that indicates the minimum percentage of available stock that must be allocated to the customer if a back order balance remains on a customer order. For example, an order fill rate of 100 for a C/O line for 1,000 back-ordered parts, produces a minimum fill rate quantity of 1,000; an order fill rate of 80 for a C/O line of 1,000 produces a minimum fill rate quantity of 800. For more information on Customer fill rates, refer to the "Shipping Entry" chapter.

- Click the **Save** button.

### Maintaining Order Management Codes

Use the buttons in the Maintenance section to access these Order Management codes:

- - Priority Codes
    - Customer Order Types
    - WIP/VAS Codes
    - Customer Types
    - HTS Codes
    - Part Alias Types

The codes you specify in the Order Management tab apply to all of your accounting entities, not just the selected accounting entity.

#### Specifying Priority Codes

Use priority codes to determine what percentage of available stock can be used to fill a customer order. Assign a priority code to a customer in Customer Maintenance.

To set up priority codes:

- Click **Priority Codes**.
- Click **Insert**.
- Specify this information:

**Priority Code** - Specify a code.

**SKU Rate** - Specify the percentage of available stock that can be allocated to a customer with this code. Specify a number between 0 and 100.

**Description** - Specify a description of the code.

- Click **Save**.

#### Entering Customer Order Types

Use customer order types to classify the orders you place in the Order Management window.

To specify customer order types:

- Click **Customer Order Types**.
- Click **Insert**.
- Specify this information:

**Order Type** - Specify the order type.

**Description** - Specify a description of the order type.

- Click **Save**.

#### Specifying WIP/VAS IDs

Your license key governs your access to WIP/VAS functionality. Contact your Infor Global Solutions sales associate for more information.

**Note:** Before you can enter WIP/VAS information, you must select the **WIP/VAS Enabled** check box. If WIP/VAS is not enabled, none of the WIP/VAS features apply to allocation and the features for maintaining and using WIP/VAS are not visible.

WIP/VAS (Work In Process/Value Added Services) are customization services that you perform on inventory. While not completely manufacturing in nature, WIP/VAS requires the management of inventory moving in and out of service sections in a warehouse. WIP/VAS starts during order entry, when a customer requests one of these specialized services. WIP/VAS is only for warehouses or distribution centers with the capability to manage the services. WIP/VAS specifications are also available at the customer order line level.

Use the WIP/VAS dialog box to specify the WIP/VAS tasks that can be performed on inventory. To specify WIP/VAS tasks:

- Click **WIP/VAS**.
- Click **Insert**.
- Specify this information:

**WIP/VAS ID** - Specify an ID for the WIP/VAS you perform on a part.

**Description** - Specify a description of the ID.

**Unit Price** - Specify the price per unit for the WIP/VAS activity.

- Click **Save**.

#### Entering Customer Types

The customer types dialog box becomes available when you select the **Allocation Require Warehouse/Location** check box.

Customers have different order requirements. Some customers may have complex order fulfillment schedules to which they must adhere. Others may have specific allocation needs at one warehouse, or various warehouses scattered across the country. Assign ranks to customers according to the degree of their order fulfillment requirements.

Assign types to customers in Customer Maintenance. To specify customer types:

- Click **Customer Types...**.
- Click **Insert**.
- Specify this information:

**Type** - Specify the ID for this customer type.

**Description** - Specify a description for this customer type.

**Priority** - Specify the priority for this customer type. The priority is used to sort customers during the allocation process. The lower the number, the higher the priority of customer.

**Allocation Fence** - Specify a numeric allocation fence for this priority. An Allocation Fence is used to determine which orders are considered by the Allocation Utility. If an order's required ship date is outside the allocation fence, the order is ignored during the allocation process.

**Reallocate** - To remove existing allocations when the Allocation Utility is run a second time, select the **Reallocate** check box. To retain existing allocations when the Allocation Utility is run a second time, clear this check box.

**Auto Allocate** - To automatically create allocations for this customer type, select the **Auto Allocate** check box.

**Allocation Level** - Click the arrow and specify the allocation level to use. You can select:

**None** - No allocation is made.

**Partial Allocation** - This selection allows you to allocate supply to the work order requirement, but makes allocation optional.

**Full Allocation** - This selection requires you to make sure that work order requirements have allocated supply before you firm or release the requirement.

- Click **Save**.

#### Entering Harmonized Tariff Schedule (HTS) Codes

Use HTS codes when shipping to and receiving from international locations. To enter Harmonization codes:

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

#### Specifying Part Alias Types

Part aliases are alternate names for part IDs. Use Part Alias types to classify the alternate names you use.

To specify part alias types:

- Click **Part Alias Types...**.
- Click the **Insert** button.
- Specify this information:

**Part Alias Type** - Specify the part alias type.

**Description** - Specify a description of the part alias type.

## Using the Defaults Tab

Use the Defaults tab to specify requisition approval group names, how purchase requisition approval tasks are generated, and requisition rejection codes.

To specify this information:

- Click the **Defaults** tab.
- In each of the Approval Label fields, specify the label to display on the Approval tab of the Purchase Requisition Entry window.
- To generate all purchase requisition approval tasks for groups simultaneously, select the **Generate All Tasks Simultaneously** check box. To generate approval tasks in sequential order, clear the check box. When you clear the check box, the first group must approve the purchase requisition before the second group's approval tasks are generated.
- To require users to enter passwords for fields you have secured, select the **Passwords Required for Secured Fields** check box.
- To create one batch per transaction when you post to the general ledger, select the **Create Financial Batches with Only One Transaction**. If you post multiple transactions to the general ledger at the same time, a batch is created for each transaction. Clear this check box to create one batch for all transactions in the posting.
- In the AP/AR Cash Management section, specify the default bank accounts to use for this entity. You can override the defaults on individual transactions. Specify this information:

**Cash Receipt Default Bank Acc**t - Click the browse button and select the default account to use for cash receipts. You can select any bank account associated with the current entity.

**Cash Payment Default Bank Acct** - Click the browse button and select the default account to use for payments. You can select any bank account associated with the current entity.

- In the Customer Balance Method section, specify how to calculated the customer's open balance. Click one of these options:

**Orders, uninvoiced shipments and unpaid invoices** - Click this option to calculate the customer's open balance as the total of open orders, uninvoiced shipments, and unpaid invoices.

**Uninvoiced shipments and unpaid invoices** - Click this option to calculate the customer's open balance as the total of uninvoiced shipments and unpaid invoices. Open customer orders are excluded from the calculation.

**Unpaid A/R Invoices** - Click this option to calculate the customer's open balance as the total of unpaid accounts receivable invoices only.

- Click **Save**.

### Specifying Task Rejection Codes

Before creating any purchase requisitions, add the rejection codes to use to your database. Rejection codes help you classify rejected purchase requisition tasks.

Purchase Requisition Entry and ECN Entry share a common Rejection Code table. This feature is available from both the ECN tab in Site Maintenance and the Default tab in Accounting Entity Maintenance.

- Click **Rejection Codes**.
- Click **Insert**.
- Specify this information:

**Code** - Specify an identifier for the rejection code.

**Description** - Specify a description for this code.

- Click **Save**.
