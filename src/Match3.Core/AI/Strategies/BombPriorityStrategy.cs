using Match3.Core.Models.Grid;

namespace Match3.Core.AI.Strategies;

/// <summary>
/// Strategy that prioritizes creating and using bombs.
/// </summary>
public sealed class BombPriorityStrategy : IAIStrategy
{
    /// <inheritdoc />
    public string Name => "BombPriority";

    /// <inheritdoc />
    public float ScoreMove(in GameState state, Move move, MovePreview preview)
    {
        if (!preview.IsValidMove) return -1000f;

        float score = 0;

        // High bonus for bomb activations
        score += preview.BombsActivated * 500f;

        // Check if move creates a bomb (4+ match)
        // This would require checking the final state for new bombs
        // For now, use cascade depth as a proxy for complex moves
        if (preview.MaxCascadeDepth >= 2)
        {
            score += 200f; // Likely created special tiles
        }

        // Bonus for large matches (potential bomb creation)
        if (preview.TilesCleared >= 4)
        {
            score += 100f;
        }

        // Secondary: score gained
        score += preview.ScoreGained * 0.5f;

        // Bonus for cascades
        score += preview.MaxCascadeDepth * 75f;

        return score;
    }
}
