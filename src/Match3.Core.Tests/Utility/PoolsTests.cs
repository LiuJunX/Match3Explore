using System.Collections.Generic;
using Match3.Core.Utility.Pools;
using Xunit;

namespace Match3.Core.Tests.Utility;

/// <summary>
/// Pools unit tests.
///
/// Covers:
/// - ObtainList returns an empty list
/// - Release and re-obtain reuses list instances
/// - ObtainHashSet basic functionality
/// - ObtainQueue basic functionality
/// - ObtainDisposableList with using pattern
/// - Capacity hints are respected
/// - Generic Obtain/Release
/// </summary>
public class PoolsTests
{
    #region ObtainList

    [Fact]
    public void ObtainList_ReturnsEmptyList()
    {
        // Act
        var list = Pools.ObtainList<int>();

        try
        {
            // Assert
            Assert.NotNull(list);
            Assert.Empty(list);
        }
        finally
        {
            Pools.Release(list);
        }
    }

    [Fact]
    public void ObtainList_WithCapacity_ReturnsListWithAtLeastRequestedCapacity()
    {
        // Act
        var list = Pools.ObtainList<int>(256);

        try
        {
            Assert.NotNull(list);
            Assert.Empty(list);
            Assert.True(list.Capacity >= 256,
                $"Expected capacity >= 256 but got {list.Capacity}");
        }
        finally
        {
            Pools.Release(list);
        }
    }

    [Fact]
    public void ObtainList_MultipleCallsReturnDistinctInstances()
    {
        var list1 = Pools.ObtainList<int>();
        var list2 = Pools.ObtainList<int>();

        try
        {
            Assert.NotSame(list1, list2);
        }
        finally
        {
            Pools.Release(list1);
            Pools.Release(list2);
        }
    }

    #endregion

    #region Release and Re-obtain

    [Fact]
    public void Release_ThenObtain_ReusesListInstance()
    {
        // Arrange: obtain a list, populate it, release it
        var list = Pools.ObtainList<int>();
        list.Add(42);
        list.Add(99);
        Pools.Release(list);

        // Act: obtain again -- should get the same (cleared) instance back
        var reused = Pools.ObtainList<int>();

        try
        {
            // The reused list should be cleared
            Assert.Empty(reused);
            // Should be the same instance (pooled)
            Assert.Same(list, reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    [Fact]
    public void Release_ClearsList()
    {
        // Arrange
        var list = Pools.ObtainList<string>();
        list.Add("hello");
        list.Add("world");

        // Act
        Pools.Release(list);
        var reused = Pools.ObtainList<string>();

        try
        {
            Assert.Empty(reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    #endregion

    #region ObtainHashSet

    [Fact]
    public void ObtainHashSet_ReturnsEmptyHashSet()
    {
        // Act
        var set = Pools.ObtainHashSet<int>();

        try
        {
            Assert.NotNull(set);
            Assert.Empty(set);
        }
        finally
        {
            Pools.Release(set);
        }
    }

    [Fact]
    public void ObtainHashSet_AfterRelease_ReturnsCleared()
    {
        // Arrange
        var set = Pools.ObtainHashSet<int>();
        set.Add(1);
        set.Add(2);
        set.Add(3);
        Pools.Release(set);

        // Act
        var reused = Pools.ObtainHashSet<int>();

        try
        {
            Assert.Empty(reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    [Fact]
    public void ObtainHashSet_SupportsAddAndContains()
    {
        var set = Pools.ObtainHashSet<string>();

        try
        {
            set.Add("test");
            Assert.Contains("test", set);
            Assert.DoesNotContain("other", set);
        }
        finally
        {
            Pools.Release(set);
        }
    }

    #endregion

    #region ObtainQueue

    [Fact]
    public void ObtainQueue_ReturnsEmptyQueue()
    {
        // Act
        var queue = Pools.ObtainQueue<int>();

        try
        {
            Assert.NotNull(queue);
            Assert.Empty(queue);
        }
        finally
        {
            Pools.Release(queue);
        }
    }

    [Fact]
    public void ObtainQueue_AfterRelease_ReturnsCleared()
    {
        // Arrange
        var queue = Pools.ObtainQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);
        Pools.Release(queue);

        // Act
        var reused = Pools.ObtainQueue<int>();

        try
        {
            Assert.Empty(reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    [Fact]
    public void ObtainQueue_SupportsEnqueueAndDequeue()
    {
        var queue = Pools.ObtainQueue<int>();

        try
        {
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Assert.Equal(1, queue.Dequeue());
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(3, queue.Dequeue());
            Assert.Empty(queue);
        }
        finally
        {
            Pools.Release(queue);
        }
    }

    #endregion

    #region ObtainDisposableList

    [Fact]
    public void ObtainDisposableList_ReturnsEmptyList()
    {
        using var handle = Pools.ObtainDisposableList<int>(out var list);

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void ObtainDisposableList_DisposalReturnsToPool()
    {
        List<int> capturedList;

        // Scope: obtain and dispose
        using (var handle = Pools.ObtainDisposableList<int>(out var list))
        {
            list.Add(42);
            capturedList = list;
        }

        // After disposal, the list should have been returned to the pool.
        // Obtain again -- should get the same (cleared) instance.
        var reused = Pools.ObtainList<int>();

        try
        {
            Assert.Same(capturedList, reused);
            Assert.Empty(reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    #endregion

    #region Generic Obtain/Release

    [Fact]
    public void Obtain_Generic_ReturnsNewInstance()
    {
        // Act
        var obj = Pools.Obtain<TestPoolableObject>();

        try
        {
            Assert.NotNull(obj);
        }
        finally
        {
            Pools.Release(obj);
        }
    }

    [Fact]
    public void Obtain_Generic_AfterRelease_ReusesInstance()
    {
        // Arrange
        var obj = Pools.Obtain<TestPoolableObject>();
        obj.Value = 42;
        Pools.Release(obj);

        // Act
        var reused = Pools.Obtain<TestPoolableObject>();

        try
        {
            // Same instance reused (note: generic pool does NOT reset state)
            Assert.Same(obj, reused);
        }
        finally
        {
            Pools.Release(reused);
        }
    }

    #endregion

    #region BucketedListPool - Bucketing Behavior

    [Fact]
    public void ObtainList_SmallCapacity_ThenLargeCapacity_DifferentBuckets()
    {
        // Obtain and release a small list
        var small = Pools.ObtainList<int>(16);
        Pools.Release(small);

        // Obtain a large list -- should NOT get the small one
        var large = Pools.ObtainList<int>(256);

        try
        {
            Assert.True(large.Capacity >= 256);
        }
        finally
        {
            Pools.Release(large);
        }
    }

    [Fact]
    public void ObtainList_ZeroCapacity_ReturnsUsableList()
    {
        var list = Pools.ObtainList<int>(0);

        try
        {
            // Should work fine with default capacity
            list.Add(1);
            Assert.Single(list);
        }
        finally
        {
            Pools.Release(list);
        }
    }

    #endregion

    #region Thread Safety Smoke Test

    [Fact]
    public async System.Threading.Tasks.Task ObtainAndRelease_FromMultipleThreads_DoesNotThrow()
    {
        // This is a basic smoke test -- BucketedListPool uses ThreadLocal
        var tasks = new System.Threading.Tasks.Task[4];

        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var list = Pools.ObtainList<int>();
                    list.Add(i);
                    Pools.Release(list);
                }
            });
        }

        await System.Threading.Tasks.Task.WhenAll(tasks);
    }

    #endregion

    #region Test Helper

    private class TestPoolableObject
    {
        public int Value { get; set; }
    }

    #endregion
}
