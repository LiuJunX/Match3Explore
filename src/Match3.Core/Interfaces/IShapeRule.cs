using System.Collections.Generic;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching.Generation;

namespace Match3.Core.Interfaces;

public interface IShapeRule
{
    void Detect(HashSet<Position> component, ShapeFeature features, List<DetectedShape> results);
}
