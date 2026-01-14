using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility.Pools;
using Match3.Random;

namespace Match3.Core.Systems.Matching.Generation;

public class BombGenerator : IBombGenerator
{
    private readonly ShapeDetector _detector = new();

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
            if (candidates.Count == 0)
            {
                return CreateSimpleMatchGroup(component);
            }

            // 2. Sort Candidates (Weight DESC, then Affinity)
            SortCandidates(candidates, foci);

            // 3. Solve Optimal Partition
            var bestIndices = Pools.ObtainList<int>();
            try
            {
                FindOptimalPartition(candidates, component, bestIndices);

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

    // Weight thresholds for multi-layer solving
    private const int RainbowWeightThreshold = 100;  // Rainbow (130) - always exact
    private const int TntWeightThreshold = 50;       // TNT (60)
    private const int RocketWeightThreshold = 30;    // Rocket (40)
    // UFO (20) is always greedy

    // Max candidates for exact solving per layer
    private const int MaxExactSolveCount = 25;

    private void FindOptimalPartition(List<DetectedShape> candidates, HashSet<Position> component, List<int> bestIndices)
    {
        var positionToIndex = Pools.Obtain<Dictionary<Position, int>>();
        var candidateMasks = ArrayPool<BitMask256>.Shared.Rent(candidates.Count);

        try
        {
            // Map component positions to indices for BitMask
            int posIndex = 0;
            foreach (var p in component)
            {
                positionToIndex[p] = posIndex++;
            }

            // Precompute masks for all candidates
            for (int i = 0; i < candidates.Count; i++)
            {
                var mask = new BitMask256();
                foreach (var p in candidates[i].Cells!)
                {
                    if (positionToIndex.TryGetValue(p, out int index) && index < 256)
                    {
                        mask.Set(index);
                    }
                }
                candidateMasks[i] = mask;
            }

            // ═══════════════════════════════════════════════════════════════
            // Layered Exact Solving: Solve high-weight exactly, low-weight greedily
            // ═══════════════════════════════════════════════════════════════
            FindOptimalPartitionLayered(candidates, candidateMasks, bestIndices);
        }
        finally
        {
            positionToIndex.Clear();
            Pools.Release(positionToIndex);
            ArrayPool<BitMask256>.Shared.Return(candidateMasks);
        }
    }

    private void FindOptimalPartitionLayered(
        List<DetectedShape> candidates,
        BitMask256[] candidateMasks,
        List<int> bestIndices)
    {
        var rainbowIndices = Pools.ObtainList<int>();   // >= 100 (Rainbow: 130)
        var tntIndices = Pools.ObtainList<int>();       // >= 50 (TNT: 60)
        var rocketIndices = Pools.ObtainList<int>();    // >= 30 (Rocket: 40)
        var ufoIndices = Pools.ObtainList<int>();       // < 30 (UFO: 20)

        try
        {
            // ═══════════════════════════════════════════════════════════════
            // Phase 1: Categorize candidates by weight tier
            // ═══════════════════════════════════════════════════════════════
            for (int i = 0; i < candidates.Count; i++)
            {
                int weight = candidates[i].Weight;
                if (weight >= RainbowWeightThreshold)
                    rainbowIndices.Add(i);
                else if (weight >= TntWeightThreshold)
                    tntIndices.Add(i);
                else if (weight >= RocketWeightThreshold)
                    rocketIndices.Add(i);
                else
                    ufoIndices.Add(i);
            }

            var usedMask = new BitMask256();

            // ═══════════════════════════════════════════════════════════════
            // Phase 2: Solve Rainbow layer (highest priority)
            // For large grids, Rainbow candidates can be many (sliding window)
            // ═══════════════════════════════════════════════════════════════
            if (rainbowIndices.Count > 0)
            {
                if (rainbowIndices.Count <= MaxExactSolveCount)
                {
                    SolveBranchAndBoundSubset(candidates, rainbowIndices, candidateMasks, bestIndices);
                }
                else
                {
                    // Sort by size DESC (prefer larger rainbows that cover more)
                    rainbowIndices.Sort((a, b) => candidates[b].Cells!.Count.CompareTo(candidates[a].Cells!.Count));
                    SolveGreedySubset(rainbowIndices, candidateMasks, ref usedMask, bestIndices);
                }
                foreach (var idx in bestIndices)
                {
                    usedMask.UnionWith(candidateMasks[idx]);
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // Phase 3: Solve TNT + Rocket together (they compete for space)
            // Key insight: 2 Rockets (80) > 1 TNT (60), so we need exact solving
            // ═══════════════════════════════════════════════════════════════
            var tntAndRocketIndices = Pools.ObtainList<int>();
            try
            {
                // Filter out candidates that overlap with already-selected Rainbow
                foreach (var idx in tntIndices)
                {
                    if (!usedMask.Overlaps(candidateMasks[idx]))
                        tntAndRocketIndices.Add(idx);
                }
                foreach (var idx in rocketIndices)
                {
                    if (!usedMask.Overlaps(candidateMasks[idx]))
                        tntAndRocketIndices.Add(idx);
                }

                if (tntAndRocketIndices.Count > 0)
                {
                    int prevCount = bestIndices.Count;

                    if (tntAndRocketIndices.Count <= MaxExactSolveCount)
                    {
                        // Exact Branch & Bound
                        SolveBranchAndBoundSubset(candidates, tntAndRocketIndices, candidateMasks, bestIndices);
                    }
                    else
                    {
                        // Too many candidates - use smart greedy
                        // Sort by weight, but prefer smaller shapes (they block less)
                        tntAndRocketIndices.Sort((a, b) =>
                        {
                            int weightDiff = candidates[b].Weight.CompareTo(candidates[a].Weight);
                            if (weightDiff != 0) return weightDiff;
                            // Same weight: prefer smaller size (blocks less space)
                            return candidates[a].Cells!.Count.CompareTo(candidates[b].Cells!.Count);
                        });
                        SolveGreedySubset(tntAndRocketIndices, candidateMasks, ref usedMask, bestIndices);
                    }

                    // Update used mask with newly selected candidates
                    for (int i = prevCount; i < bestIndices.Count; i++)
                    {
                        usedMask.UnionWith(candidateMasks[bestIndices[i]]);
                    }
                }
            }
            finally
            {
                Pools.Release(tntAndRocketIndices);
            }

            // ═══════════════════════════════════════════════════════════════
            // Phase 4: Greedily fill UFO layer - O(n)
            // ═══════════════════════════════════════════════════════════════
            foreach (var idx in ufoIndices)
            {
                if (!usedMask.Overlaps(candidateMasks[idx]))
                {
                    bestIndices.Add(idx);
                    usedMask.UnionWith(candidateMasks[idx]);
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // Phase 5: Local search optimization (try swapping for better score)
            // ═══════════════════════════════════════════════════════════════
            LocalSearchOptimize(candidates, candidateMasks, bestIndices);
        }
        finally
        {
            Pools.Release(rainbowIndices);
            Pools.Release(tntIndices);
            Pools.Release(rocketIndices);
            Pools.Release(ufoIndices);
        }
    }

    private void SolveBranchAndBound(
        List<DetectedShape> candidates,
        int index,
        List<int> currentIndices,
        BitMask256 usedMask,
        int currentScore,
        ref int bestScore,
        List<int> bestIndices,
        int[] suffixSums,
        BitMask256[] candidateMasks)
    {
         // 1. Base Case / Pruning
         if (index >= candidates.Count)
         {
             if (currentScore > bestScore)
             {
                 bestScore = currentScore;
                 bestIndices.Clear();
                 bestIndices.AddRange(currentIndices);
             }
             return;
         }

         if (currentScore + suffixSums[index] <= bestScore)
         {
             return;
         }

         // 2. Try Include
         var candidateMask = candidateMasks[index];
         if (!usedMask.Overlaps(candidateMask))
         {
             currentIndices.Add(index);
             
             // Create new mask for next level (copy + union)
             var nextMask = usedMask; 
             nextMask.UnionWith(candidateMask);
             
             SolveBranchAndBound(candidates, index + 1, currentIndices, nextMask, currentScore + candidates[index].Weight, ref bestScore, bestIndices, suffixSums, candidateMasks);
             
             currentIndices.RemoveAt(currentIndices.Count - 1);
         }

         // 3. Try Exclude
         SolveBranchAndBound(candidates, index + 1, currentIndices, usedMask, currentScore, ref bestScore, bestIndices, suffixSums, candidateMasks);
    }

    /// <summary>
    /// Exact Branch & Bound solver for a subset of candidates (typically high-weight only).
    /// </summary>
    private void SolveBranchAndBoundSubset(
        List<DetectedShape> candidates,
        List<int> subsetIndices,
        BitMask256[] candidateMasks,
        List<int> bestIndices)
    {
        if (subsetIndices.Count == 0) return;

        var currentIndices = Pools.ObtainList<int>();
        var suffixSums = ArrayPool<int>.Shared.Rent(subsetIndices.Count + 1);

        try
        {
            // Compute suffix sums for pruning
            suffixSums[subsetIndices.Count] = 0;
            for (int i = subsetIndices.Count - 1; i >= 0; i--)
            {
                suffixSums[i] = suffixSums[i + 1] + candidates[subsetIndices[i]].Weight;
            }

            int bestScore = -1;
            SolveBranchAndBoundSubsetRecursive(
                candidates, subsetIndices, candidateMasks,
                0, currentIndices, new BitMask256(), 0,
                ref bestScore, bestIndices, suffixSums);
        }
        finally
        {
            Pools.Release(currentIndices);
            ArrayPool<int>.Shared.Return(suffixSums);
        }
    }

    private void SolveBranchAndBoundSubsetRecursive(
        List<DetectedShape> candidates,
        List<int> subsetIndices,
        BitMask256[] candidateMasks,
        int subsetPos,
        List<int> currentIndices,
        BitMask256 usedMask,
        int currentScore,
        ref int bestScore,
        List<int> bestIndices,
        int[] suffixSums)
    {
        // Base case
        if (subsetPos >= subsetIndices.Count)
        {
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestIndices.Clear();
                bestIndices.AddRange(currentIndices);
            }
            return;
        }

        // Pruning: if remaining max possible score can't beat best, skip
        if (currentScore + suffixSums[subsetPos] <= bestScore)
        {
            return;
        }

        int candidateIdx = subsetIndices[subsetPos];
        var candidateMask = candidateMasks[candidateIdx];

        // Try include
        if (!usedMask.Overlaps(candidateMask))
        {
            currentIndices.Add(candidateIdx);
            var nextMask = usedMask;
            nextMask.UnionWith(candidateMask);

            SolveBranchAndBoundSubsetRecursive(
                candidates, subsetIndices, candidateMasks,
                subsetPos + 1, currentIndices, nextMask,
                currentScore + candidates[candidateIdx].Weight,
                ref bestScore, bestIndices, suffixSums);

            currentIndices.RemoveAt(currentIndices.Count - 1);
        }

        // Try exclude
        SolveBranchAndBoundSubsetRecursive(
            candidates, subsetIndices, candidateMasks,
            subsetPos + 1, currentIndices, usedMask,
            currentScore, ref bestScore, bestIndices, suffixSums);
    }

    /// <summary>
    /// Greedy solver for a subset of candidates.
    /// </summary>
    private void SolveGreedySubset(
        List<int> subsetIndices,
        BitMask256[] candidateMasks,
        ref BitMask256 usedMask,
        List<int> bestIndices)
    {
        // subsetIndices should already be sorted by weight DESC
        foreach (var idx in subsetIndices)
        {
            if (!usedMask.Overlaps(candidateMasks[idx]))
            {
                bestIndices.Add(idx);
                usedMask.UnionWith(candidateMasks[idx]);
            }
        }
    }

    /// <summary>
    /// Local search optimization: try removing one candidate and adding multiple others
    /// to see if total score improves.
    /// Optimized version with O(1) solution membership check and cached mask computation.
    /// </summary>
    private void LocalSearchOptimize(
        List<DetectedShape> candidates,
        BitMask256[] candidateMasks,
        List<int> solution)
    {
        if (solution.Count == 0) return;

        var solutionSet = Pools.ObtainHashSet<int>();
        var potentialAdds = Pools.ObtainList<int>();
        var actualAdds = Pools.ObtainList<int>();

        try
        {
            bool improved = true;
            int maxIterations = 10;
            int iterations = 0;

            while (improved && iterations < maxIterations)
            {
                improved = false;
                iterations++;

                // Rebuild solution set at start of each iteration
                solutionSet.Clear();
                foreach (var idx in solution) solutionSet.Add(idx);

                // Precompute total mask
                var totalMask = new BitMask256();
                foreach (var idx in solution)
                {
                    totalMask.UnionWith(candidateMasks[idx]);
                }

                for (int i = 0; i < solution.Count; i++)
                {
                    int removedIdx = solution[i];
                    int removedWeight = candidates[removedIdx].Weight;
                    var freedMask = candidateMasks[removedIdx];

                    // Compute mask excluding this candidate using ClearBits
                    var currentMask = totalMask;
                    currentMask.ClearBits(freedMask);

                    potentialAdds.Clear();

                    // Find candidates that could fit in the freed space
                    for (int k = 0; k < candidates.Count; k++)
                    {
                        // O(1) solution membership check
                        if (solutionSet.Contains(k)) continue;

                        // Must overlap with freed space to be relevant
                        if (!freedMask.Overlaps(candidateMasks[k])) continue;

                        // Must fit in remaining space
                        if (!currentMask.Overlaps(candidateMasks[k]))
                        {
                            potentialAdds.Add(k);
                        }
                    }

                    if (potentialAdds.Count == 0) continue;

                    // Sort by weight DESC
                    potentialAdds.Sort((a, b) => candidates[b].Weight.CompareTo(candidates[a].Weight));

                    // Greedily select
                    var testMask = currentMask;
                    actualAdds.Clear();
                    int potentialGain = 0;

                    foreach (var k in potentialAdds)
                    {
                        if (!testMask.Overlaps(candidateMasks[k]))
                        {
                            actualAdds.Add(k);
                            potentialGain += candidates[k].Weight;
                            testMask.UnionWith(candidateMasks[k]);
                        }
                    }

                    // Apply if net gain is positive
                    if (potentialGain > removedWeight)
                    {
                        solution.RemoveAt(i);
                        solution.AddRange(actualAdds);
                        improved = true;
                        break;
                    }
                }
            }
        }
        finally
        {
            Pools.Release(solutionSet);
            Pools.Release(potentialAdds);
            Pools.Release(actualAdds);
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
        var matchingFoci = Pools.ObtainList<Position>();

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

                // Determine Bomb Origin
                Position? origin = null;

                // Priority 1: Player operation positions (foci)
                // Collect all foci that are in this shape
                matchingFoci.Clear();
                if (foci != null)
                {
                    foreach (var f in foci)
                    {
                        if (shape.Cells!.Contains(f))
                        {
                            matchingFoci.Add(f);
                        }
                    }
                }

                if (matchingFoci.Count == 1)
                {
                    // Single focus in shape - use it
                    origin = matchingFoci[0];
                }
                else if (matchingFoci.Count > 1)
                {
                    // Multiple foci in shape - randomly select one
                    if (random != null)
                    {
                        int idx2 = random.Next(0, matchingFoci.Count);
                        origin = matchingFoci[idx2];
                    }
                    else
                    {
                        origin = matchingFoci[0]; // Fallback to first
                    }
                }

                // Priority 2: Random position from shape cells
                if (origin == null && shape.Cells!.Count > 0)
                {
                    // Convert to sorted list for deterministic access
                    var cellList = Pools.ObtainList<Position>();
                    try
                    {
                        foreach (var c in shape.Cells) cellList.Add(c);
                        // Sort for deterministic ordering (by Y then X)
                        cellList.Sort((a, b) =>
                        {
                            int cmp = a.Y.CompareTo(b.Y);
                            return cmp != 0 ? cmp : a.X.CompareTo(b.X);
                        });

                        if (random != null)
                        {
                            int idx3 = random.Next(0, cellList.Count);
                            origin = cellList[idx3];
                        }
                        else
                        {
                            origin = cellList[0]; // Deterministic: top-left position
                        }
                    }
                    finally
                    {
                        Pools.Release(cellList);
                    }
                }
                group.BombOrigin = origin;

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
                // Create a simple match group for orphans
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
            Pools.Release(matchingFoci);
        }
    }

    private struct BitMask256
    {
        private ulong _p0;
        private ulong _p1;
        private ulong _p2;
        private ulong _p3;

        public void Set(int index)
        {
            if (index < 64) _p0 |= 1UL << index;
            else if (index < 128) _p1 |= 1UL << (index - 64);
            else if (index < 192) _p2 |= 1UL << (index - 128);
            else if (index < 256) _p3 |= 1UL << (index - 192);
        }

        public bool Overlaps(in BitMask256 other)
        {
            return (_p0 & other._p0) != 0 ||
                   (_p1 & other._p1) != 0 ||
                   (_p2 & other._p2) != 0 ||
                   (_p3 & other._p3) != 0;
        }

        public void UnionWith(in BitMask256 other)
        {
            _p0 |= other._p0;
            _p1 |= other._p1;
            _p2 |= other._p2;
            _p3 |= other._p3;
        }

        /// <summary>
        /// Clears bits that are set in 'other' from this mask.
        /// Equivalent to: this &= ~other
        /// </summary>
        public void ClearBits(in BitMask256 other)
        {
            _p0 &= ~other._p0;
            _p1 &= ~other._p1;
            _p2 &= ~other._p2;
            _p3 &= ~other._p3;
        }
    }
}
