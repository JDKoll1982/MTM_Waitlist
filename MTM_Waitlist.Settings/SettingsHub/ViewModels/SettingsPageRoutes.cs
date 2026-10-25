namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The page-service route keys for the settings audience pages.
/// </summary>
/// <remarks>
/// A route is identified by the full name of a view-model type, so each page needs a type of its own even though
/// every settings page binds the same <see cref="SettingsViewModel"/>. These types carry nothing: they exist so a
/// page can be routed to without giving each page a second view model that would have to restate the sixteen
/// services the settings view model already takes.
/// </remarks>
public sealed class GeneralSettingsRoute
{
}

/// <inheritdoc cref="GeneralSettingsRoute"/>
public sealed class OperationsSettingsRoute
{
}

/// <inheritdoc cref="GeneralSettingsRoute"/>
public sealed class PicturesSettingsRoute
{
}

/// <inheritdoc cref="GeneralSettingsRoute"/>
public sealed class DevicesSettingsRoute
{
}

/// <inheritdoc cref="GeneralSettingsRoute"/>
public sealed class DiagnosticsSettingsRoute
{
}

/// <inheritdoc cref="GeneralSettingsRoute"/>
public sealed class AdministrationSettingsRoute
{
}
