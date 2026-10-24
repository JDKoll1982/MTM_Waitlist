using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Every setting the hub's search can reach, named the way a person would ask for it.
/// </summary>
/// <remarks>
/// The catalogue lives beside the hub rather than on each page because the search is a hub feature: one place that
/// knows which page owns which section keeps the answer in step with the routes, and a setting that moves between
/// pages only has to be corrected here.
/// </remarks>
internal static class SettingsSearchCatalog
{
    /// <summary>Every entry, in the order the pages are offered on the hub.</summary>
    internal static IReadOnlyList<SettingsSearchTarget> All { get; } =
    [
        new("App theme", "General", SettingsViewModel.GeneralSettingsRouteViewModelName, "AppearanceExpander",
            "appearance theme light dark default system colour color look style"),
        new("New request alerts", "General", SettingsViewModel.GeneralSettingsRouteViewModelName, "NewRequestAlertsExpander",
            "notification notify announce alert toast new request arrived"),
        new("About and version", "General", SettingsViewModel.GeneralSettingsRouteViewModelName, "AboutExpander",
            "about version build release privacy info installed"),

        new("Local work centers", "Operations", SettingsViewModel.OperationsSettingsRouteViewModelName, "HotWorkCentersExpander",
            "work centre center workstation computer local press list"),
        new("Dunnage type visibility", "Operations", SettingsViewModel.OperationsSettingsRouteViewModelName, "DunnageTypeVisibilityExpander",
            "dunnage type show hide visibility receiving lists"),
        new("Ignored Infor Visual locations", "Operations", SettingsViewModel.OperationsSettingsRouteViewModelName, "IgnoredLocationsExpander",
            "ignored locations hide inventory location visual omit"),
        new("How long a sign-in lasts", "Operations", SettingsViewModel.OperationsSettingsRouteViewModelName, "SessionLengthExpander",
            "session sign in signin timeout length idle minutes log out logout"),
        new("Max allotted time", "Operations", SettingsViewModel.OperationsSettingsRouteViewModelName, "UrgencyAllotmentsExpander",
            "allotted minutes urgency deadline per item time limit allowed"),

        new("Picture folders", "Pictures", SettingsViewModel.PicturesSettingsRouteViewModelName, "SettingsPage_Images_FoldersCard",
            "picture folder folders storage path share dunnage root hopper archive keep days"),
        new("Item and work center pictures", "Pictures", SettingsViewModel.PicturesSettingsRouteViewModelName, "SettingsPage_Images_AssignmentsCard",
            "item picture pictures work centre center image manage assignments open part"),
        new("Copies on this computer", "Pictures", SettingsViewModel.PicturesSettingsRouteViewModelName, "SettingsPage_Images_CacheCard",
            "cache copy copies local picture folder offline sync computer"),

        new("Computers", "Devices", SettingsViewModel.DevicesSettingsRouteViewModelName, "ComputersExpander",
            "computer computers registry device workstation machine name add edit display"),

        new("Developer log", "Diagnostics", SettingsViewModel.DiagnosticsSettingsRouteViewModelName, "DeveloperLogExpander",
            "log logs developer diagnostics panel entries errors"),
        new("Log retention", "Diagnostics", SettingsViewModel.DiagnosticsSettingsRouteViewModelName, "LogStoreLevelExpander",
            "log level retention store keep minimum verbose amount"),
        new("Cache refresh", "Diagnostics", SettingsViewModel.DiagnosticsSettingsRouteViewModelName, "CacheRefreshExpander",
            "cache refresh mirror rebuild pull infor visual data"),

        new("Users", "Administration", SettingsViewModel.AdministrationSettingsRouteViewModelName, "SettingsPage_UserManagementEntry",
            "user users people person account accounts roster employee add create"),
        new("Permissions", "Administration", SettingsViewModel.AdministrationSettingsRouteViewModelName, "SettingsPage_PermissionsEntry",
            "permission permissions access allowed role roles who can grant"),
        new("Lugger arrangement", "Administration", SettingsViewModel.AdministrationSettingsRouteViewModelName, "SettingsPage_LuggerArrangementEntry",
            "lugger arrangement scrap types assignment assign"),
    ];
}
