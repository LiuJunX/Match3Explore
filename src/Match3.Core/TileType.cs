namespace Match3.Core;

/// <summary>
/// Defines the types (colors) of tiles available in the game.
/// </summary>
public enum TileType
{
    /// <summary>
    /// Represents an empty space or cleared tile.
    /// </summary>
    None = 0,

    /// <summary>
    /// Red color tile.
    /// </summary>
    Red,

    /// <summary>
    /// Green color tile.
    /// </summary>
    Green,

    /// <summary>
    /// Blue color tile.
    /// </summary>
    Blue,

    /// <summary>
    /// Yellow color tile.
    /// </summary>
    Yellow,

    /// <summary>
    /// Purple color tile.
    /// </summary>
    Purple,

    /// <summary>
    /// Orange color tile.
    /// </summary>
    Orange,

    /// <summary>
    /// Special Rainbow tile (Color Bomb). Matches with any color.
    /// </summary>
    Rainbow
}
