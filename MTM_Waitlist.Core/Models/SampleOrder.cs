using System.Text.Json.Serialization;

namespace MTM_Waitlist.Core.Models;

// Model for the SampleDataService.
public class SampleOrder
{
    public long OrderID
    {
        get; set;
    }

    public DateTime OrderDate
    {
        get; set;
    }

    public DateTime RequiredDate
    {
        get; set;
    }

    public DateTime ShippedDate
    {
        get; set;
    }

    // Edge Case 1: Use string.Empty as defaults to prevent null binding crashes in XAML UI textboxes
    public string ShipperName { get; set; } = string.Empty;

    public string ShipperPhone { get; set; } = string.Empty;

    public double Freight
    {
        get; set;
    }

    public string Company { get; set; } = string.Empty;

    public string ShipTo { get; set; } = string.Empty;

    public double OrderTotal
    {
        get; set;
    }

    public string Status { get; set; } = string.Empty;

    public int SymbolCode
    {
        get; set;
    }

    public string SymbolName { get; set; } = string.Empty;

    // Kept to satisfy detail/secondary page template compiled bindings
    public char Symbol => (char)SymbolCode;

    // Dynamic Image Route pointing directly to the new ChatGPT forklift/industrial png assets
    public string ImageIconPath { get; set; } = string.Empty;

    // Edge Case 2: Always initialize collections to an empty list to prevent loops (like foreach) from throwing exceptions
    public ICollection<SampleOrderDetail> Details { get; set; } = new List<SampleOrderDetail>();

    public string ShortDescription => $"Order ID: {OrderID}";

    public override string ToString() => $"{Company} {Status}";
}
