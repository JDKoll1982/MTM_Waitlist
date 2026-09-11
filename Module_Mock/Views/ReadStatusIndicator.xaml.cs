using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Module_Mock.Views;

/// <summary>
/// Non-interactive indicator that states when Infor Visual is unreachable, that cached data is in use, and
/// how old that cached data is.
/// </summary>
/// <remarks>
/// <para>
/// There is no control here: the indicator cannot change a mode, switch a source, or dismiss itself (FR-003).
/// It becomes visible on entering <see cref="VisualReadStatus.Cached"/> and disappears on returning to
/// <see cref="VisualReadStatus.Live"/>, driven entirely by <see cref="IReadStatusProvider"/>.
/// </para>
/// <para>
/// Age is stated, never enforced: data is never refused because it is old (FR-022). Before the first
/// successful refresh the control says the content is baseline seed data rather than reporting a
/// meaningless age (FR-017).
/// </para>
/// </remarks>
public sealed partial class ReadStatusIndicator : UserControl, INotifyPropertyChanged
{
    private readonly IReadStatusProvider _statusProvider;

    private bool _isCachedDataInUse;
    private string _message = string.Empty;

    /// <summary>Creates the indicator and subscribes to the read-status provider.</summary>
    public ReadStatusIndicator()
    {
        _statusProvider = App.GetService<IReadStatusProvider>();

        InitializeComponent();

        _statusProvider.Changed += OnStatusChanged;
        Unloaded += OnUnloaded;

        Apply(_statusProvider.Current);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Whether cached data is in use; exactly the condition that makes the indicator visible.</summary>
    public bool IsCachedDataInUse
    {
        get => _isCachedDataInUse;
        private set
        {
            if (_isCachedDataInUse == value)
            {
                return;
            }

            _isCachedDataInUse = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The statement shown: unreachable source, cached data in use, and the age or seed note.</summary>
    public string Message
    {
        get => _message;
        private set
        {
            if (string.Equals(_message, value, StringComparison.Ordinal))
            {
                return;
            }

            _message = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The indicator's title, resolved from resources.</summary>
    public string TitleText => "Mock_Indicator.Title".GetLocalized();

    private void OnStatusChanged(object? sender, ReadStatusSnapshot snapshot) => Apply(snapshot);

    private void Apply(ReadStatusSnapshot snapshot)
    {
        IsCachedDataInUse = snapshot.IsCachedDataInUse;

        if (!snapshot.IsCachedDataInUse)
        {
            Message = string.Empty;
            return;
        }

        Message = BuildMessage(snapshot);
    }

    private static string BuildMessage(ReadStatusSnapshot snapshot)
    {
        var unreachable = "Mock_Indicator.Unreachable".GetLocalized();
        var inUse = "Mock_Indicator.CachedInUse".GetLocalized();

        if (snapshot.IsSeedContentOnly)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Mock_Indicator.SeedContent".GetLocalized(),
                unreachable,
                inUse);
        }

        var age = snapshot.CachedDataAgeUtc is { } cachedAge ? FormatAge(cachedAge) : "Mock_Indicator.UnknownAge".GetLocalized();

        return string.Format(
            CultureInfo.CurrentCulture,
            "Mock_Indicator.AgeText".GetLocalized(),
            unreachable,
            inUse,
            age);
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        if (age.TotalMinutes < 60)
        {
            return string.Format(CultureInfo.CurrentCulture, "Mock_Indicator.AgeMinutes".GetLocalized(), (int)age.TotalMinutes);
        }

        if (age.TotalHours < 48)
        {
            return string.Format(CultureInfo.CurrentCulture, "Mock_Indicator.AgeHours".GetLocalized(), (int)age.TotalHours);
        }

        return string.Format(CultureInfo.CurrentCulture, "Mock_Indicator.AgeDays".GetLocalized(), (int)age.TotalDays);
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        _statusProvider.Changed -= OnStatusChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
