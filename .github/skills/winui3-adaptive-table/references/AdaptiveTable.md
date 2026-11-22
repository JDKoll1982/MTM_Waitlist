# Adaptive data table — wiring

Reference for wrapping the CommunityToolkit `DataGrid` into the shared adaptive table. The
toolkit grid has no built-in column show/hide, so column visibility is driven by `ColumnState`.

## Requirements

1. Horizontal scroll only when needed; full width when it fits.
2. Column picker (checkable menu) to show/hide columns.
3. One shared control used by every table page.

## XAML shape

```xml
<!-- Host: stretches to fill the column; scrolls sideways only when the grid is wider. -->
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              HorizontalScrollMode="Auto"
              VerticalScrollBarVisibility="Auto">
    <controls:DataGrid
        x:Name="Grid"
        AutoGenerateColumns="False"
        GridLinesVisibility="Horizontal"
        IsReadOnly="True" />
</ScrollViewer>
```

A column's visibility follows the state:

```xml
<controls:DataGridTextColumn
    Binding="{Binding PartId}"
    Header="Part"
    Visibility="{x:Bind ColumnState.IsVisible('PartId'), Mode=OneWay}" />
```

(If `x:Bind` on a method call is not available in your XAML, expose a `Visibility` property per
column on the view model instead.)

## Column picker

A `MenuFlyout` of checkable items, one per column, toggling `ColumnState`:

```csharp
private void BuildColumnMenu()
{
    foreach (var column in ColumnState.AllColumns)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = column,
            IsChecked = ColumnState.IsVisible(column)
        };
        item.Click += (_, _) =>
        {
            ColumnState.Toggle(column);
            ApplyColumnVisibility();
        };
        ColumnMenu.Items.Add(item);
    }
}

private void ApplyColumnVisibility()
{
    foreach (var column in Grid.Columns)
    {
        var key = (column.Header as string) ?? column.Tag as string;
        if (!string.IsNullOrEmpty(key))
        {
            column.Visibility = ColumnState.IsVisible(key)
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }
}
```

Persist the hidden set per view — a JSON settings file under `%LOCALAPPDATA%` is enough, with a
schema version so an older file can be migrated rather than crashing.

## Gotchas

- The toolkit `DataGrid` is in namespace `CommunityToolkit.WinUI.UI.Controls`.
- `DataGridTextColumn.Binding` is `{Binding}`, not `{x:Bind}`.
- Collapsing the last visible column leaves an empty grid — prevent hiding the final column.
- Put the host `ScrollViewer` inside the capped content column (MaxWidth 1000) so the table
  never stretches the page.
