using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching;

public class ClassicMatchFinder : IMatchFinder
{
    // Local pool for MatchGroup to keep it internal and simple
    private static readonly IObjectPool<MatchGroup> _matchGroupPool = Pools.Create(
        () => new MatchGroup(),
        g => {
            g.Positions.Clear();
            g.Type = TileType.None;
            g.BombOrigin = null;
            g.SpawnBombType = BombType.None;
        }
    );

    public bool HasMatches(in GameState state)
    {
        var groups = FindMatchGroups(in state);
        bool has = groups.Count > 0;

        ReleaseGroups(groups);

        return has;
    }

    public List<MatchGroup> FindMatchGroups(in GameState state, Position? focus = null)
    {
        var groups = Pools.ObtainList<MatchGroup>();
        var visited = Pools.ObtainHashSet<Position>();
        
        try
        {
            int w = state.Width;
            int h = state.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = new Position(x, y);
                    if (visited.Contains(p)) continue;
                    
                    var type = state.GetType(x, y);
                    if (type == TileType.None || type == TileType.Rainbow || type == TileType.Bomb) continue;

                    var component = GetConnectedComponent(in state, p, type);
                    try
                    {
                        var validMatch = AnalyzeMatch(component, focus);
                        
                        if (validMatch != null)
                        {
                            validMatch.Type = type;
                            groups.Add(validMatch);
                            foreach(var mp in validMatch.Positions) visited.Add(mp);
                        }
                    }
                    finally
                    {
                        Pools.Release(component);
                    }
                }
            }
            return groups;
        }
        finally
        {
            Pools.Release(visited);
        }
    }

    private HashSet<Position> GetConnectedComponent(in GameState state, Position start, TileType type)
    {
        var component = Pools.ObtainHashSet<Position>();
        var queue = Pools.ObtainQueue<Position>();
        
        try 
        {
            queue.Enqueue(start);
            component.Add(start);
            
            while(queue.Count > 0)
            {
                var curr = queue.Dequeue();
                CheckNeighbor(state, curr.X + 1, curr.Y, type, component, queue);
                CheckNeighbor(state, curr.X - 1, curr.Y, type, component, queue);
                CheckNeighbor(state, curr.X, curr.Y + 1, type, component, queue);
                CheckNeighbor(state, curr.X, curr.Y - 1, type, component, queue);
            }
            return component;
        }
        catch
        {
            Pools.Release(component);
            throw;
        }
        finally
        {
            Pools.Release(queue);
        }
    }
    
    private void CheckNeighbor(in GameState state, int x, int y, TileType type, HashSet<Position> component, Queue<Position> queue)
    {
        if (x < 0 || x >= state.Width || y < 0 || y >= state.Height) return;
        if (state.GetType(x, y) == type)
        {
            var p = new Position(x, y);
            if (!component.Contains(p))
            {
                component.Add(p);
                queue.Enqueue(p);
            }
        }
    }

    private MatchGroup? AnalyzeMatch(HashSet<Position> component, Position? focus)
    {
        var positions = Pools.ObtainList<Position>();
        foreach(var p in component) positions.Add(p);
        
        try
        {
            if (!HasValidRun(positions)) return null;

            var group = _matchGroupPool.Get();
            // Deep copy positions because component will be released
            foreach(var p in component) group.Positions.Add(p);
            
            group.SpawnBombType = DetermineBombType(component, positions);
            
            if (focus.HasValue && component.Contains(focus.Value))
                group.BombOrigin = focus.Value;
            else
                group.BombOrigin = positions[component.Count / 2];

            return group;
        }
        finally
        {
            Pools.Release(positions);
        }
    }

    private bool HasValidRun(List<Position> positions)
    {
        positions.Sort((a, b) => {
            int c = a.Y.CompareTo(b.Y);
            if (c != 0) return c;
            return a.X.CompareTo(b.X);
        });
        
        if (CheckRuns(positions, true)) return true;
        
        positions.Sort((a, b) => {
            int c = a.X.CompareTo(b.X);
            if (c != 0) return c;
            return a.Y.CompareTo(b.Y);
        });
        
        if (CheckRuns(positions, false)) return true;

        return false;
    }

    private bool CheckRuns(List<Position> sorted, bool horizontal)
    {
        if (sorted.Count == 0) return false;
        
        int currentRun = 1;
        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i-1];
            var curr = sorted[i];
            
            bool sameLine = horizontal ? prev.Y == curr.Y : prev.X == curr.X;
            bool consecutive = horizontal ? curr.X == prev.X + 1 : curr.Y == prev.Y + 1;
            
            if (sameLine && consecutive)
            {
                currentRun++;
            }
            else
            {
                if (currentRun >= 3) return true;
                currentRun = 1;
            }
        }
        return currentRun >= 3;
    }

    private BombType DetermineBombType(HashSet<Position> component, List<Position> positions)
    {
        int count = component.Count;
        if (count < 4) return BombType.None;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach(var p in positions)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        if (count >= 5)
        {
            if (width == count || height == count)
                return BombType.Color;
            else
                return count == 5 ? BombType.Ufo : BombType.Square3x3;
        }
        else if (count == 4)
        {
            if (width == 2 && height == 2) { /* Bird? Reserved for future logic */ }
            else return (width > height) ? BombType.Vertical : BombType.Horizontal;
        }

        return BombType.None;
    }
    
    public static void ReleaseGroups(List<MatchGroup> groups)
    {
        foreach(var g in groups)
        {
            _matchGroupPool.Return(g);
        }
        Pools.Release(groups);
    }
}
