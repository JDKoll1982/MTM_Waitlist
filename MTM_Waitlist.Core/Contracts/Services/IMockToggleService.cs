namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Single mock-toggle coordinator. The app exposes ONE "Mock Data" toggle to users, but two settings keys are
/// kept in the store (<c>Feature.InforVisualMockData</c> — the master/visual key — and <c>Feature.RecvMockData</c>).
/// This service reads the master as the effective value and, on write, keeps both keys equal so every existing
/// reader sees the same value. Central to the "make both keys the same thing" decision.
/// </summary>
public interface IMockToggleService
{
    /// <summary>The master/visual settings key.</summary>
    string MasterKey { get; }

    /// <summary>The mirrored settings key kept equal to the master.</summary>
    string MirrorKey { get; }

    /// <summary>Reads the effective mock-data toggle value from the master key (defaults OFF when absent).</summary>
    Task<bool> GetEffectiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets the toggle: writes <paramref name="value"/> to BOTH the master and mirror keys.</summary>
    Task SetAsync(bool value, CancellationToken cancellationToken = default);
}
