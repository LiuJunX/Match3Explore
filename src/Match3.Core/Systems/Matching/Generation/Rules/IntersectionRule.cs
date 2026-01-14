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
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching.Generation.Rules;

public class IntersectionRule : IShapeRule
{
    public void Detect(HashSet<Position> component, ShapeFeature features, List<DetectedShape> results)
    {
        foreach (var hLine in features.HLines)
        {
            foreach (var vLine in features.VLines)
            {
                // Check for intersection
                bool intersects = false;
                foreach (var p in hLine)
                {
                    if (vLine.Contains(p))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (intersects)
                {
                    // L/T shape requires total count >= 5 (e.g. 3+3-1 = 5)
                    var unionCount = hLine.Count + vLine.Count - 1;
                    if (unionCount >= BombDefinitions.TNT.MinLength)
                    {
                        var shape = Pools.Obtain<DetectedShape>();
                        shape.Type = BombType.Square5x5; // Using Square5x5 as TNT/Bomb
                        shape.Weight = BombDefinitions.TNT.Weight;
                        shape.Shape = MatchShape.Cross;
                        shape.Cells = Pools.ObtainHashSet<Position>();
                        
                        foreach(var p in hLine) shape.Cells.Add(p);
                        foreach(var p in vLine) shape.Cells.Add(p);
                        
                        results.Add(shape);
                    }
                }
            }
        }
    }
}
