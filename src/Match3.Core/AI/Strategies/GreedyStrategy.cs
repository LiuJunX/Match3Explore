using Match3.Core.Models.Grid;

namespace Match3.Core.AI.Strategies;

/// <summary>
/// Greedy strategy that maximizes immediate score.
/// </summary>
public sealed class GreedyStrategy : IAIStrategy
{
    /// <inheritdoc />
    public string Name => "Greedy";

    /// <inheritdoc />
    public float ScoreMove(in GameState state, Move move, MovePreview preview)
    {
        if (!preview.IsValidMove) return -1000f;

        // Primary: score gained
        float score = preview.ScoreGained;

        // Bonus for cascades
        score += preview.MaxCascadeDepth * 50f;

        // Bonus for tiles cleared
        score += preview.TilesCleared * 10f;

        return score;
    }
}
