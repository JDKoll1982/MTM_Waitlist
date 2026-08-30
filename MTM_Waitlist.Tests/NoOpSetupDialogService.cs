using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Tests;

public sealed class NoOpSetupDialogService : ISetupDialogService
{
    public Task<SetupDunnagePart?> ShowDunnageImageSearchDialogAsync() => Task.FromResult<SetupDunnagePart?>(null);

    public Task<bool> ConfirmNoDunnageAsync() => Task.FromResult(true);
}
