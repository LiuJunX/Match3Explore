using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching.Generation.Rules;

public class LineRule : IShapeRule
{
    public void Detect(HashSet<Position> component, ShapeFeature features, List<DetectedShape> results)
    {
        // Check Horizontal Lines
        foreach (var line in features.HLines)
        {
            int len = line.Count;
            if (len >= BombDefinitions.Rainbow.MinLength) // 5
            {
                CreateShape(results, BombType.Color, BombDefinitions.Rainbow.Weight, MatchShape.Line5, line, true);
            }
            else if (len >= BombDefinitions.Rocket.MinLength) // 4
            {
                CreateShape(results, BombType.Vertical, BombDefinitions.Rocket.Weight, MatchShape.Line4Horizontal, line, true);
            }
        }

        // Check Vertical Lines
        foreach (var line in features.VLines)
        {
            int len = line.Count;
            if (len >= BombDefinitions.Rainbow.MinLength) // 5
            {
                CreateShape(results, BombType.Color, BombDefinitions.Rainbow.Weight, MatchShape.Line5, line, false);
            }
            else if (len >= BombDefinitions.Rocket.MinLength) // 4
            {
                CreateShape(results, BombType.Horizontal, BombDefinitions.Rocket.Weight, MatchShape.Line4Vertical, line, false);
            }
        }
    }

    private void CreateShape(List<DetectedShape> results, BombType type, int weight, MatchShape matchShape, HashSet<Position> line, bool isHorizontal)
    {
        var shape = Pools.Obtain<DetectedShape>();
        shape.Type = type;
        shape.Weight = weight;
        shape.Shape = matchShape;
        shape.Cells = Pools.ObtainHashSet<Position>();
        foreach(var p in line) shape.Cells.Add(p);
        results.Add(shape);
    }
}
