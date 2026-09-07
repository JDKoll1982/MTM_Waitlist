using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One editable row in the "max allotted time per request sub-type" settings editor: the sub-type name plus its
/// max-allotted minutes (backed by a NumberBox). Persisted through <see cref="MTM_Waitlist.Module_Core.Contracts.Services.IUrgencySettingsService"/>.
/// </summary>
public partial class UrgencyAllotmentItem : ObservableObject
{
    public UrgencyAllotmentItem(string subtypeName, double minutes)
    {
        SubtypeName = subtypeName;
        Minutes = minutes;
    }

    public string SubtypeName
    {
        get;
    }

    [ObservableProperty]
    public partial double Minutes
    {
        get; set;
    }
}
