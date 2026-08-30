using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

public sealed class ModuleCoreService : IModuleCoreService
{
    public string GetModuleName() => "Module_Core";
}
