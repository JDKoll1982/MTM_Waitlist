using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MTM_Waitlist.Mock.Service.ViewModels;

namespace MTM_Waitlist.Mock.Service.Views;

/// <summary>
/// The service status surface (T077): per-shape last-refresh state, per-store last-backup state, and a
/// clear report when the backup tool is missing (FR-013).
/// </summary>
/// <remarks>
/// Reading status has no side effects — it never triggers a refresh or a backup — and the text is
/// resolved from resources rather than embedded in XAML (constitution V).
/// </remarks>
public sealed partial class ServiceStatusPage : Page
{
    /// <summary>Creates the page. The view model is attached in <see cref="OnNavigatedTo"/>.</summary>
    public ServiceStatusPage()
    {
        InitializeComponent();
    }

    /// <summary>The page's view model.</summary>
    public ServiceStatusViewModel ViewModel { get; private set; } = null!;

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not IServiceProvider services)
        {
            return;
        }

        ViewModel = services.GetRequiredService<ServiceStatusViewModel>();
        ViewModel.LoadStatusCommand.Execute(null);
    }
}
