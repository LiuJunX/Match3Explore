using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.PowerUps.Effects;

public class UfoEffect : IBombEffect
{
    public BombType Type => BombType.Ufo;

    public void Apply(in GameState state, Position origin, HashSet<Position> affectedTiles)
    {
        // UFO targets 1 random tile (usually a goal objective or special tile)
        // Here we just pick a random non-empty tile.
        
        var candidates = Pools.ObtainList<Position>();
        try
        {
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    if (x == origin.X && y == origin.Y) continue;
                    
                    var t = state.GetTile(x, y);
                    if (t.Type != TileType.None)
                    {
                        candidates.Add(new Position(x, y));
                    }
                }
            }

            if (candidates.Count > 0)
            {
                int idx = state.Random.Next(0, candidates.Count);
                affectedTiles.Add(candidates[idx]);
            }
        }
        finally
        {
            Pools.Release(candidates);
        }
    }
}
