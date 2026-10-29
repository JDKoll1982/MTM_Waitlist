using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Controls.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.GenericRequest.ViewModels;
using MTM_Waitlist.GenericRequest.Views;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.GenericRequest.Temp;

/// <summary>
/// The preview window: a chooser on the left, one screen or the card comparison on the right.
/// </summary>
/// <remarks>
/// Every screen is created from the same view models the shipping application will use, and every one of them talks
/// only to <see cref="TempGenericRequestStepData"/>. Nothing here opens a connection or writes anything.
/// </remarks>
public sealed partial class MainWindow : Window
{
    private readonly TempGenericRequestStepData _data = new();
    private readonly TempGenericRequestNavigator _navigator = new();
    private readonly Dictionary<GenericRequestStep, GenericRequestStepViewModel> _viewModels = new();
    private IReadOnlyList<TempPreviewScreen> _screens = Array.Empty<TempPreviewScreen>();
    private bool _syncing;

    /// <summary>The flow's steps, in order, so the chooser lists them the way the flow visits them.</summary>
    private static readonly GenericRequestStep[] s_stepOrder =
    [
        GenericRequestStep.WorkCenter,
        GenericRequestStep.Category,
        GenericRequestStep.Item,
        GenericRequestStep.Part,
        GenericRequestStep.Dunnage,
        GenericRequestStep.Scrap,
        GenericRequestStep.Die,
        GenericRequestStep.Details,
        GenericRequestStep.Summary,
        GenericRequestStep.Result,
    ];

    public MainWindow()
    {
        InitializeComponent();

        Title = "TempGenericRequest_Window_Title.Text".GetLocalized();

        _navigator.StepChanged += (_, step) => ShowStep(step);

        BuildViewModels();
        _screens = BuildScreens();

        GenericRequestPageServices.Configure(new TempPreviewServices(_viewModels));

        _syncing = true;
        TempScreenPicker.ItemsSource = _screens;
        TempScenarioPicker.ItemsSource = TempPreviewScenario.All;
        TempVariantPicker.ItemsSource = Enum.GetValues<GenericRequestCardVariant>();
        TempScreenPicker.SelectedIndex = 0;
        TempScenarioPicker.SelectedIndex = 0;
        TempVariantPicker.SelectedIndex = 0;
        _syncing = false;

        _navigator.GoTo(GenericRequestStep.WorkCenter);
    }

    private GenericRequestCardVariant SelectedVariant =>
        TempVariantPicker.SelectedItem is GenericRequestCardVariant variant
            ? variant
            : GenericRequestCardVariant.Part;

    private void OnScreenChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || TempScreenPicker.SelectedItem is not TempPreviewScreen screen)
        {
            return;
        }

        if (screen.Step is { } step)
        {
            _navigator.GoTo(step);
        }
        else
        {
            ShowCards();
        }
    }

    private void OnScenarioChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        _data.Scenario = TempScenarioPicker.SelectedItem as TempPreviewScenario ?? TempPreviewScenario.All[0];

        if (TempScreenPicker.SelectedItem is TempPreviewScreen screen && screen.Step is { } step)
        {
            _navigator.GoTo(step);
        }
    }

    private void OnVariantChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || TempScreenPicker.SelectedItem is TempPreviewScreen { Step: not null })
        {
            return;
        }

        ShowCards();
    }

    private void ShowStep(GenericRequestStep step)
    {
        _data.Scenario = TempScenarioPicker.SelectedItem as TempPreviewScenario ?? TempPreviewScenario.All[0];

        _syncing = true;
        TempScreenPicker.SelectedItem = _screens.FirstOrDefault(screen => screen.Step == step);
        _syncing = false;

        TempContent.Content = CreatePage(step);
    }

    private void ShowCards() =>
        TempContent.Content = new TempPreviewCardPage(SelectedVariant);

    private static Page CreatePage(GenericRequestStep step) => step switch
    {
        GenericRequestStep.WorkCenter => new GenericRequestWorkCenterPage(),
        GenericRequestStep.Category => new GenericRequestCategoryPage(),
        GenericRequestStep.Item => new GenericRequestItemPage(),
        GenericRequestStep.Part => new GenericRequestPartPage(),
        GenericRequestStep.Dunnage => new GenericRequestDunnagePage(),
        GenericRequestStep.Scrap => new GenericRequestScrapTypePage(),
        GenericRequestStep.Die => new GenericRequestDiePage(),
        GenericRequestStep.Details => new GenericRequestDetailsPage(),
        GenericRequestStep.Summary => new GenericRequestSummaryPage(),
        GenericRequestStep.Result => new GenericRequestResultPage(),
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, "The preview has no page for that step."),
    };

    private void BuildViewModels()
    {
        foreach (var step in s_stepOrder)
        {
            _viewModels[step] = step switch
            {
                GenericRequestStep.WorkCenter => new GenericRequestWorkCenterViewModel(_data, _navigator),
                GenericRequestStep.Category => new GenericRequestCategoryViewModel(_data, _navigator),
                GenericRequestStep.Item => new GenericRequestItemViewModel(_data, _navigator),
                GenericRequestStep.Part => new GenericRequestPartViewModel(_data, _navigator),
                GenericRequestStep.Dunnage => new GenericRequestDunnageViewModel(_data, _navigator),
                GenericRequestStep.Scrap => new GenericRequestScrapTypeViewModel(_data, _navigator),
                GenericRequestStep.Die => new GenericRequestDieViewModel(_data, _navigator),
                GenericRequestStep.Details => new GenericRequestDetailsViewModel(_data, _navigator),
                GenericRequestStep.Summary => new GenericRequestSummaryViewModel(_data, _navigator),
                GenericRequestStep.Result => new GenericRequestResultViewModel(_data, _navigator),
                _ => throw new ArgumentOutOfRangeException(nameof(step), step, "The preview has no view model for that step."),
            };
        }
    }

    /// <summary>
    /// The screens, named by each step's own heading, so the chooser and the page cannot disagree about a name.
    /// </summary>
    private IReadOnlyList<TempPreviewScreen> BuildScreens() =>
    [
        .. s_stepOrder.Select(step => new TempPreviewScreen(_viewModels[step].Title, step)),
        new TempPreviewScreen("TempGenericRequest_Card_Title.Text".GetLocalized(), null),
    ];
}
