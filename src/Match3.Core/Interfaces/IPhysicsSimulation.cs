using Match3.Core.Models.Grid;

namespace Match3.Core.Interfaces;

public interface IPhysicsSimulation
{
    void Update(ref GameState state, float deltaTime);
    bool IsStable(in GameState state);
}
