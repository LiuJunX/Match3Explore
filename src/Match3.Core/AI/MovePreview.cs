using Match3.Core.Models.Grid;

namespace Match3.Core.AI;

/// <summary>
/// Result of previewing a move through simulation.
/// </summary>
public sealed class MovePreview
{
    /// <summary>
    /// The move that was previewed.
    /// </summary>
    public Move Move { get; init; }

    /// <summary>
    /// Number of ticks until stable state.
    /// </summary>
    public int TickCount { get; init; }

    /// <summary>
    /// Score gained from this move.
    /// </summary>
    public long ScoreGained { get; init; }

    /// <summary>
    /// Number of tiles cleared.
    /// </summary>
    public int TilesCleared { get; init; }

    /// <summary>
    /// Number of matches processed.
    /// </summary>
    public int MatchesProcessed { get; init; }

    /// <summary>
    /// Number of bombs activated.
    /// </summary>
    public int BombsActivated { get; init; }

    /// <summary>
    /// Maximum cascade depth achieved.
    /// </summary>
    public int MaxCascadeDepth { get; init; }

    /// <summary>
    /// Whether the move resulted in a valid match.
    /// </summary>
    public bool IsValidMove => TilesCleared > 0 || MatchesProcessed > 0;

    /// <summary>
    /// Final state after the move (cloned).
    /// </summary>
    public GameState? FinalState { get; init; }
}

/// <summary>
/// Represents a potential move (swap two adjacent tiles).
/// </summary>
public readonly struct Move
{
    /// <summary>
    /// Position of the first tile.
    /// </summary>
    public Position From { get; init; }

    /// <summary>
    /// Position of the second tile.
    /// </summary>
    public Position To { get; init; }

    /// <summary>
    /// Creates a new move.
    /// </summary>
    public Move(Position from, Position to)
    {
        From = from;
        To = to;
    }

    /// <summary>
    /// Whether this is a horizontal swap.
    /// </summary>
    public bool IsHorizontal => From.Y == To.Y;

    /// <summary>
    /// Whether this is a vertical swap.
    /// </summary>
    public bool IsVertical => From.X == To.X;

    /// <inheritdoc />
    public override string ToString() => $"Move({From} -> {To})";

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Move other &&
        ((From == other.From && To == other.To) ||
         (From == other.To && To == other.From));

    /// <inheritdoc />
    public override int GetHashCode() =>
        From.GetHashCode() ^ To.GetHashCode();

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}
