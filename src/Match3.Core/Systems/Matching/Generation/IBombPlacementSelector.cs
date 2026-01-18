using System;
using System.Collections.Generic;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;
using Match3.Random;

namespace Match3.Core.Systems.Matching.Generation;

/// <summary>
/// Static comparer for Position sorting (Y first, then X).
/// </summary>
internal static class PositionComparers
{
    /// <summary>
    /// Compare positions by Y ascending, then X ascending (deterministic ordering).
    /// </summary>
    public static readonly Comparison<Position> ByYThenX = static (a, b) =>
    {
        int cmp = a.Y.CompareTo(b.Y);
        return cmp != 0 ? cmp : a.X.CompareTo(b.X);
    };
}

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
        var matchingFoci = Pools.ObtainList<Position>();
        try
        {
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
            var cellList = Pools.ObtainList<Position>();
            try
            {
                foreach (var p in shapeCells)
                {
                    cellList.Add(p);
                }
                cellList.Sort(PositionComparers.ByYThenX);

                if (random != null)
                {
                    int idx = random.Next(0, cellList.Count);
                    return cellList[idx];
                }

                return cellList[0];
            }
            finally
            {
                Pools.Release(cellList);
            }
        }
        finally
        {
            Pools.Release(matchingFoci);
        }
    }
}
