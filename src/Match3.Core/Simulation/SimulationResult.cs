using Match3.Core.Models.Grid;

namespace Match3.Core.Simulation;

/// <summary>
/// Result of a full simulation run (RunUntilStable).
/// Contains final state and statistics.
/// </summary>
public readonly struct SimulationResult
{
    /// <summary>
    /// Number of ticks executed.
    /// </summary>
    public int TickCount { get; init; }

    /// <summary>
    /// Final game state after simulation.
    /// This is a clone, safe to modify.
    /// </summary>
    public GameState FinalState { get; init; }

    /// <summary>
    /// Whether the simulation reached a stable state.
    /// False if MaxTicksPerRun was reached.
    /// </summary>
    public bool ReachedStability { get; init; }

    /// <summary>
    /// Total elapsed simulation time in seconds.
    /// </summary>
    public float ElapsedTime { get; init; }

    /// <summary>
    /// Score gained during simulation.
    /// </summary>
    public int ScoreGained { get; init; }

    /// <summary>
    /// Number of tiles cleared during simulation.
    /// </summary>
    public int TilesCleared { get; init; }

    /// <summary>
    /// Number of matches processed during simulation.
    /// </summary>
    public int MatchesProcessed { get; init; }

    /// <summary>
    /// Number of bombs activated during simulation.
    /// </summary>
    public int BombsActivated { get; init; }

    /// <summary>
    /// Maximum cascade depth reached.
    /// </summary>
    public int MaxCascadeDepth { get; init; }
}
