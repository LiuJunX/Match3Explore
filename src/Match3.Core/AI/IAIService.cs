using System.Collections.Generic;
using Match3.Core.Models.Grid;

namespace Match3.Core.AI;

/// <summary>
/// AI service for move evaluation and difficulty analysis.
/// Uses high-speed simulation for predictions.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Get all valid moves for the current state.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>List of valid moves.</returns>
    IReadOnlyList<Move> GetValidMoves(in GameState state);

    /// <summary>
    /// Evaluate the current state (board quality score).
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>Evaluation score (higher = better position).</returns>
    float EvaluateState(in GameState state);

    /// <summary>
    /// Preview a move by simulating until stable.
    /// Uses high-speed simulation (no events).
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="move">Move to preview.</param>
    /// <returns>Preview result with predicted outcomes.</returns>
    MovePreview PreviewMove(in GameState state, Move move);

    /// <summary>
    /// Get the best move according to the current strategy.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>Best move, or null if no valid moves.</returns>
    Move? GetBestMove(in GameState state);

    /// <summary>
    /// Analyze the difficulty of the current board state.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>Difficulty analysis result.</returns>
    DifficultyAnalysis AnalyzeDifficulty(in GameState state);

    /// <summary>
    /// Set the AI strategy to use.
    /// </summary>
    /// <param name="strategy">Strategy to use for move selection.</param>
    void SetStrategy(IAIStrategy strategy);

    /// <summary>
    /// Get all move previews for analysis.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <returns>List of all move previews.</returns>
    IReadOnlyList<MovePreview> GetAllMovePreviews(in GameState state);
}
