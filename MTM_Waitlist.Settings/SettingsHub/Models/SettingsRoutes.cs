namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The six pages the settings hub offers, routed the way every page in this application is routed: by the view
/// model's full name.
/// </summary>
/// <remarks>
/// These names live here rather than on <c>SettingsViewModel</c> because the hub's search catalogue has to name
/// the page that owns a section, and a catalogue that read them off the view model made the hub and its own
/// catalogue depend on each other. A route is a fact about the hub's layout, not about the view model that draws
/// it, so the catalogue and the view model both read it from here.
/// </remarks>
internal static class SettingsRoutes
{
    /// <summary>The General page: the settings every person may change for themselves.</summary>
    internal const string GeneralSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.GeneralSettingsRoute";

    /// <summary>The Operations page: the settings a lead changes for the floor.</summary>
    internal const string OperationsSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.OperationsSettingsRoute";

    /// <summary>The Pictures page: picture assignments, picture folders and the local copies.</summary>
    internal const string PicturesSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.PicturesSettingsRoute";

    /// <summary>The Devices page: the computer registry.</summary>
    internal const string DevicesSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.DevicesSettingsRoute";

    /// <summary>The Diagnostics page: the developer log, its retention, and the cache refresh.</summary>
    internal const string DiagnosticsSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.DiagnosticsSettingsRoute";

    /// <summary>The Administration page: people, permissions and the lugger arrangement.</summary>
    internal const string AdministrationSettingsRouteViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.AdministrationSettingsRoute";
}
