using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Structs;

namespace Match3.Core.Logic;

public class StandardMatchProcessor : IMatchProcessor
{
    public int ProcessMatches(ref GameState state, List<MatchGroup> groups)
    {
        int points = 0;
        var tilesToClear = new HashSet<Position>();
        var protectedTiles = new HashSet<Position>();

        foreach (var g in groups)
        {
            points += g.Positions.Count * 10;
            
            foreach (var p in g.Positions)
            {
                tilesToClear.Add(p);
            }

            if (g.SpawnBombType != BombType.None && g.BombOrigin.HasValue)
            {
                var p = g.BombOrigin.Value;
                tilesToClear.Remove(p);
                protectedTiles.Add(p);
                
                var newType = g.SpawnBombType == BombType.Color ? TileType.Rainbow : g.Type;
                state.SetTile(p.X, p.Y, new Tile(state.NextTileId++, newType, p.X, p.Y, g.SpawnBombType));
            }
        }

        var queue = new Queue<Position>(tilesToClear);
        var cleared = new HashSet<Position>();

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (protectedTiles.Contains(p)) continue;
            if (cleared.Contains(p)) continue;

            var t = state.GetTile(p.X, p.Y);
            if (t.Type == TileType.None) continue;

            cleared.Add(p);

            if (t.Bomb != BombType.None)
            {
                var explosionRange = GetExplosionRange(in state, p.X, p.Y, t.Bomb);
                foreach (var exP in explosionRange)
                {
                    if (!cleared.Contains(exP))
                        queue.Enqueue(exP);
                }
            }
            
            state.SetTile(p.X, p.Y, new Tile(0, TileType.None, p.X, p.Y));
        }
        
        return points;
    }
    
    private List<Position> GetExplosionRange(in GameState state, int cx, int cy, BombType type)
    {
        var list = new List<Position>();
        int w = state.Width;
        int h = state.Height;

        switch (type)
        {
            case BombType.Horizontal:
                for (int x = 0; x < w; x++) list.Add(new Position(x, cy));
                break;
            case BombType.Vertical:
                for (int y = 0; y < h; y++) list.Add(new Position(cx, y));
                break;
            case BombType.SmallCross:
                list.Add(new Position(cx, cy));
                if (cx > 0) list.Add(new Position(cx - 1, cy));
                if (cx < w - 1) list.Add(new Position(cx + 1, cy));
                if (cy > 0) list.Add(new Position(cx, cy - 1));
                if (cy < h - 1) list.Add(new Position(cx, cy + 1));
                break;
            case BombType.Square9x9:
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                            list.Add(new Position(nx, ny));
                    }
                }
                break;
            case BombType.Color:
                list.Add(new Position(cx, cy));
                break;
        }
        return list;
    }
}
