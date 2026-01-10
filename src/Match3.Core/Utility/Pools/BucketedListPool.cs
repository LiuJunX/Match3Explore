using System;
using System.Collections.Generic;

namespace Match3.Core.Utility.Pools
{
    /// <summary>
    /// A specialized pool for List&lt;T&gt; that categorizes lists by capacity to reduce memory waste.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class BucketedListPool<T> : IObjectPool<List<T>>
    {
        // Lists with Capacity < SmallCapacityThreshold go here.
        private readonly Stack<List<T>> _smallPool = new Stack<List<T>>();
        
        // Lists with Capacity >= SmallCapacityThreshold go here.
        private readonly Stack<List<T>> _largePool = new Stack<List<T>>();

        private readonly object _lock = new object();

        // Thresholds
        private const int SmallCapacityThreshold = 128;
        private const int MaxRetainCapacity = 2048;
        
        // Limits on pool size to prevent hoarding too many unused lists
        private const int MaxSmallPoolCount = 128;
        private const int MaxLargePoolCount = 32;

        /// <summary>
        /// Gets a list from the pool. Defaults to a small list to save memory.
        /// </summary>
        public List<T> Get()
        {
            return Get(0);
        }

        /// <summary>
        /// Gets a list with at least the specified capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity hint.</param>
        public List<T> Get(int capacity)
        {
            List<T>? list = null;

            lock (_lock)
            {
                // If the user requests a large capacity, try the large pool first.
                if (capacity >= SmallCapacityThreshold)
                {
                    if (_largePool.Count > 0)
                    {
                        list = _largePool.Pop();
                    }
                }
                else
                {
                    // For small requests, try the small pool first.
                    if (_smallPool.Count > 0)
                    {
                        list = _smallPool.Pop();
                    }
                }
            }

            // If we didn't find a suitable list in the pool, create a new one.
            if (list == null)
            {
                // If capacity is 0, new List<T>() creates one with 0 capacity (growing to 4 on first add).
                // If capacity is large, we allocate it immediately.
                list = new List<T>(capacity);
            }
            else
            {
                // Ensure the recycled list meets the requirement (in case we pulled from a pool but it's still too small, 
                // though our logic tries to match buckets. 
                // Note: If we pulled from SmallPool, it might have cap 64. If user asked for 100, 64 < 100.
                // List auto-expansion handles this, or we can force it.
                if (list.Capacity < capacity)
                {
                    list.Capacity = capacity;
                }
            }

            return list;
        }

        /// <summary>
        /// Returns a list to the pool.
        /// </summary>
        public void Return(List<T> list)
        {
            if (list == null) return;

            // Always clear the list before returning.
            list.Clear();

            // 1. Capacity Capping: If the list is too huge, discard it to let GC reclaim memory.
            if (list.Capacity > MaxRetainCapacity)
            {
                return;
            }

            lock (_lock)
            {
                // 2. Bucketing: Put it in the right stack based on its CURRENT capacity.
                if (list.Capacity >= SmallCapacityThreshold)
                {
                    if (_largePool.Count < MaxLargePoolCount)
                    {
                        _largePool.Push(list);
                    }
                }
                else
                {
                    if (_smallPool.Count < MaxSmallPoolCount)
                    {
                        _smallPool.Push(list);
                    }
                }
            }
        }
    }
}
