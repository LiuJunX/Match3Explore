using System.Collections.Generic;

namespace Match3.Core.AI;

/// <summary>
/// Result of analyzing the difficulty of a board state.
/// </summary>
public sealed class DifficultyAnalysis
{
    /// <summary>
    /// Number of valid moves available.
    /// </summary>
    public int ValidMoveCount { get; init; }

    /// <summary>
    /// Number of bomb-creating moves available.
    /// </summary>
    public int BombCreatingMoves { get; init; }

    /// <summary>
    /// Average score potential across all valid moves.
    /// </summary>
    public float AverageScorePotential { get; init; }

    /// <summary>
    /// Maximum score potential from best move.
    /// </summary>
    public long MaxScorePotential { get; init; }

    /// <summary>
    /// Average cascade depth across all valid moves.
    /// </summary>
    public float AverageCascadeDepth { get; init; }

    /// <summary>
    /// Maximum cascade depth achievable.
    /// </summary>
    public int MaxCascadeDepth { get; init; }

    /// <summary>
    /// Calculated difficulty score (0-100, higher = harder).
    /// </summary>
    public float DifficultyScore { get; init; }

    /// <summary>
    /// Difficulty category.
    /// </summary>
    public DifficultyCategory Category { get; init; }

    /// <summary>
    /// The best move available (if any).
    /// </summary>
    public Move? BestMove { get; init; }

    /// <summary>
    /// Top N moves ranked by score potential.
    /// </summary>
    public IReadOnlyList<MovePreview> TopMoves { get; init; } = System.Array.Empty<MovePreview>();

    /// <summary>
    /// Board state health indicators.
    /// </summary>
    public BoardHealth Health { get; init; } = new();
}

/// <summary>
/// Difficulty category based on analysis.
/// </summary>
public enum DifficultyCategory
{
    /// <summary>Very easy - many high-scoring moves available.</summary>
    VeryEasy,

    /// <summary>Easy - good moves available.</summary>
    Easy,

    /// <summary>Medium - some decent moves available.</summary>
    Medium,

    /// <summary>Hard - few good moves available.</summary>
    Hard,

    /// <summary>Very hard - limited options, low potential.</summary>
    VeryHard,

    /// <summary>Deadlock - no valid moves available.</summary>
    Deadlock
}

/// <summary>
/// Board health indicators for difficulty analysis.
/// </summary>
public sealed class BoardHealth
{
    /// <summary>
    /// Number of existing bombs on the board.
    /// </summary>
    public int ExistingBombs { get; init; }

    /// <summary>
    /// Tile type distribution variance (lower = more uniform).
    /// </summary>
    public float TypeDistributionVariance { get; init; }

    /// <summary>
    /// Number of isolated tiles (no adjacent same-type).
    /// </summary>
    public int IsolatedTiles { get; init; }

    /// <summary>
    /// Number of cluster groups (connected same-type tiles).
    /// </summary>
    public int ClusterCount { get; init; }

    /// <summary>
    /// Average cluster size.
    /// </summary>
    public float AverageClusterSize { get; init; }
}
