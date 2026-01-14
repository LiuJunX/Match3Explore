using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Core;

public interface IAnimationSystem
{
    bool IsVisuallyStable { get; }
    bool Animate(ref GameState state, float dt);
    bool IsVisualAtTarget(in GameState state, Position p);
}
