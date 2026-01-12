using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.PowerUps.Effects;

public class AreaBombEffect : IBombEffect
{
    public BombType Type => BombType.Area;

    public void Apply(in GameState state, Position origin, HashSet<Position> affectedTiles)
    {
        // 3x3 Area
        for (int y = origin.Y - 1; y <= origin.Y + 1; y++)
        {
            for (int x = origin.X - 1; x <= origin.X + 1; x++)
            {
                if (x >= 0 && x < state.Width && y >= 0 && y < state.Height)
                {
                    affectedTiles.Add(new Position(x, y));
                }
            }
        }
    }
}
