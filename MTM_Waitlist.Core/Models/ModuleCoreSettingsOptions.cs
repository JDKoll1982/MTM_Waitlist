namespace MTM_Waitlist.Module_Core.Models;

public sealed class ModuleCoreSettingsOptions
{
    public int DefaultRefreshIntervalSeconds { get; set; } = 30;

    public bool EnableModuleDiagnostics { get; set; } = true;
}
