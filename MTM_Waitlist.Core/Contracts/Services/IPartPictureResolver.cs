namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The reader every surface that draws a part calls, so the surfaces depend on this contract rather than on the
/// resolver that answers it.
/// </summary>
/// <remarks>
/// <para>
/// The implementation lives beside the other image services in <c>MTM_Waitlist.Settings</c>, because that is where
/// the picture store, the storage service and the configured root already live. This declaration sits in
/// <c>MTM_Waitlist.Core</c> so the waitlist libraries, Setup and the app can all depend on it without depending on
/// each other.
/// </para>
/// <para>
/// Everything here is answered in terms of a scope and a part number, both plain strings, because a project that
/// references nothing cannot reference this feature's own model types either.
/// </para>
/// </remarks>
public interface IPartPictureResolver
{
    /// <summary>
    /// The picture file a control draws for one part: this machine's copy when it holds one, otherwise the source
    /// on the share, otherwise nothing.
    /// </summary>
    /// <param name="scope">The part's store scope: <c>visual_part</c> or <c>wip_part</c>.</param>
    /// <param name="partNumber">The part number.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The absolute path of a picture the application accepts, or <see langword="null"/> when there is no picture,
    /// or none the acceptance rule allows. A caller that receives null draws the one shared placeholder.
    /// </returns>
    /// <remarks>
    /// This is a drawing decision, not a data one: a part with no picture and a share that cannot be reached both
    /// answer null, and neither is reported from here as a fault. A surface resolves this while it loads and holds
    /// the answer on its model, which is how a card can draw synchronously.
    /// </remarks>
    Task<string?> ResolvePartPicturePathAsync(string scope, string partNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// The path a part's picture is stored at, relative to the configured picture root.
    /// </summary>
    /// <param name="scope">The part's store scope: <c>visual_part</c> or <c>wip_part</c>.</param>
    /// <param name="partNumber">The part number.</param>
    /// <param name="extension">The picture's own extension, including the dot.</param>
    /// <returns>The relative path the row would hold, such as <c>Visual/MMC/MMC0001000.png</c>.</returns>
    string? ResolveRelativeStoredPath(string scope, string partNumber, string extension);
}
