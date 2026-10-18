# Chapter 9: Workflow

This chapter includes this information:

**Topic Page**

[What is Workflow? 9-2](#_bookmark596)

[Starting Workflow 9-4](#_bookmark601)

[Understanding Workflow Components 9-6](#_bookmark610)

[Building Workflow Templates 9-17](#_bookmark706)

[Working with Steps 9-18](#_bookmark709)

[Working with Rules 9-22](#_bookmark722)

[Working with Statements 9-26](#_bookmark741)

[Selecting Workflow Preferences 9-32](#_bookmark769)

[Logging Workflow Timer Events 9-33](#_bookmark772)

[Using the Workflow Tracker 9-34](#_bookmark773)

[Using the Workflow Gatekeeper 9-37](#_bookmark796)

# What is Workflow?

Use Workflow to create predefined conditions and rules for guiding users through specific tasks within applications, such as Purchase Order Entry and Customer Order Entry. Embedded into the logic of VISUAL, Workflow works behind the scenes. When you activate a workflow, all the areas of VISUAL, as defined in the workflow, must behave in accordance with the workflow and its set of rules.

Using a workflow-based approach can help you achieve these goals:

- Streamline repeatable processes.
- Define a rule and condition based environment. Because individuals or their knowledge of processes should not determine process flows.
- Minimize operational errors.

Using the Workflow Designer, you can assign the _steps_ necessary to complete tasks and define the _rules_ that govern those steps. The Designer saves the workflows you create as templates that VISUAL uses to create Workflow IDs.

Common workflows contain:

- Process Definition
- A graphical representation of the flow of business information
- System of controls, monitors, and management tools to help automate certain aspects of your business process.

What can Workflow do for you?

- Provide a graphical representation of your processes.
- Graphically create and display the flow of work and tasks.
- Embed business logic into process-in the form of rules.
- Build rules for exception handling.
- Track the status of process steps.
- Involve third party applications to perform tasks.
- Inform users of new tasks or warn them of late tasks.
- Connect to Task Maintenance functionality.

## Preliminary Requirements

Before using Workflow, you should be familiar with VISUAL applications, such as Task Maintenance and Document Maintenance. You must also know how to add users and create user groups.

Refer to these sections:

- For information on using Task Maintenance, refer to the "Engineering Change Notices" chapter.
- For information on adding documents in Document Maintenance, refer to the "Document Maintenance" chapter.
- For information on setting up groups for tasks and authorizations, refer to the _Infor VISUAL System Administrator's Guide_.
- For information on adding new users, refer to the _Infor VISUAL System Administrator's Guide_.

## Workflow and Double-byte Characters

Workflow does not support the use of double-byte characters. This includes all aspects of the workflow itself, such as rules and properties, and the VISUAL records on which the workflow is run. For example, if you set up a workflow to run on parts in Part Maintenance, then all Part IDs must use single-byte characters. Otherwise, the workflow will not be triggered.

# Starting Workflow

To start the Workflow Designer:

- Select **Admin**, **Workflow Designer**.

The Workflow Designer window opens with the Open Existing Workflow dialog.

- Do one of these:
  - To open an existing Workflow template, refer to the "Opening and Existing Workflow Template" section.
  - To create a new Workflow template, refer to the "Creating a New Workflow Template" section.

## Creating a New Workflow Template

Use workflow templates to specify the application where you will use the workflow. To create a new Workflow template:

- In the Open Existing Workflow dialog, click the **New** button.
- In the Type list, select one of these types:

**Master** - This feature is not available at this time.

**Predefined** - Starts with an empty template.

**User Defined** - This feature is not available at this time.

- In the Application Area list, select the application area (for example, A/P Invoice Entry or Cash Book) where you are creating a workflow.
- In the Template Name field, type the name for this template.
- In the Usage list, select one of these:

**All Documents** - VISUAL applies this template each time you create a workflow.

**Single Document** - VISUAL uses this template for a specific document. For example, you can create a custom template for a specific purchase order or quote.

- Do one of these actions:
  - If you selected **All Documents** in the Usage list, go to the next step.
  - If you selected **Single Document** in the Usage list, type the exact document ID (for example, specify a specific order ID if you are creating a workflow for a particular purchase order) then click **OK**.

**Note:** The Associated DB Table field is "view only" and lists the database tables associated with the application that you selected in the Application Area list.

- In the Status list, select one of these options:

**Active** - The template is active within the VISUAL logic. When a template is active, the application you selected in the Application Area list (for example, A/P Invoice Entry or Cash Book) behaves in accordance with the workflow and its set of rules.

**Inactive** - The template is not active within the logic.

- Click **Ok**.
- To build a workflow, refer to the "Building Workflow Templates" section later in this chapter.

## Opening an Existing Workflow Template

To open an existing Workflow template:

- In the Open Existing Workflow dialog, click the Application Area arrow and select the application area.
- Click the **Template Name** button and select the template to open.
- Click **Ok**.

The Work Area appears with the selected template.

# Understanding Workflow Components

There are four components of the Workflow Designer window:

- - The **Menu Bar** contains a comprehensive navigation system giving you access to Workflow Designer functionality.
    - The **Main Toolbar** provides you shortcuts to several of the most common functions of Workflow.
    - The **Vertical Toolbar** gives you the tools you need to build your workflows.
    - The **Work Area** is where you will build, manipulate, and view your workflows. You may want to think of the work area as a canvas or pasteboard.

**Note:** All the Workflow components described in this section are available in Workflow Designer. Workflow Tracker has limited functionality.

## Menu Bar

The menu bar options are alternatives to the Workflow toolbars, and includes features that are not included on the toolbars. The menu bar includes these menus:

### File Menu

The File menu contains commands for manipulating files within Workflow. You can select these menu options:

**Save** - Saves the current workflow to your database. You can also click the **Save File** button on the toolbar or press CTRL+S.

**Save As** - Allows you to save the current workflow with a new name or in a different location.

**New...** - Opens a new workflow. You can also click the **New File** button on the toolbar or press CTRL+N.

**Delete** - Deletes the current workflow. You can also right-click in the work area and select **Delete Workflow** from the menu.

**Refresh** - Updates the information in the current workflow.

**Open...** - Opens a previously saved workflow. You can also click the **Open File** button on the toolbar or press CTRL+O.

**Close** - Closes the current workflow.

**Import** - Allows you to import workflows from other VISUAL users.

**Export** - Allows you to export the workflow to other VISUAL users.

**Print Diagram** - Prints the current workflow to a selected printer. You can also click the **Print** button on the toolbar or press CTRL+P.

**Print Details** - Opens the Print Workflow dialog where you can print, email, or view workflow reports.

**Print Setup** - Allows you to set up default print options for printing a workflow file.

**Print Preview** - Allows you to view the workflow before printing.

**Exit** - Closes the current workflow.

**Sales** - Allows you to open the Sales modules.

**Inventory** - Allows you to access modules in the Inventory application. **Purchasing** - Allows you to access modules in the Purchasing application. **Scheduling** - Allows you to access modules in the Scheduling application.

**Eng/Mfg** - Allows you to access modules in the Engineering/Manufacturing application.

**EqMnt** - Allows you to access modules in the Equipment Maintenance application.

### Edit Menu

The Edit menu contains commands for editing files in Workflow.

**Undo** - Allows you to reverse previous actions. You can also click the **Undo** button on the toolbar or press CTRL+Z.

**Redo** - Allows you to reverse previous actions. You can also click the **Redo** button on the toolbar or press CTRL+Y.

**Cut** - Deletes the selected object from the work area and moves it to the Clipboard. You can also click the **Cut** button on the toolbar or press CTRL+X.

**Copy** - Copies the selected object to the Clipboard. You can also click the **Copy** button on the toolbar or press CTRL+C.

**Paste** - Pastes an object from the Clipboard to the work area. You can also click the **Paste** button on the toolbar or press CTRL+V.

**Clear** - Deletes the selected object from the work area. You can also press the **DELETE** key.

**Select All** - Selects all objects in the work area. You can also press CTRL+A.

**Change Shape Color** - Opens the color palette where you can select basic colors or create custom colors for shapes in the work area.

**Edit Label** - Opens the Shape Label editor where you can change the name of the selected object.

**Edit Properties...** - Opens the selected object's properties dialog.

**Workflow Properties** - Opens the Workflow Properties dialog where you can edit the properties of the current workflow. For more information, refer to the "Using the Workflow Properties dialog" section later in this chapter.

### View Menu

The View menu allows you to hide or show the Personal Toolbar, and cycle through open workflows.

**Personal Toolbar** - Hides or shows the personal toolbar. A check mark indicates that the personal toolbar will appear in the Workflow window.

To hide the personal toolbar, remove the check mark by selecting **Personal Toolbar** on the View menu.

**Back** - Allows you to cycle through open workflows similar to a Web browser. You can also press

**Back** button or press CTRL+B.

**Forward** - Allows you to cycle through open workflows similar to a Web browser. Displays the workflow you were using before you selected Back. You can also click the **Forward** button or press CTRL+F.

### Help Menu

The Help menu contains commands for accessing the Workflow help system.

## Using the Main Toolbar

The Main toolbar contains shortcuts to the most commonly used functions of Workflow.

- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAUklEQVQ4jWP88OEDAwWAiRLNVNDPgsYXEBDAqg6XN7HY/x8D4DGXWPf///8fqxHE6mdkZMTqCnT/47Ic2RRy7McFRvUPrH4s8Y8ZyXgA4xDP/wAutyFMrbbhXwAAAABJRU5ErkJggg==)Use the **New File** button to open the Create New Workflow dialog.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAmElEQVQ4jcWT0Q3DIAxEn6sMcCtl8qzEBs4HhUbGKEWpVH8gJPzusA1WSuFBvJ7AP+C3b5Ik1c1Y7L2/pNKiCnU5wFb7V+FObeNZes+QkPhLcgcczCyBU9HQPwcDd7e+r2suOfQvYpc1j9y/lWBN9F3gWEvgP/BxzCzZ9+n9HbiFr42M/ktw9F+FCfOfosBk/svvN8S///8J6A9L7PGoT1gAAAAASUVORK5CYII=)Use the **Open File** button to open the Open Existing Workflow dialog.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAbUlEQVQ4jWP88OEDAwWAiRLNVNDPAmcJCAgQqQfZyyj2NzRApbECNJ1Y9BMEmG4kTT/EgeTrxwQDrZ8Fjd/QgDMiFyxY8OBBAgH9DNgCCQIwNZOmH6sUUf6Hpx9MMNDhz4jsMCKzELIWxgEuPwD5JDoMj9V3yQAAAABJRU5ErkJggg==)Use the **Save File** button to save the current workflow to your database.

![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABcAAAAVCAIAAAAigOL8AAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAnUlEQVQ4jc2U2w3FIAiGaeMAjOoIjNAV2MDRcAL6YGqaCuT08FLii1w+f4i6qSrkjJkLAPTek6A9Wf99yoEYZHvRJ6WKeKkHYhX5ieKBAgQAFNM7QHRtCSBA2FomiC4EeUmrFrTGMRFrVG7qiukdNpoaa43eze1ozoVedfRAVJG6eF5oMQuCe2RQgjMD0KaqyZ+htfblN/2fFWbOU05Q7knLZr+8WwAAAABJRU5ErkJggg==)\- Use the Gatekeeper button to open the Gatekeeper window displaying the rules where VISUAL encounters processes awaiting action.

- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAnklEQVQ4ja2TQRaAIAhEpec+jupRPCqdgBaaEFL6MpbiZ8ZRgYjCQm0r8A98dFcR8Qkw53V4RGRmFwaAKf1+KzMDQB+25Zv4k4UpfVc8XLloF7f8tbiu0i0YEel0o4Z75VYFM7DwpfdyzgYP9IcjzKLwfS/nOjElagJmSr1Sd/a+V/44hDdKk/6FNNvkSb1HqPPz+ZEF69zhP9Tq/z8BuzlyN/b/+oQAAAAASUVORK5CYII=)Use the **Print** button to print a copy of the current workflow to your printer.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAmUlEQVQ4jdWT0Q2DMBBD44oBbtRuAN2go94G7kekcOcEUkR/6i9Q7tnnCODu5YYed+Af8Et7MjM5+6ZaymfQ0HHCi0hOLdCWNDOSACoJoJRNpt2fEz6d4UWu8bW30P0RNGq0mr3PeLnC5hhXiBaL8Dl2q44tPLpcyz8qlfjhKLOE3/fvv7baUzLFQvv36jMv8PGqxgN//v9/APCTa6dCLDDRAAAAAElFTkSuQmCC)Use the **Copy** button to copy the selected object to the Clipboard.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAdElEQVQ4jWP88OEDAwWAiRLNNNMvICCAlU0v+6mgH+Js/I7HqR85UvFHMD73E7Qcn36ItQRTFx7/T4CT5OiHWy4gMAGPKSy4bEYypQAiCGEQsB+iDqtSrIDS9IPF/cjgw4cCiHdwuYgRM4bwayCsnyRAqf8Berwt+GuoIG4AAAAASUVORK5CYII=)Use the **Cut** button to delete the selected object from the work space.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAq0lEQVQ4jcWU0Q3DIAxEz1UG8IjeJMkGGYHROoIzgftB4xJwIyoqxV8G/O5sgSBVxUA8RuA/8FO4y8yeXw9IflwyAMwAgOhU3WpNDqeUci4iZm8yJ37EzJVE3b+IqOq+I5flpFSP/cuoBrmOmv/m08t7iDyHeABmc7kkWoGlqvnt/ZjNzFuv/+H56cWXvXw5QijR69/KnXhVba59CYGYR/O2mbd22qDBm/+PF9sVSp3jFziZAAAAAElFTkSuQmCC)Use the **Paste** button to paste an object from the Clipboard to the work area.
  - ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADQAAAATCAIAAABQs7kyAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAA1klEQVRIie2UwQ2DMAxFfyvueARGygiM0FEygtnAI3WEZIMeEFEK1DEJUjnwTyTifz+E7UcIAQCAvu/RpmmanHONIbm6/BBjPDG6Xc9/A2i64Wp1w9WqK7+yiMinZ+bBuDVEZBzf6RjCy17xJxyRT0EzVp5L5JmlyDeTrYx5VF7lAFxy7vqXe50NK7KElZJ1e6Hn9C+rloUMloGwpByVMdM0rdssIs88FI3Mw67XUhRKzyl/U0QAWKbVOQd4ka/RsffJgVWSlyyOQgXKVpdewjdcrS4N9wEnTVpVS9TxKgAAAABJRU5ErkJggg==)Use the **Undo** and **Redo** buttons to cycle through your changes.
  - ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAC0AAAAUCAIAAACWDSOoAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAACeUlEQVRIic2WMWjbQBSGf4opGjzcIIwIGo6S4YYOGkwQwVPw4KGUG0MQxYNHETJmCJkyZAzFQwYPopiQIYQbPZhOoRwlg4cMN5hygykiaLiABw2BdlCSupbkkCpD/0Gg09P/vnvvoRN+VVMURRUdMpPa3d0dKqu6yZvqEK+iVRzNjWZ7q109R3ur3dxo/iNHsNmKtSaOU52DOE6sdbDZejHH0eGBRWlaJ6Q6BUCAtE4sSo8OD8piagUQn4KTGwUp0e2mWlfnSLWG74sogu9D64Mvw+c5+rvh+VQDACGYm4lSvY8f0nm6FDb8Oi5MGeTmyapbE6VgExAC4HyqyW4Yfu6v4ujtBHKq4ntAKQBgLE6MeElJRkm8vJQAjIExAFAqZuxUyslOMDj7qyp/ONyGTWwntixI+bAU7iEsSMb398s4+HtPHB+vIpUy9n05mbgNe3abLHO4DRvA7Oes9mMKQmAMCMk2YQHp4zW7XS2LsafIxRcBPNiOR7N36/W3tUWUB47ZbeI2bHfN9cM9EUVZL7PuZC5PXsuTklOa9TT3IhjLdsjDPbnmmiQuqEeG0tsJAPBuVwgBAP0TXElO6VImIeWgBEJIme+a0BotH/3TzByA73ml8wFgcDZ0GzZZZ5xzIQSUykMA4L5fglH8iFMqlALAOZdam6larEQBBx4bNJoqAKiTVOvht6uyrHkt7TJTsNkCpQBG41GWIh9T8D2d3SaddgeeBymtonq8VBalkBKe12l3CiGKOQAMzobbjmPNjbl/di6fl7lPrbnZdpzCaq3iANC/vHAoNYl5BY7EOJT2Ly9WxBScL0+6/n5dHQLAuOQQWNT/8h/0G6SognU79BRWAAAAAElFTkSuQmCC)Use the **Back** and **Forward** buttons to cycle through the open workflows.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAATklEQVQ4jWP88OEDAwWAiRLNVNDPgsYXEBBAE8HvQXT9DAwM//8j2IyMBOwfbP5nIMLNKIoHOP4Jxx8aQHMvgfhDA5hBM9DxN9Ljn1L9AKWgEqR9gKdiAAAAAElFTkSuQmCC)Use the **Align Left** button to align selected objects on the left.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAU0lEQVQ4jWP88OEDAwWAiRLNVNDPgl9aQEAATQTNvwT0MzAw/P+PYDMyossOcv8zYHMziuwAxz+6+zEjDA0Qjj/kCEMDgy/+aBD/+CMcXfEQz/8AWoISpBVIbZIAAAAASUVORK5CYII=)Use the **Align Right** button to align selected objects on the right.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAVElEQVQ4jWP88OEDAwWAiRLNVNDPAmcJCAggS0D8hSaIJouin4GB4f9/KIOREYsgHCDLDrT/qRd+DKgeI1k/eQlpoP0/0PpZCKrAHymMQzz/UqofAECdEqNdl4QxAAAAAElFTkSuQmCC)Use the **Align Top** button to align selected objects on the top.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAVUlEQVQ4jWP88OEDAwWAiRLNVNDPgl9aQEAAqzjc1wT0MzAw/P+PLsLIiGAPtP8HWj8i/NCCmsh0hRL+8KBGDmH8YKD9T9X0i9Xb+MOCcYjnX0r1AwDSrA/T9yI2OAAAAABJRU5ErkJggg==)Use the **Align Bottom** button to align selected objects on the bottom.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAWElEQVQ4jWP88OEDAwWAiRLNVNDPgktCQEAAmYvLmzj1MzAw/P8PZTAy4lQzaP3PgNfZCDUDHP/o7keLNkyA5l4s/odHGybADJGBjj8axD8x0Y5QPMTzPwCBBBKkCPZYGQAAAABJRU5ErkJggg==)Use the **Align Vertically** button to align selected objects on the vertical center axis.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAWElEQVQ4jWP88OEDAwWAiRLNVNDPgl9aQEAAqzjc1wT0MzAw/P+PLsLIiGBTz/1oTiUyXlDcD3cqsgvxg4GOP6rGP/HexqKfvIxA4/TLQMhTjEM8/1KqHwBsQxKn7YppAgAAAABJRU5ErkJggg==)Use the **Align Horizontal** button to align selected objects on the horizontal center axis.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAATCAIAAADwLNHcAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAeklEQVQ4jcVT0QrAIAjcjT7A//9K/6AeBDuctUHCJErI4zq9oKrXQdwn4AJ8s0NENkWq6gVBb/Os9xwMTNiTpuj9r+HMlriKr/wsm/OJB/IVYKF/qPePKdzvZfxJ/0VkNW0zElMmeL5mU7GRPJb6HRMGUaz/7/6f/p8BL3w7BJ6eIYsAAAAASUVORK5CYII=)Use the **Copy Size** button to resize selected objects to the size of the object you selected first.
- ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABUAAAAWCAIAAACg4UBvAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAmklEQVQ4jcWSQQ7EIAhFsXGPR/OIHPFzAmbBpHWMZRpclAWayAM/+UVEaCMqEfXe0/zhBzMns4gAAGBmifzlc7CZVf+/qjLzXS6lmNnytZ76A5iI7loc8XyHg5ojmD/CAJY1P/od8NIn8Kzf76210SEBPOsHMNkrhhf6xxZ/4fX+vcUTWFV3/fe2/9+ef+0/l4uZ0UbU07a5+ACIQKDOlhYeGAAAAABJRU5ErkJggg==)Use the **Sticky Palette** button to paste multiple copies of a selected object onto the work area.
  - ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADAAAAATCAIAAABZWBlIAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAx0lEQVRIiWO8dvncuvXb9u3fxzA4AAuctXffXhrZcf3KeeIVsxBWQg2gqWNIjLLW5lY6OYiB6HBiwirKyMiIxoUAKriLEEAPIUxbGRkZ////j8mmEUAPof///9PaSvyAfmkIArR0jTAFr10+B2djT0O0A8h2YxWht4PQXIDpvgFwENwdmK5hGCgHMeBwDQMxifr////wsoAOGRC7g9AspmdBMGBRhgsMOgfRqWBsbW4lUiU9HJSTV0K8YkZIizEo0It2DiIJAAC3GkTO9dA/QAAAAABJRU5ErkJggg==)Use the **Font Size** list box to select the font size of text in the work area.
  - ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADsAAAAVCAIAAAB34QGiAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAABCUlEQVRIidWWoQ7CMBCGr2QSgeQhEEgeAYngMRBIZCUSwQPsTTYJDhLwk8gJfBGXNEfblXa0u+xXtz/X3pdLr6uQUsKoVABAXdfcGKE6n44FRlVVsRA879fw5N3+ANhjXk1n85C0siwx4CcGgHf7Ck92EAshlFL0EwNtdjl0VT59EWsU6lAspZTHsYPsxLpe1BYITZdnbXaac2x0vd8mi+XKNh+3i+FM+u3u1J/dteFsBxISJzkMFNGJC6mI6czFjoEhBO3ChSTExhWhB7G3PLjwc/JoecRyOkby0HeFUc8u7wEa4CeS8q4YRuMj5n8J6UdZoJiJ8ckbJSGl3G7WOWgySTRNw80Qpw/e0ZKrs+zU/QAAAABJRU5ErkJggg==)Use the **View Size** drop-down box to change the magnification of the workflow in the Workflow window.

## Using the Vertical Toolbar

![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACYAAAFRCAIAAACXD0g0AAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAEEklEQVR4nO3d7ZGyMBAH8OXGAijJDizLNmzBDvxqN1wF3Id4CJqXfQsG+e84zzznqL8JBEx2IXaXy4XWjQMR3e/3tUkiOp/Pq5E/q0lTHOZ/nE4n48ddr9fiaz7QSpAfI2+329qku8rdsI6qYF96qbLu46KKe6xd1RwkRlV5XFrUQ/klyzgej2osBLeVdklGBi/8u0b3cWwfl3znP9NjLdFwj3WMT29YzvjMHvvYsCBBggRJ719efd/XYIZhSJJEROPoDHbd/K997EuQIEGCBAkSJEiQILdIxiYIy/F8dXI+X6kU+9iXjXSfZSgmufkOkSMfmHyG23ddBk5u2L7vaRyVM+pxpHFMbZ44+fCMkVAjpI+XVhs4SDybGOKtoQ20EiRIkCBBgtwcOQyD85yk616Geg20knwb+tbEOOmmxrwk+VR1cNelPMpPEMJ7ermqn5Nw3q+INnosSJAgt0zqLkLQk2G+rVCV5HN+L1c3si9fUxjChorJeMpEosrIXIqGrba9L8tZKF5DuSQ368VQWaQsy1ZSy6Qmq5dV2+s++sSlNOts9bJqKbdujEazziGqptcT6Xx6zJ58HsvxQ2QaVLu418y+BAkS5BSCG3WYA7jimYRL8s+6fTrJFAJjH6VXUvc8EPnEqKBmBXwfJzyQIEGCbIasXTWtWUyMeUnSTeWXTH1UacnUqqZHs+Ueq1Gzo2fWQSJTS6N17gRhGAZmhdhtTsL5LGa0cfapHfENOw07p43p9UySfN9tXs9Q/GJV78DFqlQnTdnkJUZrk+ixlUj02EokemwlEj22EokeW4lEj61EfmePZdyZ6DEPyZHz11WamTTQfUA6hXg5RVoeZ4oTpKRKO8WsPDXPzDB5SZV2esxj9jzzzFVopew+5XHM354cglEyFVX5GM1t6SAxVTEV5W+Hqqmu/F0pdku6lb+l1xXUi92SbtUnXWmmRuyZdNidipqXSbXXvHyDUb8UtTV752WIwtjneQsm56xbwkJIiomza5fnzPRgDioFxcTp/4qxq4ZM8YporMeCBAkSJEhbWPM+JP9iseZ9SP71ac77kDj145r3IVbqxzvvQ+XUT7VL5TeQEcESfObYJ7nCQoOxatDvrydJ9NJ/VvpNixyJ9WNBggQJMhZY38eXxGI7viRWvpGouPsbd38b1Dbu/sadiWYvRzqouDPRV3VZR0SgMqqYgq/osupYpWWp7CqteLgVV9mehoyoEk9JLlShpycnVZG0MU2DdEmirU32QIIE+S2k5xpqblc1PaXSrGEq1674e17/r8xXTbOLBxguIs/A6ZmXx+Sy1creCjWvBlq5t98n8ZjPRoeAuRU2HmcTRW9SnAoiMMe2n/Be3l9chsb5YlX+JxajjR4LEiRIkN9J/gGakcabAskAKwAAAABJRU5ErkJggg==)

The vertical toolbar allows you to add traditional logic diagram objects to the work area. Each object contains assignable properties that define the rules and conditions of the workflow.

## Using the Status Area

At the bottom of the work area, you can view details of the currently open workflow template (in Workflow Designer) or workflow document (in Workflow Tracker). The status area displays workflow template information in this format: _Type:Application Area:Template Name:Document ID_. This is the same information displayed in the Workflow Properties dialog.

## Using the Work Area

The work area provides standard graphics tools for creating workflow diagrams that define the flow of work and tasks that must be completed. Each object within the workflow contains assignable properties used to define the rules and conditions of the workflow.

### Adding Objects to the Work Area

The vertical toolbar contains objects that represent steps or tasks in a workflow. To add properties to the step, you need to edit the properties dialog as explained in the "Building Workflow Templates" section later in this chapter.

To add an object to the work area:

- On the vertical toolbar, select the object to add to the work area.
- Click anywhere in the work area to add the selected object.

### Deleting Objects from the Work Area

To delete objects from the work area::

- Right-click the object to delete; then select **Delete** from the menu.
- Select the objects to delete; then click the **Cut** button on the main toolbar.
- Select the objects to delete; then click **Clear** on the Edit menu.
- Select the objects to delete; then press the **Delete** key.

### Rotating Objects in the Work Area

To rotate objects in the work area:

- Select an item in the work area.
- Do one of these actions:
  - Right-click the object to rotate; then select **Rotate clockwise 90degs** from the menu.
  - Right-click the object to rotate; then select **Rotate counterclockwise 90degs** from the menu.

### Moving Objects in the Work Area

To move objects in the work area:

- On the vertical toolbar, select the cursor icon.
- Drag the workflow step to a new location in the work area.

### Selecting Multiple Objects in the Work Area

To select multiple objects in the work area:

- On the vertical toolbar, select the cursor icon.
- Do one of these actions:
  - Press the CTRL key and click the objects to select them.
  - Drag the cursor to create a rectangle around the objects. The rectangle must enclose all objects completely to select them.
  - On the Edit menu, click **Select All** to delete all objects in the work area.

### Resizing Objects in the Work Area

To resize objects in the work area:

- Select an item in the work area.
- Do one or more of these actions:
  - To resize an item, click and drag one of the corners or edge handles.
  - To resize an item while keeping the current proportions, click and drag a corner handle.

### Aligning Objects in the Work Area

To align objects in the work area:

- On the vertical toolbar, select the cursor icon.
- Select the first object. All other objects you select will align to this object.
- Press the CTRL key and select each additional object to align.
- On the Main toolbar, click one of the object alignment buttons:

**Align Left, Align Right, Align Top, Align Bottom, Align Vertically, Align Horizontal.**

### Making Multiple Objects the Same Size

To make multiple objects the same size:

- On the vertical toolbar, select the cursor icon.
- Select the first object. The first object controls how all the other selections will be resized.
- Press the CTRL key and select the objects to resize.
- On the main toolbar, click the **Copy Size** button.

### Changing the Text Font Size

To change the text font size:

- On the vertical toolbar, select the cursor icon.
- Select the object or rule line for which you want to change the label font size.
- On the main toolbar, a font size from the Font Size list box.

### Changing the View Setting

If you are working with a large workflow that does not fit in the Workflow window, use the View Size list box to change the view setting.

### Adding Rule Lines to the Work Area

In a workflow, the graphical representation of a rule is a line connecting two steps. To add properties to the rule, edit the properties dialog as explained in the "Building Workflow Templates" section later in this chapter.

To add a rule line to the work area:

- Right-click a step to select it.
- While holding down the right mouse button, drag the cursor onto a second step. A dotted line is displayed that indicates that Workflow is ready to create the rule.
- Release the mouse button to create the rule. A solid line is displayed.

### Adding Control Points to Rule Lines

To add control points to rule lines:

- Right-click the rule line to which you are adding a control point; then select **Add Control Point**

from the menu.

A control point appears on the rule line.

- To reshape the rule line, drag the control point to a new position in the work area.

### Converting Lines to Curves

To convert rule lines to curves:

- Right-click the rule line to which you are adding a curve; then select **Add Control Point** from the menu.

A control point appears on the rule line.

- Right-click the rule line again; then select **Curve** from the menu. The rule line appears as a curve.
- To reshape the rule curve, drag the control point to a new position in the work area.

**Note:** You can add more control points to reshape the curve.

### Deleting Control Points

To delete a control point:

- Right-click the rule line to delete; then select **Delete Control Point** from the menu.

Workflow deletes the control point from the rule line. If the rule line has more than one control point, VISUAL deletes the control you most recently added.

- To reshape the rule line, drag the control point to a new position in the work area.

### Using the Dotted Box

The dotted box is an organizational tool that lets you define and label sections of the work area. You cannot add properties to a dotted box.

To use the dotted box:

- On the vertical toolbar, select the dashed box icon.
- Click anywhere in the work area to add the selected item.
- Move and resize the box to best suit your needs.
- To add a label, right click the box.
- In the Enter new label field, specify a name for the box.
- Click **OK** to save the label or click **Cancel** to return to the work area without adding a label.

### Printing the Work Area

To print the work area, do one of these actions:

- - On the main toolbar, click the **Print** button.
    - On the File menu, click **Print Diagram**.

### Using the Print Details Menu

To use the Print Details menu:

- On the File menu, select **Print Details**.
- In the Print Workflow dialog, specify this information:

**Application Area** - Select the application for which you want to print a workflow.

**Template Name** - Select the workflow template to print.

**Ref ID** - Select the reference ID of the workflow to print. To print a range of workflows, add a beginning reference ID and an ending reference ID.

- Click the **Print Setup** button to open the Printer dialog where you select a printer and set up printer properties.
- In the list box at the bottom of the Print Workflow dialog, select one of these options:

**Print** - Prints the workflow to the selected printer.

**View** - Displays the workflow.

**File** - Save the workflow to a file.

**E-Mail** - Send the workflow using your electronic mail program.

- Click **Ok** or click **Cancel** to close the Print Workflow dialog without printing a workflow.

## Exporting Workflows

Use the Export menu to save the currently open workflow (including all step and rule properties) to a file on your computer. The exported file will have a .dia file extension.

To export a workflow:

- If necessary, open Workflow.
- On the File menu, select **Export**.
- In the Export File window, select a location to save the file, type a file name; then click the Save button.

## Importing Workflows

Use the Import menu to open a workflow file that has been previously exported. To import a workflow:

- If necessary, open Workflow.
- On the File menu, select **Import**.
- In the Warning message, do one of these actions:
  - Click **Yes** to save the open workflow document before importing a new document.
  - Click **No** to import a workflow document without saving the open document.
  - Click **Cancel** to return to the open workflow document without importing a document.
- In the Create New Workflow dialog, specify this information:

**Type** - Select **Predefined**.

**Application Area** - Select the application area of the workflow to import. The application area you select must be the same as the application area of the file you are importing.

**Template Name** - Type the name to use for this template.

**Usage** - Select **All Documents** to apply this template each time you create a workflow or **Single Document** to use this template for a specific document.

**Document ID** - If you selected **All Documents** in the Usage list, go to the next step. If you selected **Single Document** in the Usage list, type the exact document ID then click the **OK** button.

**Note:** The Associated DB Table field is "view only" and lists the database associated with the application that you selected in the Application Area field.

**Status** - Select **Active** to make the template available or **Inactive** to not make the template available at this time.

- Click **Ok**.
- In the Select an Import File window, select the file to import; then click the **Open** button. The imported workflow appears in the work area.

## Using Right-click Menus

Right-click menus allow you to open properties dialogs, delete items, and manage objects in the work area.

To use right-click menus:

- Right-click the work area, rule, or step. A menu appears.
- Select an item from the menu.

For example, to access the properties of an object in the work area, right-click the object and select **Properties**. The Properties dialog appears.

# Building Workflow Templates

A workflow template is basically a diagram that organizes a business process into actions and paths, with _Steps_ representing actions and _Rules_ representing paths. For each step and rule you define properties, including statements and conditions. The properties of the action determine the tasks to be completed. The properties of the path define the path of the flow.

After you create and save a template, you can change the status from inactive to active. When a template is active, VISUAL starts a workflow each time a document (for example, a purchase order or work order) is saved and meets the conditions specified in the template.

To build a workflow template, refer to these sections:

- - Working with Steps
    - Working with Rules
    - Working with Statements

# Working with Steps

When working with step properties, you may want to take some time to investigate and think about what you want to achieve with the step. Some of the things you may want to consider are:

- - What are you attempting to do with this step?
    - What conditions do you want met?
    - Are there any errors on which you want to halt the process? Do you want to notify the user on these errors but allow a continuation of the process?
    - Are there any documents you want attached to this step of the process?
    - Are there any people you want to notify at this step?
    - Are there any programs or commands you want to run when the user reaches this step?

Because what happens on one step or rule can effect other steps, you may also want to consider what happens on prior and subsequent rules and steps.

### Adding Properties to a Step

To add properties to a step:

- Right-click the step to which you are adding properties.
- Select **Properties**

**Note:** This read-only information appears in the dialog status section: Status, Started on, Last notified, and Completed. For more information on status fields, refer to the "Viewing Status Information" section later in this chapter.

- In the Label field, specify a name for the step. You may want to consider using a name indicative of what you are attempting to accomplish on this step.
- To re-evaluate the workflow each time a document (for example, a Purchase Order or Purchase Requisition) is saved, select a re-evaluation method from the Re-evaluation list. You can select one of these options:

**Never** - VISUAL never re-evaluates the workflow.

**and restart workflow here** - When re-evaluating, VISUAL undoes all previously completed steps in the workflow and restarts the workflow at the current step.

This function is necessary if the information in a workflow document (for example, a customer order) changes frequently.

**and continue where left off** - VISUAL re-starts the workflow from the "start" step and checks the properties of each rule and step in the workflow to make sure conditions have not changed since the last time the document was saved. If a condition has changed, the workflow proceeds down the appropriate path.

When re-evaluating a workflow, VISUAL also checks the evaluation sequence in the Rule Properties dialog.

For more information on evaluations, refer to the "Working with Evaluations" section later in this chapter.

**Note:** You must consider all possible conditions or the workflow may not complete the intended business process.

- If this is the first step in the workflow, select the **Start Workflow Here** check box.
- To evaluate this step periodically, select the **Evaluate on Timer** check box. The system evaluates the step based on the interval you set in Preference Maintenance. For example, if you specified 15 in Preferences Maintenance, the system would evaluate the step every 15 minutes.

To set up the interval to use in Preferences Maintenance:

- 1. Select **Admin**, **Preferences Maintenance**.
  - If the Preferences table does not contain a Workflow section and DetectMinutes entry, click Insert Row and specify this information:

**Section** - Workflow

**Entry** - DetectMinutes

**Value** - Specify the frequency in minutes with which the system should evaluate the work flow step. You can specify a value between 1 and 60.

- 1. Click **Save**.

Clear the check box to check to see if the step needs to be when the user saves the type of document governed by the workflow.

- In the Notifications section, specify this information:

**E-mail Address** - Click the **E-mail Address** button and select the address of the person who receives an email notification. You can also specify an email address using your keyboard.

**E-mail Subject** - VISUAL populates the subject line of the notification email with the Workflow ID (\$WrkID) and Step number (\$WrkStep).

For example, **Workflow Notification 9030/ Step 3**_._

To change the format of the subject line, click in the E-mail Subject field and specify the information.

**E-mail Body** - Workflow populates the body space of the notification email with this information:

- Workflow Type (\$WrkType)
- Workflow Name (\$WrkName)
- Workflow ID (\$WrkID)
- Workflow Step (\$WrkStep)

For example, **Workflow \$WrkType / \$WrkName / \$WrkID is at step \$WrkStep**.

**E-mail Attachments** - To attach a document to your email, click the **Attachments** button to open the Email Attachments dialog where you can select a file.

**Note:** The VE Programs Link and Documents buttons are reserved for future use.

**Execute Command** - To open a program file (for example, an Excel spreadsheet) when the notification takes place, click the **Execute Command** button and select the action from the dialog.

To open an VISUAL executable and sign in with the user's ID and password, specify this information:

**\${VE:program}**

where program is the name of the executable you would like to open. For example, if you would like to open Customer Order Entry, specify this information:

**\${VE.VMORDENT.EXE}**

- You can use workflow rules to identify errors in a process. For example, if you require an expiration date on all quotes, you can set up a workflow rule that tests for the existence of an expiration date. See ["Working with Rules" on page 9-22 in this guide](#_bookmark722).

Use the VE Program Action area to specify the actions that occur when the rule that is immediately before the step is used to identify an error, and the error conditions have been met.

**Return error** - Select this check box to indicate that an error has occurred. When an error occurs, the user cannot save the document that triggered the error.

**Stop here if error** - If you selected the **Return error** check box, select the **Stop here if error**

check box to stop the workflow. Clear the Stop here if error check box to continue the workflow.

**Return message** - If you selected the **Return error** check box, select this check box to display a message in the error dialog. Specify the text in the Message field. If you selected the **Return error** check box, we recommend that you also select the Return message check box and specify text. If you do not select the Return message check box, then &lt;null&gt; is displayed in the error dialog.

- To attach any documents to this step, click the **Document Reference** button and select the documents. For more information on Document Referencing, refer to the "Referencing a Document" section later in this chapter.
- Click **Ok**.

### Referencing Documents

Use the Workflow Document Reference dialog to add supportive documentation, such as instructions, specifications, or CAD drawings, to a workflow step.

To reference documents in a workflow, you must first add the document in Document Maintenance. See ["Document Maintenance" on page 6-1 in this guide](#_bookmark488)

- In the Step Properties dialog, click the **Document Reference** button.
- In the Workflow Document Reference dialog, click the **Insert** button.
- Specify the ID of the document to reference or double-click the Document ID column heading to open the List Documents dialog where you can select a Document ID.
- Click **Save**.

## Viewing Status Information

The Step Properties dialog The Status, Started on, Last notified, and Completed fields are read only and display information only when the step has been triggered in a workflow. You can use the information in these fields when creating workflow statements. For more information on workflow statements, refer to the "Working with Statements" section later in this chapter.

The status area displays these fields:

**Status** - A workflow step is either In Process or Complete.

**Started On** - The date when VISUAL first evaluated this step. This date does not change.

**Last notified** - The date when VISUAL last evaluated the step. This date changes each time VISUAL evaluates the step.

**Completed** - The date when VISUAL completed the step.

## Deleting Steps

To delete steps, select the step, then right-click and select **Delete** from the right-click menu.

**Note:** The workflow program does not display a confirmation message when you delete a step. You can undo a deletion.

# Working with Rules

Rules determine the path of the workflow. Using the Rule Properties dialog, you can set up approval tasks (which appear in Task Maintenance application) for a specific user or a group of users. These users must sign off electronically on the activity before the workflow proceeds to the next step.

You can also assign Authorizations to restrict the enactment of a workflow step to a specific user or group of users.

## Adding Rule Properties

To add rule properties:

- If necessary, create a rule line.
- Right-click the rule to which you want to add or edit properties.
- Select **Properties**

The Rule Properties dialog appears.

- In the Label field, specify a name for the rule.

You may want to consider using a name indicative of what you are attempting to accomplish with this rule.

- In the Description field, specify a description for the rule.

If you are editing a rule property, the Status, Completed By, and Date/Time fields appear populated.

- In the Evaluation Sequence field, specify a number to indicate the sequence that VISUAL uses to re-evaluate a workflow path. VISUAL re-evaluates workflow paths sequentially, beginning with 1.

For more information on evaluations, refer to the "Working with Evaluation Sequences" section later in this chapter.

- To use this Rule for assigning tasks, click the **Approval** button and set up the Task Notification list for this rule. For more information on setting up approvals, refer to the "Working with Tasks" section later in this chapter.

To set up rule statements, click the **Insert** button and set up the Rules statements to use.

For more information on setting up Rule Statements, refer to the "Working with Statements" section later in this chapter.

- If you have specified rule statements and want to include Authorizations, click the **Authorization**

button and set up the Authorization Users for this Rule.

For more information on authorizations, refer to the "Working with Authorizations" section later in this chapter.

- When you have finished setting up the rule, click **Ok**.

## Working with Evaluation Sequences

Using the Re-evaluation list in the Step Properties dialog, you can allow VISUAL to restart a workflow each time a document (for example, a Purchase Order or Purchase Requisition) is saved. Because a workflow step may have multiple paths, you can define the sequence in which VISUAL evaluates each path. The value in the Evaluation Sequence field determines the evaluation order, starting with Sequence 1 and ending with Sequence 3.

![](data:image/png;base64,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)

Re-evaluate Step

Sequence 1

Sequence 3

Sequence 2

Task 1

Task 3

Task 2

## Working with Tasks

The Task List Notification dialog allows you to identify a group of users responsible for approving the activity before the workflow proceeds to the next step.

For each member of the Task List Notification, Workflow generates a task notification in the Task Maintenance window, Outlook, or both. [For more information, refer to "Selecting Workflow](#_bookmark769) [Preferences" on page 9-32 in this guide.](#_bookmark769)

To set up a task list:

- In the Rule Properties dialog, click the **Tasks** button. The Task List Notification dialog appears.
- Click the **Insert** button.

The User/Group ID arrow appears.

- Click the arrow and select a user or User Group.

**Note:** When you select a group, all members of that group appear on separate lines in the Task List Notification dialog.

- To allow sign off privileges for a user, select the **Signoff** check box. You can give more than one user signoff privileges.

When you select the Signoff check box, VISUAL creates a task for each user.

- To specify that a user is the leader, select the **Leader** check box.

**Note:** You can only have one leader within your user list: if you change the leader, Workflow automatically clears the original leader's check box.

- In the Task Specification field, specify a description of the task for this step.

The description you specify in this field also appears in the Specification column in Task Maintenance.

- When you have finished adding users to this step's task list, click **Save**.

#### Deleting Users from Task Lists

To remove users from a task list:

- From the Rule dialog, click **Approvals**.
- Click the row header for the user to remove. The row appears highlighted.
- Click **Delete**.
- Click **Save**.

#### Workflow and Outlook Tasks

To receive workflow tasks from another user, you must grant that user permission to write to your Outlook Tasks folder.

To grant permission:

- In Microsoft Outlook, display **Tasks**.
- Right-click the Tasks folder and select **Properties**.
- Click the **Permissions** tab.
- Click **Add....**
- Specify the user who should have write-access to your Tasks folder.
- In the Write section, select the **Create Items** and **Edit Own** check boxes. See the Outlook online help for information about other permission levels.
- Click **Ok**.

## Working with Authorizations

The Authorization dialog allows you to restrict the enactment of a workflow step to a specific user or users only.

To set up authorization lists:

- After clicking **Authorization**, the Authorization dialog appears.
- Click the **Insert** button.

The User/Group ID arrow appears.

- Click the arrow and select a User or User Group from the list.

**Note:** If you select a group, the names of all the members of that group appear in the list.

- Click **Save**.

# Working with Statements

Statements are conditions that trigger actions. They can range from simple one-line statements to more complex multi-line statements.

To set up statements:

- Click the **Insert** button:
- In the Table column, click the arrow and select the data table to use for this statement.

The Table column lists all the data tables associated with the application for which you are creating a workflow. It also contains two "non-standard tables" named Workflow_Rule and Workflow_Step that allow you to create statements for the condition of a rule or step.

- Click in the Column column, click the arrow; then select the column you want to use for this statement.

The Column column lists all the columns associated with the table you select in the Table column. If you select Workflow_Rule the list displays Label, Status, and Time Complete. If you select Workflow_Step, the list displays Action, Label, Status, Time_Started, Time_Complete, and Time_Notified. For more information, refer to the "Using the Workflow_Step Column" and "Using the Workflow_Rule Column" sections later in this chapter.

- If your statement requires an operator, click in the Operator column, click the arrow; then select an operator value. Select one of these options:

**\+** - Add (Column value + Operator value)

**\-** - Subtract (Column value - Operator value)

**\*** - Multiply (Column value \* Operator value)

**/** - Divide (Column value / Operator value)

- If your statement requires an operator value, specify a value in the &lt;Operator&gt; value column header or double-click the column header and select a value.
- Click in the Comparison column, click the arrow; then select the comparison function to use for this statement. Select one of these options:

**<** - Less than (Column value + Operator value < Comparison value)

**\>** - Greater than (Column value + Operator value, Comparison value)

**\=** - Equal to (Column value + Operator value = Comparison value)

**\>=** - Greater than or equal to (Column value + Operator value >= Comparison value)

**<=** - Less than or equal to (Column value + Operator value <= Comparison value)

**!=** - Not equal to (Column value + Operator value != Comparison value)

**in** - In (Column value + Operator value **in** Comparison value)

**not in** - Not in (Column value + Operator value not in Comparison value)

**like** - Like (Column value + Operator value **like** Comparison value)

**not like** - Not like (Column value + Operator value not like Comparison value)

**between** - Between two values (Column value + Operator value _between_Comparison value)

**not between** - Not between (Column value + Operator value not between comparison values)

- Click in the Value column and specify the value to use for this statement. When creating statements that require status values, double-click the column header to view a browse list of acceptable values.
- To combine two statements or compare more than one value, click in the Logical column and select one of these options:

**Or** - If one or more conditions or other conditions must exist before statement can be true, select the **Or** option. For example, if you have a two line statement the first line OR the second line can be true for the whole statement to be true.

**And** - If multiple conditions must exist before the statement can be true, select the **And** option. For example, if you have a two line statement, both lines must be true before the whole statement can be true.

**And any** - If the first line and ANY of the subsequent lines must exist before the statement can be true, select the **And Any** option.

**Or any** - If the first line OR any of the subsequent lines must exist before the statement can be true, select the **Or Any** option.

**Next Line** - Use the Next Line option to consider one database column in relation to a second database column to determine the records to select. For example, to create a statement that selects parts with a unit price (part.unit_price) that is greater than or equal to 20% more than the wholesale unit cost (part.whsale_unit_cost), you would specify these two lines:

![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAvQAAAArCAIAAACYSYuSAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAEaElEQVR4nO3dzY3jNhgAUDqYEgaYFqafHNJBygi2jaSCLJCUEeSQBmZLmMVsDc7Bu4ZAkTItS6JEvncYyFr90aTob8XP5unj/S2E8Pzyej6fv339EmiR+m1bb/WrvG3rrby1tPc+P7+8XkKaEMLT88vrZel0OtW7JNb18f6mfhvWW/0qb9t6K28t7b3P18gmhHA6n88hhNPp+wJNUr9t661+lbdtvZW3lvbe52GJnlp6JAUA8FPtCwAAWJLgBgBoylPJRlHO0XCULhq0y205cQQ2Nq6yZN7VeH1hJSY3m2gYyUZy1xkBeESu/599qOoJPUXBTZgMaEq2lLZ8CCU1W1KJE20gd6LcraXBAGxg3gd0bq/qXfdDw1LXAG2pq4GL6jcG7JP+Fkpkgxu3UIeEqrBzblJWkmtapx+uL3ML0V7Dv8NtogOupHRYKpkPcR04mM6coDfJNjCvYVz30pbgYqLLDe4UFhW1tBkjUFH6ZskBF5EIbqJw7N7Uh6WSkqhikUSwmzdA+Sm0Hxgb3qfuEZaS7P/H0fMwNig5ZrRmm0ePieCmPG/U09Emee4NO+fJDSsZ9/+Lt6htmmjpsFTk5rdd4BGaE+REd4c7hW1E40qP99JbD0tduGFaFQXmuSGkjR/e5K5KChcMiftZ27A3jpZzG9+VuLJNJmXRk5vxFeTWROunX1JLSVLY9eW9lTj74CXNDDrnpmANE/38zd47ucE4JMhtv5KZw1IQUklXel4AqhPcMJ9QBoAdMnEmANAUwQ0A0BTBDQDQoo/3t9qXAAAw3/mHp6V+jYc9U79t661+lbdtvZW3lvbe5+EXeJ++ff1S8VIAAJYl5wYAaIrgBgBoStGP+E3MERoN2uW2NG/tTkwPskbzoo13yc0JMrFLtGNy95vHubm+vIHdNYPVuLz7b8nJSmxerrzbzGID7E3pLxRPf3Td3FLacgOiKOfeqlx1/tjCBpYMkkoiqgO15N4im4kZXh9sscBxPTQsdf1sWOpqWFtUX9HyXTO7RjsesRnMK+/OHbc65rl87TP3TxtfDLAT2eCmn86RQ+jqA/sR48l4AXpTOiw1TkoYPqhPpmUEPexhJcOIYXiRHK+5mdAT7btzyfJyRMakoDeJ4ObaoV8W7v2PYHl2DlVco5Dx3+E21+VcBu7GOTehRjaJjI0GqDvoUCK4KU+ZNExA2LYZGJziLiIb6FPpsFQk+V1fnchRDB+BlD8OKan0YzWDki/Gb3k9LEsNQreywY1OgchdOSjJJK1ozfQpcps98vAmefzcSeXcHJQUQKDoyc24U8itidZPv6SiXIbN9MubFZrL0HrwS9e5885oYCXHn72eWpKtVDVBt2YOS8GDxk9flv0oWvv4AOyW4IY61g41hDIA3TJxJgDQFMENANAUwQ0A0JTvSZcf72/PL691LwUA2MZff/5e+xIW8/Mvvw7nVAghPJX/HjHHpX7b1lv9Km/beitvLX9//uOff/+rfRUL+PTptzD6xbL/AZ0zNC2efFC8AAAAAElFTkSuQmCC)

You can combine more than one Next Line statement. For example, to create a statement that selects parts with a unit price greater than or equal to 20% of the wholesale unit cost but less than or equal to 50% of the wholesale unit cost, you would specify these two lines:

![](data:image/png;base64,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)

You can also create Next Line statements that search for information across two different database tables. For example, to search for all part IDs that have been used in customer order lines, you would specify these lines:

![](data:image/png;base64,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)

- When you have finished setting up your statement, click **Ok**.

## Using the Workflow Rule Table

In a typical workflow, you create statements for tables within an application (for example, PURCHASE_ORDER for the Purchase Order application). However, some workflows require that you create a statement for the condition of a specific rule or step. To allow the creation of rule and step statements, Workflow provides non-database tables, such as Workflow_Rule and Workflow_Step. These tables allow you to create statements for status and time conditions. For example, you can set a time condition for a step that must be completed by a specific date.

### Creating a Workflow Rule/Step Label Statement

A statement containing a Label value affects the current rule. The trigger for the Label value is the name of the current rule in the Label field.

For example, the label name of this rule is "PO, \$10000." To create a Workflow Rule/Statement Label statement:

- If necessary open a Rule Properties dialog.
- Click the **Insert** button.
- In the Table column, click the arrow and select either **Workflow_Rule** or **Workflow_Step**.
- Click in the Column column, click the arrow and select **Label**.
- If the statement requires a value in the &lt;Operator&gt; Value or &lt;Comparison&gt; Value column, specify the label name of the current rule.
- For more information on completing workflow statements, refer to the "Working with Statements" section.

### Creating a Workflow Rule/Step Status Statement

A statement containing a Status value affects the current rule. The trigger for Status is the value in the Status field. The Status field is read-only and displays "Complete" when VISUAL has completed the current rule or "InProcess" when VISUAL has started, but not completed, the current rule.

To create a Workflow_Rule Status statement:

- If necessary open a Rule Properties dialog.
- Click the **Insert** button.
- In the Table column, click the arrow and select either **Workflow_Rule** or **Workflow_Step**
- Click in the Column column, click the arrow; then select **Status**.
- If the statement requires a value in the &lt;Operator&gt; Value or &lt;Comparison&gt; Value column, specify one of these values:

**InProcess** - The statement requires that the status of the current rule is in process. The

**InProcess** value is case and space sensitive.

**Complete** - The statement requires that the status of the current rule is complete. The **Complete**

value is case sensitive.

- For more information on completing workflow statements, refer to the "Working with Statements" section.

### Creating a Workflow Step Time Started Statement

A statement containing a Time Started value affects the previous step. To calculate this value, Workflow looks at the Started on section in the previous Step Properties dialog.

To create a Workflow_Step Time Started statement:

- If necessary open a Step Properties dialog.
- Click the **Insert** button.
- In the Table column, click the arrow; then select **Workflow_Step**.
- Click in the Column column , click the arrow, then select **Time_Started**.
- If the statement requires a value in the &lt;Operator&gt; Value or &lt;Comparison&gt; Value column, double-click the column's header.
- In the Offset section, select the time for your operator value; then click the **OK** button.
- For more information on completing workflow statements, refer to the "Working with Statements" section.

### Creating a Workflow Step Time Last Notified Statement

A statement containing a Time Notified value affects the previous step. To calculate this value, VISUAL looks at the Last Notified field in the Step Properties dialog of the previous step.

To create a Workflow_Step Time Notified statement:

- If necessary open a Step Properties dialog.
- Click the **Insert** button.
- In the Table column, click the arrow and select **Workflow_Step**.
- Click in the Column column, click the arrow; then select **Time_Notified**.
- If the statement requires a value in the &lt;Operator&gt; Value or &lt;Comparison&gt; Value column, double-click the column's header.
- In the Offset section, select the time for your operator value; then click **OK**.
- For more information on completing workflow statements, refer to the "Working with Statements" section.

### Creating a Workflow Rule Time Complete Statement

A statement containing a Time Complete value affects the current rule. The trigger for the Time Complete value is the date in the Date/Time field of the current rule.

To create a Workflow_Rule Time Complete statement:

- If necessary open a Rule Properties dialog.
- Click the **Insert** button.
- In the Table column, click the arrow and select either **Workflow_Rule** or **Workflow_Step**.
- Click in the Column column, click the arrow; then select **Time_Complete**.
- If the statement requires a value in the &lt;Operator&gt; Value or &lt;Comparison&gt; Value column, double-click the column's header.

The calendar dialog appears.

- In the Offset section, select the time for your operator value; then click the **OK** button.
- For more information on completing workflow statements, refer to the "Working with Statements" section.

## Viewing Workflow Status Codes

The Workflow Status Codes window displays a list of database tables, status column names, and status values. This window also displays the actual text that appears in status fields. For example, the Status field in Purchase Order Entry displays values such as **Firmed**, **Released**, and **Hold**. The corresponding status values in the Purchase_Order database table are **F**, **R**, and **H**.

Use the Workflow Status Codes window as a reference when creating Workflow statements that require condition comparisons on status columns.

### Opening the Workflow Status Codes Window

To open the Workflow Status Codes window:

- On the Admin menu, select **Application Global Maintenance**.
- On the Maintain menu, select **Workflow Codes**. The Workflow Status Codes window appears.

This information is shown in the Workflow Status Codes Window:

**Language** - Click the arrow to view a list all supported languages in the system.

**Status Text** - The actual text that appears in the status field of a particular application. For example, when the value "C" appears in the STATUS column of the PURCHASE_ORDER table, the status field in Purchase Order Entry displays "Closed." You can edit status text for any language except USA.

**Status Value** - The actual value that appears in the status column of the database table. You cannot edit this field.

**Table Name** - The name of the database table.

**Column Name** - The name of the status column in the corresponding database table.

### Changing Status Text Language

You can edit definitions in the Status Text column to support other languages. You cannot edit definitions for the USA language code.

To change the status text language:

- In the Status Text column, change the text from English to another supported language.
- Click the **Copy to Language ID** button.
- In the Copy to Language ID list, select the ID of the language to copy the status definition.
- Click **Ok**.

# Selecting Workflow Preferences

To access the Workflow Preferences dialog, select **Preferences** from the Options menu. You can select these preferences for Workflow:

**Rotation Angle** - Specify the angle to which you want to rotate the Workflow window.

**Delete on right-click menu** - To display the delete command on the right-click menu, select the

**Delete** on Right-click menu check box.

**Logging Level** - Specify the number of "undo" levels to track. The greater the quantity of undo levels, the greater the memory usage.

**Make Tasks in** - Click the arrow and select where to make your Workflow tasks. You can select:

**VISUAL Task List** - To create tasks only in VISUAL's task list, select the **VISUAL Task List**

option.

**Outlook** - If you use Microsoft® Office Outlook® and want to create tasks only for Outlook, select the **Outlook** option.

**Both** - To create tasks in VISUAL's task list and in Outlook, select the **Both** option.

**ECN and Purchase Requisitions Tasks** - Click the arrow and select where to create your Engineering Change Notice and Purchase Requisition tasks. You can select:

- Task Table Only
- Outlook and Task Table

In the E-mail Settings (SMTP) section, set this outgoing email server information:

**Auto Discover** - To detect your settings, select the **Auto Discover** check box. If you select this setting, you do not need to make any other settings.

**Server Name** - Specify the name of your outgoing email server.

**Email Address** - Specify the email address to use as a From email address.

**MS Exchange User Name** - If you are using MS Exchange, specify the User Name.

**Display Name** - Specify a From name.

# Logging Workflow Timer Events

If you would like to keep a record of all workflow timer events, you can enable the WORKFLOW.LOG file.

- Select **Admin**, **Preferences Maintenance**.
- Find the LogWorkflowEvents entry in the Workflow section. If it does not exist, click the Insert button and add the entry to the preferences table.
- In the Value field, specify Y.

The system records workflow timer events in the WORKFLOW.LOG file. The system stores the file in the same directory as your VISUAL executables.

# Using the Workflow Tracker

Workflow Tracker is primarily a view only application that allows you to monitor the status of workflow documents. Each step section in the work area displays a color-coded border to indicate where the workflow started, which steps have been completed, and the current step.

With the appropriate permissions, you can manually complete a step in the process or print workflow reports.

## Opening Workflow Tracker

You can start the Workflow Tracker from the Admin menu of the main menu or click the Workflow Tracker button in any document that has an associated workflow.

**Note:** A highlighted Workflow Tracker button indicates that the document has an associated workflow. A shaded button indicates there is no associated workflow.

To start the Workflow Tracker from an application (for example, Purchase Requisition Entry or Purchase Order Entry) that has a workflow document in process, do one of these actions:

- Click the **Workflow Tracker** button on the main toolbar of the application.
- On the Info menu, click **Workflow**.

To start the Workflow Tracker from the main window:

- On the Admin menu of the main window, click **Workflow Tracker**.
- Click the **Application Area** arrow and select the application area where you created the template.
- Click the **Document ID** button and select the workflow to open.

VISUAL creates a unique workflow document is for every enactment of the workflow.

- Click **Ok**.

The workflow you selected appears in the Workflow Tracker window.

## Understanding Color Codes in Workflow Tracker

The color of each step in the workflow shows the status of the step:

**Blue** - A blue dash outline indicates the starting workflow step. **Black** - A black dash outline indicates a completed workflow step. **Yellow** - A yellow dash outline indicates the current workflow step.

## Viewing Rule Status Information

To view the status of a workflow rule:

- Right-click the rule for which you want to view status information. The Rule Properties dialog appears.
- View status information in these fields:

**Status** - Indicates whether the task or authorization is complete or in process.

**Completed by** - Indicates who completed the task or authorization.

**Date/Time** - Indicates when the task or authorization was completed.

## Viewing Step Status Information

To view the status of a workflow step:

- Right-click the step for which you want to view status information. The Step Properties dialog appears.
- View status information in these fields:

**Status** - Indicates whether the workflow step is complete or in process.

**Started On** - The date when VISUAL first evaluated this step. This date does not change.

**Last notified** - The date when VISUAL last evaluated the step. This date changes each time VISUAL evaluates the step.

**Completed** - The date when VISUAL completed the step.

## Marking a Step Done

With appropriate permissions, you can manually override an authorization. For example, when a rule requires the authorization of a user who is on vacation, you can mark the step done so that the workflow can continue to the next step.

To mark a step done:

- Right-click the rule to authorize.
- In the menu, select **Mark Done**.

The workflow continues to the next step.

## Marking a Step Undone

With appropriate permissions, you can revert the state of an authorization to an undone condition. To mark a step undone:

- Right-click the rule to authorize.
- In the menu, select **Mark Undone**.

The workflow reverts to an undone condition.

## Printing a Workflow Tracker Diagram

To print a Workflow Tracker diagram, do one of these actions:

- - On the Main toolbar, click **Print**.
    - On the File menu, select **Print**.

# Using the Workflow Gatekeeper

While the Workflow Tracker shows you what is occurring within the confines of a specific workflow, the Gatekeeper allows you to view all of the related processes as they relate to the current Workflow Template. For example, if you are viewing a workflow that incorporates Purchase Orders, you can use the Gatekeeper to view all of the rules within the workflow and all of your processes at that rule awaiting action-too many purchase orders at one rule may indicate a bottleneck.

This allows you to view what steps have and have not been completed, thus indicating where possible bottlenecks may be occurring. This allows management to view the overall effectiveness of the workflow as it relates to VISUAL as a whole and helps in the design of more efficient workflows.

To start and use the Gatekeeper:

- Open the Workflow Tracker or Workflow Designer and select the Template Name and Document ID in the Open Existing Workflow dialog.

The Workflow Tracker opens populated with the document you selected.

- Click the **Gatekeeper** toolbar button.

The Gatekeeper window opens populated with the rules at which VISUAL encounters processes awaiting action.

A graphical representation of the number of records awaiting at each rule appears in the Gatekeeper.

**Note:** If you have a wide range of records, you can use the Bar Scale to change the graphical representation.

- To view the records at a specific rule, double-click the rule in which you are interested. The Bottleneck List window appears populated with the records at the rule you selected.
- To view the document on a line in the table, double-click the line in which you are interested. VISUAL populates the Workflow Tracker window with the document you selected.
- To close the Bottleneck List window, click **OK**.
- To close the Gatekeeper window, click **OK**.

