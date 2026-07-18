using MTM_Waitlist.Core.Contracts.Services;
using MTM_Waitlist.Core.Models;

namespace MTM_Waitlist.Core.Services;

public class SampleDataService : ISampleDataService
{
    private readonly Dictionary<string, List<SampleOrder>> _ordersByBuilding = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Expo Drive"] =
        [
            new() { OrderID = 10001, Company = "Spot Weld", SymbolName = "PickUp (NCM - Non-Conforming Material)", ImageIconPath = "ms-appx:///Assets/pickup_ncm.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-11) },
            new() { OrderID = 10002, Company = "100-30", SymbolName = "PickUp (Outside Service - O/S)", ImageIconPath = "ms-appx:///Assets/pickup_os.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-7) },
            new() { OrderID = 10003, Company = "Robot 3", SymbolName = "PickUp (WIP - Work In Progress)", ImageIconPath = "ms-appx:///Assets/pickup_wip.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-3) },
            new() { OrderID = 10004, Company = "Pem Cell", SymbolName = "PickUp (FG - Finished Goods)", ImageIconPath = "ms-appx:///Assets/pickup_fg.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-5) },
            new() { OrderID = 10005, Company = "100-17", SymbolName = "Scrap: Coil Tail", ImageIconPath = "ms-appx:///Assets/scrap.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-14) },
            new() { OrderID = 10006, Company = "100-1807", SymbolName = "Coil - MMC0000254", ImageIconPath = "ms-appx:///Assets/coil.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-22) },
            new() { OrderID = 10007, Company = "100-05", SymbolName = "PickUp - WIP", ImageIconPath = "ms-appx:///Assets/pickup_wip.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-18) },
            new() { OrderID = 10008, Company = "100-12", SymbolName = "Scrap: 3003 Aluminum", ImageIconPath = "ms-appx:///Assets/scrap.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-9) }
        ],
        ["Vits Drive"] =
        [
            new() { OrderID = 20001, Company = "Laser 2", SymbolName = "PickUp (Finished Goods)", ImageIconPath = "ms-appx:///Assets/pickup_fg.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-4) },
            new() { OrderID = 20002, Company = "Vits Dock", SymbolName = "Scrap: Mixed Steel", ImageIconPath = "ms-appx:///Assets/scrap.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-15) },
            new() { OrderID = 20003, Company = "Press 12", SymbolName = "Coil - MMC0001294", ImageIconPath = "ms-appx:///Assets/coil.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-28) },
            new() { OrderID = 20004, Company = "Robot 9", SymbolName = "PickUp (Outside Service - O/S)", ImageIconPath = "ms-appx:///Assets/pickup_os.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-8) },
            new() { OrderID = 20005, Company = "Assembly 4", SymbolName = "PickUp (WIP - Work In Progress)", ImageIconPath = "ms-appx:///Assets/pickup_wip.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-12) },
            new() { OrderID = 20006, Company = "Inspection", SymbolName = "PickUp (NCM - Non-Conforming Material)", ImageIconPath = "ms-appx:///Assets/pickup_ncm.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-19) },
            new() { OrderID = 20007, Company = "Cell 28", SymbolName = "PickUp (Finished Goods)", ImageIconPath = "ms-appx:///Assets/pickup_fg.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-2) },
            new() { OrderID = 20008, Company = "Shipping", SymbolName = "Scrap: Aluminum Trim", ImageIconPath = "ms-appx:///Assets/scrap.png", Status = "Waiting", OrderDate = DateTime.Now.AddMinutes(-34) }
        ]
    };

    public async Task<IEnumerable<SampleOrder>> GetContentGridDataAsync(string building)
    {
        if (string.IsNullOrWhiteSpace(building))
        {
            throw new ArgumentException("Building cannot be null or whitespace.", nameof(building));
        }

        await Task.CompletedTask;

        return _ordersByBuilding.TryGetValue(building, out var orders)
            ? orders
            : Enumerable.Empty<SampleOrder>();
    }
}
