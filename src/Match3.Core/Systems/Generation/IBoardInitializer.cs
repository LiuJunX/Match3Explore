using Match3.Core.Config;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Generation;

public interface IBoardInitializer
{
    void Initialize(ref GameState state, LevelConfig? levelConfig);
}
