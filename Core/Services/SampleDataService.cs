using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.Core.Services;

public class SampleDataService : ISampleDataService
{
    private static readonly IReadOnlyList<SampleOrder> AllOrders =
    [
        new SampleOrder
        {
            OrderID = 1001,
            Building = "Expo Drive",
            Company = "Anderson Pipe & Steel",
            Symbol = "\uE8A7",
            SymbolName = "Pickup - FG",
            Status = "Ready",
            OrderDate = "2026-01-08",
            ShipTo = "Dock A",
            OrderTotal = "$3,240.00",
            ImageIconPath = "ms-appx:///Assets/pickup_fg.png"
        },
        new SampleOrder
        {
            OrderID = 1002,
            Building = "Expo Drive",
            Company = "Clarkson Industrial",
            Symbol = "\uE8A7",
            SymbolName = "Pickup - WIP",
            Status = "Queued",
            OrderDate = "2026-01-09",
            ShipTo = "Dock C",
            OrderTotal = "$1,185.50",
            ImageIconPath = "ms-appx:///Assets/pickup_wip.png"
        },
        new SampleOrder
        {
            OrderID = 2001,
            Building = "Vits Drive",
            Company = "North Ridge Fabrication",
            Symbol = "\uE8A7",
            SymbolName = "Pickup - NCM",
            Status = "On Hold",
            OrderDate = "2026-01-10",
            ShipTo = "Dock B",
            OrderTotal = "$890.75",
            ImageIconPath = "ms-appx:///Assets/pickup_ncm.png"
        },
        new SampleOrder
        {
            OrderID = 2002,
            Building = "Vits Drive",
            Company = "Summit Components",
            Symbol = "\uE8A7",
            SymbolName = "Pickup - OS",
            Status = "Ready",
            OrderDate = "2026-01-11",
            ShipTo = "Dock D",
            OrderTotal = "$2,416.90",
            ImageIconPath = "ms-appx:///Assets/pickup_os.png"
        }
    ];

    public Task<IEnumerable<SampleOrder>> GetContentGridDataAsync(string building)
    {
        var results = AllOrders
            .Where(order => string.Equals(order.Building, building, StringComparison.Ordinal))
            .OrderBy(order => order.OrderID)
            .AsEnumerable();

        return Task.FromResult(results);
    }
}
