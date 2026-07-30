using System.Collections.ObjectModel;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class SampleDataService : ISampleDataService
{
    public IReadOnlyList<object> GetSampleOrders(string? building = null)
    {
        var normalizedBuilding = (building ?? string.Empty).Trim();

        if (string.Equals(normalizedBuilding, "Vits Drive", StringComparison.OrdinalIgnoreCase))
        {
            return new object[]
            {
                CreateItem(101, "Load request", "Vits Drive", "Pending", "pickup_fg.png", "Request type", "Material", "Requested by", "Ops team"),
                CreateItem(102, "Scrap return", "Vits Drive", "Queued", "scrap.png", "Return code", "SR-204", "Submitted", "Today")
            };
        }

        return new object[]
        {
            CreateItem(1, "Material request", "Expo Drive", "Ready", "coil.png", "Request type", "Coil", "Priority", "High"),
            CreateItem(2, "Pickup request", "Expo Drive", "Processing", "pickup_ncm.png", "Pickup lane", "North", "Requested at", "08:30")
        };
    }

    private static SampleOrder CreateItem(int id, string title, string subtitle, string status, string imagePath, string fieldLabel, string fieldValue, string secondaryLabel, string secondaryValue)
    {
        return new SampleOrder
        {
            Id = id,
            Title = title,
            Subtitle = subtitle,
            Status = status,
            ImagePath = imagePath,
            Fields =
            {
                new WaitlistField { Label = fieldLabel, Value = fieldValue },
                new WaitlistField { Label = secondaryLabel, Value = secondaryValue }
            }
        };
    }
}
