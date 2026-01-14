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

public class HorizontalRocketEffect : IBombEffect
{
    public BombType Type => BombType.Horizontal;

    public void Apply(in GameState state, Position origin, HashSet<Position> affectedTiles)
    {
        for (int x = 0; x < state.Width; x++)
        {
            affectedTiles.Add(new Position(x, origin.Y));
        }
    }
}
