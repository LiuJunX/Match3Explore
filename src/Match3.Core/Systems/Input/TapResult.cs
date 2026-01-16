using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Input;

/// <summary>
/// Result of a tap interaction.
/// </summary>
public readonly struct TapResult
{
    /// <summary>
    /// Type of action resulting from the tap.
    /// </summary>
    public TapActionType ActionType { get; init; }

    /// <summary>
    /// Move to execute (for Swap action).
    /// </summary>
    public Move? Move { get; init; }

    /// <summary>
    /// Position for bomb activation (for ActivateBomb action).
    /// </summary>
    public Position? BombPosition { get; init; }

    /// <summary>
    /// Status message describing the result.
    /// </summary>
    public string StatusMessage { get; init; }

    public static TapResult None(string message = "No action") => new()
    {
        ActionType = TapActionType.None,
        StatusMessage = message
    };

    public static TapResult Selected(string message = "Select destination") => new()
    {
        ActionType = TapActionType.Selected,
        StatusMessage = message
    };

    public static TapResult Deselected(string message = "Selection cleared") => new()
    {
        ActionType = TapActionType.Deselected,
        StatusMessage = message
    };

    public static TapResult Swap(Move move, string message = "Swapping...") => new()
    {
        ActionType = TapActionType.Swap,
        Move = move,
        StatusMessage = message
    };

    public static TapResult ActivateBomb(Position position, string message = "Bomb activated!") => new()
    {
        ActionType = TapActionType.ActivateBomb,
        BombPosition = position,
        StatusMessage = message
    };
}

/// <summary>
/// Types of actions that can result from a tap.
/// </summary>
public enum TapActionType
{
    /// <summary>No action taken.</summary>
    None,
    /// <summary>Tile was selected.</summary>
    Selected,
    /// <summary>Selection was cleared.</summary>
    Deselected,
    /// <summary>Swap move initiated.</summary>
    Swap,
    /// <summary>Bomb activation initiated.</summary>
    ActivateBomb
}
