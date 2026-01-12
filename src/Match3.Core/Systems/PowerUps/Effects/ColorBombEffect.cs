using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.PowerUps.Effects;

public class ColorBombEffect : IBombEffect
{
    public BombType Type => BombType.Color;

    public void Apply(in GameState state, Position origin, HashSet<Position> affectedTiles)
    {
        // Strategy: Find the most frequent color on the board and clear it.
        // This is the fallback behavior when a Color Bomb is exploded by another bomb
        // rather than being swapped by a player.
        
        var counts = Pools.Obtain<Dictionary<TileType, int>>();
        
        try
        {
            // 1. Count colors
            for (int i = 0; i < state.Grid.Length; i++)
            {
                var t = state.Grid[i];
                if (t.Type != TileType.None && t.Type != TileType.Rainbow && t.Type != TileType.Bomb)
                {
                    if (!counts.ContainsKey(t.Type)) counts[t.Type] = 0;
                    counts[t.Type]++;
                }
            }
            
            // 2. Find max
            TileType maxType = TileType.None;
            int maxCount = -1;
            foreach(var kvp in counts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    maxType = kvp.Key;
                }
            }
            
            // 3. Mark for clearing
            if (maxType != TileType.None)
            {
                for (int y = 0; y < state.Height; y++)
                {
                    for (int x = 0; x < state.Width; x++)
                    {
                        if (state.GetType(x, y) == maxType)
                        {
                            affectedTiles.Add(new Position(x, y));
                        }
                    }
                }
            }
        }
        finally
        {
            counts.Clear();
            Pools.Release(counts);
        }
    }
}
