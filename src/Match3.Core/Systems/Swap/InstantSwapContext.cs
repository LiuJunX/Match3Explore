using Match3.Core.Events;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Swap;

/// <summary>
/// Swap context for SimulationEngine with timer-based animation completion.
/// Syncs position immediately and emits events for presentation layer.
/// </summary>
public sealed class InstantSwapContext : ISwapContext
{
    private readonly float _swapAnimationDuration;

    /// <summary>
    /// Creates a new instant swap context.
    /// </summary>
    /// <param name="swapAnimationDuration">Duration to wait before validating swap (matches EventInterpreter.MoveDuration).</param>
    public InstantSwapContext(float swapAnimationDuration = 0.15f)
    {
        _swapAnimationDuration = swapAnimationDuration;
    }

    /// <summary>
    /// SimulationEngine syncs position immediately for logic consistency.
    /// </summary>
    public bool SyncPositionOnSwap => true;

    /// <summary>
    /// Check animation completion via timer.
    /// </summary>
    public bool IsSwapAnimationComplete(in GameState state, Position a, Position b, float animationTime)
    {
        return animationTime >= _swapAnimationDuration;
    }

    /// <summary>
    /// Emit TilesSwappedEvent with IsRevert=true for presentation layer.
    /// </summary>
    public void EmitRevertEvent(
        in GameState state,
        Position from,
        Position to,
        int tick,
        float simTime,
        IEventCollector events)
    {
        if (!events.IsEnabled)
            return;

        // After swap back: tiles are at their original positions
        var tileA = state.GetTile(from.X, from.Y);
        var tileB = state.GetTile(to.X, to.Y);

        events.Emit(new TilesSwappedEvent
        {
            Tick = tick,
            SimulationTime = simTime,
            TileAId = tileA.Id,
            TileBId = tileB.Id,
            PositionA = to,   // Tile A moves from 'to' back to 'from'
            PositionB = from, // Tile B moves from 'from' back to 'to'
            IsRevert = true
        });
    }
}
