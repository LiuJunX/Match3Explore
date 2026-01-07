namespace Match3.Core;

/// <summary>
/// Defines the type of bomb or special effect associated with a tile.
/// </summary>
public enum BombType
{
    None = 0,

    /// <summary>
    /// Clears the entire row.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Clears the entire column.
    /// </summary>
    Vertical,

    /// <summary>
    /// Clears a small cross area (e.g., center and 1 step in each direction).
    /// </summary>
    SmallCross,

    /// <summary>
    /// Clears a large square area (9x9).
    /// </summary>
    Square9x9,

    /// <summary>
    /// Matches with any color and clears all tiles of that color.
    /// </summary>
    Color
}
