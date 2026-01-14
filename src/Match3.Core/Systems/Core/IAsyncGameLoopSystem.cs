using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Core;

public interface IAsyncGameLoopSystem
{
    void Update(ref GameState state, float dt);
    void ActivateBomb(ref GameState state, Position p);
}
