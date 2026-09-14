using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Services;

/// <summary>
/// App-side implementation of <see cref="IDunnageSubstitutePicker"/>. It shows the <b>existing</b>
/// <c>SetupDunnageImageSearchDialog</c> through <see cref="ISetupDialogService"/> — the same searchable,
/// image-backed dunnage catalogue the Setup dunnage workflow already uses — and maps the part it returns onto the
/// wizard's value, so the application gains no second dunnage picker (FR-049).
/// </summary>
/// <remarks>
/// The mapped part is always <see cref="RequestDunnagePart.IsAssignedToJob"/> <c>false</c>: a part chosen here is
/// by definition one the requesting job does not carry, which is the whole point of the picker.
/// </remarks>
public sealed class DunnageSubstitutePicker : IDunnageSubstitutePicker
{
    private readonly ISetupDialogService _setupDialogService;

    public DunnageSubstitutePicker(ISetupDialogService setupDialogService)
    {
        _setupDialogService = setupDialogService;
    }

    /// <inheritdoc />
    public async Task<RequestDunnagePart?> PickSubstituteAsync(CancellationToken cancellationToken = default)
    {
        var part = await _setupDialogService.ShowDunnageImageSearchDialogAsync().ConfigureAwait(false);
        return part is null ? null : Map(part);
    }

    /// <summary>
    /// Translates a Setup-side dunnage part into the wizard's value, resolving its picture to an absolute path
    /// because the Settings side cannot see the Setup-side image root. Pure, so it is unit-testable without a
    /// dialog or a window.
    /// </summary>
    public static RequestDunnagePart Map(SetupDunnagePart part)
    {
        ArgumentNullException.ThrowIfNull(part);

        return new RequestDunnagePart
        {
            Id = part.Id ?? string.Empty,
            TypeId = part.TypeId ?? string.Empty,
            PartNumber = part.PartNumber ?? string.Empty,
            DisplayName = string.IsNullOrWhiteSpace(part.DisplayName) ? part.PartNumber ?? string.Empty : part.DisplayName,
            ImagePath = DunnageImagePathResolver.GetDisplayPath(part.ImagePath) ?? string.Empty,
            IsAssignedToJob = false,
        };
    }
}
