using Microsoft.Win32;
using MTM_Waitlist.Mock.Service.Contracts;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The per-user <c>Run</c> key registration used to start the service at logon (FR-007).
/// </summary>
/// <remarks>
/// <para>
/// The key is <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>, written for the <b>current
/// user only</b>: the service must not need elevation, and it must never register itself machine-wide
/// behind an operator's back (research.md R4).
/// </para>
/// <para>
/// Nothing here reads or writes any other value in the key, so unrelated startup entries are never
/// disturbed.
/// </para>
/// </remarks>
public sealed class CurrentUserRunRegistrationStore : IStartupRegistrationStore
{
    /// <summary>Value name of the service's auto-start entry.</summary>
    public const string ValueName = "MTM_Waitlist.Mock.Service";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <inheritdoc />
    public string? GetRegisteredCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string;
    }

    /// <inheritdoc />
    public void SetRegisteredCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException($"The per-user Run key could not be opened for writing: HKCU\\{RunKeyPath}.");

        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    /// <inheritdoc />
    public void RemoveRegistration()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is null)
        {
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
