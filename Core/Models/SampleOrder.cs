namespace MTM_Waitlist.Core.Models;

public class SampleOrder
{
    public long OrderID { get; set; }

    public string Building { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string SymbolName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string OrderDate { get; set; } = string.Empty;

    public string ShipTo { get; set; } = string.Empty;

    public string OrderTotal { get; set; } = string.Empty;

    public string ImageIconPath { get; set; } = string.Empty;
}
