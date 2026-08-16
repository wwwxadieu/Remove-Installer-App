namespace UnInstall.Models;

public enum ForceDeleteOutcome
{
    /// <summary>Deleted immediately (possibly after clearing attributes/ACLs).</summary>
    Deleted,

    /// <summary>Still locked by another process; scheduled for deletion at next Windows startup.</summary>
    ScheduledForReboot,

    /// <summary>Could not be deleted or scheduled for deletion.</summary>
    Failed,
}
