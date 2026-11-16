using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Database_Deployment.Models;
using MTM_Database_Deployment.Services;
using MySqlConnector;

namespace MTM_Database_Deployment.ViewModels;

/// <summary>
/// Three-step deployment wizard written for an operator with no database background: connect, look at
/// what will change, then save. Every label is plain language; the SQL detail sits behind a disclosure
/// and in the saved report.
/// </summary>
public partial class ViewModel_DatabaseDeployment : ObservableObject
{
    private const string GlyphAdd = "\uE710";
    private const string GlyphRefresh = "\uE72C";
    private const string GlyphList = "\uE8FD";
    private const string GlyphWarning = "\uE7BA";
    private const string GlyphCheck = "\uE73E";
    private const string GlyphError = "\uEA39";
    private const string GlyphLock = "\uE72E";

    private readonly Service_DeploymentSettings _settingsService;
    private Service_ObjectCatalog _catalog;
    private readonly Model_ConnectionSettings _settings;
    private CancellationTokenSource? _cancellation;
    private Model_DeploymentPlan? _plan;

    [ObservableProperty]
    public partial string PreferredServer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FallbackServer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Port { get; set; } = 3306;

    [ObservableProperty]
    public partial string Database { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int DatabaseIndex { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int StepIndex { get; set; }

    [ObservableProperty]
    public partial string StepTitle { get; set; } = "Step 1 of 3 - Connect to the database";

    [ObservableProperty]
    public partial string StepHint { get; set; } =
        "This tool brings the MTM database up to date. It only adds what is missing - it never removes "
        + "your records.";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool HasChecked { get; set; }

    [ObservableProperty]
    public partial string ConnectionHeadline { get; set; } = "Not connected yet";

    [ObservableProperty]
    public partial string ConnectionDetail { get; set; } =
        "Press Connect and we will check the database for you. You do not need to change anything on this page.";

    [ObservableProperty]
    public partial string ChangeHeadline { get; set; } = "Nothing checked yet";

    [ObservableProperty]
    public partial string ChangeDetail { get; set; } = "Connect first, then press Check the database.";

    [ObservableProperty]
    public partial string SafetyLine { get; set; } = "Your records are kept. Nothing is deleted.";

    [ObservableProperty]
    public partial bool RemoveDatabaseOnlyObjects { get; set; }

    [ObservableProperty]
    public partial bool ConfirmRebuild { get; set; }

    [ObservableProperty]
    public partial int ModeIndex { get; set; }

    [ObservableProperty]
    public partial string ResultHeadline { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultDetail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultGlyph { get; set; } = GlyphCheck;

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    [ObservableProperty]
    public partial bool ResultIsFailure { get; set; }

    [ObservableProperty]
    public partial string ProgressMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TechnicalDetails { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LogText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReportPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ServerDetail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SignInMessage { get; set; } = "Not checked yet.";

    [ObservableProperty]
    public partial string SignInGlyph { get; set; } = GlyphLock;

    [ObservableProperty]
    public partial string SignInSummary { get; set; } = "Sign in";

    /// <summary>The name the operator types for the copy made on this page.</summary>
    [ObservableProperty]
    public partial string CopyTargetName { get; set; } = string.Empty;

    /// <summary>Set only when the operator accepts replacing a database that already has the copy's name.</summary>
    [ObservableProperty]
    public partial bool ReplaceCopyIfExists { get; set; }

    [ObservableProperty]
    public partial string CopyStatus { get; set; } = string.Empty;

    public ObservableCollection<Model_ChangeGroup> ChangeGroups { get; } = new();

    /// <summary>
    /// The databases the operator can choose between: the configured ones in order, then anything the server holds
    /// that the settings file has not heard of, added once Connect has reached it.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<Model_DatabaseOption> DatabaseOptions { get; set; } = new();

    public string FolderDetail => _catalog.SqlRoot;

    /// <summary>
    /// True when the chosen database is one this tool ships scripts for. The waitlist store and the cache mirror
    /// have different schemas, and neither is written with the other's tree.
    /// </summary>
    public bool IsDatabaseSupported => DeploymentScope.SqlRootFor(_settingsService.SqlRoot, Database) is not null;

    public bool IsSafeMode => ModeIndex == 0;

    public bool IsRebuildMode => ModeIndex == 1;

    public bool CanConnect => !IsBusy;

    public bool CanCheck => !IsBusy && IsConnected && IsDatabaseSupported;

    public bool CanSave => !IsBusy && HasChecked && IsDatabaseSupported && (IsSafeMode || ConfirmRebuild);

    /// <summary>
    /// The copy needs a connection, a name of its own, and no run in progress.
    /// </summary>
    public bool CanCopy => !IsBusy
        && IsConnected
        && !string.IsNullOrWhiteSpace(CopyTargetName)
        && !string.Equals(CopyTargetName.Trim(), Database, StringComparison.OrdinalIgnoreCase);

    public bool HasCopyStatus => !string.IsNullOrWhiteSpace(CopyStatus);

    public string SaveButtonText => IsRebuildMode ? "Erase everything and rebuild" : "Save the changes";

    public string SaveButtonBusyText => IsRebuildMode ? "Rebuilding the database..." : "Saving the changes...";

    public string CheckButtonText => HasChecked ? "Check again" : "Check the database";

    public bool HasReport => !string.IsNullOrWhiteSpace(ReportPath);

    public bool HasTechnicalDetails => !string.IsNullOrWhiteSpace(TechnicalDetails);

    public bool IsStep1 => StepIndex == 0;

    public bool IsStep2 => StepIndex == 1;

    public bool IsStep3 => StepIndex >= 2;

    public bool CanGoBack => StepIndex > 0 && !IsBusy;

    public ViewModel_DatabaseDeployment()
    {
        _settingsService = new Service_DeploymentSettings();
        _catalog = CreateCatalog(Database);
        _settings = _settingsService.Load();

        PreferredServer = _settings.PreferredServer;
        FallbackServer = _settings.FallbackServer;
        Port = _settings.Port;
        Database = _settings.Database;
        UserName = _settings.User;
        Password = _settings.Password;
        ServerDetail = $"First tries {PreferredServer}, then {FallbackServer}.";
        SignInSummary = string.IsNullOrWhiteSpace(UserName) ? "Sign in" : $"Sign in as {UserName}";
        CopyTargetName = SuggestedCopyName(Database);

        DatabaseOptions = new ObservableCollection<Model_DatabaseOption>(
            _settings.ResolveAvailableDatabases().Select(Model_DatabaseOption.For));

        for (var index = 0; index < DatabaseOptions.Count; index++)
        {
            if (string.Equals(DatabaseOptions[index].Name, Database, StringComparison.OrdinalIgnoreCase))
            {
                DatabaseIndex = index;
                break;
            }
        }

        UpdateSafetyLine();
    }

    /// <summary>
    /// A plan describes one database, so switching target throws the connection and the plan away
    /// instead of letting the operator save a plan that was built against the other one.
    /// </summary>
    partial void OnDatabaseIndexChanged(int value)
    {
        if (value < 0 || value >= DatabaseOptions.Count)
        {
            return;
        }

        var option = DatabaseOptions[value];
        if (string.Equals(Database, option.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Database = option.Name;
        _plan = null;
        HasChecked = false;
        HasResult = false;
        IsConnected = false;
        _catalog = CreateCatalog(option.Name);
        OnPropertyChanged(nameof(FolderDetail));
        OnPropertyChanged(nameof(IsDatabaseSupported));
        CopyTargetName = SuggestedCopyName(option.Name);
        CopyStatus = string.Empty;
        ReplaceCopyIfExists = false;
        ChangeGroups.Clear();
        ChangeHeadline = "Nothing checked yet";
        ChangeDetail = "Connect first, then press Check the database.";
        ConnectionHeadline = "Not connected yet";
        ConnectionDetail = "Press Connect and we will check the database for you.";
        ResultHeadline = string.Empty;
        ResultDetail = string.Empty;
        TechnicalDetails = string.Empty;
        StepIndex = 0;
        ApplyStepText();
        UpdateSafetyLine();
        AppendLog($"Target database is now {Database}.");
    }

    partial void OnIsBusyChanged(bool value)
    {
        RaiseRunState();
        OnPropertyChanged(nameof(CanGoBack));
    }

    partial void OnIsConnectedChanged(bool value) => RaiseRunState();

    partial void OnHasCheckedChanged(bool value)
    {
        OnPropertyChanged(nameof(CheckButtonText));
        RaiseRunState();
    }

    partial void OnStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(CanGoBack));
    }

    partial void OnTechnicalDetailsChanged(string value) => OnPropertyChanged(nameof(HasTechnicalDetails));

    partial void OnConfirmRebuildChanged(bool value)
    {
        UpdateSafetyLine();
        RaiseRunState();
    }

    partial void OnModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsSafeMode));
        OnPropertyChanged(nameof(IsRebuildMode));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(SaveButtonBusyText));
        UpdateSafetyLine();
        RaiseRunState();
    }

    partial void OnRemoveDatabaseOnlyObjectsChanged(bool value) => UpdateSafetyLine();

    private void RaiseRunState()
    {
        OnPropertyChanged(nameof(CanConnect));
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanCopy));
    }

    /// <summary>
    /// The catalog for the chosen database's own tree. An unsupported name keeps the waitlist tree, which can be
    /// displayed but never saved: <see cref="IsDatabaseSupported"/> gates the buttons.
    /// </summary>
    private Service_ObjectCatalog CreateCatalog(string database) =>
        new(DeploymentScope.SqlRootFor(_settingsService.SqlRoot, database) ?? _settingsService.SqlRoot);

    partial void OnCopyTargetNameChanged(string value) => OnPropertyChanged(nameof(CanCopy));

    partial void OnCopyStatusChanged(string value) => OnPropertyChanged(nameof(HasCopyStatus));

    /// <summary>
    /// The name a copy is offered under. It ends in <c>_copy</c> rather than <c>_test</c> on purpose: a database
    /// called <c>mtm_waitlist_test</c> already exists, and offering that name would put the replace box in front of
    /// the practice copy.
    /// </summary>
    private static string SuggestedCopyName(string database) =>
        string.IsNullOrWhiteSpace(database) ? "_copy" : $"{database.Trim()}_copy";

    private void UpdateSafetyLine()
    {
        if (IsRebuildMode)
        {
            SafetyLine = ConfirmRebuild
                ? "Every table is emptied and rebuilt. A copy of what is in there now is saved first as "
                  + $"{DatabaseBackup.BackupName(Database)}."
                : "This erases every record. Only do this on a database you are happy to wipe.";
            return;
        }

        SafetyLine = (RemoveDatabaseOnlyObjects
            ? "Your records are kept. Leftover columns and tables this tool does not use will be deleted."
            : "Your records are kept. Nothing is deleted.")
            + BackupSentence();
    }

    /// <summary>
    /// The copy the run takes before its first statement, said in the same sentence as the rest of the safety line
    /// so the operator reads it where the reassurance is, not only in the report afterwards.
    /// </summary>
    private string BackupSentence() => string.IsNullOrWhiteSpace(Database)
        ? string.Empty
        : $" A copy of every table is saved as {DatabaseBackup.BackupName(Database)} before anything changes.";

    /// <summary>
    /// Adds every <c>mtm_*</c> database the server holds to the picker, keeping the configured order in front and the
    /// current choice selected.
    /// </summary>
    /// <remarks>
    /// A refresh that fails leaves the configured list in place: not being able to list databases is not a reason to
    /// tell an operator they cannot connect.
    /// </remarks>
    private async Task RefreshDatabaseOptionsAsync(Service_MySqlGateway gateway, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await gateway
                .QueryAsync("information_schema", "SHOW DATABASES;", cancellationToken)
                .ConfigureAwait(true);

            var discovered = rows
                .Select(row => row[0])
                .Where(name => name.StartsWith("mtm_", StringComparison.OrdinalIgnoreCase));

            var options = Model_DatabaseOption.Merge(_settings.ResolveAvailableDatabases(), discovered);
            var selected = options
                .Select((option, index) => (option, index))
                .FirstOrDefault(pair => string.Equals(pair.option.Name, Database, StringComparison.OrdinalIgnoreCase));

            DatabaseOptions = new ObservableCollection<Model_DatabaseOption>(options);

            if (selected.option is not null)
            {
                DatabaseIndex = selected.index;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            AppendLog("The database list could not be read: " + exception.Message);
        }
    }

    /// <summary>
    /// Best-effort "find my database": probes the usual server, then the spare, so the operator never
    /// has to know an IP address.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _cancellation = new CancellationTokenSource();
        ConnectionHeadline = "Looking for the database...";
        ConnectionDetail = "Checking the usual database computer first.";
        AppendLog("Looking for the database computer.");

        try
        {
            SaveSettings();
            var gateway = CreateGateway();
            var message = await gateway.ResolveServerAsync(_cancellation.Token).ConfigureAwait(true);

            // The probe only proves the port is open. Sign in for real before telling the operator
            // they are connected, so a wrong password surfaces here and not halfway through a save.
            var databaseMissing = false;
            try
            {
                await using var connection = await gateway.OpenAsync(Database, _cancellation.Token).ConfigureAwait(true);
                await connection.PingAsync(_cancellation.Token).ConfigureAwait(true);
            }
            catch (MySqlException mysql) when (mysql.Number == 1049)
            {
                databaseMissing = true;
            }

            IsConnected = true;
            ServerDetail = $"Connected to {gateway.ActiveServer}:{gateway.Port}";
            SignInSummary = string.IsNullOrWhiteSpace(UserName) ? "Signed in" : $"Signed in as {UserName}";

            // Whatever the server actually holds now becomes selectable, so a copy made with this tool — or by
            // anybody else — can be chosen without editing deployment.settings.json first.
            await RefreshDatabaseOptionsAsync(gateway, _cancellation.Token).ConfigureAwait(true);

            if (databaseMissing)
            {
                ConnectionHeadline = "Connected - but that database does not exist yet";
                ConnectionDetail =
                    $"We reached the database computer, but there is nothing called '{Database}' on it. "
                    + "'Start over' can create it, or ask IT for the right name in Advanced settings.";
            }
            else if (!IsDatabaseSupported)
            {
                // The waitlist store and the cache mirror have different schemas. Saying so here is what stops a
                // deployment from writing one application's tables into the other's database.
                ConnectionHeadline = "Connected - but this tool does not update that database";
                ConnectionDetail = DeploymentScope.UnsupportedDatabaseMessage(Database);
                AppendLog(ConnectionDetail);
                return;
            }
            else if (gateway.UsingFallback)
            {
                ConnectionHeadline = "Connected - using this computer";
                ConnectionDetail =
                    $"The usual database computer did not answer, so we are using this PC. Database: {Database}.";
            }
            else
            {
                ConnectionHeadline = "Connected to the database";
                ConnectionDetail = $"We signed in on {gateway.ActiveServer}. Database: {Database}.";
            }

            StepIndex = 1;
            ApplyStepText();
            AppendLog(message);
        }
        catch (MySqlException mysql) when (mysql.Number == 1045)
        {
            IsConnected = false;
            ConnectionHeadline = "The database refused the sign-in";
            ConnectionDetail =
                "The user name or password is not right. Ask IT to check them in Advanced settings, then "
                + "press Connect again.";
            TechnicalDetails = mysql.Message;
            AppendLog("Sign-in refused: " + mysql.Message);
        }
        catch (Exception exception)
        {
            IsConnected = false;
            ConnectionHeadline = "We could not reach the database";
            ConnectionDetail =
                "Ask IT whether the database computer is switched on, then press Connect again. "
                + "The details are below if you need to send them.";
            TechnicalDetails = exception.Message;
            AppendLog("Connection check failed: " + exception.Message);
        }
        finally
        {
            IsBusy = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (IsBusy || !IsDatabaseSupported)
        {
            return;
        }

        IsBusy = true;
        _cancellation = new CancellationTokenSource();
        ProgressMessage = "Reading the database...";
        AppendLog("Checking what the database is missing.");

        try
        {
            var gateway = CreateGateway();
            _plan = await BuildPlanAsync(gateway).ConfigureAwait(true);
            HasChecked = true;
            DescribeChanges(_plan);
            TechnicalDetails = _plan.ToReportText();
            StepIndex = 2;
            ApplyStepText();
            AppendLog($"Check finished. {_plan.Summary}");
        }
        catch (Exception exception)
        {
            HasChecked = false;
            ChangeHeadline = "We could not check the database";
            ChangeDetail = "Press Check the database to try again. The details are below for IT.";
            TechnicalDetails = exception.Message;
            AppendLog("Check failed: " + exception.Message);
        }
        finally
        {
            IsBusy = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || !IsDatabaseSupported)
        {
            return;
        }

        if (IsRebuildMode && !ConfirmRebuild)
        {
            HasResult = true;
            ResultIsFailure = true;
            ResultGlyph = GlyphWarning;
            ResultHeadline = "Please confirm before anything is erased";
            ResultDetail = "Tick the box that says you understand every record will be erased.";
            return;
        }

        IsBusy = true;
        _cancellation = new CancellationTokenSource();
        HasResult = false;
        ProgressMessage = IsRebuildMode ? "Erasing and rebuilding..." : "Saving the changes...";
        AppendLog("Saving the changes to the database.");

        try
        {
            var gateway = CreateGateway();
            var engine = new Service_DeploymentEngine(gateway, _catalog, new DatabaseBackup(gateway));
            var progress = new Progress<string>(message =>
            {
                ProgressMessage = message;
                AppendLog(message);
            });

            Model_DeploymentResult result;
            if (IsRebuildMode || _plan is null)
            {
                result = await engine
                    .RebuildAsync(Database, progress, _cancellation.Token)
                    .ConfigureAwait(true);
            }
            else
            {
                result = await engine
                    .ApplyAsync(_plan, RemoveDatabaseOnlyObjects, progress, _cancellation.Token)
                    .ConfigureAwait(true);
            }

            ReportPath = SaveReport(result, gateway);
            OnPropertyChanged(nameof(HasReport));
            TechnicalDetails = result.ToReportText();
            DescribeResult(result);
            AppendLog(ResultHeadline);
        }
        catch (OperationCanceledException)
        {
            HasResult = true;
            ResultIsFailure = true;
            ResultGlyph = GlyphWarning;
            ResultHeadline = "Stopped before finishing";
            ResultDetail = "The database may be partly updated. Press Check the database, then Save again to finish.";
            AppendLog("Saving was stopped.");
        }
        catch (Exception exception)
        {
            HasResult = true;
            ResultIsFailure = true;
            ResultGlyph = GlyphError;
            ResultHeadline = "Something went wrong";
            ResultDetail = "Nothing was erased. Send the details below to IT and they can help.";
            TechnicalDetails = exception.Message;
            AppendLog("Saving failed: " + exception.Message);
        }
        finally
        {
            IsBusy = false;
            ProgressMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    /// <summary>
    /// Saves the sign-in details on this PC only. Called from the flyout, which then closes itself.
    /// </summary>
    [RelayCommand]
    private void SaveCredentials()
    {
        SaveSettings();
        SignInSummary = string.IsNullOrWhiteSpace(UserName) ? "Sign in" : $"Sign in as {UserName}";
        SignInGlyph = GlyphLock;
        SignInMessage = $"Saved on this PC. The tool will use {UserName} from now on.";
        AppendLog($"Database sign-in saved for {UserName}.");
    }

    /// <summary>
    /// Duplicates the chosen database into a database of its own, leaving the source untouched. This is the "keep
    /// what is there before we work on it" action, and the only one that copies rows and stored instructions rather
    /// than just a shape.
    /// </summary>
    [RelayCommand]
    private async Task CopyDatabaseAsync()
    {
        if (!CanCopy)
        {
            return;
        }

        var target = CopyTargetName.Trim();
        IsBusy = true;
        _cancellation = new CancellationTokenSource();
        CopyStatus = $"Copying {Database} into {target}...";
        AppendLog($"Copying {Database} into {target}.");

        try
        {
            var gateway = CreateGateway();
            await gateway.ResolveServerAsync(_cancellation.Token).ConfigureAwait(true);

            var progress = new Progress<string>(message =>
            {
                CopyStatus = message;
                AppendLog(message);
            });

            var outcome = await new DatabaseBackup(gateway)
                .DuplicateAsync(Database, target, ReplaceCopyIfExists, progress, _cancellation.Token)
                .ConfigureAwait(true);

            CopyStatus = outcome.Summary;
            AppendLog(outcome.Summary);
        }
        catch (OperationCanceledException)
        {
            CopyStatus = "The copy was stopped before it finished, so it may be incomplete.";
            AppendLog("Copy stopped.");
        }
        catch (Exception exception)
        {
            CopyStatus = exception.Message;
            AppendLog("Copy failed: " + exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Checks the user name and password without moving the wizard on, so a wrong password is caught
    /// while the operator is still looking at the field.
    /// </summary>
    [RelayCommand]
    private async Task CheckSignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        SaveSettings();
        SignInGlyph = GlyphLock;
        SignInMessage = "Checking the sign-in...";

        try
        {
            var gateway = CreateGateway();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await gateway.ResolveServerAsync(cancellation.Token).ConfigureAwait(true);

            await using var connection = await gateway.OpenAsync(Database, cancellation.Token).ConfigureAwait(true);
            await connection.PingAsync(cancellation.Token).ConfigureAwait(true);

            SignInGlyph = GlyphCheck;
            SignInMessage = $"Signed in as {UserName} on {gateway.ActiveServer}. Database: {Database}.";
            SignInSummary = $"Signed in as {UserName}";
            AppendLog(SignInMessage);
        }
        catch (MySqlException mysql) when (mysql.Number == 1045)
        {
            SignInGlyph = GlyphError;
            SignInMessage = "That user name or password was not accepted. Check them with IT.";
            SignInSummary = "Sign-in failed";
            AppendLog("Sign-in refused for " + UserName + ".");
        }
        catch (MySqlException mysql) when (mysql.Number == 1049)
        {
            SignInGlyph = GlyphWarning;
            SignInMessage = $"Signed in as {UserName}, but there is no database called {Database} on that computer.";
            SignInSummary = "Signed in - database missing";
            AppendLog("Signed in, but " + Database + " does not exist.");
        }
        catch (Exception exception)
        {
            SignInGlyph = GlyphError;
            SignInMessage = "We could not reach the database computer. Ask IT whether it is switched on.";
            SignInSummary = "Sign-in failed";
            TechnicalDetails = exception.Message;
            AppendLog("Sign-in check failed: " + exception.Message);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        StepIndex = Math.Max(0, StepIndex - 1);
        ApplyStepText();
    }

    [RelayCommand]
    private void SaveDetailsCopy()
    {
        try
        {
            var directory = ReportDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"mtm-database-details-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, ComposeDetailsFile());
            ReportPath = path;
            OnPropertyChanged(nameof(HasReport));
            AppendLog("Details saved to " + path);
        }
        catch (Exception exception)
        {
            AppendLog("Could not save the details: " + exception.Message);
        }
    }

    [RelayCommand]
    private void OpenReport()
    {
        if (string.IsNullOrWhiteSpace(ReportPath) || !File.Exists(ReportPath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{ReportPath}\"") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            AppendLog("Could not open the details: " + exception.Message);
        }
    }

    private string ComposeDetailsFile()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("MTM database tool - details for IT");
        builder.AppendLine($"When: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Database: {Database}");
        builder.AppendLine(ServerDetail);
        builder.AppendLine($"Step: {StepTitle}");
        builder.AppendLine(new string('-', 90));
        builder.AppendLine(TechnicalDetails);
        builder.AppendLine(new string('-', 90));
        builder.AppendLine("Activity log");
        builder.AppendLine(LogText);
        return builder.ToString();
    }

    private void ApplyStepText()
    {
        (StepTitle, StepHint) = StepIndex switch
        {
            0 => (
                "Step 1 of 3 - Connect to the database",
                "This tool brings the MTM database up to date. It only adds what is missing - it never "
                + "removes your records."),
            1 => (
                "Step 2 of 3 - Check what will change",
                "We will look at the database and tell you in plain words what is missing. Nothing is "
                + "changed yet."),
            _ => (
                "Step 3 of 3 - Save the changes",
                "Read the list, then press Save the changes. Your existing records stay exactly as they are."),
        };
    }

    private void DescribeChanges(Model_DeploymentPlan plan)
    {
        ChangeGroups.Clear();

        var blocking = plan.Changes.Where(change => change.IsBlocking).ToList();
        var leftovers = plan.Changes.Where(change => change.IsRemoval && !change.IsBlocking).ToList();

        AddGroup(
            "Must be removed first",
            GlyphWarning,
            blocking,
            "This column only exists in the database and this tool cannot fill it in, so the standard lists "
            + "cannot be saved until it is gone.",
            isWarning: true);

        AddGroup(
            "New tables",
            GlyphAdd,
            plan.Changes.Where(change => change.Kind == Enum_ChangeKind.CreateTable).ToList(),
            "Brand-new tables. Nothing that already exists is touched.");

        AddGroup(
            "New detail fields",
            GlyphAdd,
            plan.Changes.Where(change => change.Kind == Enum_ChangeKind.AddColumn).ToList(),
            "Extra information the newest version stores. Existing rows simply have it blank.");

        AddGroup(
            "Roomier fields",
            GlyphRefresh,
            plan.Changes.Where(change => change.Kind == Enum_ChangeKind.AlterColumn).ToList(),
            "A field needs more room. Your values are kept.");

        AddGroup(
            "Speed-up rules",
            GlyphLock,
            plan.Changes.Where(change => change.Kind is Enum_ChangeKind.AddIndex or Enum_ChangeKind.AddForeignKey).ToList(),
            "Housekeeping that keeps lookups fast and keeps related records in step.");

        AddGroup(
            "Saved queries and rules",
            GlyphRefresh,
            plan.Changes.Where(change => change.Kind is Enum_ChangeKind.UpdateView
                or Enum_ChangeKind.UpdateProcedure or Enum_ChangeKind.UpdateTrigger).ToList(),
            "The stored instructions the app runs are replaced with the current version.");

        AddGroup(
            "Standard reference lists",
            GlyphList,
            plan.Changes.Where(change => change.Kind == Enum_ChangeKind.RunSeed).ToList(),
            "Lists such as dunnage types are checked and topped up. Anything you added yourself stays.");

        AddGroup(
            "Leftovers found in the database",
            GlyphWarning,
            leftovers,
            "These exist only in the database. They are left alone unless you tick the removal box.",
            isWarning: true);

        if (plan.Changes.Count == 0)
        {
            ChangeHeadline = "Everything is already up to date";
            ChangeDetail = "There is nothing to save. You can close this window.";
            SafetyLine = "Nothing to do - nothing will be changed.";
            return;
        }

        var count = plan.Changes.Count == 1 ? "1 change" : $"{plan.Changes.Count} changes";
        ChangeHeadline = blocking.Count > 0
            ? $"{count} ready - one leftover must go first"
            : $"{count} ready";

        ChangeDetail = blocking.Count > 0
            ? "The database has a leftover column this tool does not use. Tick 'Also remove leftovers' below, "
              + "then save, so the standard lists can be written."
            : "None of your existing records will be deleted.";

        UpdateSafetyLine();
    }

    private void AddGroup(
        string title,
        string glyph,
        IReadOnlyList<Model_SchemaChange> changes,
        string summary,
        bool isWarning = false)
    {
        if (changes.Count == 0)
        {
            return;
        }

        ChangeGroups.Add(new Model_ChangeGroup
        {
            Title = $"{title} ({changes.Count})",
            Glyph = glyph,
            Summary = summary,
            IsWarning = isWarning,
            Items = changes
                .Select(Describe)
                .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToList()
        });
    }

    /// <summary>Turns "Add column: dunnage_parts.quantity_type" into "Dunnage parts - quantity type".</summary>
    private static string Describe(Model_SchemaChange change)
    {
        var target = change.Target;
        if (change.Kind is Enum_ChangeKind.UpdateView
            or Enum_ChangeKind.UpdateProcedure
            or Enum_ChangeKind.UpdateTrigger
            or Enum_ChangeKind.RunSeed)
        {
            var name = target.Split('/').LastOrDefault() ?? target;
            return Humanize(Path.GetFileNameWithoutExtension(name));
        }

        var parts = target.Split('.', 2);
        return parts.Length == 2
            ? $"{Humanize(parts[0])} - {Humanize(parts[1])}"
            : Humanize(target);
    }

    private static string Humanize(string identifier)
    {
        var words = identifier
            .Replace("sp_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('_', ' ')
            .Trim();
        if (words.Length == 0)
        {
            return identifier;
        }

        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    private void DescribeResult(Model_DeploymentResult result)
    {
        HasResult = true;
        ResultIsFailure = !result.IsSuccess;

        if (!result.IsSuccess)
        {
            ResultGlyph = GlyphError;
            ResultHeadline = "Some changes could not be saved";
            ResultDetail = result.FailureCounts.Count == 1
                ? "The database refused one kind of change. Your records are untouched. Send the details to IT."
                : $"The database refused {result.FailureCounts.Count} kinds of change. Your records are untouched. "
                  + "Send the details to IT.";
            return;
        }

        ResultGlyph = GlyphCheck;
        ResultHeadline = IsRebuildMode ? "The database was rebuilt" : "All done - the database is up to date";

        // The copy is reported with the sentence the backup service produced, so a database that had nothing to
        // copy says that rather than claiming a copy exists.
        var backupNote = result.Messages.FirstOrDefault(message =>
            message.Contains(DatabaseBackup.BackupName(Database), StringComparison.OrdinalIgnoreCase));

        ResultDetail = IsRebuildMode
            ? "Every table was recreated empty and the standard lists were filled in. " + backupNote
            : $"{result.AppliedCount} item(s) saved, {result.BenignSkipCount} were already in place. "
              + "Your records were not changed. " + backupNote;
    }

    private async Task<Model_DeploymentPlan> BuildPlanAsync(Service_MySqlGateway gateway)
    {
        var comparer = new Service_SchemaComparer(gateway, _catalog);
        var progress = new Progress<string>(message =>
        {
            ProgressMessage = message;
            AppendLog(message);
        });

        return await comparer
            .BuildPlanAsync(Database, progress, _cancellation!.Token)
            .ConfigureAwait(true);
    }

    private Model_ConnectionSettings BuildSettings()
    {
        var settings = _settings.Clone();
        settings.PreferredServer = PreferredServer?.Trim() ?? string.Empty;
        settings.FallbackServer = FallbackServer?.Trim() ?? string.Empty;
        settings.Port = (int)Math.Round(Port);
        settings.Database = Database?.Trim() ?? string.Empty;
        settings.User = UserName?.Trim() ?? string.Empty;
        settings.Password = Password ?? string.Empty;
        return settings;
    }

    private Service_MySqlGateway CreateGateway() => new(BuildSettings());

    private void SaveSettings() => _settingsService.Save(BuildSettings());

    private void AppendLog(string message) =>
        LogText = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}{LogText}";

    private static string ReportDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MTM Database Tool",
        "reports");

    private string SaveReport(Model_DeploymentResult result, Service_MySqlGateway gateway)
    {
        try
        {
            var directory = ReportDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"database-update-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Database: {Database}");
            builder.AppendLine($"Server: {gateway.ActiveServer}:{gateway.Port}");
            builder.AppendLine($"Mode: {(IsRebuildMode ? "Fresh rebuild" : "Update in place")}");
            builder.AppendLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine(result.ToReportText());
            File.WriteAllText(path, builder.ToString());
            return path;
        }
        catch (Exception exception)
        {
            return "(details could not be saved: " + exception.Message + ")";
        }
    }
}
