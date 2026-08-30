namespace MTM_Waitlist.Module_Core.Models;

public enum ComputerGateStatus
{
    Registered,

    SkippedNoMac,

    Missing,

    RenamedMachine,

    DatabaseUnavailable
}

public sealed record ComputerGateCheck(ComputerGateStatus Status, ComputerRecord? ExistingComputer = null);
