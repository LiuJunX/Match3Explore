using Match3.Core.Config;
using Match3.Core.Systems.Core;
using Match3.Core.Systems.Generation;
using Match3.Core.Systems.Input;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Physics;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Scoring;
using Match3.Core.View;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Generation;

public class BoardInitializer : IBoardInitializer
{
    private readonly ITileGenerator _tileGenerator;

    public BoardInitializer(ITileGenerator tileGenerator)
    {
        _tileGenerator = tileGenerator;
    }

    public void Initialize(ref GameState state, LevelConfig? levelConfig)
    {
        if (levelConfig != null)
        {
            // Initialize difficulty settings from level config
            state.MoveLimit = levelConfig.MoveLimit;
            state.TargetDifficulty = levelConfig.TargetDifficulty;

            for (int i = 0; i < levelConfig.Grid.Length; i++)
            {
                int x = i % levelConfig.Width;
                int y = i / levelConfig.Width;
                
                if (x < state.Width && y < state.Height)
                {
                    var type = levelConfig.Grid[i];
                    var bomb = BombType.None;
                    if (levelConfig.Bombs != null && i < levelConfig.Bombs.Length)
                    {
                        bomb = levelConfig.Bombs[i];
                    }
                    state.SetTile(x, y, new Tile(state.NextTileId++, type, x, y, bomb));
                }
            }
        }
        else
        {
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    var type = _tileGenerator.GenerateNonMatchingTile(ref state, x, y);
                    state.SetTile(x, y, new Tile(state.NextTileId++, type, x, y));
                }
            }
        }
    }
}
