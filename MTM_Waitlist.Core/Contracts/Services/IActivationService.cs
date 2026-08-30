namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs, bool activateMainWindow = true);
}
