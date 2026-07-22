namespace MTM_Waitlist.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs, bool activateMainWindow = true);
}
