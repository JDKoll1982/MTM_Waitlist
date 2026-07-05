using Microsoft.UI.Xaml;

using MTM_Waitlist.Contracts.Services;
using MTM_Waitlist.ViewModels;

namespace MTM_Waitlist.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(MainShellViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
