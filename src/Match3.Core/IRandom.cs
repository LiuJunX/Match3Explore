using System;

namespace Match3.Core;

/// <summary>
/// Abstract interface for random number generation.
/// <para>
/// <b>CRITICAL:</b> All random number generation in the Core logic MUST use this interface.
/// Do NOT use <see cref="System.Random"/> or <see cref="Guid.NewGuid"/> directly.
/// This ensures that the game state is fully deterministic and replayable given a specific seed.
/// </para>
/// </summary>
public interface IRandom
{
    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    /// <param name="minInclusive">The inclusive lower bound of the random number returned.</param>
    /// <param name="maxExclusive">The exclusive upper bound of the random number returned.</param>
    /// <returns>A 32-bit signed integer greater than or equal to minValue and less than maxValue.</returns>
    int Next(int minInclusive, int maxExclusive);
}

/// <summary>
/// The default implementation of <see cref="IRandom"/> using <see cref="System.Random"/>.
/// </summary>
public sealed class DefaultRandom : IRandom
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRandom"/> class.
    /// </summary>
    /// <param name="seed">Optional seed for deterministic behavior. If null, a time-dependent default is used.</param>
    public DefaultRandom(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
