using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Random;

namespace Match3.Core.Systems.Matching.Generation;

/// <summary>
/// Interface for selecting the position where a bomb should spawn.
/// </summary>
public interface IBombPlacementSelector
{
    /// <summary>
    /// Select the position where the bomb should spawn within a shape.
    /// </summary>
    /// <param name="shapeCells">All cells in the shape.</param>
    /// <param name="foci">Player interaction positions (swap from/to).</param>
    /// <param name="random">Random number generator for tie-breaking.</param>
    /// <returns>The position where the bomb should spawn, or null if no bomb.</returns>
    Position? SelectBombPosition(HashSet<Position> shapeCells, IEnumerable<Position>? foci, IRandom? random);
}

/// <summary>
/// Default implementation that prioritizes player interaction positions.
/// </summary>
public class DefaultBombPlacementSelector : IBombPlacementSelector
{
    /// <inheritdoc />
    public Position? SelectBombPosition(HashSet<Position> shapeCells, IEnumerable<Position>? foci, IRandom? random)
    {
        if (shapeCells.Count == 0)
            return null;

        // Priority 1: Player operation positions (foci)
        var matchingFoci = new List<Position>();
        if (foci != null)
        {
            foreach (var f in foci)
            {
                if (shapeCells.Contains(f))
                {
                    matchingFoci.Add(f);
                }
            }
        }

        if (matchingFoci.Count == 1)
        {
            return matchingFoci[0];
        }
        else if (matchingFoci.Count > 1)
        {
            if (random != null)
            {
                int idx = random.Next(0, matchingFoci.Count);
                return matchingFoci[idx];
            }
            return matchingFoci[0];
        }

        // Priority 2: Random position from shape cells (sorted for determinism)
        var cellList = new List<Position>(shapeCells);
        cellList.Sort((a, b) =>
        {
            int cmp = a.Y.CompareTo(b.Y);
            return cmp != 0 ? cmp : a.X.CompareTo(b.X);
        });

        if (random != null)
        {
            int idx = random.Next(0, cellList.Count);
            return cellList[idx];
        }

        return cellList[0];
    }
}
