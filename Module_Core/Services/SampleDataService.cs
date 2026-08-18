using System.Collections.ObjectModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class SampleDataService : ISampleDataService
{
    private const string RecvMockDataSettingKey = "Feature.RecvMockData";
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";
    private readonly ILocalSettingsService? _localSettingsService;

    public SampleDataService()
        : this(null)
    {
    }

    public SampleDataService(ILocalSettingsService? localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public IReadOnlyList<object> GetSampleOrders(string? building = null)
    {
        if (!IsMockDataEnabled())
        {
            return Array.Empty<object>();
        }

        var normalizedBuilding = (building ?? string.Empty).Trim();

        if (string.Equals(normalizedBuilding, "Vits Drive", StringComparison.OrdinalIgnoreCase))
        {
            return new object[]
            {
                CreateItem(101, "Finished Goods Pickup", "pickup_fg.png", "Maria Torres", "Press 07", "00:18"),
                CreateItem(102, "VITS Coil Request", "coil.png", "Devon Price", "Press 09", "00:42"),
                CreateItem(103, "VITS Scrap Return", "scrap.png", "Alicia Kent", "Press 03", "00:11"),
                CreateItem(104, "VITS WIP Pickup", "pickup_wip.png", "Noah Rivera", "Press 11", "00:22"),
                CreateItem(105, "VITS NCM Pickup", "pickup_ncm.png", "Elena Brooks", "Press 04", "00:35"),
                CreateItem(106, "VITS Outside Service Pickup", "pickup_os.png", "Harper Wells", "Press 08", "00:09")
            };
        }

        return new object[]
        {
            CreateItem(1, "Coil Request", "coil.png", "Jordan Lee", "Press 12", "00:27"),
            CreateItem(2, "NCM Pickup", "pickup_ncm.png", "Riley Shaw", "Press 05", "00:33"),
            CreateItem(3, "Outside Service Pickup", "pickup_os.png", "Cameron Diaz", "Press 01", "01:05"),
            CreateItem(4, "WIP Pickup", "pickup_wip.png", "Sage Chen", "Press 06", "00:41"),
            CreateItem(5, "Finished Goods Pickup", "pickup_fg.png", "Mason Patel", "Press 10", "00:28"),
            CreateItem(6, "Scrap Return", "scrap.png", "Liam Ortiz", "Press 02", "00:16")
        };
    }

    private bool IsMockDataEnabled()
    {
        if (_localSettingsService is null)
        {
            return false;
        }

        var recvValue = _localSettingsService.ReadSettingAsync<bool?>(RecvMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        var inforVisualValue = _localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey).GetAwaiter().GetResult() ?? false;
        return recvValue || inforVisualValue;
    }

    private static SampleOrder CreateItem(int id, string title, string imagePath, string requestedByName, string requestedPressName, string remainingTimeText)
    {
        var item = new SampleOrder
        {
            Id = id,
            Title = title,
            Subtitle = string.Empty,
            Status = string.Empty,
            RequestedByName = requestedByName,
            RequestedPressName = BuildMockPressName(title, id),
            RemainingTimeText = remainingTimeText,
            ImagePath = imagePath,
        };

        AddCardFields(item, imagePath);
        return item;
    }

    private static string BuildMockPressName(string title, int id)
    {
        var suffixNumber = Math.Abs(id % 100);
        return $"{title} 100-{suffixNumber:D2}";
    }

    private static void AddCardFields(SampleOrder item, string imagePath)
    {
        switch (imagePath)
        {
            case "coil.png":
                item.Fields.Add(new WaitlistField { Label = "Requested coil", Value = "COIL-204" });
                item.Fields.Add(new WaitlistField { Label = "Quantity in house", Value = "18 coils" });
                item.Fields.Add(new WaitlistField { Label = "Coil description", Value = "0.060 x 48 in galvanized coil" });
                item.Fields.Add(new WaitlistField { Label = "Average coil weight", Value = "1,240 lb" });
                item.Fields.Add(new WaitlistField { Label = "Requesting work center", Value = item.RequestedPressName });
                break;
            case "pickup_fg.png":
                item.Fields.Add(new WaitlistField { Label = "Part number", Value = "FG-10042" });
                item.Fields.Add(new WaitlistField { Label = "Part description", Value = "Finished bracket assembly" });
                item.Fields.Add(new WaitlistField { Label = "Quantity remaining", Value = "24 each" });
                item.Fields.Add(new WaitlistField { Label = "Customer", Value = "Northstar Manufacturing" });
                item.Fields.Add(new WaitlistField { Label = "Packlist", Value = "PL-80421" });
                break;
            case "pickup_ncm.png":
                item.Fields.Add(new WaitlistField { Label = "Part", Value = "RM-50218 / Customer RM-77" });
                item.Fields.Add(new WaitlistField { Label = "Quantity to move", Value = "2 containers" });
                item.Fields.Add(new WaitlistField { Label = "Pickup location", Value = item.RequestedPressName });
                item.Fields.Add(new WaitlistField { Label = "Destination", Value = "NCM Area" });
                item.Fields.Add(new WaitlistField { Label = "Traceability ID", Value = "NCM-260803-014" });
                break;
            case "pickup_os.png":
                item.Fields.Add(new WaitlistField { Label = "Part or work order", Value = "WO-073112 / RM-48190" });
                item.Fields.Add(new WaitlistField { Label = "Quantity to move", Value = "6 pieces" });
                item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = item.RequestedPressName });
                item.Fields.Add(new WaitlistField { Label = "Outside-service destination", Value = "Heat Treat Section" });
                item.Fields.Add(new WaitlistField { Label = "Vendor or service", Value = "Midwest Heat Treat" });
                break;
            case "pickup_wip.png":
                item.Fields.Add(new WaitlistField { Label = "Work order", Value = "WO-072368" });
                item.Fields.Add(new WaitlistField { Label = "Part and quantity", Value = "WIP-218 / 12 pieces" });
                item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = item.RequestedPressName });
                item.Fields.Add(new WaitlistField { Label = "WIP destination", Value = "WIP Area / Rack B-14" });
                item.Fields.Add(new WaitlistField { Label = "Operation sequence", Value = "30" });
                break;
            case "scrap.png":
                item.Fields.Add(new WaitlistField { Label = "Part number", Value = "RM-3003-18" });
                item.Fields.Add(new WaitlistField { Label = "Pickup work center", Value = item.RequestedPressName });
                item.Fields.Add(new WaitlistField { Label = "Quantity involved", Value = "1 lugger" });
                item.Fields.Add(new WaitlistField { Label = "Scrap lugger", Value = "3003 Aluminum" });
                item.Fields.Add(new WaitlistField { Label = "Scrap reason", Value = "Setup defect" });
                break;
        }
    }
}
