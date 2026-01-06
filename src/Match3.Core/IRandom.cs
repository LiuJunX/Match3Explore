using System;
namespace Match3.Core;
public interface IRandom
{
    int Next(int minInclusive, int maxExclusive);
}
public sealed class DefaultRandom : IRandom
{
    private readonly Random _random;
    public DefaultRandom(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }
    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
