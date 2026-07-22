using Microsoft.UI.Xaml;

namespace MTM_Waitlist.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    public DefaultActivationHandler()
    {
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the activation handlers has handled the activation.
        return true;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await Task.CompletedTask;
    }
}
