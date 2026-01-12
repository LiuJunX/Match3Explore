using System;
using System.Collections.Generic;
using System.Reflection;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Matching
{
    public class ExplosionRangeTests
    {
        private class StubScoreSystem : IScoreSystem
        {
            public int CalculateMatchScore(MatchGroup group) => 10;
            public int CalculateSpecialMoveScore(TileType t1, BombType b1, TileType t2, BombType b2) => 50;
        }

        private class StubRandom : IRandom
        {
            public float NextFloat() => 0.5f;
            public int Next(int max) => 0;
            public int Next(int min, int max) => min;
            public void SetState(ulong state) { }
            public ulong GetState() => 0;
        }

        [Fact]
        public void Square5x5_ShouldExplode5x5Area()
        {
            // Arrange
            var processor = new StandardMatchProcessor(new StubScoreSystem());
            var methodInfo = typeof(StandardMatchProcessor).GetMethod("GetExplosionRange", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Create a 10x10 grid
            var state = new GameState(10, 10, 5, new StubRandom());
            for (int i = 0; i < 100; i++)
            {
                int x = i % 10;
                int y = i / 10;
                state.SetTile(x, y, new Tile(i, TileType.Red, x, y));
            }

            // Act
            // Center at (5, 5)
            int cx = 5, cy = 5;
            var result = (List<Position>)methodInfo.Invoke(processor, new object[] { state, cx, cy, BombType.Square5x5 });

            // Assert
            Assert.NotNull(result);
            
            // 5x5 area = 25 tiles
            Assert.Equal(25, result.Count);
            
            // Check boundaries
            bool allWithinRange = true;
            foreach (var p in result)
            {
                int dx = p.X - cx;
                int dy = p.Y - cy;
                if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
                {
                    allWithinRange = false;
                    break;
                }
            }
            Assert.True(allWithinRange, "Some positions are outside the 5x5 range");
        }
    }
}
