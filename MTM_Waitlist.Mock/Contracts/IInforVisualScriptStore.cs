namespace MTM_Waitlist.Mock.Contracts;

/// <summary>
/// Loads a checked-in Infor Visual queue script from the application's content root.
/// </summary>
public interface IInforVisualScriptStore
{
    /// <summary>
    /// Loads the script at <paramref name="relativePath"/> under the content root.
    /// </summary>
    /// <returns>The script text, or an empty string when the script does not exist.</returns>
    Task<string> LoadAsync(string relativePath, CancellationToken cancellationToken = default);
}
