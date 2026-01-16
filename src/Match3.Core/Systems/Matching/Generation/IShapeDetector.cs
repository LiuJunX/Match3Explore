using System.Collections.Generic;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Matching.Generation;

/// <summary>
/// Interface for detecting bomb-generating shapes in a matched component.
/// </summary>
public interface IShapeDetector
{
    /// <summary>
    /// Detect all possible shapes in a component that can generate bombs.
    /// </summary>
    /// <param name="component">The matched cells.</param>
    /// <param name="candidates">Output list to fill with detected shapes.</param>
    void DetectAll(HashSet<Position> component, List<DetectedShape> candidates);
}
