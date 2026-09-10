namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Resolves the Infor Visual SQL Server connection string.
/// </summary>
/// <remarks>
/// Environment variables win over configuration so a host can override without editing a file. Never
/// logs or exposes the resolved value.
/// </remarks>
public interface IVisualConnectionStringProvider
{
    /// <summary>Returns the resolved connection string, or an empty string when none is configured.</summary>
    string Resolve();
}
