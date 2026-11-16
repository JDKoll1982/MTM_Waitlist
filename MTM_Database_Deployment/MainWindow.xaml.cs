using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Database_Deployment.ViewModels;

namespace MTM_Database_Deployment;

/// <summary>
/// Deployment shell. The view model is created before the XAML loads so compiled bindings resolve.
/// The code behind only wires controls that cannot be bound (SelectorBar, PasswordBox, window size).
/// </summary>
public sealed partial class MainWindow : Window
{
    public ViewModel_DatabaseDeployment ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new ViewModel_DatabaseDeployment();
        InitializeComponent();

        Title = "Update the MTM database";
        TryResize(1120, 860);
        ApplyStepBarStyle();
        Closed += (_, _) => ViewModel.CancelCommand.Execute(null);
    }

    /// <summary>
    /// StepBar reads its template out of Application.Current.Resources from inside its constructor.
    /// The DevWinUI dictionary is merged in App.xaml, so the style is normally found; this only
    /// covers the case where the implicit style did not stick.
    /// </summary>
    private void ApplyStepBarStyle()
    {
        if (Trail.Style is null
            && Application.Current.Resources.TryGetValue("DefaultStepBarStyle", out var resource)
            && resource is Style style)
        {
            Trail.Style = style;
        }
    }

    private void TryResize(int width, int height)
    {
        try
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        }
        catch (Exception)
        {
            // Sizing is cosmetic; the window keeps its default size if the platform refuses.
        }
    }

    /// <summary>PasswordBox.Password is not bindable, so it is pushed in when the flyout opens.</summary>
    private void OnSignInFlyoutOpening(object? sender, object e) =>
        FlyoutPasswordBox.Password = ViewModel.Password;

    private void OnFlyoutPasswordChanged(object sender, RoutedEventArgs e) =>
        ViewModel.Password = FlyoutPasswordBox.Password;

    private void OnSaveCredentialsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveCredentialsCommand.Execute(null);
        SignInFlyout.Hide();
    }

    /// <summary>SelectorBarSegmented reports a changed selection; the tag is the view model mode index.</summary>
    private void OnModeChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = (sender.SelectedItem as SelectorBarItem)?.Tag as string;
        ViewModel.ModeIndex = string.Equals(tag, "reset", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }
}