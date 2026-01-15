namespace Match3.Core.Systems.Matching.Generation;

/// <summary>
/// A 256-bit mask for efficient set operations on position indices.
/// Used by partition solving algorithms to track which positions are covered.
/// </summary>
internal struct BitMask256
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
    /// Equivalent to: this &amp;= ~other
    /// </summary>
    public void ClearBits(in BitMask256 other)
    {
        _p0 &= ~other._p0;
        _p1 &= ~other._p1;
        _p2 &= ~other._p2;
        _p3 &= ~other._p3;
    }
}
