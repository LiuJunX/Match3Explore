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
                // Extract only positions that form valid lines (not the entire connected component)
                var linePositions = ExtractValidLinePositions(component);
                if (linePositions.Count >= 3)
                {
                    return CreateSimpleMatchGroup(linePositions);
                }
                Pools.Release(linePositions);
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

    private List<MatchGroup> CreateSimpleMatchGroup(HashSet<Position> linePositions)
    {
        var simpleGroup = Pools.Obtain<MatchGroup>();
        simpleGroup.Positions.Clear();
        foreach (var p in linePositions) simpleGroup.Positions.Add(p);
        simpleGroup.Shape = MatchShape.Simple3;
        simpleGroup.SpawnBombType = BombType.None;
        simpleGroup.Type = TileType.None; // Set by caller
        simpleGroup.BombOrigin = null;

        // Release the linePositions set (caller expects us to take ownership)
        Pools.Release(linePositions);

        var results = Pools.ObtainList<MatchGroup>();
        results.Add(simpleGroup);
        return results;
    }

    /// <summary>
    /// Extract only positions that are part of valid lines (3+ consecutive in a row or column).
    /// This prevents stray tiles connected to a valid match from being incorrectly cleared.
    /// For example, in an L-shape like:
    ///   A A A
    ///   B C A
    /// Only the top 3 A's should be extracted, not the bottom-right A.
    /// </summary>
    private static HashSet<Position> ExtractValidLinePositions(HashSet<Position> component)
    {
        var result = Pools.ObtainHashSet<Position>();

        if (component.Count < 3) return result;

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

        // Find horizontal lines and add their positions
        for (int y = minY; y <= maxY; y++)
        {
            int startX = -1;
            int consecutive = 0;

            for (int x = minX; x <= maxX + 1; x++) // +1 to handle end of line
            {
                if (x <= maxX && component.Contains(new Position(x, y)))
                {
                    if (consecutive == 0) startX = x;
                    consecutive++;
                }
                else
                {
                    // End of a run - add if it was 3+
                    if (consecutive >= 3)
                    {
                        for (int i = startX; i < startX + consecutive; i++)
                        {
                            result.Add(new Position(i, y));
                        }
                    }
                    consecutive = 0;
                }
            }
        }

        // Find vertical lines and add their positions
        for (int x = minX; x <= maxX; x++)
        {
            int startY = -1;
            int consecutive = 0;

            for (int y = minY; y <= maxY + 1; y++) // +1 to handle end of line
            {
                if (y <= maxY && component.Contains(new Position(x, y)))
                {
                    if (consecutive == 0) startY = y;
                    consecutive++;
                }
                else
                {
                    // End of a run - add if it was 3+
                    if (consecutive >= 3)
                    {
                        for (int i = startY; i < startY + consecutive; i++)
                        {
                            result.Add(new Position(x, i));
                        }
                    }
                    consecutive = 0;
                }
            }
        }

        return result;
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

                        // Determine if scrap can be absorbed:
                        // - Cross/Square shapes: always absorb
                        // - Line shapes: only absorb if scrap is collinear (extends the line)
                        if (bestOwner != null && CanAbsorbScrap(bestOwner, scrap))
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

    /// <summary>
    /// Determines if a specific scrap cell can be absorbed into a shape.
    /// Rules (有结构依据的吸收):
    /// - Simple3: never absorb
    /// - Line4/Line5: collinear + continuous (must be adjacent to existing line)
    /// - Square (2x2): orthogonal adjacent only (no diagonal), recursive via BFS
    /// - Cross (T/L/+): collinear with intersection point + continuous
    /// </summary>
    private static bool CanAbsorbScrap(DetectedShape shape, Position scrap)
    {
        // Square shapes: only absorb orthogonally adjacent (not diagonal)
        if (shape.Shape == MatchShape.Square)
        {
            return IsOrthogonallyAdjacent(shape.Cells!, scrap);
        }

        // Cross shapes: collinear with intersection point + continuous
        if (shape.Shape == MatchShape.Cross)
        {
            return CanAbsorbIntoCross(shape, scrap);
        }

        // Line4/Line5 shapes: collinear + continuous (adjacent to line)
        if (shape.Shape == MatchShape.Line4Horizontal || shape.Shape == MatchShape.Line4Vertical ||
            shape.Shape == MatchShape.Line5)
        {
            return IsCollinearAndAdjacent(shape.Cells!, scrap);
        }

        // Simple3 and unknown shapes - no absorption
        return false;
    }

    /// <summary>
    /// Check if scrap is orthogonally adjacent to any cell in the shape (not diagonal).
    /// Used for Square (2x2) absorption.
    /// </summary>
    private static bool IsOrthogonallyAdjacent(HashSet<Position> cells, Position scrap)
    {
        // Check 4 orthogonal neighbors
        var up = new Position(scrap.X, scrap.Y - 1);
        var down = new Position(scrap.X, scrap.Y + 1);
        var left = new Position(scrap.X - 1, scrap.Y);
        var right = new Position(scrap.X + 1, scrap.Y);

        return cells.Contains(up) || cells.Contains(down) ||
               cells.Contains(left) || cells.Contains(right);
    }

    /// <summary>
    /// Check if scrap can be absorbed into a Cross (T/L/+) shape.
    /// Rule: scrap must be collinear with intersection point AND the path must be continuous.
    /// Equivalent: rectangle formed by scrap and intersection must have all cells filled.
    /// </summary>
    private static bool CanAbsorbIntoCross(DetectedShape shape, Position scrap)
    {
        if (!shape.Intersection.HasValue || shape.Cells == null)
            return false;

        var intersection = shape.Intersection.Value;

        // Must be collinear with intersection (same row or same column)
        if (scrap.X != intersection.X && scrap.Y != intersection.Y)
            return false;

        // Check continuity: all cells between scrap and intersection must exist in shape
        // AND scrap must be adjacent to an existing cell
        return IsPathContinuousAndAdjacent(shape.Cells, scrap, intersection);
    }

    /// <summary>
    /// Check if scrap is collinear with the line AND adjacent to an existing cell.
    /// Used for Line4/Line5 absorption.
    /// </summary>
    private static bool IsCollinearAndAdjacent(HashSet<Position> cells, Position scrap)
    {
        if (cells.Count == 0) return false;

        // Determine line direction
        int? commonX = null;
        int? commonY = null;
        bool first = true;

        foreach (var p in cells)
        {
            if (first)
            {
                commonX = p.X;
                commonY = p.Y;
                first = false;
            }
            else
            {
                if (commonX.HasValue && p.X != commonX.Value) commonX = null;
                if (commonY.HasValue && p.Y != commonY.Value) commonY = null;
            }
        }

        // Horizontal line: scrap must have same Y AND be adjacent to existing cell
        if (commonY.HasValue)
        {
            if (scrap.Y != commonY.Value) return false;
            // Check if adjacent (left or right neighbor exists)
            return cells.Contains(new Position(scrap.X - 1, scrap.Y)) ||
                   cells.Contains(new Position(scrap.X + 1, scrap.Y));
        }

        // Vertical line: scrap must have same X AND be adjacent to existing cell
        if (commonX.HasValue)
        {
            if (scrap.X != commonX.Value) return false;
            // Check if adjacent (up or down neighbor exists)
            return cells.Contains(new Position(scrap.X, scrap.Y - 1)) ||
                   cells.Contains(new Position(scrap.X, scrap.Y + 1));
        }

        return false;
    }

    /// <summary>
    /// Check if path from scrap to intersection is continuous AND scrap is adjacent to an existing cell.
    /// </summary>
    private static bool IsPathContinuousAndAdjacent(HashSet<Position> cells, Position scrap, Position intersection)
    {
        // First check if scrap is adjacent to any existing cell
        bool isAdjacent = cells.Contains(new Position(scrap.X - 1, scrap.Y)) ||
                          cells.Contains(new Position(scrap.X + 1, scrap.Y)) ||
                          cells.Contains(new Position(scrap.X, scrap.Y - 1)) ||
                          cells.Contains(new Position(scrap.X, scrap.Y + 1));

        if (!isAdjacent) return false;

        // Check continuity: all cells between scrap and intersection must exist
        if (scrap.X == intersection.X)
        {
            // Same column - check vertical path
            int minY = System.Math.Min(scrap.Y, intersection.Y);
            int maxY = System.Math.Max(scrap.Y, intersection.Y);
            for (int y = minY; y <= maxY; y++)
            {
                var pos = new Position(scrap.X, y);
                if (pos != scrap && !cells.Contains(pos))
                    return false;
            }
            return true;
        }
        else if (scrap.Y == intersection.Y)
        {
            // Same row - check horizontal path
            int minX = System.Math.Min(scrap.X, intersection.X);
            int maxX = System.Math.Max(scrap.X, intersection.X);
            for (int x = minX; x <= maxX; x++)
            {
                var pos = new Position(x, scrap.Y);
                if (pos != scrap && !cells.Contains(pos))
                    return false;
            }
            return true;
        }

        return false;
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

            // Only create orphan group if they form valid lines (3+ consecutive)
            // Single stray cells or small groups are discarded
            if (orphans.Count >= 3)
            {
                var orphanSet = Pools.ObtainHashSet<Position>();
                foreach (var p in orphans) orphanSet.Add(p);

                var validOrphans = ExtractValidLinePositions(orphanSet);
                Pools.Release(orphanSet);

                if (validOrphans.Count >= 3)
                {
                    var orphanGroup = Pools.Obtain<MatchGroup>();
                    orphanGroup.Positions.Clear();
                    foreach (var p in validOrphans) orphanGroup.Positions.Add(p);
                    orphanGroup.Shape = MatchShape.Simple3;
                    orphanGroup.SpawnBombType = BombType.None;
                    orphanGroup.Type = TileType.None;
                    orphanGroup.BombOrigin = null;
                    results.Add(orphanGroup);
                }
                Pools.Release(validOrphans);
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
