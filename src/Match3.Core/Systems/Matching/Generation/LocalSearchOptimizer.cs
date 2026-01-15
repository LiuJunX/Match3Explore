using System.Collections.Generic;
using Match3.Core.Models.Gameplay;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Systems.Matching.Generation;

/// <summary>
/// Local search optimization for bomb generation partition.
/// Tries removing one candidate and adding multiple others to improve total score.
/// </summary>
internal static class LocalSearchOptimizer
{
    /// <summary>
    /// Optimizes the solution by trying swap operations.
    /// </summary>
    public static void Optimize(
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
}
