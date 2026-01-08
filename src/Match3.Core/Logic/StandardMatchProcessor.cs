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
        // 使用策略模式将爆炸范围计算封装到各个策略类中，便于后续扩展
        IExplosionStrategy strategy = type switch
        {
            BombType.Horizontal => new HorizontalExplosionStrategy(),
            BombType.Vertical => new VerticalExplosionStrategy(),
            BombType.Ufo => new UfoExplosionStrategy(),
            BombType.Square3x3 => new Square3x3ExplosionStrategy(),
            BombType.Color => new ColorExplosionStrategy(),
            _ => new NoneExplosionStrategy()
        };
        return strategy.GetRange(in state, cx, cy);
    }

    /// <summary>
    /// 爆炸范围计算策略接口
    /// </summary>
    private interface IExplosionStrategy
    {
        List<Position> GetRange(in GameState state, int cx, int cy);
    }

    /// <summary>
    /// 横向爆炸策略
    /// </summary>
    private sealed class HorizontalExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            var list = new List<Position>();
            for (int x = 0; x < state.Width; x++)
                list.Add(new Position(x, cy));
            return list;
        }
    }

    /// <summary>
    /// 纵向爆炸策略
    /// </summary>
    private sealed class VerticalExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            var list = new List<Position>();
            for (int y = 0; y < state.Height; y++)
                list.Add(new Position(cx, y));
            return list;
        }
    }

    private sealed class UfoExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            var candidates = new List<Position>();
            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    if (x == cx && y == cy) continue;
                    var t = state.GetTile(x, y);
                    if (t.Type != TileType.None)
                    {
                        candidates.Add(new Position(x, y));
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return new List<Position>();
            }
            int idx = state.Random.Next(0, candidates.Count);
            return new List<Position> { candidates[idx] };
        }
    }

    private sealed class Square3x3ExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            var list = new List<Position>();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx >= 0 && nx < state.Width && ny >= 0 && ny < state.Height)
                        list.Add(new Position(nx, ny));
                }
            }
            return list;
        }
    }

    /// <summary>
    /// 彩色炸弹爆炸策略（仅自身）
    /// </summary>
    private sealed class ColorExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            return new List<Position> { new(cx, cy) };
        }
    }

    /// <summary>
    /// 默认空爆炸策略
    /// </summary>
    private sealed class NoneExplosionStrategy : IExplosionStrategy
    {
        public List<Position> GetRange(in GameState state, int cx, int cy)
        {
            return new List<Position>();
        }
    }
}
