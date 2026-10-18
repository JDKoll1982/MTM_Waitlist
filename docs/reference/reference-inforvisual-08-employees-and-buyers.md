# Chapter 8: Employees and Buyers

This chapter includes this information:

**Topic Page**

[What is Employee Maintenance? 8-2](#_bookmark554)

[Starting Employee Maintenance 8-3](#_bookmark557)

[Defining Shifts 8-5](#_bookmark573)

[Editing Employee Information 8-9](#_bookmark579)

[Deleting Employee Information 8-10](#_bookmark582)

[Printing Employee Information 8-13](#_bookmark588)

[Buyer Maintenance 8-14](#_bookmark591)

# What is Employee Maintenance?

Use Employee Maintenance to:

- Create employee records
- Set up earning codes
- Set up indirect codes
- Set up shift definitions
- Assign employees to sites

Employee IDs are used on Labor Tickets. If you use the actual or average costing method, the pay rate you specify is used to determine labor costs.

Employees are different from users. A user can sign into the database. An employee cannot sign into the database unless you associate a user ID with the employee.

Starting Employee Maintenance

# Starting Employee Maintenance

Select **Admin**, **Employee Maintenance**.

# Defining Codes and Shifts

Use the options available on the Edit menu to set up earning codes, indirect codes, and shifts. You use earning codes and shift IDs on the employee record. You use indirect codes in Labor Ticket Entry when the work performed cannot be charged to a particular work order.

You must create earning codes before you can create an employee record.

## Defining Earning Codes

Earning codes classify the work done by your employees. Earning codes are used on labor tickets and for payroll processing.

To define an earning code:

- Select **Edit**, **Earning Codes**.
- Click **Insert**.
- Specify an ID and a description for the earning code.
- Click **Save**.

### Deleting Earning Codes

To delete an Earning Code, highlight the earning code and click **Delete**. Click **Save**.

## Defining Indirect Codes

Indirect labor codes specify the accounts to which labor not associated with a work order is charged. To define an indirect code:

- Select **Edit**, **Indirect Codes**.
- Click **Insert**.
- Specify this information:

**Indirect ID** - Enter a unique identifier for this code.

If you are using Barcode Labor Ticket Entry, specify indirect labor codes using UPPERCASE alphanumeric characters only. Barcode scanning devices cannot read indirect codes than contain lowercase characters.

**Description** - Specify a description of the indirect code.

**G/L Account ID** - Click the browse button and select the account to which this indirect labor type is charged.

**Code** - Specify the type of indirect labor. You can choose:

**M** - Miscellaneous

**V** - Vacation **H** - Holiday **S** - Sick

- Click **Save**.

### Deleting Indirect Codes

To delete an Indirect ID:

- Select the ID to delete.
- Click **Delete**.
- Click **Save**.

The code is deleted.

### Copying Indirect Code Information

To copy information from one indirect code to a new line:

- Select the information to copy and click **Copy**.
- Click **Insert**.
- Click **Paste**.

All information except the indirect code is copied to the new line.

- Enter the unique identifier for this new earning code in Indirect Code column.
- Click **Save**.

## Defining Shifts

Before you can assign employees to shifts, you must first define the Shift IDs. Shift IDs define the break, lunch, and grace periods for a given shift.

You can break down Shift schedules by activities that occur during the shift. These activities include allowed clock-in and clock-out grace periods, between-job lag times, and break times.

To define shifts:

- Select **Edit**, **Shift Definition**.
- In the Shift ID field, specify a unique identifier for this shift.
- In the Description field, specify a description of the shift ID.
- In the Allowed Clock In/Out Grace Periods section, specify this information:

**Minutes before start of shift** - Specify the grace period before the start of the shift. If employees clock in during the grace period, their clock in time is adjusted to the start time of the shift.

If the start of the shift is 7:00 AM and the grace period is 5 minutes, employees clocking in between 6:55 and 7:00 will have a clock in time of 7:00. If they clock in at 6:54 or earlier, the actual clock in time is used.

**Minutes after start of shift** - Specify the grace period after the start of the shift. If employees clock in during the grace period, their clock in time is adjusted to the start time of the shift.

If the start of the shift is 7:00 AM and the grace period is 5 minutes, employees clocking in between 7:00 and 7:05 will have a clock in time of 7:00. If they clock in at 7:06 or later, the actual clock in time is used.

**Minutes before end of shift** - Specify the grace period before the end of the shift. If employees clock out during the grace period, their clock out time is adjusted to the end time of the shift.

If the end of the shift is 4:00 p.m. and the grace period is 5 minutes, employees clocking out between 3:55 and 4:00 will have a clock out time of 4:00. If they clock out at 4:54 or earlier, then the actual clock out time is used.

**Minutes after end of shift** - Specify the grace period after the end of the shift. If employees clock out during the grace period, their clock out time is adjusted to the end time of the shift.

If the end of the shift is 4:00 p.m. and the grace period is 5 minutes, employees clocking out between 4:00 and 4:05 will have a clock out time of 4:00. If they clock out at 4:06 or later, then the actual clock out time is used.

**Between Job Lag Time** - Specify the amount of time allowed between jobs. If employees exceed the amount of lag time between jobs, the time is charged to an indirect ID.

For example, presume you specify 5 in this field. If an employee clocks out of the first job at 2:00, then clocks into the next job at 2:04, then no lag time is charged to an indirect ID. The four minutes are posted to the second job. If the employee clocks in to the next job at 2:06, then six minutes are charged to an indirect ID.

**Minutes Before/After Breaks** - Specify the grace period before and after breaks. If employees clock in during the grace period before a break, their time is adjusted to the start of the break. If employees clock out during the grace period after a break, their time is adjusted to the end of the break.

- In the Break/Meal Indirect ID field, specify the indirect labor ID to use when a break or lunch period is deducted from time posted to the job.
- Click **Save**.
- To define the duration of the shift, break times, and meal times, click the **Edit Shift Codes** button. The ID Of the shift is inserted in the Shift ID field.
- Specify this information:

**Day** - Click the arrow and select the day of the week for which you are defining the shift times. If every day has the same shift schedule, select **Every Day**.

If each day has a different schedule, you must define the shift for each day. You can also use **Mon thru Fri** to define the shifts for Monday through Friday, and **Sat, Sun** to define the code for the weekend.

**Description** - Specify a description of the shift schedule.

**Shift** - Use the Start and End fields to specify the duration of the shift.

**Meal and Breaks fields** - Use the Start and End fields to specify when the break starts and when the break ends. If employees are paid during the break, select the Paid check box. If employees are not paid during the break, clear the Paid check box. If employees must manually clock in and out for the break, select the Manual Clock In/Out check box. If employees are clocked in and out of breaks automatically, clear the Manual Clock In/Out check box.

**Paid breaks Charged to Indirect** - To charge paid breaks to an indirect ID, select the **Paid Breaks Charged to Indirect** check box. Specify the indirect ID in the Shift Definition dialog box.

**Periods Per Hour** - The system inserts 60. You cannot change this value. The system rounds clock in and clock out times to the nearest minute.

- Click **Save**.
- Click **Close** to return to the Shift Definition Dialog box.

# Creating Employee Records

Before you can create an employee record, you must first create Departments and Earning Codes. Create Departments in Application Global Maintenance. Create Earning Codes in the Earning Codes dialog box available on the Edit menu.

To create an employee record.

- In the header, specify this information:

**Employee ID** - Specify a unique identifier for the employee. To make complying with the European Union GDPR rules easier, we recommend that you do not use personal information, such as a name, for the ID. See "Individual Privacy" on page 4-1 in the system administrator's guide.

**First/MI/Last** - Specify the employee's name in these fields.

**Type** - Click an option to specify how an employee is paid. Click either **Hourly** or **Salary**.

**Pay Rate** - This field is available only if you are licensed to use a single site. If you are licensed to use multiple sites, define the employee's pay rate in t.he Sites dialog box. [For more information,](#_bookmark585) [refer to "Assigning Employees to Sites" on page 8-11 in this guide.](#_bookmark585)

If you are licensed to use a single site, specify the employee's pay rate in Pay Rate field. If you selected Hourly in the Type section, then specify the employee's hourly rate. If you selected Salary, then specify the employee's yearly salary.

The pay rate is not displayed anywhere else in the system. The pay rate is used to calculate labor costs for labor tickets entered by this employee.

**Department** - Click the browse button and select the default department for the employee. You can override the default department on the labor ticket.

Maintain departments in Application Global Maintenance.

**Earning Code** - Click the arrow and select the default Earning Code for the employee. You can override the earning code on the labor ticket.

**Default Shift ID** - Click the browse button and select the default shift for this employee. The employee can work during a different shift.

**Cost Category** - If you are licensed to use Projects/A&D, click the browse button and select the default labor cost category for this employee. If you are not licensed to use Projects/A&D, this field is not available.

**User ID** - To associate this employee with a User ID, click the arrow and select the user ID. The employee can sign into the database using the User ID you specify.

**Email Address** - Specify the employee's email address.

- In the address section, specify the employee's address.
- If this is a current employee, select the **Active** check box. If this is not an active employee, clear the Active check box. Inactive employees cannot be selected on labor tickets.
- Click the **Save** toolbar button.

## Adding a Picture

You can add a picture of the employee to the employee record. Depending on your settings in Preferences Maintenance, you can display the picture directly on the Employee Maintenance window.

To add a picture:

- In the Employee ID field, specify the ID of the employee.
- Select **Edit**, **Picture/Object...**.
- Click **Paste From**.
- Navigate to the file containing the picture, and then click **Open**. The picture is imported.
- Click **Save and Close**.

If you display the picture directly in the Employee Maintenance window, you can click the picture to open the Picture/Object dialog box. If a picture has not been added to the employee record, then you must select **Edit**, **Picture/Object...** to access the Picture/Object dialog.

### Displaying the Picture in the Employee Maintenance Window

To display the picture in the Employee Maintenance window, set up the ShowPicture preference setting.

To set up the preference:

- Select **Admin**, **Preferences Maintenance**.
- Click **Insert**.
- Specify this information:

**Section** - Specify **EmployeeMaintenance**. **Entry** - Specify **ShowPicture**.

**Value** - Specify **Y**.

- Click **Save**.

## Editing Employee Information

You can edit employee information using Employee Maintenance.

- Click the **Employee ID** browse button and select the employee record to edit.
- Make the changes to the employee information.

You can change any field in the Employee Maintenance window except the Employee ID; changing the Employee ID creates a new employee.

- Click the **Save** toolbar button.

## Deleting Employee Information

**Caution:** Deleting an employee record permanently removes the information from the database. You cannot recover deleted employee information. You cannot delete an employee that has associated information.

- Click the **Employee ID** arrow and select the employee record to delete from the list that appears.
- Click the **Delete** toolbar button.

A dialog box appears prompting you to confirm the deletion.

- Click **Yes** to continue, or **No** to cancel the deletion. The employee record is removed from your database.

# Assigning Employees to Sites

This procedure applies to users licensed to use multiple sites only.

Use the Allowable Sites function to assign the employee to sites and to specify the employee's pay rate in each site.

Employees can enter labor tickets for any site to which they are assigned. If employees are not Use assigned to a site, they cannot enter labor tickets for that site.

When employees use Labor Ticket Entry, they can only select work orders associated with the sites to which they are assigned. They cannot select a work order form a site to which they are not assigned.

You can also assign employees to sites in Site Maintenance. To assign an employee to a site:

- Click the browse button and select the employee to assign to sites.
- Select **Edit**, **Allowable Sites**.
- Click the **Assigned** check box next to each site the employee is allowed to use.
- Click the **Default** check box next to the employee's default site. When you specify this employee in Labor Ticket Entry, the default site is inserted in the Site ID field. You can override this site with any of the employee's assigned sites. Specifying a default site is not required.
- For each site the employee is assigned to, specify the pay rate. The currency of the site is inserted in the Currency ID field. If you clicked the Hourly option in the Type section for this employee, specify the pay rate per hour. If you clicked the Salary option in the Type section for this employee, specify the pay rate per year.
- Click **Save**.

When you select an employee ID in Labor Ticket Entry, the Site ID drop-down list shows only those sites assigned to the employee. When you browse for work orders, only the work orders associated with the Site ID selected in the Site ID field are displayed.

# Assigning User Dimensions to Employees

If you employ User Dimensions, specify the dimensions to associate with the employee. You can use employee user dimensions in these transactions:

- - Work Order - Issue
    - Work Order - Labor
    - Work Order - Service
    - Indirect Labor

To specify user dimensions:

- Click the **Employee ID** browse button and select the employee for whom you are assigning dimensions.
- Select **Edit**, **User Dimensions**.
- In the left pane, each user dimension group is listed. Expand the list under the user dimension group to view the transactions in which this record is used.

To assign the same dimensions to all transaction types, click the name of the dimension group in the left pane. All Subledgers is inserted in the Subledger field.

To assign dimensions to a particular transaction type, select the appropriate transaction type. The transaction type is inserted in the Subledger field.

- Click **Insert**.
- Specify this information:

**Valid From** - Specify the date the dimension assignment becomes effective.

**Debit Dimension** - Double-click the browse button and select the dimension to use for account debits.

**Credit Dimension** - Double-click the browse button and select the dimension to use for account credits.

- Click **Save**.

# Printing Employee Information

There are a number of options available for outputting employee information. By using the Print command, you can output a complete listing of employee information.

- Select the **Print** toolbar button.

If necessary, you can change the printer setup from the print dialog box by selecting the **Print Setup** button.

- Click the **Starting Employee ID** button to select the employee with which to begin the report.

Click the **Ending Employee ID** button to select the employee with which to end the report. Leaving these fields blank to include all employees in the report.

- Select how to output the report:

**Print** - Select this to view a print preferences dialog box after clicking **Ok**. **View** - Select this to view the report on your screen after clicking **Ok**.

**File** - Select this to save the report to a file after clicking **Ok**.

**E-mail** - To email the report, select this option. Your report is generated in a Rich Text Format (.RTF) and attached it to a Microsoft Outlook email message. To attach a PDF of the report, select the PDF button.

- To print barcode for your employees, select the **Print Barcode** check box and select a barcode type for your report.

**Code39** - This barcode type, also known as Code 3 of 9, contains variable length, discrete symbology. You must have a Code 39 barcode font installed to view the barcode. If you do not have the Code 39 font installed, then the alphanumeric ID is displayed instead with a prefix and suffix. This pattern is used: \*%ID%\*.

**QR Code** - This is a two-dimensional or matrix barcode. QR stands for quick response.

- To include inactive employees in the report, select the **Print Inactive Employees** check box.
- Click **Ok**.

If you selected **Print**, a print dialog box appears allowing you to select print ranges and setup your printer output.

If you selected **View**, VISUAL uses the built-in employee report and the report appears in the report viewer.

If you selected **File**, the Print To File dialog box appears prompting you to enter the name of the file to use for the report. Specify the full path and name to use for the output file. A comma separated value (csv) file is created.

If you selected **E-Mail**, the report is created in rtf format-or PDF if you selected the PDF check box-and attached it to a Microsoft Outlook email message. For more information on addressing and sending the email, refer to your Microsoft Outlook user documentation.

- Enter the recipient's name and the message and click **Send**.

# Buyer Maintenance

Use the Buyer Maintenance dialog to specify the names of individuals who make purchases. By default, the database users are inserted into this table. You can insert additional names. If you insert an additional name in this dialog box, the name is NOT added to your list of database users.

When you enter a transaction that includes a buyer, you can click a browse button to select a buyer from the list. You can also enter a new buyer directly in the Buyer ID field in a transaction window. If you specify a new buyer ID in a transaction window, the buyer name is added to the Buyer Maintenance table.

You can attach user dimensions to buyers. To add a buyer:

- Select **Admin**, **Buyer Maintenance**.
- Click **Insert**.
- Specify an ID and Name for the buyer.
- Click **Save**.

## Deleting a Buyer ID

You can delete a buyer ID only if it is not used in a transaction. To delete a buyer ID:

- Select the row that contains the buyer ID to delete.
- Click **Delete**.
- Click **Save**.

If the ID you selected is used in a transaction, you are informed that the buyer ID cannot be deleted.

## Specifying User Dimensions

If you use user dimensions, you can attach dimension codes to buyers. To attach dimension codes:

- Select the buyer to whom you are assigning user dimensions.
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

