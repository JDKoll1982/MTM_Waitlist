using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Module_Settings.Views;

/// <summary>
/// The developer log panel, hosted inside Settings (FR-038, SC-005, `contracts/logging-contract.md` §7).
/// </summary>
/// <remarks>
/// The control holds no rule of its own. What may be read, what a filter means, what a copy contains and whether
/// the reader may open the panel at all come from the view model, so the gate is enforced where the read and the
/// copy happen rather than by hiding an element.
/// </remarks>
public sealed partial class DeveloperLogPanelView : UserControl
{
    public DeveloperLogPanelViewModel ViewModel
    {
        get;
    }

    public DeveloperLogPanelView()
    {
        ViewModel = App.GetService<DeveloperLogPanelViewModel>();
        InitializeComponent();

        Loaded += OnLoaded;
    }

    /// <summary>
    /// Reads the gate and then the first page when the control reaches the tree.
    /// </summary>
    /// <remarks>
    /// The gate is read first and on its own, so a reader who may not open the panel never causes a read of the
    /// log store at all. Both reads are asynchronous, so neither blocks the interface thread (FR-056).
    /// </remarks>
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppLog.Info("DeveloperLogPanelView", "Reached the tree; reading the panel's gate and its first page.");

        ViewModel.LoadSeverityOptions();
        await ViewModel.LoadPermissionAsync();
        await ViewModel.LoadAsync();
    }

    /// <summary>
    /// Copies one entry as text a developer can paste to somebody who has no access to the store (FR-038).
    /// </summary>
    /// <remarks>
    /// A card inside a list is outside the view model's binding scope, so the entry travels as the button's tag
    /// rather than through a second copy of the list the view model already holds.
    /// </remarks>
    private void OnCopyEntryClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: LogPanelEntry entry })
        {
            ViewModel.CopyEntryCommand.Execute(entry);
        }
    }
}
