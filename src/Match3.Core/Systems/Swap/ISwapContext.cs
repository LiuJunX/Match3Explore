using Match3.Core.Events;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Swap;

/// <summary>
/// Strategy interface for swap operations.
/// Encapsulates behavioral differences between Match3Engine and SimulationEngine.
/// </summary>
public interface ISwapContext
{
    /// <summary>
    /// Whether to sync Tile.Position to grid coordinates after swap.
    /// Match3Engine: false (AnimationSystem handles visual position)
    /// SimulationEngine: true (immediate sync for logic)
    /// </summary>
    bool SyncPositionOnSwap { get; }

    /// <summary>
    /// Check if swap animation is complete.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="a">First swap position.</param>
    /// <param name="b">Second swap position.</param>
    /// <param name="animationTime">Accumulated animation time.</param>
    /// <returns>True if animation is complete.</returns>
    bool IsSwapAnimationComplete(in GameState state, Position a, Position b, float animationTime);

    /// <summary>
    /// Emit revert event when invalid swap is reverted.
    /// </summary>
    void EmitRevertEvent(
        in GameState state,
        Position from,
        Position to,
        long tick,
        float simTime,
        IEventCollector events);
}
