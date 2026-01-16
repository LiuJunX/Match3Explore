using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Models.Gameplay;

namespace Match3.Core.Systems.Input;

public interface IInteractionSystem
{
    string StatusMessage { get; }

    /// <summary>
    /// Handle a tap interaction. Returns detailed result including bomb activation.
    /// </summary>
    TapResult HandleTap(ref GameState state, Position p, bool isBoardInteractive);

    /// <summary>
    /// Handle a swipe interaction.
    /// </summary>
    bool TryHandleSwipe(ref GameState state, Position from, Direction direction, bool isBoardInteractive, out Move? move);

    /// <summary>
    /// Legacy method for backward compatibility.
    /// </summary>
    bool TryHandleTap(ref GameState state, Position p, bool isBoardInteractive, out Move? move);
}
