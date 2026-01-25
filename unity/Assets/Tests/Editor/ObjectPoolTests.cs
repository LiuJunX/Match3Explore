using Match3.Unity.Pools;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Unity.Tests
{
    /// <summary>
    /// Tests for ObjectPool rent/return logic.
    /// </summary>
    public class ObjectPoolTests
    {
        private GameObject _container;

        [SetUp]
        public void SetUp()
        {
            _container = new GameObject("TestContainer");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_container);
        }

        #region Basic Operations

        [Test]
        public void Rent_FromEmptyPool_CreatesNewObject()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            var item = pool.Rent();

            Assert.IsNotNull(item);
            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.IsTrue(item.WasSpawned);
        }

        [Test]
        public void Rent_FromPrewarmedPool_ReusesObject()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 5
            );

            Assert.AreEqual(5, pool.AvailableCount);

            var item = pool.Rent();

            Assert.AreEqual(4, pool.AvailableCount);
            Assert.IsNotNull(item);
        }

        [Test]
        public void Return_AddsToPool()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            var item = pool.Rent();
            Assert.AreEqual(0, pool.AvailableCount);

            pool.Return(item);

            Assert.AreEqual(1, pool.AvailableCount);
            Assert.IsFalse(item.gameObject.activeSelf);
            Assert.IsTrue(item.WasDespawned);
        }

        [Test]
        public void Return_Null_DoesNotThrow()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            Assert.DoesNotThrow(() => pool.Return(null));
        }

        #endregion

        #region Lifecycle Callbacks

        [Test]
        public void Rent_CallsOnSpawn()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 1
            );

            var item = pool.Rent();

            Assert.IsTrue(item.WasSpawned);
        }

        [Test]
        public void Return_CallsOnDespawn()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            var item = pool.Rent();
            item.WasDespawned = false;

            pool.Return(item);

            Assert.IsTrue(item.WasDespawned);
        }

        [Test]
        public void RentAfterReturn_CallsOnSpawnAgain()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            var item = pool.Rent();
            pool.Return(item);
            item.WasSpawned = false;

            var item2 = pool.Rent();

            Assert.AreSame(item, item2, "Should reuse same object");
            Assert.IsTrue(item2.WasSpawned, "OnSpawn should be called again");
        }

        #endregion

        #region MaxSize

        [Test]
        public void Return_WhenAtMaxSize_DestroysObject()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0,
                maxSize: 2
            );

            var item1 = pool.Rent();
            var item2 = pool.Rent();
            var item3 = pool.Rent();

            pool.Return(item1);
            pool.Return(item2);
            Assert.AreEqual(2, pool.AvailableCount);

            pool.Return(item3); // Should be destroyed, not added

            Assert.AreEqual(2, pool.AvailableCount, "Pool should not exceed maxSize");
        }

        [Test]
        public void MaxSizeZero_MeansUnlimited()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0,
                maxSize: 0 // Unlimited
            );

            // Rent many items first
            var items = new TestPoolable[100];
            for (int i = 0; i < 100; i++)
            {
                items[i] = pool.Rent();
            }

            // Return all items
            for (int i = 0; i < 100; i++)
            {
                pool.Return(items[i]);
            }

            // All should be pooled (no limit)
            Assert.AreEqual(100, pool.AvailableCount);
        }

        #endregion

        #region Clear

        [Test]
        public void Clear_DestroysAllPooledObjects()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 5
            );

            Assert.AreEqual(5, pool.AvailableCount);

            pool.Clear();

            Assert.AreEqual(0, pool.AvailableCount);
        }

        [Test]
        public void Clear_ThenRent_CreatesNewObjects()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 2
            );

            pool.Clear();
            var item = pool.Rent();

            Assert.IsNotNull(item);
            Assert.AreEqual(0, pool.AvailableCount);
        }

        #endregion

        #region Parent Transform

        [Test]
        public void CreatedObjects_HaveCorrectParent()
        {
            var pool = new ObjectPool<TestPoolable>(
                factory: () => CreateTestPoolable(),
                parent: _container.transform,
                initialSize: 0
            );

            var item = pool.Rent();

            Assert.AreEqual(_container.transform, item.transform.parent);
        }

        #endregion

        #region Helper

        private TestPoolable CreateTestPoolable()
        {
            var go = new GameObject("TestPoolable");
            return go.AddComponent<TestPoolable>();
        }

        private class TestPoolable : MonoBehaviour, IPoolable
        {
            public bool WasSpawned { get; set; }
            public bool WasDespawned { get; set; }

            public void OnSpawn()
            {
                WasSpawned = true;
            }

            public void OnDespawn()
            {
                WasDespawned = true;
            }
        }

        #endregion
    }
}
