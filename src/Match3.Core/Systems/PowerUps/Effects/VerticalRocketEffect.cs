using System.Collections.Generic;
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

namespace Match3.Core.Systems.PowerUps.Effects;

public class VerticalRocketEffect : IBombEffect
{
    public BombType Type => BombType.Vertical;

    public void Apply(in GameState state, Position origin, HashSet<Position> affectedTiles)
    {
        for (int y = 0; y < state.Height; y++)
        {
            affectedTiles.Add(new Position(origin.X, y));
        }
    }
}
