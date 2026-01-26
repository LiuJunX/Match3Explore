using Match3.Core.Events;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Swap;

/// <summary>
/// Shared swap operations interface.
/// Used by both Match3Engine and SimulationEngine.
/// </summary>
public interface ISwapOperations
{
    /// <summary>
    /// Swap two tiles in the grid.
    /// </summary>
    /// <param name="state">Game state to modify.</param>
    /// <param name="a">First position.</param>
    /// <param name="b">Second position.</param>
    void SwapTiles(ref GameState state, Position a, Position b);

    /// <summary>
    /// Check if a position has a match.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="p">Position to check.</param>
    /// <returns>True if position is part of a match.</returns>
    bool HasMatch(in GameState state, Position p);

    /// <summary>
    /// Validate a pending move after animation completes.
    /// Reverts the swap if no match was created.
    /// </summary>
    /// <param name="state">Game state to modify.</param>
    /// <param name="pending">Pending move state.</param>
    /// <param name="deltaTime">Time since last update.</param>
    /// <param name="tick">Current simulation tick.</param>
    /// <param name="simTime">Current simulation time.</param>
    /// <param name="events">Event collector for revert events.</param>
    /// <returns>True if validation is complete (move accepted or reverted), false if still waiting.</returns>
    bool ValidatePendingMove(
        ref GameState state,
        ref PendingMoveState pending,
        float deltaTime,
        int tick,
        float simTime,
        IEventCollector events);
}
