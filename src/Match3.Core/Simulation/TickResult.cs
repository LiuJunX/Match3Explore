namespace Match3.Core.Simulation;

/// <summary>
/// Result of a single simulation tick.
/// Provides status information for the caller.
/// </summary>
public readonly struct TickResult
{
    /// <summary>
    /// Current tick number after this tick.
    /// </summary>
    public long CurrentTick { get; init; }

    /// <summary>
    /// Total elapsed simulation time in seconds.
    /// </summary>
    public float ElapsedTime { get; init; }

    /// <summary>
    /// Whether the simulation has reached a stable state.
    /// Stable = no falling tiles, no active projectiles, no pending matches.
    /// </summary>
    public bool IsStable { get; init; }

    /// <summary>
    /// Whether there are active projectiles in flight.
    /// </summary>
    public bool HasActiveProjectiles { get; init; }

    /// <summary>
    /// Whether there are tiles currently falling.
    /// </summary>
    public bool HasFallingTiles { get; init; }

    /// <summary>
    /// Whether there are pending matches to process.
    /// </summary>
    public bool HasPendingMatches { get; init; }

    /// <summary>
    /// Time step used for this tick.
    /// </summary>
    public float DeltaTime { get; init; }
}
