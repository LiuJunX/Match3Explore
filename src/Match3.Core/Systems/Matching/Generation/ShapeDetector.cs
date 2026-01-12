using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching.Generation.Rules;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching.Generation;

public class ShapeDetector
{
    private readonly List<IShapeRule> _rules = new();

    public ShapeDetector()
    {
        // Register Default Rules
        _rules.Add(new LineRule());
        _rules.Add(new SquareRule());
        _rules.Add(new IntersectionRule());
    }

    public void RegisterRule(IShapeRule rule)
    {
        _rules.Add(rule);
    }

    public void DetectAll(HashSet<Position> component, List<DetectedShape> candidates)
    {
        if (component == null || component.Count < 3) return;

        var features = new ShapeFeature();
        GetBounds(component, out features.MinX, out features.MaxX, out features.MinY, out features.MaxY);

        features.HLines = Pools.ObtainList<HashSet<Position>>();
        features.VLines = Pools.ObtainList<HashSet<Position>>();

        try 
        {
            ExtractLines(component, features);

            foreach (var rule in _rules)
            {
                rule.Detect(component, features, candidates);
            }
        }
        finally
        {
            // Release pooled sets
            foreach(var s in features.HLines) Pools.Release(s);
            foreach(var s in features.VLines) Pools.Release(s);
            Pools.Release(features.HLines);
            Pools.Release(features.VLines);
        }
    }

    private void GetBounds(HashSet<Position> component, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        foreach (var p in component)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }
    }

    private void ExtractLines(HashSet<Position> component, ShapeFeature features)
    {
        // Horizontal
        for (int y = features.MinY; y <= features.MaxY; y++)
        {
            for (int x = features.MinX; x <= features.MaxX; x++)
            {
                if (!component.Contains(new Position(x, y))) continue;

                int len = 0;
                while (component.Contains(new Position(x + len, y))) len++;

                if (len >= 3)
                {
                    var lineCells = Pools.ObtainHashSet<Position>();
                    for (int k = 0; k < len; k++) lineCells.Add(new Position(x + k, y));
                    features.HLines.Add(lineCells);
                    
                    // Skip processed cells to avoid duplicate lines (though loop increments by 1)
                    // The loop continues x++, so next iteration x+1 will be found.
                    // Wait, if I have O-O-O-O at x=0.
                    // Loop x=0: len=4. Add line 0,1,2,3.
                    // Loop x=1: len=3. Add line 1,2,3. -> This is redundant!
                    // I must skip x by len.
                    x += len - 1; 
                }
            }
        }

        // Vertical
        for (int x = features.MinX; x <= features.MaxX; x++)
        {
            for (int y = features.MinY; y <= features.MaxY; y++)
            {
                if (!component.Contains(new Position(x, y))) continue;

                int len = 0;
                while (component.Contains(new Position(x, y + len))) len++;

                if (len >= 3)
                {
                    var lineCells = Pools.ObtainHashSet<Position>();
                    for (int k = 0; k < len; k++) lineCells.Add(new Position(x, y + k));
                    features.VLines.Add(lineCells);
                    
                    y += len - 1;
                }
            }
        }
    }
}
