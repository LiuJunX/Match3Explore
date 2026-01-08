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

    Ufo,

    Square3x3,

    /// <summary>
    /// Matches with any color and clears all tiles of that color.
    /// </summary>
    Color
}
