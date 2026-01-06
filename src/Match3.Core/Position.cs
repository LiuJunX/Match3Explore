namespace Match3.Core;

/// <summary>
/// Represents a 2D coordinate on the game board.
/// </summary>
public readonly struct Position
{
    /// <summary>
    /// Gets the X coordinate (column index).
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y coordinate (row index).
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
}
