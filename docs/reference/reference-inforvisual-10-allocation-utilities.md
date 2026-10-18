# Chapter 10: Allocation Utilities

This chapter includes this information:

**Topic Page**

[What is the Allocation Utility? 10-2](#_bookmark799)

[Allocation Processing 10-2](#_bookmark801)

[Behind the Scenes of the Allocation Process 10-3](#_bookmark808)

[Setting Up and Running the Allocation Utility 10-3](#_bookmark811)

[Understanding Allocation 10-4](#_bookmark815)

# What is the Allocation Utility?

The Allocation Utility performs soft allocations of supply quantities to customer order demand in a "batch mode." Use the Allocation Utility to establish allocations, or "links," of supply to customer orders by part and/or warehouse and decrease the time you would normally spend in Customer Order Entry looking for suitable links of supply for customer orders, one line at a time.

Allocation is by site.

## Allocation Processing

The Allocation Utility performs allocations in two modes: by part and by warehouse. Allocation by part commits supply to demand irrespective of warehouse; allocation by warehouse commits supply to demand from the same warehouse.

### De-allocation

Before creating new allocations, you can remove existing allocations. Permission to do this, to "de- allocate," is controlled first by the REALLOCATE setting on the demand/supply link record.

If REALLOCATE is set to **No** on the link, the Allocation Utility does not de-allocate the link.

If REALLOCATE is set to **Yes** on the link record, the Allocation Utility references the customer REALLOCATE setting.

This allows a given allocation to be "fixed" by the user, regardless of the customer setting.

During the entering of Customer Types in Accounting Entity Maintenance, you can specify, per type, if reallocation is a default setting. In Customer Maintenance, when you select a Customer Type for a customer, this default carries over to the customer. You can, however, override this setting by selecting or clearing the Reallocate check box as appropriate.

**Note:** The REALLOCATE function does remove the allocation, but it puts it back, together with any new allocations, when the allocation utility is run. To fully remove an allocation, you must manually remove the allocation.

### Sequencing

The first step in performing allocations is the proper sequencing of supply and demand. The first material available to the Allocation Utility is on-hand inventory. After the inventory has been committed, the Allocation Utility sequences future supply orders by due date. Relevant supply order types include purchase orders, work orders, work order coproducts, IBTs, and possibly planned orders.

The first set of demand orders the Allocation Utility considers are back orders sequenced by rank and due date. It sequences other demand orders by rank (priority) according to customer type and due date. **The Allocation Utility considers only customer orders as demand.**

## Behind the Scenes of the Allocation Process

Allocation is done by site. The Allocation utility allocates supply to demand in three passes:

- It attempts to allocate on hand inventory to demand orders in rank (priority), due date order.

If supply is sufficient and committed to all demand orders, allocation is complete. If not, pass 2 and 3 are still available.

- Allocate on hand inventory to demand in rank, due date order, up to the fill rate on the order.
- Allocate remaining on hand inventory and future supply orders to demand orders in rank, due date order until supply runs out.

Orders that received fill rate allocation in pass two are eligible to receive future supply allocation in this pass.

The Allocation Utility only considers demand orders that have a ship date within the allocation fence.

During allocation by warehouse, the Allocation Utility disregards any orders that do not have Warehouse IDs.

## Setting Up and Running the Allocation Utility

To set up and run the allocation preferences:

- From the Admin menu, select the **Allocation Utilities**. option.
- If you are licensed to use multiple sites, click the **Site ID** arrow and select the site to use. If you are licensed to use a single site, this field is unavailable.
- To allocate for a range of parts, click the appropriate browse button and select the Starting and Ending Part IDs to use.
- To allocate for a range of warehouses, click the appropriate browse button and select the Starting and Ending Warehouse IDs to use.

**Note:** By entering only a Starting ID, VISUAL ignores all parts prior to that ID and includes the ID and all those after it. By entering only and Ending ID, VISUAL includes all parts up to and including the ID, and ignores all parts after it.

- In the Options for allocating inventory section, select these options:

**Allocate On-hold Inventory** - To allocate inventory that is currently on-hold, select the **Allocate On-Hold Inventory** check box. Classify parts as on-hold in Warehouse Maintenance.

**Allocate Unavailable Inventory** - To allocate inventory that is currently unavailable, select the **Allocate Unavailable Inventory** check box. Classify parts as unavailable in Warehouse Maintenance.

**Reevaluate Partial Allocations** - To reevaluate those demands to which it has already made partial allocations, select the **Reevaluate Partial Allocations** check box.

- In the Options for Allocating Future Supply section, select these options:

**Allocate Future Supply Orders** - To consider supply orders to be received in the future as it allocates supply to customer order demand, select the **Allocate Future Supply Orders** check box.

Relevant supply order types include purchase orders, work orders, work order coproducts, interbranch transfers, and possibly planned orders.

If you select the Allocate Future Supply Orders check box, you can select where to assign the allocation:

**Allocate Unreleased Orders** - To allocate unreleased work order quantities to customer order demand, select the **Allocate Unreleased Orders** check box.

**Allocate Firmed Orders** - To allocate firmed work order quantities to customer order demand, select the **Allocate Firmed Orders** check box.

- Click the **Run** toolbar button.

As the Utility carries out your allocation instructions, a progress dialog box appears.

**Note:** To stop the allocation before it has finished, click the **Cancel** button. If the allocation session is successful, a message appears.

- Click **Ok**.
- When you have finished allocating, select **Exit** from the File menu.

## Understanding Allocation

This example illustrates how the Allocation Utility allocates supply to a customer order for which there is insufficient stock of on-hand inventory to satisfy demand.

Example scenario:

- There is, at the time of order, a quantity of 383 in Warehouse MMC-MAIN.
- There is a purchase order for 300 of the same parts due in on 1/10/2008.
- There is an unreleased work order for 5,000 due to reach released status on 1/11/2008.
- No quantity of this part is on hold or unavailable in warehouse MMC-MAIN. You would set up the allocation utility as follows:

By selecting the same Starting and Ending Part IDs, VISUAL will allocate supply only for the part you select.

By selecting the same Starting and Ending Warehouse IDs, VISUAL will allocate supplies from within that warehouse.

Because all of the check boxes in the Options for Allocating Inventory section are clear, VISUAL will only use available supplies for current demand.

Because all of the check boxes in the Options for Allocating Future Supply sections are selected, VISUAL will try to use orders you expect to receive in the future for demand you expect to need in the future.

When you run the Allocation Utility, these actions are performed:

On the first pass, the Allocation Utility allocates the 383 parts in inventory in MMC-MAIN to the customer order according to the customer's priority and the due date of the order. Because this is the only order for this part, all 383 parts are allocated to this order. The customer's priority and fill rate mean nothing.

On the second pass, nothing occurs because this is the only customer order for this part.

On the third pass, the Allocation utility checks future supply for quantities to allocate to the customer order. It looks for the earliest date. In the case of unreleased or firmed work orders, the release date, or in the case of purchase orders, the receive date.

Finding the purchase order with a release date of 1/10/2008, it allocates the purchase order quantity of 300 to the customer order. Continuing to look, it finds the unreleased work order with a release date of 1/11/2008 and an order quantity of 5,000, and allocates the remainder to the customer order.

When you open the customer order after running the Allocation utility, "Multiple Links" appears in the Supply Type column indicting that VISUAL obtained supply for this customer order from more than one source. In this case, VISUAL obtained supply from inventory, a purchase order, and an unreleased work order.

In the Customer Order entry window, highlight the order line and select the **Assign Supply to Customer Order Line** option from the Edit menu. The Supply Links dialog box appears with the three links of supply the Allocation Utility created appearing in the line item table. See"Customer Order Entry" on page 7-1 in the Sales guide.

