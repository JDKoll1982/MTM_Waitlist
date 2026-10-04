using System.Collections.ObjectModel;
using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.ApplicationModel.DataTransfer;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The developer log panel: what the application recorded, newest first, narrowed by the filter set the store
/// indexes, with the two copies a developer makes to hand a fault to somebody else (FR-038, SC-005, SC-014,
/// `contracts/logging-contract.md` §7).
/// </summary>
/// <remarks>
/// <para>
/// <b>The panel is gated where the work happens, not where the control is drawn.</b> A hidden control is not a
/// permission, so the gate is the first thing every read and every copy checks. A caller who reaches a member
/// without the permission gets nothing done and a stated reason, which is the same answer as the control's
/// absence (FR-056, plan D11).
/// </para>
/// <para>
/// <b>Every read is bounded, twice.</b> A time window is always in force, because a reader who supplies none is
/// not asking for everything: an unbounded read of a store that only grows is the freeze this panel must not
/// cause. A page size is always in force for the same reason, and both are clamped here as well as in the
/// procedure so the screen cannot ask for more than the store will return (SC-014, §7).
/// </para>
/// <para>
/// <b>The store is read, never written.</b> Nothing in this type inserts, updates or deletes anything. Copying
/// is a read as well: it changes no row and it is not itself recorded (FR-038).
/// </para>
/// <para>
/// <b>The clipboard call goes through the options overload.</b> <c>Clipboard.SetContent</c> throws when the
/// process is not in the foreground, and a panel must not raise a fault while it is reporting one. The options
/// overload answers <c>false</c> instead, and that answer is reported to the reader as a refusal
/// (`contracts/logging-contract.md` §7.1).
/// </para>
/// </remarks>
public partial class DeveloperLogPanelViewModel : ObservableObject
{
    /// <summary>The reader that returns one page of entries, newest first.</summary>
    internal const string FilterProcedureName = "sp_ops_startup_logs_filter";

    /// <summary>The reader that returns one row per distinct fault.</summary>
    internal const string GroupProcedureName = "sp_ops_startup_logs_fingerprint_groups_get";

    /// <summary>The default window, in days, which is the one the procedure applies when it is given none.</summary>
    public const int DefaultWindowDays = 30;

    /// <summary>The widest window the panel offers, so a reader cannot turn a filter into a full-table read.</summary>
    public const int MaximumWindowDays = 90;

    /// <summary>The default page, which is the one the procedure applies when it is given none.</summary>
    public const int DefaultPageSize = 200;

    /// <summary>The largest page the panel offers, which matches the procedure's own clamp.</summary>
    public const int MaximumPageSize = 1000;

    private readonly IMySqlHelperServer _store;
    private readonly IPermissionService _permissionService;
    private readonly Func<string, bool> _clipboardWriter;

    /// <summary>
    /// Creates the panel.
    /// </summary>
    /// <param name="store">The stored-procedure seam every read goes through.</param>
    /// <param name="permissionService">The one lookup that answers the gate.</param>
    /// <param name="clipboardWriter">
    /// How the copy reaches the clipboard. It is a delegate rather than a call made where the copy happens so the
    /// refusal path can be exercised without a live clipboard, which is the only way "a refused copy is reported
    /// rather than raised" can be proved (`contracts/logging-contract.md` §7.1, FR-038).
    /// </param>
    public DeveloperLogPanelViewModel(
        IMySqlHelperServer store,
        IPermissionService permissionService,
        Func<string, bool>? clipboardWriter = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _clipboardWriter = clipboardWriter ?? WriteToClipboard;
    }

    /// <summary>
    /// Whether this reader may open the panel at all.
    /// </summary>
    /// <remarks>
    /// It stays false until an answer arrives, so the panel is never offered on a guess. It is an answer handed
    /// in by <see cref="ApplyPermission"/> or read by <see cref="LoadPermissionAsync"/> rather than a second
    /// lookup, because one screen asks the permission service once (FR-056).
    /// </remarks>
    [ObservableProperty]
    public partial bool IsPermitted
    {
        get; set;
    }

    /// <summary>The entries the last read returned, newest first.</summary>
    public ObservableCollection<LogPanelEntry> Entries { get; } = new();

    /// <summary>
    /// The repeated faults the last grouped read returned, most frequent first.
    /// </summary>
    /// <remarks>
    /// These are lines rather than rows because a group is a summary a developer reads, not a record the panel
    /// acts on: nothing here is copied, opened or edited, and a row type would exist to hold five formatted
    /// values and no behaviour.
    /// </remarks>
    public ObservableCollection<string> Groups { get; } = new();

    /// <summary>Every severity the store holds, plus the choice that means any of them.</summary>
    public ObservableCollection<string> SeverityOptions { get; } = new();

    /// <summary>
    /// The windows the panel offers, in days.
    /// </summary>
    /// <remarks>
    /// A fixed set rather than a free number, because the window is a bound on a read and a bound a reader can
    /// type any value into is not a bound the screen can promise. It is a plain list of whole days so the control
    /// needs no conversion between a number box's decimal and the integer the procedure takes.
    /// </remarks>
    public IReadOnlyList<int> WindowDayOptions { get; } = [1, 7, DefaultWindowDays, MaximumWindowDays];

    /// <summary>The page sizes the panel offers, in entries. The store clamps to the same ceiling.</summary>
    public IReadOnlyList<int> PageSizeOptions { get; } = [50, DefaultPageSize, 500, MaximumPageSize];

    /// <summary>The chosen severity, or null for any severity.</summary>
    [ObservableProperty]
    public partial string? SelectedSeverity
    {
        get; set;
    }

    /// <summary>The machine to narrow to, or blank for any machine.</summary>
    [ObservableProperty]
    public partial string HostId
    {
        get; set;
    } = string.Empty;

    /// <summary>The person to narrow to, or blank for any person.</summary>
    [ObservableProperty]
    public partial string ActorId
    {
        get; set;
    } = string.Empty;

    /// <summary>The module to narrow to, or blank for any module.</summary>
    [ObservableProperty]
    public partial string Module
    {
        get; set;
    } = string.Empty;

    /// <summary>The fault type to narrow to, or blank for any fault type.</summary>
    [ObservableProperty]
    public partial string ErrorType
    {
        get; set;
    } = string.Empty;

    /// <summary>How far back the read looks, in days. Clamped to the panel's stated range.</summary>
    [ObservableProperty]
    public partial int WindowDays
    {
        get; set;
    } = DefaultWindowDays;

    /// <summary>How many entries one read returns. Clamped to the store's own ceiling.</summary>
    [ObservableProperty]
    public partial int PageSize
    {
        get; set;
    } = DefaultPageSize;

    /// <summary>
    /// Whether the panel is showing repeated faults gathered into one line each instead of one line per
    /// occurrence, which is what makes a noisy fault readable (SC-014).
    /// </summary>
    [ObservableProperty]
    public partial bool GroupByFingerprint
    {
        get; set;
    }

    /// <summary>Whether a read is in flight, so the screen says so rather than looking empty.</summary>
    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>Whether the store could not be read. Only ever true together with a stated reason.</summary>
    [ObservableProperty]
    public partial bool IsStoreUnavailable
    {
        get; set;
    }

    /// <summary>What the panel says about the last thing that happened, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    /// <summary>Whether the last read returned nothing, which the panel states rather than leaving blank.</summary>
    public bool HasNothing => Entries.Count == 0 && Groups.Count == 0 && !IsBusy && !IsStoreUnavailable;

    /// <summary>Whether the grouped view is the one on screen.</summary>
    public bool IsGrouped => GroupByFingerprint && Groups.Count > 0;

    /// <summary>Whether the flat view is the one on screen.</summary>
    public bool IsFlattened => !GroupByFingerprint || Groups.Count == 0;

    /// <summary>
    /// The filter the current view came from, in one sentence, which the copy carries as its header.
    /// </summary>
    /// <remarks>
    /// A reader who is handed a pasted copy has to be able to tell a filtered sample from a whole history, so the
    /// filter travels with the text rather than being left behind on the screen it came from (FR-038).
    /// </remarks>
    public string FilterDescription => string.Join(
        ", ",
        DescribeFilters());

    public string HeadingText => "Settings_LogPanel_Heading".GetLocalized();

    public string DescriptionText => "Settings_LogPanel_Description".GetLocalized();

    public string SeverityLabelText => "Settings_LogPanel_Severity.Label".GetLocalized();

    public string MachineLabelText => "Settings_LogPanel_Machine.Label".GetLocalized();

    public string PersonLabelText => "Settings_LogPanel_Person.Label".GetLocalized();

    public string ModuleLabelText => "Settings_LogPanel_Module.Label".GetLocalized();

    public string ErrorTypeLabelText => "Settings_LogPanel_ErrorType.Label".GetLocalized();

    public string WindowLabelText => "Settings_LogPanel_Window.Label".GetLocalized();

    public string PageSizeLabelText => "Settings_LogPanel_PageSize.Label".GetLocalized();

    public string GroupLabelText => "Settings_LogPanel_Group.Label".GetLocalized();

    public string ApplyLabelText => "Settings_LogPanel_Apply.Label".GetLocalized();

    public string CopyListLabelText => "Settings_LogPanel_Copy_List.Label".GetLocalized();

    public string RetryLabelText => "Settings_LogPanel_Retry.Label".GetLocalized();

    public string NothingText => "Settings_LogPanel_Nothing.Text".GetLocalized();

    public string NotPermittedText => "Settings_LogPanel_NotPermitted.Text".GetLocalized();

    public string UnavailableText => "Settings_LogPanel_Unavailable.Text".GetLocalized();

    /// <summary>
    /// Answers the gate from an answer the Settings screen already read, so this panel does not read the store
    /// for a fact the page is holding (FR-056).
    /// </summary>
    /// <param name="isPermitted">Whether the signed-in person holds the panel's permission key.</param>
    public void ApplyPermission(bool isPermitted) => IsPermitted = isPermitted;

    /// <summary>
    /// Reads the gate itself, for a host that opens this panel without the Settings screen in front of it.
    /// </summary>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    public async Task LoadPermissionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyPermission(await _permissionService
                .HasPermissionAsync(PermissionKeys.SettingsLogPanel, cancellationToken)
                .ConfigureAwait(true));
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "LogPanel",
                ex,
                "The log panel's permission answer could not be read, so the panel stays closed rather than opening on a guess.");
        }
    }

    /// <summary>Fills the severity choices once, from the one vocabulary the store holds.</summary>
    public void LoadSeverityOptions()
    {
        if (SeverityOptions.Count > 0)
        {
            return;
        }

        foreach (var severity in Enum.GetValues<LogSeverity>())
        {
            SeverityOptions.Add(severity.ToString().ToLowerInvariant());
        }
    }

    /// <summary>
    /// Reads one bounded page of entries, or the grouped view when the reader asked for it.
    /// </summary>
    /// <param name="cancellationToken">The caller's cancellation.</param>
    /// <remarks>
    /// The gate is checked first and the refusal is stated, because a read that went ahead behind a hidden
    /// control would be the very thing the gate exists to prevent (plan D11).
    /// </remarks>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPermitted)
        {
            Entries.Clear();
            Groups.Clear();
            IsStoreUnavailable = false;
            MessageText = NotPermittedText;
            NotifyCounts();
            return;
        }

        IsBusy = true;
        IsStoreUnavailable = false;
        MessageText = string.Empty;

        try
        {
            if (GroupByFingerprint)
            {
                await LoadGroupsAsync(cancellationToken).ConfigureAwait(true);
            }
            else
            {
                await LoadEntriesAsync(cancellationToken).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "LogPanel",
                ex,
                "The log store could not be read, so the panel shows its unavailable state rather than an empty answer.");

            Entries.Clear();
            Groups.Clear();
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
        }
        finally
        {
            IsBusy = false;
            NotifyCounts();
        }
    }

    /// <summary>
    /// Copies one entry as text a developer can paste to somebody who has no access to the store.
    /// </summary>
    /// <param name="entry">The entry the reader pressed copy on.</param>
    [RelayCommand]
    public void CopyEntry(LogPanelEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        Copy([entry], "Settings_LogPanel_Copied_Entry.Text");
    }

    /// <summary>
    /// Copies the entries currently listed, which the read has already bounded by the page size.
    /// </summary>
    [RelayCommand]
    public void CopyList() => Copy([.. Entries], "Settings_LogPanel_Copied_List.Text");

    /// <summary>
    /// Choosing the grouped view changes what the next read asks for; it does not read by itself.
    /// </summary>
    /// <remarks>
    /// A control that read the store the instant it was ticked would be a read the reader did not ask for, and it
    /// would leave the panel holding one answer while the screen claimed another. The reader presses the panel's
    /// own read, which is the one place a bounded read is issued from.
    /// </remarks>
    partial void OnGroupByFingerprintChanged(bool value) => NotifyCounts();

    private async Task LoadEntriesAsync(CancellationToken cancellationToken)
    {
        var rows = await _store
            .ExecuteStoredProcedureQueryAsync(
                FilterProcedureName,
                FilterParameters(includePage: true),
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(true);

        Entries.Clear();
        Groups.Clear();

        foreach (var row in rows)
        {
            Entries.Add(LogPanelEntry.FromRow(row));
        }

        MessageText = Entries.Count == 0 ? NothingText : string.Empty;
    }

    private async Task LoadGroupsAsync(CancellationToken cancellationToken)
    {
        var parameters = FilterParameters(includePage: false);
        parameters["p_max_groups"] = Math.Clamp(PageSize, 1, MaximumPageSize);

        var rows = await _store
            .ExecuteStoredProcedureQueryAsync(
                GroupProcedureName,
                parameters,
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(true);

        Entries.Clear();
        Groups.Clear();

        foreach (var row in rows)
        {
            Groups.Add(DescribeGroup(row));
        }

        MessageText = Groups.Count == 0 ? NothingText : string.Empty;
    }

    /// <summary>
    /// Builds the parameter set both readers take, so the two views cannot disagree about what a filter means.
    /// </summary>
    /// <remarks>
    /// The window and the page size are always supplied rather than left to the procedure's defaults, because the
    /// bound is what makes the read safe and a reader should not have to trust a default to get it.
    /// </remarks>
    private Dictionary<string, object?> FilterParameters(bool includePage)
    {
        var to = DateTime.UtcNow;
        var days = Math.Clamp(WindowDays, 1, MaximumWindowDays);

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["p_level"] = Blank(SelectedSeverity) ? null : SelectedSeverity!.Trim(),
            ["p_host_id"] = Blank(HostId) ? null : HostId.Trim(),
            ["p_actor_id"] = Blank(ActorId) ? null : ActorId.Trim(),
            ["p_module"] = Blank(Module) ? null : Module.Trim(),
            ["p_error_type"] = Blank(ErrorType) ? null : ErrorType.Trim(),
            ["p_error_fingerprint"] = null,
            ["p_from_utc"] = to.AddDays(-days),
            ["p_to_utc"] = to,
        };

        if (includePage)
        {
            parameters["p_page_size"] = Math.Clamp(PageSize, 1, MaximumPageSize);
            parameters["p_offset"] = 0;
        }

        return parameters;
    }

    /// <summary>
    /// Writes the text to the clipboard and reports what the clipboard said.
    /// </summary>
    /// <remarks>
    /// The refusal is a sentence on the panel rather than an exception the reader would see as a crash, which is
    /// the whole reason the options overload is used instead of <c>Clipboard.SetContent</c> (§7.1).
    /// </remarks>
    private void Copy(IReadOnlyList<LogPanelEntry> entries, string copiedResourceKey)
    {
        if (!IsPermitted || entries.Count == 0)
        {
            return;
        }

        var text = LogExportFormatter.Format(FilterDescription, entries);

        if (_clipboardWriter(text))
        {
            MessageText = copiedResourceKey.GetLocalized();
            return;
        }

        MessageText = "Settings_LogPanel_Copy_Refused.Text".GetLocalized();
    }

    private static string DescribeGroup(IReadOnlyDictionary<string, object?> row)
    {
        var occurrences = row.TryGetValue("occurrences", out var count) ? count : null;
        var fault = Text(row, "error_type");
        var module = Text(row, "module");
        var first = Timestamp(row, "first_seen_utc");
        var last = Timestamp(row, "last_seen_utc");
        var sample = Text(row, "sample_message");

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} x {1} [{2}] {3} to {4}: {5}",
            occurrences ?? 0,
            fault ?? "unknown fault",
            module ?? "unknown module",
            first,
            last,
            sample);
    }

    private IEnumerable<string> DescribeFilters()
    {
        yield return string.Format(
            CultureInfo.InvariantCulture,
            "window=last {0} day(s)",
            Math.Clamp(WindowDays, 1, MaximumWindowDays));

        if (!Blank(SelectedSeverity))
        {
            yield return "severity=" + SelectedSeverity!.Trim();
        }

        if (!Blank(HostId))
        {
            yield return "machine=" + HostId.Trim();
        }

        if (!Blank(ActorId))
        {
            yield return "person=" + ActorId.Trim();
        }

        if (!Blank(Module))
        {
            yield return "module=" + Module.Trim();
        }

        if (!Blank(ErrorType))
        {
            yield return "error type=" + ErrorType.Trim();
        }

        yield return string.Format(
            CultureInfo.InvariantCulture,
            "page size={0}",
            Math.Clamp(PageSize, 1, MaximumPageSize));
    }

    /// <summary>
    /// Hands the text to the clipboard through the options overload, which answers a refusal instead of raising
    /// one (§7.1).
    /// </summary>
    private static bool WriteToClipboard(string text)
    {
        try
        {
            var content = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            content.SetText(text);

            return Clipboard.SetContentWithOptions(
                content,
                new ClipboardContentOptions
                {
                    // Both default to true. A diagnosis names a machine and a person, so it may neither sit in the
                    // clipboard's history nor follow the reader onto another device.
                    IsAllowedInHistory = false,
                    IsRoamable = false,
                });
        }
        catch (Exception ex)
        {
            // The call itself can fail on a host with no clipboard at all. That is a refusal, not a fault to
            // raise: a panel must not raise a diagnostic while it is showing one.
            AppLog.Error("LogPanel", ex, "The clipboard refused the copy, so the panel reports a refusal.");
            return false;
        }
    }

    private static string? Text(IReadOnlyDictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) && value is not null and not DBNull
            ? value.ToString()
            : null;

    private static string Timestamp(IReadOnlyDictionary<string, object?> row, string column)
    {
        try
        {
            return row.TryGetValue(column, out var value) && value is not null and not DBNull
                ? Convert.ToDateTime(value, CultureInfo.InvariantCulture)
                    .ToUniversalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z"
                : "unknown";
        }
        catch (Exception)
        {
            return "unknown";
        }
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private void NotifyCounts()
    {
        OnPropertyChanged(nameof(HasNothing));
        OnPropertyChanged(nameof(IsGrouped));
        OnPropertyChanged(nameof(IsFlattened));
        OnPropertyChanged(nameof(FilterDescription));
    }
}
