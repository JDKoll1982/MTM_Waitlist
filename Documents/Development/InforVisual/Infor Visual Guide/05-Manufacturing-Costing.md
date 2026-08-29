# Chapter 5: Manufacturing Costing

This chapter includes this information:

**Topic Page**

[What is Costing? 5-2](#_bookmark192)

[Setting Up Costing 5-3](#_bookmark197)

[Costs and Accounts 5-18](#_bookmark222)

[Preparing Manufacturing Journals 5-33](#_bookmark314)

[What are Costing Tools/Audits? 5-56](#_bookmark437)

# What is Costing?

To determine an organization's profits and losses, the costs associated with the production of the end product must be accumulated, measured, and recorded.

To set up costing, you must determine how to calculate costs and which accounts to use to record the costs. Use the Costing tab in Accounting Entity Maintenance to determine how to calculate the costs you record. Use the Application Global Maintenance (Financials) window to assign default accounts to record the costs. In many cases, you can override the default account on an individual record.

After you set up how to measure costs and where to record them, costs are tracked when you perform transactions, such as inventory issues to work orders, purchases of inventory, and shipments of customer orders. To post these costs to your general ledger, run the Costing Utilities to calculate the current costs, then use the Post Manufacturing Journals window to post the transactions to your general ledger.

You can analyze costs before or after you post them in the Costing Tools window. Costing Tools show when an expected cost does not match to the cost actually applied.

This chapter describes:

- How to set up costing, including selecting a costing method
- How to assign accounts to use for costing
- How to analyze costs

## Types of Manufacturing Costs

Four types of costs are typically incurred during manufacturing. These costs are:

**Material** - Material cost is the cost of the raw materials that comprise your product.

**Labor** - Labor is the cost incurred by employees. Labor costs can be direct or indirect. Direct labor costs are incurred when raw materials are assembled into finished goods. Indirect labor costs are overhead costs that cannot be directly charged to the manufacture of a product. Indirect labor includes costs such as vacation time and administrative time.

**Burden** - Burden costs are overhead expenses, such as rent, utilities, and depreciation. Burden costs can be associated with the purchase and issue of material and with the operation of shop resources.

**Service** - Service costs are incurred when an outside agency performs an operation to a part. The costing method and costing settings you select determine how these costs are calculated.

# Setting Up Costing

To access the Costing tab, select **Admin**, **Accounting Entity Maintenance**. Click the **Costing** tab.

**Caution:** The costing options that you select, especially costing method, drastically affect how the costing portion of VISUAL operates. Before deciding on any of these options, be sure to read the Costing chapter of the System-wide guide. Do not make a permanent selection without fully understanding the implications; if you are unsure, contact your sales associate, or Customer Support. After you begin executing transactions, you CANNOT change these options.

## About Costing Methods

You can select one of these costing methods:

**Standard Costing** - Standard Costing values every inventory transaction at the cost standards you set in Part Maintenance at the time of the transaction. Differences between the part standard value and the actual cost are recorded in variance accounts in the general ledger.

**Actual Costing** - Actual Costing uses the source of the raw material cost to define the value of all inventory in the system. It uses the hourly labor rate of the employee that creates the labor ticket and the invoice value of any services received. Actual costing uses your First-In-First-Out (FIFO) rules to assign value when the inventory is consumed.

If you are licensed to use Projects/A&D functionality, you must select Actual as your costing method in all entities. If a particular entity does not engage in project work, that entity must still use the Actual costing method if you have applied a projects/A&D license.

**Average Costing** - Average costing also uses the source of the raw material cost, but calculates costs based on the average material cost in your inventory. Average Costing calculates the current running average of inventory for each part and assigns that value when inventory is consumed.

The options on the Costing tab become available or unavailable based on the costing method you choose.

In addition to specifying a costing method for finished goods, you also specify a costing method for work in process (WIP).

## Considerations Before Choosing a Costing Method

Certain functionality is only available if you use a particular costing method. If you intend to use the functions described in this section, select a costing method that is compatible with the functions.

#### Projects/A&D

If you have applied a Projects/A&D license to your database, you must use the Actual costing method and the By Part Location FIFO Method / Inventory Grouping setting.

### Landed costs

If you intend to use landed costs, you must use either the Average or Actual costing method. You cannot use the Standard costing method. When you enable landed costs, you can link multiple Accounts Payable Invoices to a single purchase receipt line.

You can activate the landed costs feature in Financials Application Global Maintenance.

### Consignment

If you use Actual or Average Costing methods and you store consigned items from your customers or vendors, you must use By Part Location as your FIFO Method / Inventory Grouping. If you use Actual or Average Costing methods with FIFO By Part, then the From Vendor and From Vendor location types are removed from Warehouse Maintenance, and Consignment Receiving cannot be used.

If you use Actual or Average Costing methods and only use consignment to store your inventory at vendors or customers, you can use either FIFO by Part or FIFO by Part Location.

You can use Standard Costing methods for your consigned inventory without restriction. See the "Consignment" chapter in the Inventory user's guide.

## Setting Up Standard Costing

Standard costing is the method of comparing predetermined estimates of cost to the actual expenditures for building/purchasing a product. Any difference between the standard and actual is a **variance**. With standard costing, every part, component, operation, and assembly has a standard cost. Typically, you would derive standard costs annually, usually in conjunction with the annual physical inventory.

To set up standard costing, specify the Standard option in Accounting Entity Maintenance and determine other costing settings. Use Part Maintenance, Shop Resource Maintenance, Service Maintenance, and Employee Maintenance to set up cost standards.

### Specifying Standard Costing Options

This procedure describes how to set up options specific to Standard costing method. Refer to Setting Up a WIP Costing Method and Selecting a Receipt Exchange Rate Date to complete costing set up.

- Select **Admin**, **Accounting Entity Maintenance**.
- In the Costing Method section, select the **Standard** option.
- In the Labor Cost Basis section, select one of these options:

**Hours Worked** - Select this option to calculate labor costs by multiplying the hours reported on a labor ticket by the standard cost of the operation.

**Quantity Produced** - Select this option to calculate labor costs by multiplying the quantity completed for an operation by the standard cost per unit for the operation.

- In the Costing Between Levels section, specify how to process costs when one fabricated part is a material requirement in another fabricated part.

When you fabricate a part, costs are incurred in all four cost categories: material, labor, burden, and service. When you use a fabricated part as a material requirement, you can combine the four cost categories and use the total as the material cost of the requirement, or you can keep each cost category separate.

Choose one of these options:

**Fold to Material Cost** - If you select this option, the Material, Labor, Burden, and Service cost of an internally fabricated part are summed into the material cost when it is required in another fabricated part. The entire cost of the requirement is counted as material cost.

**Keep Separate Costs** - If you select this option, the Material, Labor, Burden, and Service costs of an internally manufactured material requirement contribute to those individual categories in parent assemblies.

For example: where Fabricated Part B requires Fabricated Part A.

|     | **Service** | **Material** | **Labor** | **Burden** |
| --- | --- | --- | --- | --- |
| Costs Totals for **Fabricated Part A** | 250 | 500 | 200 | 50  |
| Costs Totals for **Fabricated Part B** | 100 | 100 | 100 | 100 |
| (Not including costs of Part A) |     |     |     |     |
| **Totals for Part B Including Part A** |     |     |     |     |
| With Keep Separate Costs | 350 | 600 | 300 | 150 |
| With Fold to Material Cost | 1100 | 100 | 100 | 100 |

With Keep Separate Costs, each column is simply added together separately. With Fold to Material Costs, all costs of Part A are totaled (250+500+200+50=1000)

and this total is added to the material cost only of Part B (100+1000=1100)

- Click **Save**.

### Specifying Standards

Use Part Maintenance, Shop Resource Maintenance, and Service Maintenance to set up standard costs.

#### Specifying Part Standard Costs

Use Part Maintenance to specify the standard costs for manufactured and purchased parts. You can also define which G/L accounts to use to record the costs.

For fabricated parts, you can specify material, labor, burden, and service standard costs. For raw materials (parts that are not fabricated), you specify materials costs. For a purchased part, you can also specify a fixed cost.

The cost standards are used in these transactions:

- Material adjust-in transactions use these values to determine the transaction costs. The quantity adjusted-in is multiplied by the standard costs to determine the FIFO layer's value.
- Engineering masters use the standard costs for their estimates. Based on the Engineering masters, costs will flow next to the work order estimate cost when they are created.
- Purchase order receipts use the standard costs when goods are received. The total received amount is compared to the accounts payable voucher costs to determine purchase price variances.
- Finished Goods receipts use standard costs for receipts. The costs received are compared to the incurred WIP amounts to determine Finished Goods Variances.

You can also specify burden amounts or rates that apply to purchases and material issues. Burden is the capitalization of current overhead manufacturing costs to the parts that are manufactured during that period. Purchase burden is applied when materials are purchased and received into inventory. Issue burden is applied when materials are issued to a job. Issue burden is carried in the part's inventory value until it is shipped. Inventory value moves to Costs of Goods Sold upon shipment.

If you are licensed to use multiple sites, specify costing information at the site level. To specify standard costs for parts:

- Select **Inventory**, **Part Maintenance**.
- If you are licensed to use multiple sites, click the Site ID arrow and select the site to use to specify costs. If you are licensed to use a single site, this field is unavailable.
- Click the **Part ID** browse and select the part for which you are establishing standards.
- In the Costs section of the Costing tab, specify this information:

**Material** - For fabricated parts, enter the per unit cost of the raw material you use to produce the part. For purchased parts, enter the per unit cost, including price and any other per unit cost you incur in the purchase of this part.

**Labor** - For fabricated parts, enter the labor cost standard to produce this part. For purchased parts, you typically do not incur labor costs and should enter zero.

**Fixed** - If you have any fixed costs associated with purchased parts, such as vendor setup charges, enter them in the Fixed field. Fixed charges are not per unit charges. They are one-time only charges regardless of quantity. You cannot enter fixed costs for Fabricated parts.

**Burden** - For fabricated parts, enter your estimated cost of the burden to produce this part.

For purchased parts, you cannot specify a value in this field. If you specify a value in the Purchase Burdens section, the value is inserted into this field.

**Service** - For fabricated parts, enter the estimated cost of outside services necessary to produce this part.

**Total** - The total of all costs is calculated and inserted in this field.

- In the Issue Burdens section, specify the costs incurred internally when issuing the part to a work order. This does not include shipping costs.

**Percent** - If a percentage of the material cost is incurred when the material is issued, specify the percent in this field.

**Per Unit** - If burden is applied per unit when the material is issued, specify per unit cost in this field.

You can specify either a percent or a per unit cost, or both.The burden cost is applied when the part is issued to a work order.

- In the Purchase Burdens section, specify the costs incurred when purchasing the part. This does not include shipping costs. These fields are available only if the part is a purchased part.

**Percent** - If a percentage of the material cost is incurred when materials are purchased, specify the percent in this field. The percent value is multiplied by the material cost and the result is inserted in the Burden field.

**Per Unit** - If burden is applied per unit when the material is purchased, specify per unit cost in this field. The value is inserted in the Burden field.

- Click **Save**.

#### Calculating Fabricated Part Cost Standards from Engineering Masters

Use the Implode Costs function to calculate cost standards for your fabricated parts based on the raw materials, resources, and services on the fabricated part's engineering master.

You can use this function at the site level only.

If your fabricated part is made up of purchased parts, these calculations are made to determine the costs for the fabricated part:

**Material** - The material costs of all material requirements, plus the fixed costs for the material requirements, are added and inserted in the Material field for the parent part.

**Labor** - The labor costs for any shop resources used in operations are added and inserted in the Labor field for the parent part.

**Burden** - The burden costs for shop resources in operations plus the burden costs for material requirements are added and inserted in the Burden field for the parent part.

**Services** - The service costs from operations are combined and inserted into the Services field.

Fabricated parts can be made up of other fabricated parts. If your parent part uses fabricated parts for material requirements, costs are determined based on your selection in the Costing Between Levels section on the Costing tab in Accounting Entity Maintenance. If you selected Fold to Material Costs, all cost categories for fabricated material requirements are combined and used as the material cost for the requirements. If you selected Keep Separate Costs, the Material, Labor, Burden, and Service costs of an internally manufactured material requirement contribute to those individual categories in parent assemblies.

To calculate standard costs:

- In Part Maintenance, select the Site ID to use.
- Select **Maintain**, **Implode Costs**.
- Click one of these options:

**Current Part Only** - If you specified a part ID in Part Maintenance, click this option to implode the currently selected part.

**Selected Parts** - To select multiple parts to implode, click this option, then click the browse button and select the parts to implode. You can select parts from within the selected site only.

**All top-level parts** - To implode costs for all top level parts in the site, click this option. Top level parts are parts with engineering masters.

- Specify these settings:

**Multi-Level** - If the part you are imploding includes fabricated parts as material requirements, select this check box to also implode the fabricated materials.

**Permanently save all levels** - If you selected the Multi-Level check box and are imploding a single part, select this check box to permanently save the new costs of any fabricated material requirements. If you are imploding multiple parts, this check box is automatically selected.

- Click **Ok**.

#### Specifying Shop Resource Standard Costs

Use Shop Resource Maintenance to specify the labor and burden standards for shop resources. When you use shop resources in an engineering master, the standards you specify in shop resource maintenance can contribute to the total standard cost of the parent part in the engineering master.

If you are licensed to use multiple sites, specify costing information at the site level. To specify standard costs for shop resources:

- Select **Eng/Mfg**, **Shop Resource Maintenance**.
- If you are licensed to use multiple sites, click the Site ID arrow and select the site to use to specify costs. If you are licensed to use a single site, this field is unavailable.
- Click the **Resource ID** browse button and select the resource for which you are setting up costs.
- In the Costs section, specify the labor costs for this resource. Specify this information:

**Setup per hour** - Specify the hourly rate for setting up the resource.

**Run per hour** - Specify the hourly rate for production.

**Run per unit** - Specify the per unit cost for the resource.

- In the Burden Costs section, specify the burden costs for this resource. You can specify particular burden rates, or you can specify burden as a percentage of the labor rates for the resource. Specify this information:

**Burden/hour (setup)** - To specify a particular set up burden rate per hour, specify the rate in this field.

**Burden/hour (setup) Percent** - To specify set up burden as a percentage of the labor set up costs, specify the percentage in the Percent field.

**Burden/hour (run)** - To specify a particular run burden rate per hour, specify the rate in this field.

**Burden/hour (run) Percent** - To specify run burden as a percentage of the labor run costs, specify the percentage in the Percent field.

**Burden/unit (run)** - To specify run burden per unit produced, specify the rate in this field.

**Fixed Burden** - To specify a one-time burden cost when this resource is used, specify the amount in this field.

- Click **Save**.

#### Specifying Service Standard Costs

Use Service Maintenance to specify standard costs for services. The standard costs you specify in Service Maintenance contribute to the Service costs for a fabricated part.

If you are licensed to use multiple sites, specify cost information at the site level. To specify service standard costs:

- Select **Eng/Mfg**, **Service Maintenance**.
- If you are licensed to use multiple sites, click the Site ID arrow and select the site to use to specify costs. If you are licensed to use a single site, this field is unavailable.
- Click the **Service ID** browse button and select the resource for which you are setting up costs.
- Specify this information:

**Cost Per Unit** - If the costs are charged on a per unit basis, specify the cost in this field.

**Base Charge** - If one-time base cost is charged for this service, specify the cost in this field.

**Minimum Charge** - If a minimum cost is charged for this service, specify the cost in this field. If the service ordered costs more than the minimum charge, then the system charges the amount ordered. If the service ordered cost is less than the minimum charge, then the minimum charge is used.

- Click **Save**.

## Setting up Actual Costing

**Note:** If you have applied a Projects/A&D license to your database, use the Actual costing method.

Use the Costing tab in Accounting Entity Maintenance to set up actual costing. Actual costing uses these costs:

**Labor** - The pay rates set up for employees in Employee Maintenance are used for labor costs in transactions. If the employee is paid hourly, the hourly rate is used. If the employee is a salaried employee, the value used in a transaction is the prorated share of the employee's salary. You can override labor rates in Labor Ticket Entry. You can also calculate overtime in Labor Ticket Entry.

**Material** - Material costs depend upon the source of raw material costs and the FIFO method you choose. You can choose either Purchase Orders or A/P Invoices as the source of the material cost. You can choose either by part or by part location as your FIFO method. If you choose by part, then the location where the part was received is ignored. When you issue a part, the cost used is the price of the oldest part in your inventory regardless of location. If you choose by part location as your FIFO method, then location is considered when determining the cost of a part. When you choose by part location the cost of the oldest part in the location from which you are issuing the part is used as the material cost.

**Burden** - Burden costs depend upon the selection you make in the Burden Basis section. If you select Determined by Resource Burden, then the burden information specified in the Burden Costs section in Shop Resource Maintenance is used. If you select Determined by Operation Burden, then the burden information specified on the operation card for the work order is used.

**Services** - Services costs depend upon the source of raw material costs you choose. If you choose Purchase Orders, then the costs of services match the costs on the purchase order. If you choose A/ P Invoices, then the costs of services match the costs on the A/P Invoice.

This procedure describes how to set up options specific to Actual costing method. Refer to Setting Up a WIP Costing Method and Selecting a Receipt Exchange Rate Date to complete costing set up.

To set up actual costing:

- In the Costing Method section, click the **Actual** option.
- In the FIFO Method/Inventory Grouping section, select how to determine first in-first out costs. Click one of these options:

**By Part** - Click this option to calculate costs based on the order in which you received the part regardless of the location. If you are licensed to use multiple sites, the locations must be within the same site.

For example, you receive the same parts, four times, at four prices, into these locations:

| Day 1 | Location 1 | 10@\$1 |
| --- | --- | --- |
| Day 2 | Location 2 | 10@\$2 |
| Day 3 | Location 1 | 10@\$3 |
| Day 4 | Location 2 | 10@\$4 |

You then issue 15 of those parts from location 1. If you select **By Part**, costs are calculated as: 10@\$1 plus 5@\$2 totaling \$20

Because you received the part at \$1 first then \$2, these prices are used for **By Part** calculations.

**By Part Location** - Costs are calculated based on the order in which you received the part into the location.

For example, you receive the same parts, four times, at four prices, into these locations:

| Day 1 | Location 1 | 10@\$1 |
| --- | --- | --- |
| Day 2 | Location 2 | 10@\$2 |
| Day 3 | Location 1 | 10@\$3 |
| Day 4 | Location 2 | 10@\$4 |

You then issue 15 of those parts from location 1. If you select **By Part Location**, costs are calculated as:

10@\$1 plus 5@\$3 totaling \$25

Because FIFO is by part location, only those parts in location 1 are used.

If you are licensed to use VISUAL DCMS and part trace, you can use trace IDs in your FIFO calculations. Select the Actual Costing by Trace ID check box to issue parts based on trace ID. For example, presume you received four Part X at \$10 on trace ID 100, then receive four more at

\$20 on trace ID 110. If you issued three based on trace ID 110, the parts would be valued at \$20 each.

If you are licensed to use Projects/A&D, you must use By Part Location as your FIFO method.

- In the Source of Raw Material Cost section, specify the transaction to use as the source of costs for purchased materials. Click one of these options:

**Purchase Orders** - Select this option to use the price specified on the purchase order as the source of raw material cost. The cost is taken from the purchase order when you receive the part into your inventory. This option is only preferable if you are not using VISUAL Financials.

**A/P Invoices** - Select this option to use the price paid on the A/P invoice as the final cost for the part. When you select this option, costs are first taken from the purchase order as an estimate, then updated from the invoice for the purchase. This option provides a more accurate assessment of costs. If you use VISUAL Financials, you should select this option.

- In the Costing Between Levels section, specify how to process costs when one fabricated part is a material requirement in another fabricated part.

When you fabricate a part, costs are incurred in all four cost categories: material, labor, burden, and service. When you use a fabricated part as a material requirement, you can combine the four cost categories and use the total as the material cost of the requirement, or you can keep each cost category separate.

Choose one of these options:

**Fold to Material Cost** - If you select this option, the Material, Labor, Burden, and Service cost of an internally fabricated part are summed into the material cost when it is required in another fabricated part. The entire cost of the requirement is counted as material cost.

**Keep Separate Costs** - If you select this option, the Material, Labor, Burden, and Service costs of an internally manufactured material requirement contribute to those individual categories in parent assemblies.

For example: where Fabricated Part B requires Fabricated Part A.

|     | **Service** | **Material** | **Labor** | **Burden** |
| --- | --- | --- | --- | --- |
| Costs Totals for **Fabricated Part A** | 250 | 500 | 200 | 50  |
| Costs Totals for **Fabricated Part B** | 100 | 100 | 100 | 100 |
| (Not including costs of Part A) |     |     |     |     |
| **Totals for Part B Including Part A** |     |     |     |     |
| With Keep Separate Costs | 350 | 600 | 300 | 150 |
| With Fold to Material Cost | 1100 | 100 | 100 | 100 |

With Keep Separate Costs, each column is simply added together separately. With Fold to Material Costs, all costs of Part A are totaled (250+500+200+50=1000)

and this total is added to the material cost only of Part B (100+1000=1100)

- In the Burden Basis section, select the basis to use when calculating burden rates. Click one of these options:

**Determined By Resource Burden** - Click this option to calculate burdens based on the rates you enter for the resource. You cannot override these rates on the work order's operation card.

**Determined By Operational Burden** - Click this option to calculate burdens based on the rates you enter on the work order's operation card. This can be useful if you periodically update your burden costs at the resource level without affecting current work orders.

- Click **Save**.

## Setting up Average Costing

If you use average costing, the costs of raw materials are valued based on the average cost of the part in your site's inventory. If you are licensed to use multiple sites, average costs are calculated on a site-by-site basis. When you issue a material, the average cost is recalculated.

Average costing uses these costs:

**Labor** - The pay rates set up for employees in Employee Maintenance are used for labor costs in transactions. If the employee is paid hourly, the hourly rate is used. If the employee is a salaried employee, the value used in a transaction is the prorated share of the employee's salary. You can override labor rates in Labor Ticket Entry. You can also calculate overtime in Labor Ticket Entry.

**Material** - The total cost of all parts in your inventory is divided by the total number of parts to determine the average cost.

The source of raw material cost you choose determines the values used to calculate the total cost of the parts in your inventory. You can choose either Purchase Orders or A/P Invoices as the source of the material cost. The FIFO method you choose determines how the total value of parts in your inventory is recalculated when you issue parts.

For example, presume you received parts at these values into these locations:

| Day 1 | Location 1 | 3@\$1 |
| --- | --- | --- |
| Day 2 | Location 2 | 3@\$2 |
| Day 3 | Location 1 | 3@\$3 |
| Day 4 | Location 2 | 3@\$4 |

The total value of the part in your inventory is \$30, and the total number of parts in your inventory is

12\. If you issued one part from Location 1 to a work order, the cost of the part would be valued at

\$2.50 (\$30 divided by 12). The second part would be valued at \$2.63 (\$30 minus the actual value of the part equals \$29, divided by 11). The third part would be valued at \$2.80 (\$28 divided by 10). The fourth part would be valued at \$3.00 (\$27 divided by 9).

The value of the fifth part depends upon your FIFO setting. If you select By Part Location, then Location 1 would be used as the source of the material cost, and \$3 would be subtracted from the total value of your inventory for a result of \$27. The result would be divided by 8 for a value of \$3.00. If you select By Part, then Location 2 would be used as the source of the material cost, and \$2 would be subtracted from the total value of your inventory. The value of the part would be \$3.125 (\$28 divided by 8).

**Burden** - Burden costs depend upon the selection you make in the Burden Basis section. If you select Determined by Resource Burden, then the burden information specified in the Burden Costs section in Shop Resource Maintenance is used. If you select Determined by Operation Burden, then the burden information specified on the operation card for the work order is used.

**Services** - Services costs depend upon the source of raw material costs you choose. If you choose Purchase Orders, then the costs of services match the costs on the purchase order. If you choose A/ P Invoices, then the costs of services match the costs on the A/P Invoice.

This procedure describes how to set up options specific to Average costing method. Refer to Setting Up a WIP Costing Method and Selecting a Receipt Exchange Rate Date to complete costing set up.

To set up average costing:

- In the Costing Method section, click the **Average** option.
- In the FIFO Method/Inventory Grouping section, select how to determine first in-first out costs. Click one of these options:

**By Part** - Click this option to calculate costs based on the order in which you received the part regardless of the location.

**By Part Location** - Costs are calculated based on the order in which you received the part into the location.

- In the Source of Raw Material Cost section, specify the transaction to use as the source of costs for purchased materials. Click one of these options:

**Purchase Orders** - Select this option to use the price specified on the purchase order as the source of raw material cost. The cost is taken from the purchase order when you receive the part into your inventory. This option is only preferable if you are not using VISUAL Financials.

**A/P Invoices** - Select this option to use the price paid on the A/P invoice as the final cost for the part. When you select this option, costs are first taken from the purchase order as an estimate, then updated from the invoice for the purchase. This option provides a more accurate assessment of costs. If you use VISUAL Financials, you should select this option.

- In the Burden Basis section, select the basis to use when calculating burden rates. Click one of these options:

**Determined By Resource Burden** - Click this option to calculate burdens based on the rates you enter for the resource. You cannot override these rates on the work order's operation card.

**Determined By Operational Burden** - Click this option to calculate burdens based on the rates you enter on the work order's operation card. This can be useful if you periodically update your burden costs at the resource level without affecting current work orders.

- In the Costing Between Levels section, Keep Separate Costs is selected. You cannot change this setting if you use Average costing.
- Click **Save**.

## Selecting a WIP Costing Method

To specify the WIP costing method, select one of these options:

**Actual** - Actual WIP values any receipts from Work In Process at the full value of the Work Order.

For example, you are making 100 parts and each part requires \$1.00 of material. On the first day you issue \$100 of material to the Work Order. If you finish 1 part on the first day it will be valued at \$100 - the full value of the Work Order. On the second day, when you receive the other 99 parts, the costing utility will re-value the receipt from previous day to \$1 and share the other \$99 with the other receipts. The value of an Actual WIP costed Work Order without any receipts is always zero.

**Projected** - Projected WIP values any receipts at the estimated unit cost based on the quantity received and the remaining quantity to complete. For example, you are making 100 parts and each part requires \$1.00 of material. On the first day, you issue \$100 of material to the Work Order. If you finish one part on the first day, the work order would produce a cost of \$1 for 1 part on the first day and then \$1 each for the remaining 99 parts the next day. Projected WIP receipt values will never exceed the actual cost in the job.

## Selecting a Receipt Exchange Rate Date

In the Receipt Exchange Rate section, specify the date to use to determine the exchange rate applied to purchased goods. Select one of these options:

**Use Receiver Date** - Select this option to apply the exchange rate as of the date the purchased goods are received.

**Use Invoice Date** - Select this option to apply the exchange rate as of the date of the invoice. The invoice date is either the date specified on the transaction or the current date, depending on your setting in the Effective Exchange Rate date section on the General tab.

### Specifying a POC Revenue Recognition Method

If you are licensed to use Aerospace & Defense modules, select a Percentage of Completion (POC) revenue recognition method. The primary difference between the two revenue recognition methods is how direct cost and burden amounts are calculated. With the Revenue First method, costs are calculated by applying the POC to a pro-rated share of each cost source. With the Cost to Cost method, the actual costs incurred are included in the revenue calculation.

To choose a POC revenue recognition method, click one of these options:

**Revenue First** - When you click this option, total revenue to be recognized is calculated first by multiplying the total price of the contract by the percentage of completion. Then, the total costs incurred are calculated by multiplying the total costs in the EAC by the percentage of the total cost each cost area represents. Then, the product is multiplied by the percentage of completion.

For example, presume your project has these costs and fees in the Estimate at Completion (EAC):

| **Cost Source** | **EAC Amount** | **Percentage of total cost** |
| --- | --- | --- |
| Labor | \$2,500 | 31.25% |
| Material | \$4,000 | 50% |
| Services/Other Direct Costs | \$500 | 6.25% |
| Burden | \$1,000 | 12.5% |
| Total Cost | \$8,000 |     |
| Fee | \$2,000 |     |
| Total Price | \$10,000 |     |

If the POC value at the end of the first period is 25%, then these calculations are made:

\$10,000 \* 0.25 = \$2,500. The total revenue recognized for the period is \$2,500. The \$2,500 includes these costs:

Labor = \$8,000 \* 0.3125 \* 0.25 = \$625

Material = \$8,000 \* 0.5 \* 0.25 = \$1,000 Services/ODC = \$8,000 \* 0.0625 \* 0.25 = \$125 Burden = \$8,000 \* 0.125 \* 0.25 = \$250

Total costs to be recognized = \$2,000

To calculate the total fee recognized, the total costs recognized are subtracted from the total revenue recognized:

\$2,500 - \$2,000 = \$500

In subsequent periods, the same calculations are made, then any previously recognized revenue is subtracted. For example, if the POC value at the end of the second period is 35%, these calculations are made:

\$10,000 \* 0.35 = \$3,500. The total revenue recognized is \$3,500 minus the \$2,500 already recognized, or \$1,000. The \$1,000 includes these costs:

Labor = \$8,000 \* 0.3125 \* 0.35 = \$875. \$875 - \$625 previously recognized = \$250 recognized in second period.

Material = \$8,000 \* 0.5 \* 0.35 = \$1,400. \$1,400 - \$1,000 previously recognized = \$400 recognized in second period.

Services/ODC = \$8,000 \* 0.0625 \* 0.35 = \$175. \$175 - \$125 previously recognized = \$50 recognized in second period.

Burden = \$8,000 \* 0.125 \* 0.35 = \$350 minus \$250 previously recognized = \$100 recognized in second period.

Total costs to be recognized in second period = \$800.

To calculate the total fee recognized, the total costs recognized for the period is subtracted from the total revenue recognized for the period:

\$1,000 - \$800 = \$200

In summary, these are revenue amounts recognized for the two periods:

| **Cost Source** | **EAC Amount** | **Percentage of total cost** | **Period 1 Revenue (25% POC)** | **Period 2 Revenue (35% POC)** |
| --- | --- | --- | --- | --- |
| Labor | \$2,500 | 31.25% | \$625 | 250 |
| Material | \$4,000 | 50% | \$1,000 | 400 |
| Services/Other Direct Costs | \$500 | 6.25% | \$125 | 50  |
| Burden | \$1,000 | 12.5% | \$250 | 100 |
| Total Cost | \$8,000 |     | \$2,000 | \$800 |
| Fee | \$2,000 |     | \$500 | \$200 |
| Total | \$10,000 |     | \$2,500 | \$1,000 |

**Cost to Cost** - When you click this option, revenue recognized for a period is comprised of the actual costs incurred plus a fee amount based on the POC. For example, presume your project has these costs and fees in the EAC:

**Cost Source EAC Amount**

Labor \$2,500

| **Cost Source** | **EAC Amount** |
| --- | --- |
| Material | \$4,000 |
| Services/Other Direct Costs | \$500 |
| Burden | \$1,000 |
| Total Cost | \$8,000 |
| Fee | \$2,000 |
| Total Price | \$10,000 |
| In the first period, you incur these costs: |     |
| **Cost Source** | **Actual Cost** |
| Labor | \$250 |
| Material | \$1,500 |
| Services/Other Direct Costs | \$125 |
| Burden | \$125 |
| Total Cost | \$2,000 |
| **POC for period** | **\$25%** |

To calculate the fee amount to recognize, the total fee specified on the EAC is multiplied by the POC:

\$2,000 \* 0.25 = \$500

The total amount recognized is \$2,000 + \$500 = \$2,500.

In subsequent periods, the same calculations are made, then any previously recognized revenue amounts are subtracted.

# Costs and Accounts

When you post a transaction, costs are recorded in various accounts in your general ledger. You set up default accounts in Application Global Maintenance (Financials).

For some types of costs, you can specify override accounts to use instead of the default accounts. You can specify override accounts in these areas:

- Shop Resource Maintenance
- Part Maintenance
- Customer Order Entry
- Purchase Order Entry
- Customer Maintenance
- Vendor Maintenance

You can use override accounts to provide more granularity to your manufacturing costs. For example, if you used a different burden override account for each of your shop resources, you could track exactly how much each shop resource contributed to your burden costs.

### Shop Resource Maintenance

Shop Resource Maintenance allows you to track and maintain the resources necessary to perform the manufacturing process as well as providing the necessary method to maintain accurate definitions of your shop resources. This information is vital for proper scheduling and work order costing. The information entered here will appear and be used in many other parts of the system.

This section discusses how to setup Shop Resources to report more specifically in the General Ledger.

In most cases, Labor and Burden are reported to the default Applied Labor and Applied Factory Burden accounts from the General Ledger Interface Accounts table.

In some cases, it may be best to show specific Applied Burden and Applied Labor in the General Ledger. To do this, you can assign an account to each of the shop resources, or to those for which you want to maintain detailed cost information in the General Ledger.

If the Account ID fields are left blank, the cost of this shop resource is added in with the default account for Total Applied Labor and Total Applied Burden as specified in the General Ledger Interface Accounts table.

Costs are entered into the fields that apply for that resource. These are the types of costs that can be selected for the resource:

#### Labor Costs

**Setup per hour** - Cost per hour to setup this resource.

**Run per hour** - Cost to run the resource for one hour.

**Run per unit** - Cost to run the resource per unit produced.

#### Burden Costs

**Burden / hour (setup)** - This is the burden cost per one hour of setup time. Use the Percent field and

/or the dollar amount setup burden for the resource.

**Burden / hour (run)** - This is the burden cost per run of the resource. Use the Percent field and / or the dollar amount setup burden for this resource.

**Burden / unit (run)** - This is the burden cost per unit produced.

**Fixed Burden** - A onetime cost (burden) charged when this resource is used.

### Part Maintenance

In Part Maintenance, you can override the default accounts in several ways.

In a Part record, you can override the default material, labor, burden, and service accounts by clicking the Accounting tab and specifying the overrides. For fabricated parts, you can override all four cost accounts. For purchased parts, you can override the material and burden accounts. Any overrides specified on the Accounting tab for a part take precedence over any override accounts specified elsewhere. If you do not set up override accounts, the default Inventory accounts are used instead.

You can also set up override accounts by product codes. You can override these accounts:

- Inventory (material)
- Inventory (labor)
- Inventory (burden)
- Inventory (service)
- Work in process (material)
- Work in process (labor)
- Work in process (burden)
- Work in process (service)
- Variance (material)
- Variance (labor)
- Variance (burden)
- Variance (service)
- Cost of Goods Sold (material)
- Cost of Goods Sold (labor)
- Cost of Goods Sold (burden)
- Cost of Goods Sold (service)

## Other Cost Sources

### Inventory Adjustments

Material adjust-in transactions use the cost standards in Part Maintenance to determine the transaction costs. Quantity adjusted-in times the Part Maintenance cost will extend to the FIFO layers value.

### Product Code G/L Interface Account Table Overrides

#### Accounts by Product Code

The accounts used by the Costing Utility program can be a function of the Product Code Table from Part Maintenance. Here you can specify the accounts to be used by a part's product code unless an override account was specified elsewhere. If no product code is specified in the part, then the appropriate override account is used.

You can specify these types of accounts:

**Revenue Account** - The default account for use when shipments are invoiced. You can override this account at the Customer Order Line. The default account is from the General Ledger Interface Accounts Table. (Default Accounts Receivable Sales Revenue).

**Adjustment** - The default to be used when making inventory adjustments. You can override this account when entering inventory adjustments in Inventory Transaction Entry. If not specified here, the adjustment account from the General Ledger Interface table is used.

**Inventory** - The Raw Material or Finished Goods account(s) for use when receiving, issuing, returning, or adjusting inventory for a specified product code. If not specified here, the account numbers specified in the Part Master and then the default from the General Ledger Interface Accounts table are used.

**Work In Process** - The default Work In Process account(s) for this product code. You can override this account at the Quote / Engineering Master / Work Order level(s). If not specified here or in the work order, the default account in the General Ledger Interface Accounts table is used.

**Variance** - The Purchase Price Variance Account for purchased parts or the Manufacturing Variance Account for fabricated parts. These accounts are only used in standard costing. The variance account is used for purchased parts in an actual cost system if costs are captured at Purchase Order price rather than invoice value. This variance is taken when the Accounts Payable invoice is matched to the Purchase Order Receiver in Accounts Payable Invoice Entry.

**Cost of Goods Sold** - The Cost of Goods Sold account used for the specified product code. You can override this account(s) at the Customer Order line item in Customer Order Entry. If not specified here or in Order Entry, the default COGS account(s) in the General Ledger Interface Accounts table are used.

#### Account Override Sequence Summary

Inventory:

**Raw & Goods Work in Process Cost of Goods Sold Finished**

- Part Master File 1. Work Order 1. Customer Order

Line

- Product Code Table

2\. Product Code Table

2\. Product Code Table

3\. G/L Interface Table 3. G/L Interface Table 3. G/L Interface Table

Purchase Price Variance:

**Manufacturing Variance Inventory Adjustments Revenue**

- Product Code Table 1. Inv. Transaction Entry 1. Customer Order Line
- G/L Interface Table 2. Product Code Table 2. Product Code Table
- G/L Interface Table 3. G/L Interface Table

Absorbed Labor and Burden:

**Burden Indirect Labor Expense**

- Shop Resource 1. Labor Ticket Entry
- G/L Interface Table 2. Indirect Code
- G/L Interface Table

### Manufacturing Variances

After you have set standards, all transactions regarding the manufacture of the part must be examined to determine variances, if any, from standard. The main categories of variances are:

- Material variance
- Labor variance
- Burden (overhead) absorption variance

### Material Variance

Material usage variances can be traced to one of four source areas:

- Unit of Measure
- Miscellaneous Items
- Scrap
- Substitution

Material variances can be caused by purchase price changes and the amount of material used. **Purchase price variances** (PPV) occur when the actual cost of the raw material is different from the established standard. If the actual cost is lower or higher than the established standard, this generates a favorable or unfavorable variance respectively. You should perform an analysis to determine if this is a one time occurrence, a result of a change in vendor pricing, or a change in purchasing procedures.

Material usage variances occur when more or less than the required material is used in the manufacturing process. Material usage variance can occur in manufacturing environments where materials have to be issued in bulk due to the way it is purchased and stocked. The standard may be based on the average number of units that can be produced from a given unit of stock issued to the floor.

Usage variance can also be caused by miscellaneous materials such as small hardware, wire, paint, etc. that are not included in the Bill of Material but are required to complete the product. This material may be inventoried when purchased and charged to a variance (expense) account as consumed.

Other causes of material usage variance are scrap and substitution. Scrap variances occur when defective parts are produced. Scrap, as a result of the manufacturing process, may already be built into the standard. Scrap due to error also creates a variance.

A material variance due to substitution can occur when it is necessary to use a material other than what is called for in the Bill of Material. This material may cost more or less, yield a different quantity, etc. This variance may be caused by a one time occurrence due to a unique situation or as the result of a change in the design of a product. This change can be the result of a cost improvement program or a change in vendors.

### Labor Variance

In a standard cost system, labor variance is the difference between the direct labor standard established and the actual amount paid. This variance can be composed of two pieces:

- Efficiency
- Rate

**Labor efficiency** **variance** is the difference between the standard hours required per the Bill of Material and the actual hours expended to produce the part, extended by the standard labor rate.

For example, if the standard is 10 hours at \$15 per hour and the actual is 8 hours, there is a favorable efficiency variance of \$30.

**Labor rate variance** is the difference between the standard hourly rate and the actual rate paid, extended by the actual hours worked.

Continuing with the above example, if the actual rate is \$18 per hour, the rate variance is an unfavorable \$24. The combined labor variance is a favorable \$6.

The above variances can also be affected by:

- Rework
- Unapplied labor
- Design changes

**Rework** occurs when a defective part is produced during the manufacturing process, but can be made usable with some additional processing. The added time required is not part of the standard and is included in the variance.

**Unapplied labor** occurs when it is not economical to apply a standard to some labor operations. Examples may include set up, painting or plating, and inspection.

**Design changes** can occur due to operations being added that were overlooked, or operations being deleted due to cost improvement programs.

### Burden (Overhead) Absorption

**Overhead** - Burden, overhead, or indirect manufacturing expense are any expenses that cannot be classified as direct material or direct labor. These include:

- Indirect labor payroll associated with the shop foreman, maintenance personnel, dispatchers
- Expense for rent, utilities, depreciation
- Non-inventory supplies
- Tools, and travel

These expenses are then allocated to the cost of the products.

A burden standard is established based on how the expense is capitalized to inventory. For every unit produced, a standard dollar amount (either fixed or calculated) is added to the inventory value of the unit.

**Absorption** - A burden absorption variance occurs when more or less expense than was actually incurred is capitalized to inventory in a given accounting period.

For example, assume the actual burden expenses incurred in a period are \$65,000 and the manufacturing process absorbs \$67,000 based on the allocation method used. The difference would be a \$2,000 favorable absorption variance.

## Manufacturing Cost

Whether using standard or actual costing, all transactions that change a part's inventory balance or the unit cost are captured as they occur.

In order to fully appreciate Costing, it is necessary to understand inventory valuation.

**Inventory Valuation** - There are a number of different methods to value inventory LIFO, FIFO and Average Cost. VISUAL uses the FIFO (First-in First-out) method of valuing inventory. The FIFO method of inventory valuation assigns cost to inventory in cost layers. Each addition (purchase, inventory receipt, or adjustment in) adds a new cost layer. Each subtraction (issue, sale, or adjustment out) removes one or more cost layers.

VISUAL uses two types of FIFO layers:

- Raw materials inventory contains layers by way of purchases from a supplier or when you create a return from an issue return from work-in-process back to inventory (in the case of an over issue).
- Work-in-process can also contain FIFO layers, which include multiple inventory issues for the same part on a work order. This is categorized as a WIP Issue Layer.

The examples in Table A[, on page 47](#_bookmark392), depict transaction flows in an actual cost database. In a standard cost database all inventory transactions are performed at the standard rate and therefore the melded issue costs carry the same value as the FIFO inventory layers.

**FIFO From Purchases** - Every receipt of raw material inventory or finished goods inventory (no distinction is made) is assigned a per unit price value. Raw material inventory FIFO layers are created when a purchase is received into inventory. The cost of the purchased inventory is derived by the number of units received times the per unit cost as identified on the purchase order. This value will also represent the purchase order accrual value. Each purchase creates a unique layer of inventory which displays the inventory valuation report accessed from the Reports menu.

**FIFO Layers from Issue Returns** - Issue returns (parts issued to a work order returned to inventory) also create a FIFO layer. When parts are issued to a work order their costs are added to work-in- process based on the raw material FIFO layer they came from. If the issue to WIP is derived from more than one layer then the costs of all the layers issued to fulfill the requirement are melded together to create a single WIP issue layer. Any subsequent issues of the same part to a work order will create an additional WIP issue layer. If a part on the work order is returned to inventory from work- in-process, a new cost layer is created with the layers value being derived from the WIP issue layers currently residing in the work order. If there is only one issue of a particular part for the work order the cost is straight forward. It would be the average unit cost (by melding original FIFO layers) times the number of units returned to inventory.

Where there are more than one raw material inventory "issues" to a work order the costs are returned from the WIP issue layers in FIFO order.

**FIFO Layers from Receipts and/or Adjust In** - Two other ways FIFO layers in inventory happen are through a receipt of inventory (finished goods) from a work order. The cost of the inventory layer is derived from the average unit cost on the work order times the number of units. If the receipt closes the work order (complete receipts) the entire cost of the work order at the time of the receipt is the value of the FIFO layer.

Adjustments into inventory create a separate cost layer in inventory for the per unit cost assigned during inventory transaction entry. If no cost is assigned at that time, the cost will be derived by the unit cost as set on the part master file when costing utilities is run.

### Inventory Transactions

There are 10 classes of inventory transactions. Each of these transactions either move goods in or out of inventory, or transfer between inventory locations. There are, therefore, 18 possible transaction types. Most transactions affecting inventory (with the exception of transfers) either add or remove costs from inventory.

#### Transaction Types (Basics)

**P/O Receipts** - The P/O Receipts are purchased items received into stock. When these costs are received they are assigned to an inventory warehouse location. The number of units purchased as well as the cost of the purchased part(s) will become a new cost layer of raw materials inventory. When the PO Receipt is received into inventory the entry is booked to record a purchase accrual (credit) with the offsetting debit to Inventory.

Inventory parts can be purchased directly to a work order. When these parts are received they are booked to raw materials inventory and immediately issued to the corresponding work order. The inventory transaction entry report will depict this process. Purchased parts that are purchased on behalf of an existing work order are given the same treatment as inventory issues when received into the warehouse. The only time these costs would become a FIFO layer is if they are subsequently received into inventory through an inventory issue return.

As stated above VISUAL uses the FIFO method of valuing inventory. However, there is another level of complexity to inventory valuation; part warehouse location. For each part in inventory, there may be multiple holding bins in the warehouse. Each inventory transaction affects one or more of these locations, and VISUAL must therefore maintain each of these locations in order to report proper inventory valuations if the FIFO by part locations in Accounting Entity Maintenance is set.

**Services** - In the same manner as purchased parts, services can be purchased directly from the manufacturing window and are treated by costing utilities as if they were purchased parts. Costs are booked to WIP with the offset to the purchase accrual account.

**Inventory Issues/Issue Returns** - Inventory issues are transactions that assign raw material inventory to a specific work order. Inventory issues will normally cause the reduction of one or more inventory layer(s). These costs become the basis of the material costs assigned to WIP. Inventory issue transactions are prepared by costing utilities to debit WIP and credit Inventory.

Issue returns are inventory parts sent back to the warehouse. Issue returns would most likely result from overissues or raw materials to a work order. Costing utilities accounts for these costs by a credit to raw materials inventory and a debit to WIP.

To improve the performance of costing utilities, an issue return with a monetary value of zero is evaluated only once during costing. After the first evaluation, these zero-value transactions are ignored by the costing utilities.

**Accounts Payable** - Costs can be directly assigned through accounts payable for items that did not undergo the normal purchasing process. In the invoice entry window these costs can be linked to the work order. In doing so, VISUAL requires the user to enter the GL code to cost the items. The GL Account assigned to an accounts payable invoice linked directly to a work order is treated as a clearing account. Costing utilities post and offset entry to the GL account and apply the transaction value to WIP.

**Warehouse Transferred** - The cost implication (i.e., treatment of warehouse transfers) is dependent on the setting for FIFO method in application global. If you select FIFO by Part, transfers from one location to another do not have any cost implications; inventory transaction entry simply records the movement (without cost implication) to the new location. If you select FIFO by part location in Accounting Entity Maintenance, cost movements between locations are recorded.

**Adjustments In/Out (Inventory)** - Usually, inventory adjustments stem from physical inventory counts. Typically, companies take physical counts to keep perpetual records in line with actual inventory on hand. Discrepancies between the actual inventory on hand and the perpetual records

require inward or outward adjustments. If an inward adjustment is required, VISUAL costs this adjustment as a new cost layer. Outward adjustments remove costs from an existing cost layer or potentially remove an entire cost layer from inventory. When entering adjustments through inventory transaction entry, you are required to assign a G/L account for the other side of the transaction. Raw materials inventory is either debited or credited depending on the adjustment type. You must define the G/L offset account to apply the other side of the transaction.

**Direct Labor / Burden** - Labor charges are applied during labor ticket entry. Whether entering labor charges through bar code or entering via a computer terminal station, costs are applied to the labor ticket area. All costs for **setup** and **run** times are recorded into WIP via costing utilities. In order to record direct labor in WIP, a debit is created to WIP labor, and a credit to Manufacturing Direct payroll.

Burden (overhead) represents costs associated with the cost of manufacturing without the ability to identify these cost to specific operations or jobs in WIP at any one point in time. For example, rent, lighting, general plant maintenance, and depreciation are costs that every manufacturing environment incurs but are not attributable to specific jobs in WIP.

Costing provides for the ability to set burden rates to attempt to capture these costs and allocate them to jobs that flow through WIP. You can apply burden to raw materials inventory as well as to specific operations used in the manufacturing process. Burden rates applied to raw materials inventory on the part master file will cause burden to be charged each time the part is issued to a work order. Burden costs assigned operations are applied to WIP at the time of Labor ticket entry.

The burden costs applied during labor ticket entry come from the settings assigned on the resource in shop resource maintenance. The burden rate is derived either from the operation set in shop resource maintenance, on the operation in the bill of materials. The determination of whether cost are extended from the resource or the work order depends on the **Burden Basis** setting as defined on the **Cost** tab in application global. If you select **Determined by Resource Burden**, the settings are examined in Resource Maintenance to determine the appropriate amount of burden to apply. If you select **Determined by Operation Burden** in application global, then the amounts applied on the work order are used to determine the amount of burden.

**Indirect Labor** - Indirect labor is applied in the same manner as direct labor; the only difference is the transaction type in the Labor Ticket Entry window must be set to **Indirect** rather than **Setup** or **Run**. Indirect labor is not charged to specific jobs. Costs associated with indirect labor are reclassed from manufacturing payroll to the appropriate manufacturing indirect labor G/L account(s).

**Shipments (Sales)** - Shipments of customer orders cause specific transactions to take place within the system. Costs accumulated in work in process are marked as **Closed**. Inventory costs (WIP) then flow through to cost of sales in the general ledger in two distinct entries. The first entry reclassifies work in process costs to finished goods inventory. The second entry records the transfer of finished goods inventory to Cost of sales to properly match costs with revenue as required by generally accepted accounting principles.

**Standard** - In standard costing, material is applied at the standard cost in the parts' database. Any difference between the purchase price of the material and the standard is taken as a variance when the Purchase Receiver is matched to the invoice in A/P Invoice entry. Labor and burden are applied based on the standard rates obtained from the shop resource.

**Actual** - With actual costing, cost information is collected as it occurs. In Accounting Entity Maintenance, you must specify the source of the raw material cost. The two options for Source of Raw Material Cost are:

- A/P Invoicing
- Purchase Order

**A/P Invoice Based** - Until you enter the invoice and VISUAL matches it to the receipt, A/P Invoice- based material costs are valued at purchase order dollar amounts. As the receipts are costed at actual invoice prices, the corresponding issues for these material receipts are then costed at the actual price. Because the actual cost is not known until invoiced, the standard cost is used from the part's database as a temporary value. When the actual value becomes available, VISUAL creates adjustment distributions (if the previous standard was posted to G/L) for the difference between the estimated (standard) cost and the actual cost.

**Purchase Order Based** - Purchase Order based material costs use the value from the purchase order. When the actual invoice is received and matched to the purchase order receipt, any difference between the purchase order and the invoice value is booked as a purchase price variance.

If you are costing at invoice value and the material requirement for a job is linked to a purchase order, the material is issued directly to the job upon receipt. Material is temporarily valued at the purchase order price, not the part standard. Materials that are purchased under an actual cost system not for a specific job are valued based on the First In First Out (FIFO) method of inventory valuation.

Actual costing uses the employee labor rate to cost labor transactions. Burden costs are applied based on the rates set up for the shop resource. This can be established based on one or a combination of these costs:

- Cost per Setup time
- Cost per Run time
- Cost per Unit produced
- Fixed Cost per Resource
- Percent of Setup and / or Run labor costs

There is a delay in the final determination of a work order cost when using actual costing. Before a final cost can be calculated, these conditions must be met:

- All operations are completed
- All material requirements are issued to the work order
- All material issues are fully costed (receipts for which the material came from have been costed, i.e. invoice received or sub work order receipt costed)
- The work order is closed

### What is Actual and Projected Cost?

**Actual cost** is the true value of the material and labor charged to a work order. Material value is based on the FIFO cost layers established when materials are purchased and / or subassemblies are finished and received into stock. Labor value is based on the rate per employee(s) working on the job and the number of hours worked by the employee(s). Burden cost is calculated based on the parameters, for each shop resource used, as defined in Shop Resource Maintenance.

**Projected cost** is calculated as the actual cost charged to the work order plus the remaining cost based on the operations and material requirements to be completed at the estimated costs. Material requirement costs to be completed are calculated based on the actual number of parts issued to the work order. If the material issues, per requirement, are equal to or greater than the estimated material requirement, VISUAL considers this complete. In other words actual will equal projected cost for this requirement.

If the material issued to a job is less than that required per estimate, VISUAL considers this requirement incomplete and calculates a remaining cost based on the remaining quantity to be issued at estimated cost.

Labor requirements to complete are based on the quantity you enter in the quantity complete field or a calculated quantity complete if the system is set to automatically compute quantity complete based on hours. The remaining cost is calculated for each requirement, based on the remaining units to complete, at the estimated hours multiplied by the estimated labor and burden rates.

### Determining Whether Actual Cost or Projected Cost is Used

Projected unit cost is calculated as Projected Cost / Desired Qty. This unit cost is then multiplied by the Received Quantity for a total Projected cost of the received quantity.

The total projected cost from this calculation is then compared to the total actual costs charged to the work order. If the total actuals are greater than the calculated projection of the received quantity, this projected cost is the value used for the quantity received. If total actual costs are less than projected, the actual cost is the value used for the quantity received.

If the quantity received is equal to or greater than the desired quantity, actual costs are used. See earlier in this chapter, for more information on Component Level Costing.

### Manufacturing Cost Flow Overview

VISUAL implements actual costs for work orders as a function of inventory and labor transactions placed against those work orders. All costs ultimately come from these two window controls.

When you first create a work order, it has no transactions. As inventory is issued and labor is posted to the work order, the work order is given actual cost. Depending on the costing method you have chosen for your system, the values used in these transactions are either based on part and resource standards or actuals. Purchase orders, when received, effectively have an actual cost also. Similarly, Customer orders, when shipped, have actuals associated with them. Just as work orders can be shown to have an actual cost by totaling the issues and labor against it, so can a purchase order and customer order by totaling the receipts and shipments, respectively, against them.

The Costing Utilities prepare inventory and labor transactions to be permanently Costed and Posted to the General Ledger. Not all costing actions can be carried out on-line-for example, during normal system interaction. A few actions must be performed in batch mode. The Costing Utilities program is designed to perform those batch functions. The functions performed are:

- Receipt Transaction Costing (Work Orders)

Shipments

- Inventory Transaction Costing
- Prepare Purchase Journal Transactions
- Prepare WIP / FG Journal Transactions
- Prepare Shipments Journal Transactions
- Prepare Part Adjustment Journal Transactions
- Prepare Indirect Tx Journal Transactions

### Purchase Receipts

Frequently run, daily or every other day Receipt Transaction Costing and Inventory Transaction Costing. As purchase order receipts become invoiced and entered, the Inventory Transaction Costing function takes the invoice cost information and costs the transactions that are being held because of missing cost information (received Purchase Order not yet invoiced). The Receipt Transaction Costing function checks open work orders and their receipts to "see" if all receipts can be costed. For example, when an invoice is matched to its receipt, that receipt can now be costed and any issue from that receipt (FIFO) can be costed. The costed issue has now affected the cost of a Work Order or Customer Shipment. In the case of the Work Order, the receipt of the finished product can now be valued with its final cost. The Receipt Transaction Costing function performs this step.

Direct Labor is reclassed from Payroll to Cost of Sale - Direct Labor Accounts. Indirect Labor is reclassed from Payroll to Manufacturing Indirect Payroll.

#### Costing Flowchart

Sale of Inventory

Cost of Sales

Revenue

WIP

Work Orders

Inventory

Raw Materials Finished Goods

Purchase

Service

Sale from Work

Burden

Labor

Indirect Labor

Factory Overhead

### Cost Flow Procedure

- Create Work Order & Requirements (Operations, Service, Costs, Status)
- Create Purchase Order for Material Requirements
- Receipt of Raw Material into Stock or to Work Order if Linked

Run Costing Utilities & prepare Purchase Journal Inv. temp valued at PO

- Enter A/P Invoices & Match to receipt & Post
- Issue Materials to Job if not linked

Run Costing Utilities & prepare Purchase and WIP Journals -Updates Material Costs to actual Applies labor & burden to work order

- Enter Labor Tickets for Operations on Job

Run Costing Utilities & prepare Purchase and WIP Journals -Updates Material Costs to actual Applies labor & burden to work order

- Finished product - Receive into stock & Ship - If linked to Customer Order = Ship

Run Costing Utilities & prepare WIP and Shipment Journals - Updates Work Order costs & FG Inv. value. Relieves FG and debits COGS for shipments.

- Invoice Forms - Print Invoices & transfer into A/R module for posting to G/L (Revenue recognition)

### Costing Utilities - Running Receipt Transaction Costing (Work Order Receipts)

A work order gathers actual cost from labor ticket postings or inventory transactions throughout its life; however, the majority of the time its final actual cost is unknown. When the work order becomes fully received or shipped, it is closed. After it becomes closed, the Receipt Transaction Costing function permanently costs receipts for the work order. When the permanent cost is established, the outgoing transactions can be costed (Issues of Work Order Receipts).

Run Receipt Transaction Costing if you are using either actual or standard costing. In actual costing, this option evaluates the total order to "see" if all inventory transactions and operations have been completed and final costed. If competed, this selection assigns a permanent cost to the receipt and marks the work order as closed. If the work order is reopened at a later date, this option reevaluates the work order for any cost changes and updates the receipt as needed. The issues from that receipt are also updated.

In standard costing, this option determines the Manufacturing Variance that is taken. All receipts in a standard cost system are valued at standard as soon as they are created. Depending upon costs incurred in WIP, a variance will be taken for any deviation from standard.

Receipt Transaction Costing performs these actions:

- Examines newly created labor transactions and inventory transactions of any permanently costed work order to "see" if changes have occurred since the work order became permanently costed. If you have added any new transactions, the corresponding work order is no longer permanently costed and is now reconsidered for costing.

Examines each work order that is not permanently costed and determines if the work order is costable. A work order can be permanently costed if these conditions are met:

- The order is fully received or marked closed.
- The operations of the order are fully reported or marked completed.
- The material requirements of the order are fully issued or marked completed.
- The material issue transactions of the order are fully costed. Invoices have been received and matched to receipts consumed by this work order.

If the work order being examined is costable, VISUAL takes its actual cost, determines the unit cost for each cost category (MLBS) and saves the result in each inventory receipt of the work order. Then it marks the work order as costed permanently (by setting COSTED_DATE to the current system date

/ time in the Work Order table).

- The first option allows you to assume that all operations and requirements for a work order are closed if the work order is closed (fully received to stock), regardless of the complete status of the individual requirements and operations. All material issues to the work order however, must have a final cost. For example, a work order may require 100 pieces of an item per the estimate. If 90 pieces have been issued and fully costed, this option assumes that the job can be final costed.
- The second option allows the Receipt and Inventory Transaction costing to continue to run until you have performed all updates to transactions. This box is checked by default. VISUAL is able to capture all activity for a change in a Transaction in one step rather than having to run this multiple times. For example, in actual costing, if an issue to a work order id fully costed by matching the Accounts Payable Invoice with the Purchase Order Receiver, the work order that received the material is considered for full cost. If the work order is closed, the receipt of the work order is fully costed and therefore, any issue (shipment or issue to a higher level work order) of the received work order, can now be costed. If this box is not checked, the receipt is not fully costed, and therefore, the issue of the fabricated item to a higher level work order is not costed.

A single pass (box not checked) first values receipts, and then the issues from those receipts. Because some receipts first require that issues are valued, a single pass does not always cost everything that can be valued. You should leave this option checked. The only time it is not necessary to leave this option checked is if all Bills of Materials for the product you fabricate are single level, and, very few returns are made. Check this when costing inventory at month's end. When the No Updates Performed message appears, all activity to date has been costed. Under normal circumstances, you should leave both of these options checked.

### Inventory Transaction Costing

The Inventory Transaction option from the Costing Utilities window causes incoming transactions to be related to outgoing transactions. Incoming costs represented by purchased part receipts or adjustments to inventory are matched to outgoing costs represented by material issues, shipments, and adjustments to inventory. Before any costs can be assigned to an outgoing transaction, VISUAL must first have a permanent cost for an incoming transaction.

The primary source of incoming costs is the purchased part receipt. Another source of incoming costs is a work order receipt from work in process. We already know that labor tickets are given a permanent cost when they are created. Purchased part receipts may not be given a permanent cost at any time of receipt. If costs for purchase part receipts are identified from Purchase Orders, then the transaction is given its final value upon creation. If costs for purchased part receipts are identified from A/P Invoices, then the transaction must wait for its permanent cost until the matching A/P Invoice is entered. After the purchased part inventory receipt is given its final, permanent value, costs are distributed from that transaction. This is done by connecting the receipt to one or more outgoing transactions for the same part. For example, a receipt of 1000 parts can be distributed to an issue of 1000 parts, 10 issues of 100 parts, or 1000 issues of 1 part, or any combination in between.

The order of assignment of transactions is First-In, First-Out (FIFO).

The Inventory Transaction Costing function examines each inventory part's incoming inventory transactions that have not been fully distributed and assigns them to the same part's outgoing inventory transactions that have not been previously assigned.

VISUAL maintains a table where it can locate all affected transactions when a given value has changed. For instance, a change to an A/P Invoice after its original entry would potentially have an affect which proceeds all the way to the Cost of Goods Sold account, assuming that the finished part which used the purchased part being costed has already been shipped.

As purchased part receipts are costed, material issues are costed. In turn, material issues are part of the cost of a work order. The cost of these material issues affects the receipt costs for parts coming from the work order. After all details of the work order are costed, the receipts of the work order can be costed. Those costs are then distributed to either material issue costs (when the part is used in the fabrication of another work order) or to shipment costs (when the part is a finished good and is shipped to a customer). The process of costing inventory transactions spirals up the bill of material and routing (i.e. engineering structure) until it becomes part of inventory or cost of goods sold.

# Preparing Manufacturing Journals

Manufacturing Journals are prepared by the Costing Utility application. Each journal is the result of an analysis of inventory transactions, and if applicable, labor tickets. These procedures create summary journal transactions called distributions, which are then ready for posting to the General Ledger. Each journal is a subsidiary journal similar in nature to the Accounts Payable or Accounts Receivable journals. The journals must be posted using the Post Manufacturing Journals function.

If you are licensed to use multiple sites, prepare manufacturing journals on a site-by-site basis.

The Costing Utility may create several distributions for each order. If an order is open for more than one period, it typically has multiple distributions. If you run the Costing Utilities more than once during the period and post the results, multiple distributions are created. The Costing Utilities remove any unposted transactions for the period being processed and replace them with up to date transactions. This simplifies the resulting distributions allowing a summary to occur across all available transactions. If you run the Costing Utilities and do not post the results, the unposted transactions are picked up in a subsequent batch the next time you run the Costing Utilities. However, you do not lose the distributions created by the previous Costing Utility run. They are added to the current distribution.

Any difference between what you are posting on the Work In Process (WIP) journal and Indirect Labor journal and the labor expense that results from your payroll transactions can usually be accounted for as one of these possible situations:

- You are not entering all transactions in VISUAL. Sometimes indirect labor is not entered as a labor ticket in VISUAL.
- Your payroll system's employee rate information is different from the employee rate used by VISUAL.
- Your office or salaried employee expense is mixed with the direct/indirect labor posting being made from your payroll system.

Labor tickets are costed immediately when created. Therefore, there is no delay in obtaining this information. Also, burden is calculated at labor ticket creation.

Material burden is applied only when a material is issued to a work order. It is not intended to capture inventory storage costs, nor does it affect the value of inventory as it waits for use. The act of issuing the material to a work order causes the work order to be burdened for the material. This cost appears in the Burden column of a work order. Each part received from the work order contains a share of the burden applied as a result of this process. This material burden can be in addition to the burden applied from the shop resource. For more information on Burden Issue, refer to the Part Maintenance chapter.

When a material issue to a work order drives the on-hand quantity for a part into negative numbers, this occurs in an Actual cost database:

**Material Cost** - Only the quantity that was actually on hand is costed to the work order. The additional quantity issued that drove the on-hand quantity into negative numbers is issued at the standard costs defined in Part Maintenance. If you are licensed to use multiple sites, these costs are defined on a site-by-site basis.

**Issue Burden** - Issue Burdens are handled as:

- **Unit Burden** - Because this figure is based on the number of pieces issued, the unit burden for the total quantity issued is costed.
- **Percent Burden** - This figure is based on a percentage of the material cost.

There are six Manufacturing Sub-Ledger Journals. Each of these journals are discussed in this section.

- Purchase Receipts Journal (PUR) - Order Based
- Work Order Journal (WIP) - Order Based
- Finished Goods Receipt Journal (FG) - Order Based
- Shipments Journal (SLS) - Order Based
- Inventory Adjustment Journal (ADJ) - Transaction Based
- Indirect Labor Journal (IND) - Transaction Based

### Purchase Journals

Purchase Journals use the site on the Purchase Order line to determine which site owns the purchase transaction costs. Since you can enter a purchase order for multiple sites, this ensures that the correct site owns the costs associated with its purchasing activity.

The Purchase Journal contains a listing of the summary transactions created by the Costing Utilities. These summary transactions are the results of processing inventory transactions of purchase orders and receipts of services from the vendor that are linked to a Purchase Order. These are receipts (or returns) of each part.

Each purchase order received (or returned) during the period is examined for its current value by summing the inventory receipts attached to the purchase order. These receipts are summarized by G/ L account number for each PO. In other words, if three lines on a PO were received at \$100.00 each and the debiting G/L account was the same, a sub-ledger distribution for this PO would be made for

\$300.00. However, if each debiting account was different, three sub-ledger distributions would be made for \$100.00 each. Any existing distributions which have been posted for the period are deducted. The resulting distribution is then saved for the purchase order.

The purchase journals taken together for a given period represent the total purchase receipts for all types of purchases of manufacturing material and expense items for that period.

### Purchase Receipt Journals

The Purchase Order accrual account from the G/L Interface is used for all Purchase Order receipts. This account is debited when the A/P Invoice is matched to the Purchase Order Receipt. If a Purchase Order line item is linked to job requirement(s), this journal posts the receipt and the WIP journal posts the automatic issue that occurs when the Purchase Order is received.

### Work In Process (WIP) Journals

The Work In Process Journal contains a listing of the summary transactions created by the Costing Utilities as a result of processing inventory transactions and labor tickets to work orders. These are issues (or returns) of materials and posting of labor to work orders during the period.

Each work order that has material issued (or returned) to it, or labor entered for it, during the period is examined for its current value. The actual value of a work order comes entirely from inventory transactions and labor tickets. Any existing distributions that have been posted are deducted from the current value, and the difference is made into a new distribution for the work order. This journal is order based and not transaction based.

The WIP journals taken together for a given period represent the total usage of material and the total value added by the manufacturing process for that period.

Miscellaneous charges posted to the job from payables are posted to the general ledger via the Accounts Payable Invoice journal. However, these values are relieved from WIP when the order is received to finished goods or shipped.

The value that VISUAL determines for labor and burden in this journal are absorbed labor and burden. Absorbed labor and burden are temporarily placed in Work In Process Inventory, and ultimately flow into Finished Goods Inventory, and then finally to Cost of Goods Sold.

### Finished Goods Journals

The Finish Goods Journal contains a listing of the summary transactions created by the Costing Utilities as a result of processing inventory transactions of work orders. These are receipts (or returns) of manufactured parts from the floor to finished goods inventory. This journal is work order based and not transaction based.

#### Finished Goods Journals in Actual WIP Costing

If you selected Actual as your WIP costing method, the system values that transaction at the total accumulated cost on the work order at that time when a partial quantity is moved from WIP to finished goods. If another partial quantity moves to stock, the system values that transaction with the costs accumulated between the previous transaction and this one. This continues until the work order is completed and closed. When the work order is closed, the Costing Utilities evaluate the total cost of the work order versus completed transactions and creates adjustment distributions for any differences.

#### Finished Goods Journals in Projected WIP Costing

If you selected Projected as your WIP costing method, the system values that transaction at the estimated unit cost based on the work order at that time when a partial quantity is moved from WIP to finished goods.

#### Costing Considerations for Finished Goods Returns

If you use FIFO by Part Location as the FIFO method, review this information to ensure that work order receipts and returns are processed as anticipated.

FIFO locations are not maintained for finished goods returns. To make sure that FIFO layers are matched for finished goods return transactions, it is highly recommended that you use one warehouse location per work order for work order receipts and potential finished goods returns. You do not need to use one warehouse location for all of your work orders. You should just make sure that for any given work order, you use the same location for work order receipts as you do for finished goods returns.

If business needs dictate that you need to use a different warehouse location for receipts and returns, you should review and physically adjust locations after you create finished goods returns. If you receive a quantity from a work order to one location, transfer the quantity to a second location, then return the quantity to the work order, the relationship between the adjust out FIFO layers will be lost. The distribution for the transfer to the second location and the original receipt will be separated. The receipt return is directly distributed to the original receipt.

If you do need to use different warehouse locations for work order receipts and returns, you can use these reports to analyze the transactions:

**Inventory Transaction Report** - Use this report to view when work order returns have been made. Sort the report by Work Order and Type of Receipt to identify receipts. Negative amounts indicate returns.

You can also use this report, sorted by Warehouse/Location/Part, to analyze part location quantity. The layer costs are expressed as current value, not as the value as of a particular point in time.

**Costing Tools** - You can use the Cost Distribution Analysis tool and FIFO/Average Costing Analysis tool to view distributions.

**Inventory Balance Report** - Use this report to view costed values of transactions as of a period end. The total of this report will reconcile, but if you use separate locations for work order receipts and returns, the amounts reported by location will not reconcile.

### Shipments Journals

The Shipments Journal contains a listing of the summary transactions created by the Costing Utilities as a result of processing inventory transactions of customer orders. These are shipments (or returns) for parts sold to customers. This is a customer order based sub-ledger and is not transactional.

Shipments can occur for items stocked in finished goods or, if the customer order is linked to the work order, directly from the work order. If the product is shipped from finished goods, the value booked to cost of goods sold is the value booked to finished goods from the Finished Goods journal on a FIFO basis.

Shipping Journals in Actual WIP Costing

If you selected Actual as your WIP costing method and a partial shipment of a customer order linked to a work order is made, the system books to cost of goods sold the total cost accumulated to date on the work order. A second shipment will be at the cost accumulated between the previous shipment

and this one. When the final shipment is made and the work order closed, if there is any difference between the total cost of the work order and the costs already transacted, the system creates adjustment distributions for this difference.

Because of this methodology, if partial shipments occur across accounting periods, there is the potential that cost of goods sold in a particular period may be under or over stated.

In actual costing it is possible that cost variances for a job are posted in a period for which no shipment was made.

For example, if an order with a total value of \$4,000 ships in February, this value is posted to the Cost of Goods Sold account(s). If an invoice for material used on the job comes in on March 15, any difference in cost (Purchase value vs. Invoice) is posted to the current periods Cost of Goods Sold.

#### Shipping Journals in Projected WIP Costing

If you selected Projected as your WIP costing method and a partial shipment of a customer order linked to a work order is made, the system values that transaction at the estimated unit cost based on the work order at that time when a partial quantity is moved from WIP to finished goods.

### Inventory Adjustment Journals (Adjust In or Out)

This journal contains a listing of the summary transactions created by the Costing Utilities as a result of processing inventory transactions of parts that adjusted inventory quantity on hand. This journal also includes transactions created from inter branch transfers and other actions that result in an adjust in or adjust out inventory transaction.

You must value incoming adjustments (increases in quantity on hand) when costing at actual. Use the current standard from the Part Master or override the value manually when the transaction is created. This action provides a cost per unit and therefore a total cost for every incoming adjustment transaction. All adjustments into inventory created by the Physical Inventory module are made at standard. Therefore, it is extremely important that all item masters contain a standard if you use the physical inventory module.

Adjustments that are outgoing (decreases in quantity on hand) must be valued by the same inventory costing method used to distribute costs from receipts to issues (Costing Utilities, Inventory Transaction Costing). You cannot set the dollar value of an outgoing adjustment.

This journal totals the value of each such transaction and posts it to the financials via this subsidiary ledger. This journal is transactional. That is, each transaction results in a separate distribution to the appropriate accounts.

### Indirect Labor Journals

The Indirect Labor journal contains a listing of the summary transactions created by the Costing Utilities as a result of processing labor tickets which are not for a specific work order.

Indirect labor tickets are posted transactionally, in a manner similar to the adjustment journal covered earlier in this chapter.

### Example Postings for Journals

The account used is based on how you elect to implement your system. You can use a lowest level G/L interface account or specify the accounts to use at the Part, Work Order, or Product Code level. This should be decided early on in the implementation of the system.

These transaction templates are used for each journal type:

#### Purchase Receipt Journals

Use the Product Code Account table, then part inventory account if the line item expense account is left blank. If the PO line item account, the Product Code Part Inventory or the part inventory accounts are blank, the Default Inventory account in the G/L Interface is used.

The PO accrual account from the G/L Interface is used for all PO receipts. This account is credited when the A/P Invoice is matched to the Purchase Order Receipt.

When a Purchase Order line item is linked to job requirement(s), the journal posts the receipt and the WIP journal posts the automatic issue that occurs when the PO is received.

#### Work Order Journals

- For debit transactions, first attempts to use work order WIP accounts, then the Product Code WIP accounts, and then default WIP accounts from the General Ledger Interface table.
- For credit transactions, first attempts to use shop resource absorption account, then default absorption accounts from the General Ledger Interface table.

#### Finished Goods Receipts Journals

- For debit transactions, use the part accounts, then the Product Code accounts, then default inventory accounts from the General Ledger Interface table.
- For credit transactions, first attempts to use work order WIP accounts, then the Product Code accounts, then default WIP accounts from the Interface table.
- Manufacturing variance account from the Product Code Interface table, then the General Ledger Account Interface (standard cost only).

#### Sales (Shipments) Journals

- For debit transactions, first attempts to use line item COGS accounts from the Customer Order Entry, then the Product Code COGS Account, then default COGS accounts from the General Ledger Interface table.
- For credit transactions, first attempts to use the part accounts, then the Product Code Account, then default inventory accounts from the General Ledger interface table.

#### Inventory Adjustment Journals

- For debit transactions, first attempts to use the part accounts, then the Product Code accounts, then default inventory accounts from the General Ledger.
- For credit transactions, first attempts to use the account specified by the user in the Inventory Transaction, then the default Adjustment Account from the Product Code table, and then the default Inventory Adjustment Account from the Interface table.

#### Indirect Labor Journals

- For debit transactions, first attempts to use the account specified by the user in the Labor transaction, then default indirect labor account from the General Ledger Interface table.
- Uses the Factory Payroll Account from the General Ledger Interface table.

Note that each template attempts to apply a specific account, usually an overridden account entered by the user for the specific order or the Product Code table, and then falls back on the interface account as the default. Thus, the user must setup the interface in the event that an account is not overridden.

If you override one account in the specific category but leave the other accounts blank, the interface table accounts are used for the blank fields. To ensure that you always get the result you expect, override all four accounts, even if it is the same account in each category.

In entries that end with (s) in the templates above, signifying more than one account, more than one transaction may be produced based on the user's specified account numbers. Every attempt is made to eliminate duplicate accounts on either side of the transaction.

### Reporting Invoices Charged Directly to Work Orders

Use the Work Order Cost Report as a means of tracking direct charges to the work order from the A/P Invoice by Comparative Totals and Comparative Totals with breakdown. Direct Charges involve Material and Service only.

For each material requirement and operation, this information is shown:

- Quantity
- Estimated Cost = Estimated Material + Labor + Burden + Service
- Actual Cost = Actual Material + Labor + Burden + Service
- Variance = Estimated - Actual
- Projected = Projected Material + Labor + Burden + Service

Comparative Totals with Breakdowns include Material, Labor, Burden and Service costs broken out into separate sections before being totaled. Invoices charged directly to the Work Order are also listed separately. Additionally, setup time and run times are split for operations.

## Manually Running Costing in the Costing Utilities Window

Use the Costing Utilities window to run costing manually at any time. You can also set up a service to run costing. See ["Running Costing with the Costing Service" on page 5-42 in this guide](#_bookmark383).

There are two sections to the Costing Utilities window. The upper portion is used to update costs related to Work Orders and inventory transactions. If you are running VISUAL without VISUAL Financials, you need only to select these two options.

The bottom half of the Costing Utilities window is where you can have the system prepare the manufacturing journals. Even though the window has a message that says to run these on a monthly basis, that is meant as a minimum. It is recommended that these journals be prepared and posted on a more frequent basis during the month to reduce the amount processing required to close your month. The frequency depends on the amount of activity generated by your business.

The default posting date that appears in the dialog box is the last fiscal period's ending date. You can overwrite this field to enter another (appropriate) date (i.e. current system or month end date). If you enter a current period date and the prior period is open, VISUAL prompts you with a message stating that you cannot process the current period information until you close the previous period. This is to prevent any information not posted in the previous period from being posted in the current period.

For example, if on the last day of the month, you enter transactions but do not post, and on the 10th day of the new month you elect to post the new month's activity, all transactions not recorded in the journals for the current and previous month are recorded and posted to the current period.

The reason for this is that costs are posted according to the value of the order (Purchase Order, Customer Order or Work Order) and not the individual transactions. The transactions are used to tell VISUAL when the change took place so that the current value of the order can be posted to the period for which the transaction took place.

For example, if a Purchase Order is received on the last day of the month for \$100.00, the Costing Utility posts \$100.00 to the inventory account for that period. If on the 10th day of the new month, an invoice is matched to the receiver for \$110.00, the variance of \$10.00 will be posted in the new month because the previous month is closed. To post these variances to the previous month, you must reopen the month and run the Costing Utilities up through the end of the previous month. All variances for activity occurring in the previous month(s) will be posted to the month reopened. The Costing Utilities ignore all activity after that date.

It is also recommended that you complete the final preparation of journals for the prior period before you prepare journals for the current period. Post Manufacturing Balance reports could be impacted if a new period is prepared before the last preparation of a prior period's journal.

### Running Costing Utilities

To prepare the manufacturing journals:

- From the main window, select the **Costing Utilities** option on the Eng/Mfg menu.
- Click **Setup**.
- To assume that all materials and operations are closed if the work order is closed, select this check box.
- To repeat the costing run until it cannot find updates to perform, select the **Continue Running...** check box. If you select this check box, click in the **Cycles are performed** text box and enter the number of costing runs to perform. Leave the field empty to continue processing until all transactions have been costed. If you specify a 0, this value is considered the same as the value of 1.
- Click **OK**.
- The Setup dialog closes.
- If you are licensed to use multiple sites, click the Site ID arrow and select the site for which you are preparing manufacturing journals. If you are licensed to use a single site, this field is unavailable.
- Select inventory related costing options:

**Receipt Transaction Costing** - VISUAL updates your work order receipt costs with the most up- to-date receipt transaction data. Actual cost transactions are updated for Closed orders and partial receipts are updated based upon WIP costing method.

Standard Costs are used to determine Finished Goods Variances.

**Inventory Transaction Costing** - VISUAL updates your inventory costs with your most up-to- date receipt transaction data. If you use the Standard costing method, this option is not displayed.

**Note:** To keep your costing values up to date, run the Costing Utilities with these settings often. You do not have to generate distributions until you are ready to post to your ledgers.

- When you are ready to generate journals, select the appropriate check boxes for the journals to prepare:

**Note:** This function only prepares the journals: you must use the Post Manufacturing Journals program to post distributions to your ledger. To prepare distributions only, select the journals to create and clear the two check boxes in the top section of the window.

- - Purchase Journal Transactions
    - WIP/FG Journal Transactions
    - Shipments Journal Transactions
    - Part adjustment Journal Transactions
    - Indirect Transactions Journal Transactions

- Click the **Run** toolbar button.

As the Costing Utility runs, the current part appears in the Costing Utility window.

**Note:** To stop the costing process, click the **Stop** toolbar button.

When the costing run has finished, an information dialog appears notifying you of the costing results. This information is also stored in the VMAPLUTL.log file.

- Click **OK** on the dialog box.
- Click the **Exit** toolbar button on the Costing Utility.

To post the distributions you have created, you must use the Post Manufacturing Journals program located in the Ledger menu.

## Running Costing with the Costing Service

You can use the Costing Service to run costing automatically on the days and times you specify. If you have multiple sites and would like to run the service for all sites, then you must install the service once for each site.

To specify when to run the service, use the Costing Service Schedule dialog in the Costing Utilities window. You can specify one schedule for running inventory costs options and a separate schedule for preparing manufacturing journals. You can use the service to run costing up to six times a day.

You can set up one costing schedule for each site. Users that have access to the Costing Service Schedule dialog in the Costing Utilities window can edit the schedule for their allowable sites. The system administrator can control which users have access to the Costing Service dialog.

After the service is installed and the service schedule is set up, the database is examined based on the polling interval you specify to see if costing needs to be run. When the service finds that costing needs to be run, the service runs costing based on the settings you specify in the Set as Scheduled dialog.

If you prepare manufacturing journals with the Costing Service, you must post the journals manually using the Post Manufacturing Journals window. The Costing Service does not post manufacturing journals.

If you set up the Costing Service, you can still run costing utilities manually.

### Preparing Manufacturing Journals Using the Costing Service

You can use the Costing Service to run costing only in the current period or the period immediately prior to the current period. In both cases the period prior to the period in which you are running costing must be closed.

For example, presume you use a monthly financial calendar. Today is April 24th and the service is scheduled to run. This table shows scenarios when the Costing Service can be used and when the costing service cannot be used:

**Scenario Can I use the Costing Service?**

April is open and March is closed. **Yes.** Costing is run for April. The default

posting date used for journals is April 30.

**Scenario Can I use the Costing Service?**

April is open, March is open, and February is closed.

April is open, March is open, February is closed, and January is open.

April is open, March is open, and February is open.

April is open, March is open, but manufacturing journals have already been prepared or posted in April.

April is closed, March is open, and February is closed.

**Yes.** Costing is run for March. The default posting date used for journals is March 31.

**Yes.** Costing is run for March only. The default posting date used for journals is March 31.

**No.** The Costing Service can only run costing in the current period or in the period immediately prior to the current period.

**No.** You should run costing manually to ensure that costs are generated as you expect.

**Yes.** Costing is run for March, provided that no manufacturing journals have been prepared or posted in April.

These rules and scenarios apply to journal preparation only. You can run receipt transaction costing and inventory transaction costing with the service at any time.

### Installing the Costing Service

The computer where you install a service must have these components installed:

- **VSRVANY.EXE** - VSRVANY.EXE is a VISUAL tool that allows the service executables to be run as a service. VSRVANY.EXE must be installed in the same directory as the service executables. VSRVANY.EXE is installed with the VISUAL installer.
- **SC.EXE** - SC.EXE is a Microsoft Windows tool used to make modifications to services and to remove services. SC.EXE is commonly installed with Microsoft Windows. Run a Microsoft Windows search to verify that SC.EXE is installed. SC.EXE does not have to be in the same directory as the services executables; you can leave SC.EXE in the directory where Microsoft installed it.
- **Unify Runtimes** - You must also have the Unify runtimes for your version of VISUAL installed on the computer where you run the service.

The Costing Service is installed by site. If you have multiple sites, install the service for each site where you want to use the service to run costing.

To install the service:

- In your VISUAL executables directory, locate VMCSTSVC.EXE.
- Right-click VMCSTSVC.EXE and select **Run as Administrator**. The Sign In dialog is displayed.
- Specify this information:

**User ID** - Specify the user ID that the service uses to sign into the VISUAL database. This can be any valid VISUAL user ID who has access to the site for which you are setting up the service. This user must also have security permissions to access Costing Utilities (VMAPLUTL.exe)

**Password** - Specify the password associated with the user ID.

**Database** - Specify the database on which to run the service.

- Click **Sign In**. The name and description of the service is displayed.
- Specify this information:

**Site ID** - Specify the ID of the site where you want to run costing with the service.

**Log File Directory** - Specify where to store the log file for the service.

**Polling Interval** - Specify how frequently the service should check for updates. The maximum value is 900 seconds.

**Log Level** - Specify the level of information to write to the log file. Click one of these options:

**None** - To write the time the service started, click this option. This option is recommended for normal production environments.

**Error** - To write the time the service started and any error messages, click this option.

**Info** - To write to the time the service started, error messages, and additional information about the service, click this option. The use of this option is recommended only if you are troubleshooting issues with the service. When you use this option, the size of the log file grows quickly.

The log file's name is VMCSTSVC_\[Your Site Name\].log. The size of the log file is limited to 1 MB. When the log file approaches 1 MB, the log is renamed to VMCSTSVC_\[Your Site Name\]\_\[Current date time\].log, and a new VMCSTSVC_\[Your Site Name\].log is created.

- Click **Install Service**.
- To start the service now, click **Yes**. To start the service later, click **No**. If you click No, you can start the service in the Windows control panel.
- To install the service for another site, repeat steps 5 through 7. Repeat these steps for each site where you want to run costing with the service.

### Scheduling the Costing Service

After you install the Costing Service, specify when the Costing Service should be prompted to run costing for a site.

You can set up different schedules for transaction costing and journal preparation. For example, you can schedule the Costing Service to run transaction costing daily and prepare manufacturing journals weekly.

To schedule the Costing Service:

- Select **Eng/Mfg**, **Costing Utilities**.
- In the site ID field, select the site where you will run costing utilities with the service. Make sure you select a site for which you have installed the Costing Service.
- Select **File**, **Costing Service Schedule**. The ID of the site you selected is displayed in the title bar of the dialog.
- Specify when the service is active. Specify this information:

**Start Date** - Specify the date that the service should start checking to see if costing needs to be run. Leave this field blank or specify today's date if you do not want to delay the start of the service.

**End Date** - Specify the last date that the service should check to see if costing needs to be run. Leave this field blank if you do not want to set up an expiration date for the service.

**Enabled** - To use the service with the selected site, select this check box. To stop using the service, clear this check box.

- In the Run Type section, weekly is selected. You cannot change this selection. This selection indicates that the service should check for updates only on the days and times you specify.
- In the Transaction Costing section, specify the settings to use when the service runs transaction costing. Specify this information:

**Receipt Transaction Costing** - To update your work order receipt costs with the most up-to-date receipt transaction data, select this check box. Actual cost transactions are updated for Closed orders and partial receipts are updated based upon WIP costing method.

Standard Costs are used to determine Finished Goods Variances.

**Inventory Transaction Costing** - To update your inventory costs with your most up-to-date receipt transaction data, select this check box. If you use the Standard costing method, this option is not displayed.

**Assume that operations and materials are closed if work order is closed** - To automatically close any operations and materials on a closed work order during the costing run, select this check box. To retain the current status of any operation and material cards on a closed work order, clear this check box.

- Specify when to run transaction costing. Specif this information:

**Days of Week** - In the Days of Week section under the Transaction Costing section, specify on which days to run transaction costing with the service.

**Run At** - Specify the times of day that the service should check to see if transaction costing should be run. The times you specify apply to all days that you run the service for transaction costing. You can run the service up to 6 times a day. If you select a day in the Days of Week section but leave all Run At fields blank, then the service is run at 12:00 AM on the days you selected.

- In the Journal Preparation section, use the check boxes to specify which journals to prepare when the service runs costing. Then, specify when to prepare the journals. Specify this information:

**Days of Week** - In the Days of Week section under the Journal Preparation section, specify on which days to prepare journals using the service.

**Run At** - Specify the times of day that the service should check to see if journals need to be prepared. The times you specify apply to all days that you run the service for journal preparation. You can run the service up to 6 times a day. If you select a day in the Days of Week section but leave all Run At fields blank, then the service is run at 12:00 AM on the days you selected.

- Click **Save**.

### Deactivating the Costing Service Schedule for a Site

To deactivate the service schedule for a particular site:

- Select **Eng/Mfg**, **Costing Utilities.**
- In the site ID field, select the site where you will run costing utilities with the service. Make sure you select a site for which you have installed the Costing Service.
- Select **File**, **Costing Service Schedule**. The ID of the site you selected is displayed in the title bar of the dialog.
- Clear the **Enabled** check box.
- Click **Save**.

### Deleting the Costing Service Schedule for a Site

You can delete the service schedule for a site without removing the Costing Service. To delete a Costing Service schedule:

- Select **Eng/Mfg**, **Costing Utilities.**
- In the site ID field, select the site where you will run costing utilities with the service. Make sure you select a site for which you have installed the Costing Service.
- Select **File**, **Costing Service Schedule**. The ID of the site you selected is displayed in the title bar of the dialog.
- Click **Delete**.
- Click **Close**. Do not click Save before you click Close. If you click Save before you click Close, the service schedule is recreated.

### Removing the Costing Service

To remove the service for a site:

- In your VISUAL executables directory, locate VMCSTSVC.EXE.
- Right-click VMCSTSVC.EXE and select **Run as Administrator**. The Sign In dialog is displayed.
- Specify this information:

**User ID** - Specify the user ID that the service uses to sign into the VISUAL database. This can be any valid VISUAL user ID.

**Password** - Specify the password associated with the user ID.

**Database** - Specify the database on which to run the service.

- Click **Sign In**.
- In the Site ID field, specify the ID of the site where you no longer want to run the Costing Service.
- Click **Remove Service**.

## WIP / Costing - Table Related Discussion

Every transaction in VISUAL affects one or more database tables. Some database tables hold detail transaction records from original sources of entry, for example, purchases, inventory transactions, labor tickets, payable entries. Other database tables record and hold summary information for other programs as well as reports to use as a feed of information. Summary database tables reduce the amount of work the system needs to do to produce accounting entries as well as provide report information. Because the same data is written in various forms to various tables in the database, it is important to keep these tables synchronized.

There are basically three kinds of database tables used to track costing related information-the bridge between manufacturing and financials.

**Transaction Tables** - Transaction tables hold transaction level information. These tables form the underlying cost information that the costing utilities use to populate the other tables it needs to keep track of WIP details, G/L postings and WIP balances.

**Hosting Tables** - VISUAL stores all of your debit and credits in hosting tables.

**Summary Tables** - Summary tables record and hold summary information for other programs as well as reports to use as a feed of information.

Table A:

| **Layer** |     |     |     |     | **Inv. Parts**<br><br>**On Hand** | **\# of Layers In Inv. Bal.** | **WIP**<br><br>**Balan ce** | **Invent ory**<br><br>**Balanc e** |
| --- |     |     | --- | --- | --- | --- | --- | --- | --- | --- |
| **Txn #** | **#** | **Type** | **In** | **Out** |
| 1   | 1   | P/O Receipt | 50  |     | 50  | 50 @ 2.00 = 100.00 |     | 100 |
| 2   | 2   | P/O Receipt | 10<br><br>0 |     | 150 | 100 @ 2.50 =<br><br>250.00 |     | 350 |
| 3   | 1   | 1Issue 75 units to a WO |     | 50  | 0   | 50 @ 2.00 = 100.00 | 162.5 | 187.5 |
|     | 2   |     |     | 25  | 75  | 25 @ 2.50 = 62.50 |     |     |
| 4   | 2   | Beginning Balance |     |     | 75  | (One FIFO Layer) |     | 187.5 |
|     | 3   | P/O Receipt | 10<br><br>0 |     | 100 | 100 @ 2.60 =<br><br>260.00 |     | 447.5 |
| 5   |     | Beginning Balance |     |     | 175 | (Two FIFO Layers) |     | 447.5 |
|     | 4   | Issue Return |     |     |     |     |     |     |

| **Layer** |     |     |     |     | **Inv. Parts**<br><br>**On Hand** | **\# of Layers In Inv. Bal.** | **WIP**<br><br>**Balan ce** | **Invent ory**<br><br>**Balanc e** |
| --- |     |     | --- | --- | --- | --- | --- | --- | --- | --- |
| **Txn #** | **#** | **Type** | **In** | **Out** |
|     |     | (Ave. txn #3) | 10  |     | 185 | 10 @ 2.167 = 21.67 | 140.8<br><br>3 | 469.17 |
| 6   |     | Beginning Balance |     |     | 185 | (Three FIFO Layers) | 140.8<br><br>3 | 469.17 |
|     | 2   | Issue 180 units to a W/O |     | 75  | 0   | 75 @ 2.50 = 187.50 | 458.3<br><br>3 |     |
|     | 3   | Issue 180 units to a W/O |     | 10<br><br>0 | 0   | 100 @ 2.60 =<br><br>260.00 |     |     |
|     | 4   | Issue 180 units to a W/O |     | 5   | 5   | 5 @ 2.167 = 10.83 |     |     |
| 7   |     | Beginning Balance |     |     | 5   | (One FIFO Layer) | 599.1<br><br>6 | 10.84 |
|     | 5   | WIP Issue Return | 10<br><br>0 |     | 75  | 65 @ 2.167 =<br><br>140.85 |     |     |
|     |     |     |     |     | 115 | 35 @ 2.546 = 89.11 |     |     |
|     |     |     |     |     |     | (per unit cost = 2.299) |     |     |
| 8   |     | Beginning Balance |     |     | 105 | (2 FIFO Layers remain) | 547.8<br><br>2 | 240.8 |
|     |     |     |     |     |     | 5 @ 2.168 = 10.84 |     |     |
|     |     |     |     |     |     | 100 @ 2.2996 =<br><br>229.96 |     |     |

Table B:

| **Transaction Type** | **Normal** |     | **Return/Correction** |     |
| --- | --- |     | --- |     | --- | --- |
|     | **Debit** | **Credit** | **Debit** | **Credit** |
| Adjustment | Inventory | Adjustments | Adjustments | Inventory |
| Transfer (In) | No Entry |     |     | No Entry |
| Transfer (Out) |     | No Entry | No Entry |     |
| P/O Receipts |     |     |     |     |
| (Parts and Service) | Inventory | Purchase Accrual | Purchase Accrual | Inventory |

| **Transaction Type** | **Normal** |     | **Return/Correction** |     |
| --- | --- |     | --- |     | --- | --- |
|     | **Debit** | **Credit** | **Debit** | **Credit** |
| Accounts Payable (Direct) | Work-in- Process | Accounts Payable | Accounts Payable | Work-in- Process |
| W/O Receipts | Inventory | Work-in- Process | Work-in- Process | Inventory |
| Inventory Issue | Work-in- Process | Inventory | Inventory | Work-in- Process |
| Services | Work-in- Process | Purchase Accrual | Purchase Accrual | Work-in- Process |
| Labor | Work-in- Process | Direct labor | Direct labor | Work-in- Process |
| Burden | Work-in- Process | Applied Overhead | Applied Overhead | Work-in- Process |
| Indirect Labor | Indirect Labor | Direct labor | Direct labor | Indirect Labor |
| Shipments (2 parts) Linked Orders |     |     |     |     |
| Transfer to Finished Goods | Inventory | Work-in- Process | Work-in- Process | Inventory |
| Transfer to Cost of Sales | Cost of Good Sold | Inventory | Inventory | Cost of Good Sold |
| Shipments Finished Goods |     |     |     |     |
| Shipment | Cost of Sales | Inventory |     |     |
| Return (RMA) |     |     | Inventory | Cost of Sales |

## Multi-Currency in Costing

VISUAL supports a wide range of Multi-Currency features. VISUAL determines and converts historical balances for tracking currency accounts based on either the exchange rate when the transaction originated or the system date when the transaction originated, depending upon your selection in Accounting Entity Maintenance. VISUAL revalues account balances to reflect the appropriate values for System and Tracking currencies whereby every transaction balances in system, and transaction currency to the current rate.

Tracking costs of materials by currency is provided by the tracking currency feature. Because the material cost is quantitative it is processed at it's historical value. You can run costing utilities as often as needed (perhaps daily) for purposes of applying Labor and Burden. All Labor and Burden (indirect) costs use the rate in existence based on the transaction date or system date as defined in Accounting Entity Maintenance.

### Cost Movements

VISUAL accumulates the appropriate currency values from Inventory or Service Charges and Estimated Bookings to calculate material costs. Each quantitative cost is tracked at its historical currency rate / value at the time of transaction entry to a work order based on the exchange rate dates for applying the exchange rate as set in Accounting Entity Maintenance.

### Inventory Valuation

VISUAL values inventory using the FIFO (First In, First Out) method. The FIFO method of inventory valuation assigns cost to inventory in cost layers. Each addition (purchase, inventory receipt, or adjust-in) adds a new cost layer. Each subtraction (issue, sale, or adjust-out) removes one or more cost layers. For every tracking currency held by the database, VISUAL records and tracks all cost movements through the manufacturing process, from inventory through to cost of goods sold.

There are two types of FIFO layers held within the system. First, raw material inventory contains layers via purchases from a supplier or they can be created when an issue return is created from work-in-process back to inventory (in the case of an over issue). Second, work-in-process can also contain FIFO layers where multiple inventory issues for the same part on a work order is contained in work-in-process.

Included below are examples to depict transaction flows in an actual cost database. In a standard cost database all inventory transactions are performed at the standard rate and therefore their computed issue costs carry the same value as the FIFO inventory layers.

### FIFO Layers from Purchases

Every receipt of raw material inventory or finished goods inventory (a distinction is not made) is assigned a per unit price value. Raw material inventory FIFO layers are created when a purchase is received into inventory. The cost of the purchased inventory is derived by the number of units received times the per unit cost as identified on the purchase order. This value also represents the purchase order accrual value. Tracking currency values are assigned based on the receipt date or system date as designated in Accounting Entity Maintenance. Each purchase creates a unique layer of inventory, which is displayed by the inventory valuation report.

#### FIFO Layers from Issue Returns

Issue returns (parts issued to a work order returned to inventory) also create a FIFO layer. When parts are issued to a work order their costs are added to work-in-process based on the raw material FIFO layer they originated from. If the issue to WIP is derived from more than one layer then the costs of all the layers issued to fulfill the requirement are combined together to create a single WIP issue layer. Any subsequent issue of the same part to a work order creates an additional WIP issue layer. If a part on the work order is returned to inventory from work-in-process, a new cost layer is created with the layers value being derived from the WIP issue layers currently residing on the work order. If there is only one issue of a particular part for the work order the cost is straightforward. It being, the average unit cost (by combining original FIFO layers) times the number of units returned to inventory. When there is more than one raw material inventory "issue" to a work order the costs are returned from the WIP issue layers in FIFO order.

#### FIFO Layers from Receipts and/or Adjust-In

A receipt of inventory (finished goods) from a work order can also cause FIFO layers in inventory to occur. The cost of the inventory layer is derived from the average unit cost on the work order times the number of units. If the receipt closes the work order (complete receipts) the entire cost of the work order at the time of the receipt is the value of the FIFO layer.

Adjustments into inventory create a separate cost layer in inventory for the per unit cost assigned during inventory transaction entry. If no cost is assigned at that time, the cost is derived by the unit cost as set on the part master file when you run costing utilities.

## Multi-Currency System Cost Flow

### Inventory Transactions

There are eight classes of inventory transactions. Each of these transactions either move goods in or out of inventory or transfers between inventory locations, therefore making 16 possible transaction types. Most transactions affecting inventory, with the exception of transfers, either adds or removes costs from inventory. All movements applied are based on historical currency levels.

### Basic Transaction Types

#### P/O Receipts

P/O Receipts are purchased items received into stock. When you receive P/Os you assign them to an inventory warehouse location. The number of units purchased as well as the cost of the purchased part(s) become a new cost layer of raw materials inventory. When the PO Receipt is received into inventory, the entry is booked to a purchase accrual, as a credit with the offsetting debit to Inventory.

You can assign purchase inventory parts directly to a work order. When you receive these parts you book to raw materials inventory and immediately issue to the corresponding work order. The inventory transaction entry report depicts this process. Purchased parts that are purchased on behalf of an existing work order are given the same treatment as inventory issues when received into the warehouse. The only time these costs become a FIFO layer is if they are received into inventory through an inventory issue return.

Accrual amounts on Purchase Order Receipt transactions are set in all currencies defined as tracking based on the exchange rate in effect and results from the exchange rate date setting defined in Accounting Entity Maintenance.

#### Services

In the same manner as purchased parts, service transactions entered as a work order can be purchased directly from the Manufacturing Window using the Costing Utilities function.

In purchasing, the Service Receipts Accrual account is debited with a credit to the PO Accrual account. In the WIP Journal, the WIP Service account is debited and the Service Receipts Accrual account is credited.

#### Inventory Issue / Issue Returns

Inventory issues are transactions that assign raw material inventory to a specific work order. Inventory issues normally cause the reduction of one or more inventory layer(s). These costs become the basis of the material costs assigned to WIP. Inventory issue transactions are prepared by costing utilities to debit WIP and credit Inventory.

Issue returns are inventory parts sent back to the warehouse. Issue returns would most likely result from over issues or raw materials to a work order. The costing utility feature accounts for these costs with a credit to raw materials inventory and a debit to WIP. All movements applied are based on historical currency levels.

#### Accounts Payable

You can directly assign costs through accounts payable for items that did not go through the normal purchasing process. Link these costs to the work order in the Invoice Entry window. You are required to enter the GL code to cost the items. The GL Account assigned to an account payable invoice linked directly to a work order is treated as a clearing account. Costing utilities posts an offset entry to the GL account and applies the transaction value to WIP.

Currency overrides are based on the receipt exchange rate date as set in Accounting Entity Maintenance.

#### Warehouse Transfers

The cost implication, such as the treatment of warehouse transfers, are dependent on the setting applied for the FIFO method in Accounting Entity Maintenance. If the FIFO method option is set to track "by part transfers" from one location to another it does not have any cost implications. Inventory transaction entry records the movement (without cost implication) to the new location. If the FIFO method to track "by part location" is set VISUAL records cost movements between locations. All movements applied are based on historical currency levels.

#### Inventory Adjustments In / Out

Inventory adjustments usually originate from physical inventory counts. Typically, companies take physical counts to keep perpetual records in line with actual inventory on hand. Discrepancies between the actual inventory on hand and the perpetual records requires an adjustment, either in or out. If an adjustment-in is required VISUAL costs this adjustment as a new cost layer. Adjustments- out removes costs from an existing cost layer or potentially removes an entire cost layer from Inventory. When entering adjustments through Inventory Transaction Entry, you are required to assign a G/L Account for the other side of the transaction. VISUAL knows raw materials inventory will be either debited or credited (depending on the adjustment type). You need to define the GL offset account to apply to the other side of the transaction. All movements applied are based on historical currency levels.

#### Direct Labor / Burden

Labor charges are applied directly to the labor ticket during Labor Ticket Entry either through bar coding or by entering via a computer terminal. All costs for setup and run times are recorded in WIP when you run Costing Utilities. VISUAL records the entry to direct labor in WIP as a debit to Work-in- Process Labor and a credit to Direct Payroll Manufacturing.

Burden (overhead) represents costs associated with the cost of manufacturing without the capability of identifying the costs to specific operations or jobs in WIP at any one point in time. For example, rent, utilities, general plant maintenance, and depreciation are costs that every manufacturing environment incurs but are not attributable to specific jobs in WIP.

Costing provides the ability to set burden rates to capture these costs and allocate them to jobs that flow through WIP. You can also apply burden to raw materials inventory as well as to specific operations used in the manufacturing process. Burden rates applied to raw materials inventory on the part master file will cause burden to be charged each time that the part is issued to a work order.

Burden costs assigned operations are applied to WIP at the time of Labor Ticket Entry.

The settings assigned on the resource in Shop Resource Maintenance are the burden costs applied during Labor Ticket Entry. The burden rate is derived either from the operation set in Shop Resource Maintenance or from the operation on the bill of materials. The determination of whether costs are extended from the resource or the work order depends on the Burden Basis setting as defined in Accounting Entity Maintenance. If the option is set to determine by resource burden, then VISUAL looks to the settings as defined in the Resource ID of Resource Maintenance to determine the proper amount of burden to apply. If the option is set to determine by operation burden, then the amounts applied on the work order are used to determine the amount of burden.

#### Indirect Labor

Indirect labor is applied in the same manner as direct labor. The only difference is the transaction type is set to Indirect as opposed to setup or run through the Labor Ticket Entry window. Indirect labor is not charged to specific jobs. Costs associated with indirect labor are reclassed from manufacturing payroll to the appropriate manufacturing indirect Labor G/L Accounts.

#### Shipments (Sales)

When you ship customer orders, work-in-process inventory costs flow through to Cost of Sales in the general ledger in two distinct offsetting entries. The first entry reclassifies work-in-process costs to finished goods inventory. The second entry records the transfer of finished goods inventory to Cost of Sales to properly match costs with revenue as required by generally accepted accounting principles. The exchange rate is based on the shipping date, or the date of invoice generation as defined in Accounting Entity Maintenance.

## Tracking Currency Conversion Utility

Infor Global Solutions, Inc. has a built-in Tracking Currency Conversion Utility routine that is designed specifically to take the historical information based on the proper exchange rates in existence at the time of the transaction and then to convert it to create transaction detail and summary balance information. You can also assign appropriate individuals with the power to purge the tracking currency records at your request.

### Adjusting Inventory Transactions

In order to set the proper value, all inventory transactions must be adjusted by a conversion (exchange) rate. This value equates to the inventory balance. You can enter inventory adjustment transactions to:

- Adjust by zero quantity.
- Set values (except system) by currency. This option is secured by SYSADM. Set the value once and let the system take over from there properly maintaining the values.

VISUAL records the adjustment as a normal inventory transaction.

### Adjusting General Journal Entries

General Journal Entries is accomplished by adjusting the inventory valuation with the inventory balance reports and reconciling your work-in-process accounts to properly reflect the balance by tracking currencies on the General Ledger.

If you need to manipulate currency values, you can post a zero value to general journal transactions and posts like any other journal entry.

Be CAREFUL, as these types of adjustments are very dangerous because they allow for the manipulation of currency data that would not be supported by an exchange rate. VISUAL has secured this functionality to component level security (managers, controllers, etc.) because of the ramifications.

# What are Costing Tools/Audits?

Detail transactions provide the basis from which summary data is formed and occurs for all transactions and balances. Cost Accounting is a very crucial area for manufacturing concerns. Companies need to know manufacturing data is reflected in the VISUAL accounting system. There are times when this data can get out of sequence due to system failures. In order to keep the integrity of cost data, you can use Costing Tools to correct instances where summary records are not supported by detail transactions.

Use Costing Tools to visually compare detail transaction records with their corresponding summary records. You also have the ability to correct them immediately or set them to correct the next time you run costing utilities. After you run Costing Utilities, VISUAL records any necessary entries or fixes, to properly reflect the manufacturing accounting data in the financial system.

The Costing Tools option is located on the Admin menu. The program option is secured by read/write access as well as program component security. To access program security SYSADM access is required.

### Data Inconsistencies

In today's computing environment, sophisticated applications must include tools available to users (through the guidance of technical support) to assist in the maintenance and upkeep of database information. Typically these tools allow for the detection and correction (if appropriate) of data inconsistencies.

Inconsistencies are defined in the dictionary as follows:

- not in agreement, not in harmony, incompatible,
- not uniform; self contradictory,
- not holding to the same principles or practice

In a business environment this definition can be specifically applied to transactions (transaction inconsistencies) or it can be applied in a general sense to expected results in the data (data inconsistencies).

Therefore, when discussing inconsistencies we cover the spectrum of data inconsistencies to transaction inconsistencies. Data inconsistencies result when transaction values are expected to be picked up in one period but are actually calculated into another. Transaction inconsistencies occur where accumulated transaction sub-totals do not support the summation of detail transaction entries.

Typically, transaction inconsistencies occur as a result of a system crash during an update stage. When a transaction is entered into the system it usually causes another record to be to updated or added to the system. VISUAL stores "summary" records, dependent on detail transaction records. If a system crash occurs during a summary record update, database inconsistencies are quite likely to occur as well. Costing Tools can check for these inconsistencies and make appropriate modifications.

To understand data inconsistencies, and how transactions expected in one period can show up in another, you must first understand how costing accumulates and processes information. Typically, Costing Utilities assign transactions to the period in which the transaction date falls, as established in application global. This, however, is not consistent when transactions dated with a prior month

transaction date (intended for the prior month) are picked up in the costing run for the current month. This may occur when the costing utilities feature is executed for the current month without having been run for the prior month. If a situation occurs where costs are posted to one period when they were expected to be closed to a different period, costing tools can be used to affect the proper change in accounting. Technical support can assist in this matter.

## Costing Tools/Audit Overview

VISUAL provides several recalculate functions to change transaction data within the database. These Recalculate options are available:

- Recalculate Distributions
- Recalculate Standard Costs
- Recalculate WIP Balances
- Recalculate Inventory Balances

The analysis reports provide a means to track the manipulated transaction data. These reports are available:

- Cost of Goods Analysis
- Cost Distribution Analysis
- P/O Accrual Analysis
- Journal Prep Analysis
- FIFO Analysis

## Costing Tools

Costing Tools tracks and tallies various types of inventory transactions; either to a work order or to raw materials inventory (purchase or transfer), applied labor & burden, and/or indirect labor. All costs are prepared for journal entry through the Costing Utilities function. It is imperative that the links between the transaction held in the transaction tables, the related distribution tables, and the Work-in- process Issue & Detail tables always support one another and more importantly tie out.

- Purchases • Adjustments
- WIP • Indirect Labor
- Finished Goods • WIP Balance
- Shipments •

The necessary checks are performed based on your selections and notes any discrepancy in the Remarks column. Using the Costing Tools window, you can find your discrepancies, fix them in the appropriate places, and ensure that your Manufacturing costs are kept in sync with your supporting General Ledger entries.

## Using Costing Tools

To analyze your costs:

- From the Admin menu, select the **Costing Tools** option.
- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site that contains the costs to check. If you are licensed to use a single site, this field is unavailable.
- Click the **Cost to Check** arrow and select the cost to check for inconsistencies from the list. You can select:

**Purchases** - Costing tools check purchase order lines against the appropriate detail and distribution (posted) transactions. Any discrepancies between the detail and posted transaction totals are identified by a note in the Remarks field; furthermore, the values in the rows do not match.

**WIP** - Every transaction assigned to a work order enters and flows through the WIP tables. Costing Tools reconcile the WIP transactions to the records that provide the links to the necessary transaction tables, work orders and ultimately the GL Posting table.

**Finished Goods** - Finished goods result from either work order receipts or shipments of work orders to customers.

When parts assigned to a work order are received into inventory (Finished Goods), the number of units and cost of the work order is assigned a FIFO layer in inventory. The costs associated with the work order are removed from WIP and classified to Inventory. When goods are shipped (if they are attached to a work order), VISUAL, through the Costing Utilities, first removes the cost from WIP and books the costs to inventory. Second, VISUAL, through the Costing Utilities, immediately removes this cost from inventory and books it to Cost of Goods Sold/Manufactured. If the goods are in inventory (finished goods) VISUAL removes the cost of the FIFO layer(s) to the Cost of Goods Sold/Manufactured.

**Shipments** - Shipment transactions are related to customer orders. The transaction entries prepared by Costing Utilities and/or posted by Post Manufacturing Journals depend on whether the transactions are linked customer-to- work order(s) or simply shipments for inventory.

If the shipments are for customer orders linked to one or more work orders, VISUAL closes the work order and transfers all costs to inventory. Subsequently the costs of the work order (now in inventory) are transferred to Cost of Goods Sold/Manufactured.

If a shipment is for inventory (customer orders not linked to one or more work orders), Costing Utilities remove costs from inventory and posts them directly to the Cost of Goods Sold/ Manufactured.

Customer orders also affect costing. As mentioned in the Finished Goods section, the shipment of customer orders may potentially affect full shipment status, thereby affecting the cost of open work orders in WIP. Shipments of customer orders or inventory ultimately remove costs from inventory and reclassify them to Cost of Sales.

**Adjustments** - Adjustments relate to inventory corrections resulting from inconsistencies between the perpetual inventory records and actual quantities held in inventory. Each adjustment either adds an inventory cost layer (FIFO) to inventory or subsequently removes costs from one or more layers.

**Indirect Labor** - Indirect labor consists of labor transactions that cannot be directly linked to specific work orders, yet are an essential component in the overall manufacturing process.

VISUAL assumes that all labor is booked to a general payroll account. As direct and indirect labor are applied, these costs are removed from the general payroll account and booked to either WIP, Indirect Labor, or Cost of Sales. These transactions are entered into VISUAL through the labor ticket entry window; the only difference is that you must set the transaction type on the labor ticket to Indirect as opposed to Setup or Run.

**WIP Balance** - The WIP Balance account supports WIP distribution table records. If any corrections are required for any of the other transaction types, WIP Balance checks for database inconsistencies.

**Note:** The table columns change depending on which cost you select to check.

- In the Selection section, select the range of costs to include.

For Purchases and Shipments, enter the starting and ending Order IDs.

For WIP, Finished Goods, and WIP Balances, enter the starting and ending Base IDs. For Adjustments and Indirect Labor, enter the starting and ending Trans IDs.

- To run this cost analysis for a different posting date than the current one, click the posting Date calendar button and select the date.

**Note:** You cannot run costing analysis for closed or invalid posting dates.

- In the **Options** section, select the type of costs to display in the table.

You can select: **Transactions**, **Detail**, **Balance**, and **Burden**. As you select or clear check boxes, columns appear or disappear in the table.

- To process only differences between the transaction value and posted value, select the

**Exceptions Only** check box in the Options section and enter the filter criteria:

**Plus/Minus %** - If the difference between the transaction value and the posted value is greater than the allowed **Plus/Minus %** and the posting candidate flag is set to N, the transaction appears as an exception in the table.

For example, if a transaction worth \$1.00 has a posted value of \$1.11 and the Plus/Minus Percent is set to 10% the transaction appears in the table because the allowable 10% of \$1.00 is less than the difference of \$0.11. If a transaction worth \$1.00 has a posted value of \$1.09 and the Plus/ Minus Percent is set to 10% the transaction will not appear in the table because the difference

\$0.09 is less than the allowed difference of \$0.10.

**Plus/Minus Amount** - If the difference between the transaction value and the posted value is greater than the allowed **Plus/Minus Amount** and the posting candidate flag is set to N, the transaction appears as an exception in the table.

**Note:** You can leave either field blank and filter on % or Amount. If you enter values in both fields, VISUAL filters differences based on it being an OR operator.

- Click the **Run** toolbar button.

When processing is finished, the relevant information appears in the table.

- To mark a found discrepancy so that VISUAL stops reprocessing the record, click the **Set Posting Candidate** toolbar button.

A Y appears in the row header.

- To save any changes you have made to posting candidates, click the **Save** toolbar button.
- When you have finished analysis your costs, select the **Exit** option on the File menu.

## Printing Costing Reports

There are several reports you can print or view from within the Costing Tools window:

**Cost of Goods Analysis** - The Cost of Goods Analysis report provides a means to analyze your cost of goods sold by customer order.

**Cost Distribution Analysis** - Use the Cost Distribution Analysis report to show how costs for a given item were issued at FIFO or how costs were distributed due to links between the Work Order requirement and the Purchase Order, or the Work Order and Customer Order in the case of a buy / resell part or manufactured part.

**P/O/Accrual Analysis** - Use the P/O Accrual Analysis report to review purchase order receipts against the amount invoiced against those receipts.

**Journal Preparation Analysis** - The Journal Preparation Analysis report examines each purchase order, work order, sales order, adjustment, and indirect labor ticket and attempts to determine if the item's current value, based on its inventory transactions, labor tickets, service receipts, direct invoices, etc., is equal to the value posted to the general ledger.

**Project Summary Analysis (only if you are licensed to use Projects/A&D)** -

**FIFO/Average Costing Analysis** - Use this report to analyze FIFO Distributions for transactions within a specified range.

You can output all costing reports to:

**Print** - To send the report to your printer, select the **Print** option.

**View** - To view the report using the report viewer, select the **View** option.

**File** - To send the report to text file, select the **File** option. VISUAL prepares your report as a CSV file and a dialog box appears prompting you to enter the location and file name for the file to be saved.

**E-mail** - To prepare the report and attach it to an email, select the **E-mail** option. VISUAL prepares the report as an RTF file and attaches it to a Microsoft Outlook email message. For information on addressing and sending the email message, refer to your Microsoft Outlook user documentation.

Click the **Send** button when you are ready to send the message.

To attach a PDF (Portable Document Format) file to your email instead of a RTF file, select the **PDF Format** check box.

### Printing Cost of Goods Sold Analysis Reports

The Cost Of Goods Sold Analysis report provides a means to analyze your cost of goods sold by customer order.

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to view in the report. If you are licensed to use a single site, this field is unavailable.
- Select the **Print Cost of Goods Analysis...** option from the File menu.
- Enter the end date of the accounting period.

You can enter any date and VISUAL automatically determines the end date of the period in which the date you entered falls.

- To limit the orders included in the report, enter the starting and ending Order IDs. The IDs you specify are included in the report, along with any IDs that fall alphabetically between the two IDs you specify.
- Select the type of report to run. You can select:

**Analysis Report** - Select the Analysis report to compare cost of quantity received to the cost of quantity shipped and posted to cost of goods sold. For each customer order, the report lists the Part ID shipped, the costed and shipped quantity by work order, and their values. The values on this report are the actuals from the work orders. With actual costing, work order costs can be affected across several periods, this report shows you a total cost by month for the last four months.

This report has an option to print for exceptions only, based on parameters you supply. By clicking on the **Exceptions Only** check box, you can specify a Plus/Minus % deviation and / or a Plus/ Minus Amount deviation. This is a useful tool to help determine if there are any missing transactions or costing information.

For example, if you received an order to inventory for \$1000.00 and the shipment (issue) is valued at \$1000.00; however, the Cost of Goods Sold value is \$200.00, this indicates that the Shipments Journal needs to be run to capture the \$800.00 to Cost of Goods Sold.

**Margin Report** - Select the Margin report to view the margin earned on that product by customer order and line item shipped. Information in the report includes ship quantity, revenue, actual cost, margin, estimated cost and a variance between estimated and actual cost. For each customer order, this report also shows you a total revenue versus total actual costs and total revenue versus estimated costs and the respective margins. Use this tool to highlight where costs have deviated from estimate and which work orders may need to be reviewed.

The option available for this report is to print all orders that affect Cost of Goods Sold for that period or only those orders shipped during that select period. Orders shipped in a previous period may affect Cost of Goods Sold in the current period due to changes to the order shipped such as posting of Accounts Payable Invoices or edits to Labor Tickets.

- If you selected the Margin type report, and want only orders you have shipped for the period you specified, select the **Shipped This Period Only** check box.
- To process only differences between the transaction value and posted value, select the

**Exceptions Only** check box in the Options section and enter the filter criteria.

- To include only the transactions you shipped for the current period, select the **Shipped This Period** check box.
- Click the print option arrow and select the print output option to use.
- Click **Print**.

### Printing Cost Distribution Analysis Reports

Use the Cost Distribution Analysis report to show how costs for a given item were issued at FIFO or how costs were distributed due to links between the Work Order requirement and the Purchase Order, or the Work Order and Customer Order in the case of a buy / resell part or manufactured part.

This report shows the Out Transaction ID (issue a shipment) and the associated In Transaction IDs (Purchase Order receipts or Work Order receipts) from which the issue received its costs. The total dollar value of the issue should be equal to the total dollar value of all receipts consumed by the issue. Use this report to determine and/or analyze any cost inconsistencies in a part's inventory valuation. The ability to search for a Plus/Minus cost% or a Plus/Minus cost amount can be performed.

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to view in the report. If you are licensed to use a single site, this field is unavailable.
- Select the **Print Cost Distribution Analysis...** option from the File menu.
- In the Starting and Ending Part ID fields, enter the starting and ending Part IDs for the range of parts to include in this report. To include all of your parts, leave these fields enpty.
- In the Transaction Starting and Ending dates field, enter the appropriate dates. for the range of transactions to include in this report.
- To process only differences between the transaction value and posted value, select the

**Exceptions Only** check box in the Options section and enter the filter criteria.

- Click the print option arrow and select the print output option.
- Click **Print**.

### Printing P/O Accrual Analysis Reports

Use the P/O Accrual Analysis report to review purchase order receipts against the amount invoiced against those receipts. VISUAL automatically accrues purchase orders that have been received by debiting the account on the purchase order and crediting the purchase accrual account from the interface table. To create this accrual, VISUAL uses the value from the purchase order. When the invoice is entered and matched to the receiver, the accrual is effectively moved to the accounts payable with any difference in amounts booked to the proper accounts.

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to view in the report. If you are licensed to use a single site, this field is unavailable.
- Select the **Print P/O Accrual Analysis...** option from the File menu.
- Enter the Received From and Received Thru dates.
- Enter the Purchase Order ID.
- Select the receiver option to use for this report:

**Show All Receivers** - To include All of your receivers, select this option.

**Received, Not Invoiced Only** - To include costs you have received but Not invoiced, select this option.

**Incorrectly Matched Amounts Only** - To process only differences between the transaction value and posted value, select the **Incorrectly Matched Amounts Only** option. You can also set the limitations for the mismatched records by entering Plus/Minus % and Amount values.

- Click the print option arrow and select the print output option.
- Click **Print**.

### Printing Journal Preparation Analysis Reports

The Journal Analysis report examines each purchase order, work order, sales order, adjustment and indirect labor ticket, and attempts to determine if the item's current value, based on its inventory transactions, labor tickets, service receipts, direct invoices, etc., is equal to the value posted to the general ledger.

You can run this report for a range of orders or transactions for the selected journal type, or for all orders or transactions for that journal. It can also be run for exceptions only. You can define the exceptions parameters in the Plus/Minus % and or the Plus/Minus Amounts fields.

This tells VISUAL that any transaction that is not equal should be reevaluated with the next run of the Costing Utilities Receipt Transaction and Inventory Transaction costing functions.

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to view in the report. If you are licensed to use a single site, this field is unavailable.
- Select the **Print Journal Preparation Analysis...** option from the File menu.
- Enter the Starting and Ending IDs for the report.
- In the Type section select the type of journal to prepare.
- To reevaluate any transactions that are not equal with the next run of the Costing Utilities Receipt Transaction and Inventory Transaction costing functions, select the **Mark as Posting Candidate** check box.
- To process only differences between the transaction value and posted value, select the

**Exceptions Only** check box in the Options section and enter the filter criteria.

- Click the print option arrow and select the print output option.
- Click **Print**.

### Printing FIFO Analysis Reports

Use this report to analyze FIFO Distributions for transactions within a specified range.

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to view in the report. If you are licensed to use a single site, this field is unavailable.
- Select the **Print FIFO Distributions Analysis...** option from the File menu.
- Enter the Starting and Ending Part IDs for the report.

**Note:** Because you database may contain many parts and transactions, you may want to filter your transactions by date.

- To filter this report, enter a date in the **Transactions Dated On or After** field.
- Click the print option arrow and select the print output option.
- Click **Print**.

## Recalculating Balances & Costs

### Recalculating Distributions

The Recalculate distribution function corrects transactions that have not been costed correctly. This function reexamines, for the selected part(s) and date(s), the receipts and related issues of that part. If any discrepancies in quantity or cost are found, those transactions are flagged as posting candidates and re-costed with the next run of the Costing Utilities. There is no report that prints with this function.

To recalculate balances:

- If you are licensed to use multiple sites, click the SIte ID arrow and select the site for which you are recalculating distributions. If you are licensed to use a single site, this field is unavailable.
- Click the **Recalculate Distributions** toolbar button.
- To recalculate distributions for a range of Part IDs, select the **Recalculate Distributions for a Range of Part IDs** option and enter the Starting and Ending Part IDs in the Starting and Ending ID fields.
- To filter the transactions that are processed, click the calendar button and select the Process Transactions date on which you want the report to start.
- Select the recalculation method to use:

**Recalculate Distributions** - VISUAL recalculates the cost of out transactions based on existing transaction links.

**Reset P/O Receipts from Matched Invoices** - VISUAL recalculates the cost of in transactions from related A/P invoices. VISUAL also updates any linked out transactions.

**Force FIFO Re-evaluation** - VISUAL breaks existing FIFO links and calculates new FIFO links. Purchases to and shipments from WIP are not affected.

- Click **Start**.

### Recalculating Standard Costs

The Recalculate Standard Costs function updates the standard costs of selected parts using the average of receipt costs for a given period.

For example, Part A may have a standard cost of \$.50, which you defined in Part Maintenance. If you have been receiving Part A with costs between \$.65 and \$.99 for the past three months, you may want to use the Recalculate Standard Costs function to update the standard cost for Part A.

To recalculate standard costs:

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site for which you are recalculating distributions. If you are licensed to use a single site, this field is unavailable.
- Click the **Recalculate Standard Costs** toolbar button.

A list of your parts appears in the Recalculate Standard Costs dialog box.

- Select a part for which you want to recalculate the standard cost. You can select multiple parts using the SHIFT and CTRL keys.

You can click the **Select All** button to select all of the parts. To search for a part:

- 1. Click **Search**.
  - Enter the search criteria and press the ENTER key.

Only the parts matching the criteria you entered appear in the table.

**Note:** You can use standard search language. For more information, refer to the "Concepts and Common Features" chapter.

- To filter costs by the date on which you received them, click the appropriate calendar buttons and enter the Start and End Dates of the receipt period.

VISUAL uses the receipts of the chosen part during this time period to recalculate the part's standard cost.

If VISUAL does not encounter any receipts within the specified range and you want to use the latest receipt of the part for the recalculation, select the **Use the Latest Receipt if No Receipts Occurred in this Date Range** check box.

- To process only differences between the transaction value and posted value, select the

**Exceptions Only** check box in the Options section and enter the filter criteria.

- Click the **Recalc Selected Parts** button.

A progress dialog appears as VISUAL recalculates the standard costs for the parts you selected. The new costs appear in the New Unit Cost column and on what VISUAL bases those costs appears in the New Cost Based On column.

A check mark appear in the row header indicating the changes that will occur to the costs in your database.

- Click **Save**.

VISUAL saves the recalculated costs to your database.

#### Exporting the Recalculate Standard Costs table to Microsoft Excel

You can export the contents of the table in the Recalculate Standard Costs dialog to Microsoft Excel. Click the **Send to Microsoft Excel** button.

### Recalculating Inventory Balances or WIP Balances

It may become necessary to recalculate inventory balances presented by the Inventory Balances Report for a specific financial period. You can recalculate the inventory balances of any period. It is available from the File menu of Costing Utility. To recalculate the inventory balance, the period must be open and the period must be fully costed. The Inventory Balance recalculation does NOT create posting candidates.

When upgrading it is imperative to run the Recalculate Inventory Balances for the Post MFG Audit and the Recalculate WIP Balances reports, and you must ensure that they are accurate. These options must be run for the current open period. The application is not designed to recalculate history on a period-by-period basis. In the future, you can use these reports on a period basis for month-end reporting.

To recalculate inventory or WIP balances:

- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site for which you are recalculating balances. If you are licensed to use a single site, this field is unavailable.
- Click the **Recalculate WIP Balances** or **Recalculate Inventory Balances** toolbar button.
- Enter the Starting and Ending IDs to use for the range of this recalculation.
- Click the Posting Date calendar button and select the posting period for which you want to recalculate balances.
- Click **Start**.
- When VISUAL has finished, the balances dialog box closes.

