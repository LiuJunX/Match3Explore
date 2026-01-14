using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Models.Gameplay;

namespace Match3.Core.Systems.Input;

public interface IInteractionSystem
{
    string StatusMessage { get; }
    bool TryHandleTap(ref GameState state, Position p, bool isBoardInteractive, out Move? move);
    bool TryHandleSwipe(ref GameState state, Position from, Direction direction, bool isBoardInteractive, out Move? move);
}
