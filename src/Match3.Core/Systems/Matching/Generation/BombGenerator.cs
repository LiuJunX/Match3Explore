using System.Collections.Generic;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;
using Match3.Random;

namespace Match3.Core.Systems.Matching.Generation;

public class BombGenerator : IBombGenerator
{
    private readonly IShapeDetector _detector;
    private readonly IBombPlacementSelector _placementSelector;

    public BombGenerator()
        : this(new ShapeDetector(), new DefaultBombPlacementSelector())
    {
    }

    public BombGenerator(IShapeDetector detector, IBombPlacementSelector placementSelector)
    {
        _detector = detector;
        _placementSelector = placementSelector;
    }

    public List<MatchGroup> Generate(HashSet<Position> component, IEnumerable<Position>? foci = null, IRandom? random = null)
    {
        // 0. Trivial Case
        if (component.Count < 3)
        {
            return Pools.ObtainList<MatchGroup>();
        }

        // 1. Detect All Candidates
        var candidates = Pools.ObtainList<DetectedShape>();

        try
        {
            _detector.DetectAll(component, candidates);

            // Handle Simple Match (No bomb candidates)
            // Only create a simple match if there's at least one valid line (3+ in a row/column)
            if (candidates.Count == 0)
            {
                // Check if component forms at least one valid line
                if (HasValidLine(component))
                {
                    return CreateSimpleMatchGroup(component);
                }
                // No valid line shape - not a match (e.g., L-shape, diagonal)
                return Pools.ObtainList<MatchGroup>();
            }

            // 2. Sort Candidates (Weight DESC, then Affinity)
            SortCandidates(candidates, foci);

            // 3. Solve Optimal Partition
            var bestIndices = Pools.ObtainList<int>();
            try
            {
                PartitionSolver.FindOptimalPartition(candidates, component, bestIndices);

                // 4. Scrap Absorption & Result Construction
                AbsorbScraps(component, candidates, bestIndices);

                // 5. Finalize Results
                return ConstructResults(candidates, bestIndices, component, foci, random);
            }
            finally
            {
                Pools.Release(bestIndices);
            }
        }
        finally
        {
            // Release detected shapes and their inner sets
            foreach(var c in candidates)
            {
                if (c.Cells != null) Pools.Release(c.Cells);
                c.Cells = null;
                Pools.Release(c);
            }
            Pools.Release(candidates);
        }
    }

    private List<MatchGroup> CreateSimpleMatchGroup(HashSet<Position> component)
    {
        var simpleGroup = Pools.Obtain<MatchGroup>();
        simpleGroup.Positions.Clear();
        foreach (var p in component) simpleGroup.Positions.Add(p);
        simpleGroup.Shape = MatchShape.Simple3;
        simpleGroup.SpawnBombType = BombType.None;
        simpleGroup.Type = TileType.None; // Set by caller
        simpleGroup.BombOrigin = null;

        var results = Pools.ObtainList<MatchGroup>();
        results.Add(simpleGroup);
        return results;
    }

    /// <summary>
    /// Check if the component contains at least one valid line (3+ consecutive in a row or column).
    /// This prevents L-shapes, diagonals, or scattered groups from being treated as matches.
    /// </summary>
    private static bool HasValidLine(HashSet<Position> component)
    {
        if (component.Count < 3) return false;

        // Get bounds
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var p in component)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        // Check horizontal lines
        for (int y = minY; y <= maxY; y++)
        {
            int consecutive = 0;
            for (int x = minX; x <= maxX + 1; x++) // +1 to check end
            {
                if (component.Contains(new Position(x, y)))
                {
                    consecutive++;
                    if (consecutive >= 3) return true;
                }
                else
                {
                    consecutive = 0;
                }
            }
        }

        // Check vertical lines
        for (int x = minX; x <= maxX; x++)
        {
            int consecutive = 0;
            for (int y = minY; y <= maxY + 1; y++) // +1 to check end
            {
                if (component.Contains(new Position(x, y)))
                {
                    consecutive++;
                    if (consecutive >= 3) return true;
                }
                else
                {
                    consecutive = 0;
                }
            }
        }

        return false;
    }

    private void SortCandidates(List<DetectedShape> candidates, IEnumerable<Position>? foci)
    {
        var fociSet = Pools.ObtainHashSet<Position>();
        if (foci != null) foreach(var f in foci) fociSet.Add(f);

        try
        {
            candidates.Sort((a, b) =>
            {
                // Primary: Weight
                int weightDiff = b.Weight.CompareTo(a.Weight);
                if (weightDiff != 0) return weightDiff;

                // Secondary: Affinity (Does it touch foci?)
                bool aTouches = a.Cells!.Overlaps(fociSet);
                bool bTouches = b.Cells!.Overlaps(fociSet);

                if (aTouches && !bTouches) return -1;
                if (!aTouches && bTouches) return 1;

                // Tertiary: Size (larger shapes preferred for same weight)
                return b.Cells!.Count.CompareTo(a.Cells!.Count);
            });
        }
        finally
        {
            Pools.Release(fociSet);
        }
    }

    private void AbsorbScraps(HashSet<Position> component, List<DetectedShape> candidates, List<int> bestIndices)
    {
        var allUsed = Pools.ObtainHashSet<Position>();
        var scraps = Pools.ObtainList<Position>();
        var unassignedScraps = Pools.ObtainHashSet<Position>();
        var toRemove = Pools.ObtainList<Position>();
        var ownerMap = Pools.Obtain<Dictionary<Position, DetectedShape>>();
        var solutionShapes = Pools.ObtainList<DetectedShape>();

        try
        {
            foreach(var idx in bestIndices) solutionShapes.Add(candidates[idx]);

            // Mark used cells
            foreach(var shape in solutionShapes)
            {
                foreach(var p in shape.Cells!) allUsed.Add(p);
            }

            // Identify scraps
            foreach(var p in component)
            {
                if (!allUsed.Contains(p)) scraps.Add(p);
            }

            // Assign scraps (BFS / Flood Fill)
            if (scraps.Count > 0 && solutionShapes.Count > 0)
            {
                foreach(var s in scraps) unassignedScraps.Add(s);

                // Map each position to its owner shape
                foreach(var shape in solutionShapes)
                {
                    foreach(var p in shape.Cells!) ownerMap[p] = shape;
                }

                bool changed = true;
                while (changed && unassignedScraps.Count > 0)
                {
                    changed = false;
                    toRemove.Clear();

                    foreach(var scrap in unassignedScraps)
                    {
                        DetectedShape? bestOwner = null;

                        // Check 4 neighbors
                        var n1 = new Position(scrap.X-1, scrap.Y);
                        var n2 = new Position(scrap.X+1, scrap.Y);
                        var n3 = new Position(scrap.X, scrap.Y-1);
                        var n4 = new Position(scrap.X, scrap.Y+1);

                        if (ownerMap.TryGetValue(n1, out var o1)) bestOwner = GetBestOwner(bestOwner, o1);
                        if (ownerMap.TryGetValue(n2, out var o2)) bestOwner = GetBestOwner(bestOwner, o2);
                        if (ownerMap.TryGetValue(n3, out var o3)) bestOwner = GetBestOwner(bestOwner, o3);
                        if (ownerMap.TryGetValue(n4, out var o4)) bestOwner = GetBestOwner(bestOwner, o4);

                        if (bestOwner != null)
                        {
                            ownerMap[scrap] = bestOwner;
                            bestOwner.Cells!.Add(scrap); // Add directly to shape
                            toRemove.Add(scrap);
                            changed = true;
                        }
                    }

                    foreach(var p in toRemove) unassignedScraps.Remove(p);
                }
            }
        }
        finally
        {
            Pools.Release(allUsed);
            Pools.Release(scraps);
            Pools.Release(unassignedScraps);
            Pools.Release(toRemove);
            ownerMap.Clear();
            Pools.Release(ownerMap);
            Pools.Release(solutionShapes);
        }
    }

    private DetectedShape? GetBestOwner(DetectedShape? currentBest, DetectedShape candidate)
    {
        if (currentBest == null) return candidate;
        return candidate.Weight > currentBest.Weight ? candidate : currentBest;
    }

    private List<MatchGroup> ConstructResults(
        List<DetectedShape> candidates,
        List<int> bestIndices,
        HashSet<Position> component,
        IEnumerable<Position>? foci,
        IRandom? random)
    {
        var results = Pools.ObtainList<MatchGroup>();
        var finalUsed = Pools.ObtainHashSet<Position>();
        var orphans = Pools.ObtainList<Position>();

        try
        {
            foreach (var idx in bestIndices)
            {
                var shape = candidates[idx];
                var group = Pools.Obtain<MatchGroup>();
                group.Positions.Clear();
                foreach (var p in shape.Cells!) group.Positions.Add(p);

                group.Shape = shape.Shape;
                group.SpawnBombType = shape.Type;
                group.Type = TileType.None; // Set by caller

                // Use placement selector for bomb origin
                group.BombOrigin = _placementSelector.SelectBombPosition(shape.Cells!, foci, random);

                results.Add(group);
            }

            // Handle Orphans (Islands not connected to any solution shape)
            foreach (var r in results)
                foreach (var p in r.Positions) finalUsed.Add(p);

            foreach (var p in component)
            {
                if (!finalUsed.Contains(p)) orphans.Add(p);
            }

            if (orphans.Count > 0)
            {
                var orphanGroup = Pools.Obtain<MatchGroup>();
                orphanGroup.Positions.Clear();
                foreach (var p in orphans) orphanGroup.Positions.Add(p);
                orphanGroup.Shape = MatchShape.Simple3;
                orphanGroup.SpawnBombType = BombType.None;
                orphanGroup.Type = TileType.None;
                orphanGroup.BombOrigin = null;
                results.Add(orphanGroup);
            }

            return results;
        }
        finally
        {
            Pools.Release(finalUsed);
            Pools.Release(orphans);
        }
    }
}
