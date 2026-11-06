using System.Runtime.ExceptionServices;

namespace Winbox.Core.Terminal;

/// <summary>
/// Lets the viewer ride out a brief guest hiccup: only several liveness failures in a row,
/// with no success in between, count as the guest really being gone.
/// </summary>
public sealed class OutageTolerance(int missesBeforeGivingUp)
{
    private int consecutiveMisses;

    public void RecordSuccess() => consecutiveMisses = 0;

    /// <summary>Counts a miss and rethrows it once there have been too many in a row.</summary>
    public void RecordMiss(GuestUnavailableException miss)
    {
        consecutiveMisses++;
        if (consecutiveMisses >= missesBeforeGivingUp) ExceptionDispatchInfo.Throw(miss);
    }
}
