namespace Match3.Core.Simulation;

/// <summary>
/// Configuration for the simulation engine.
/// Controls tick rate, limits, and behavior.
/// </summary>
public class SimulationConfig
{
    /// <summary>
    /// Default time step (60 FPS).
    /// </summary>
    public const float DefaultFixedDeltaTime = 1f / 60f;

    /// <summary>
    /// Fixed time step in seconds.
    /// Default: 0.01667s (60 FPS).
    /// AI mode can use larger values for faster simulation.
    /// </summary>
    public float FixedDeltaTime { get; set; } = DefaultFixedDeltaTime;

    /// <summary>
    /// Maximum ticks per RunUntilStable call.
    /// Safety limit to prevent infinite loops.
    /// </summary>
    public int MaxTicksPerRun { get; set; } = 10000;

    /// <summary>
    /// Whether to emit events during simulation.
    /// Set to false for AI high-speed simulation.
    /// </summary>
    public bool EmitEvents { get; set; } = true;

    /// <summary>
    /// Time scale multiplier for presentation.
    /// 1.0 = normal speed, 2.0 = 2x speed, etc.
    /// </summary>
    public float TimeScale { get; set; } = 1.0f;

    /// <summary>
    /// Creates a config optimized for AI simulation.
    /// Uses larger time step and disables events.
    /// </summary>
    public static SimulationConfig ForAI()
    {
        return new SimulationConfig
        {
            FixedDeltaTime = 0.1f,  // 10 FPS equivalent, 6x faster
            EmitEvents = false,
            MaxTicksPerRun = 50000
        };
    }

    /// <summary>
    /// Creates a config for human play with full events.
    /// </summary>
    public static SimulationConfig ForHumanPlay()
    {
        return new SimulationConfig
        {
            FixedDeltaTime = DefaultFixedDeltaTime,
            EmitEvents = true,
            TimeScale = 1.0f
        };
    }
}
