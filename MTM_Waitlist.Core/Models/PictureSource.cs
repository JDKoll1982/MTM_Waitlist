namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// One of a machine's picture sources: the kind of source, and the folder it reads from.
/// </summary>
/// <param name="Kind">
/// Which source this is, as one of the <see cref="MachineConfigurationSourceKinds"/> values
/// (<c>shared_folder</c>, <c>keys_folder</c>, <c>dunnage_root</c>). The kind is what makes the machine's three
/// rows three rows rather than one, so it is part of the source's identity and not a label.
/// </param>
/// <param name="Path">The folder this machine reads that kind of picture from.</param>
/// <remarks>
/// <b>Name collision, and why this keeps its contract name anyway.</b> A static helper class named
/// <c>PictureSource</c> already exists at <c>MTM_Waitlist.Shared/Helpers/PictureSource.cs</c> in namespace
/// <c>MTM_Waitlist.Module_Shared.Helpers</c>, and it is what a converter calls to turn a path into something a
/// control can draw. This record deliberately keeps the name the contract gives it
/// (<c>contracts/machine-configuration-contract.md</c> section 1), because the setup screen and the launch steps
/// consume it under that name.
/// <para>
/// The two are only ambiguous in a file that imports <b>both</b> namespaces <i>and</i> names
/// <c>PictureSource</c> unqualified. No file in the tree does that today: every consumer of the helper
/// (<c>ResolvedImagePathToSourceConverter</c>, <c>SetupImagePathToImageSourceConverter</c>,
/// <c>StringToImageSourceConverter</c>, <c>WaitlistViewPage.xaml.cs</c>) imports the helpers namespace and not
/// this one. A file that genuinely needs both must alias one of them, for example
/// <c>using SharedPictureSource = MTM_Waitlist.Module_Shared.Helpers.PictureSource;</c>. This is a footgun to be
/// aware of rather than a hazard that was left in place unexamined.
/// </para>
/// </remarks>
public sealed record PictureSource(
    string Kind,
    string Path);
