namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Implemented by each page hosted in the shell's content frame so the shell's title-bar search box can
/// reach the page's search target without the shell knowing which page is loaded.
/// </summary>
public interface IServiceSearchHost
{
    /// <summary>The active page's search target, or <see langword="null"/> when the page cannot be searched.</summary>
    IServiceSearchTarget? SearchTarget
    {
        get;
    }
}
