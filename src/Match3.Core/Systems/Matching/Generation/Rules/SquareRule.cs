using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching.Generation.Rules;

public class SquareRule : IShapeRule
{
    public void Detect(HashSet<Position> component, ShapeFeature features, List<DetectedShape> results)
    {
        // 2x2 Square Detection
        // Optimized: iterate bounds
        for (int x = features.MinX; x < features.MaxX; x++)
        {
            for (int y = features.MinY; y < features.MaxY; y++)
            {
                var p00 = new Position(x, y);
                var p10 = new Position(x + 1, y);
                var p01 = new Position(x, y + 1);
                var p11 = new Position(x + 1, y + 1);

                if (component.Contains(p00) && component.Contains(p10) && 
                    component.Contains(p01) && component.Contains(p11))
                {
                    var shape = Pools.Obtain<DetectedShape>();
                    shape.Type = BombType.Ufo;
                    shape.Weight = BombDefinitions.UFO.Weight;
                    shape.Shape = MatchShape.Square;
                    shape.Cells = Pools.ObtainHashSet<Position>();
                    shape.Cells.Add(p00); shape.Cells.Add(p10);
                    shape.Cells.Add(p01); shape.Cells.Add(p11);
                    results.Add(shape);
                }
            }
        }
    }
}
