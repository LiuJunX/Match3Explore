using Match3.Core.Models.Grid;

namespace Match3.Core.AI;

/// <summary>
/// Interface for AI move selection strategies.
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// Name of this strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Score a move based on preview results.
    /// Higher score = better move according to this strategy.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="move">The move being evaluated.</param>
    /// <param name="preview">Preview result from simulating the move.</param>
    /// <returns>Score for this move (higher = better).</returns>
    float ScoreMove(in GameState state, Move move, MovePreview preview);
}
