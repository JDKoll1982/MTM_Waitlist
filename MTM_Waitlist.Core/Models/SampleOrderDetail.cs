namespace MTM_Waitlist.Core.Models;

// Model for the SampleDataService. Replace with your own model.
public class SampleOrderDetail
{
    public long ProductID
    {
        get; set;
    }

    // Edge Case 1: Initialize to string.Empty to satisfy .NET 10 and prevent null references in UI bindings
    public string ProductName { get; set; } = string.Empty;

    public int Quantity
    {
        get; set;
    }

    public double Discount
    {
        get; set;
    }

    public string QuantityPerUnit { get; set; } = string.Empty;

    public double UnitPrice
    {
        get; set;
    }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryDescription { get; set; } = string.Empty;

    public double Total
    {
        get; set;
    }

    // Edge Case 2: Calculated string property will gracefully handle blank values without string errors
    public string ShortDescription => $"Product ID: {ProductID} - {ProductName}";
}
