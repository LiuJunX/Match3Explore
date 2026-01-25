using Match3.Core.Models.Grid;
using Match3.Unity.Bridge;
using NUnit.Framework;
using UnityEngine;
using SysVector2 = System.Numerics.Vector2;

namespace Match3.Unity.Tests
{
    /// <summary>
    /// Tests for CoordinateConverter Y-axis flip logic.
    /// Core uses Y=0 at top (Y+ down), Unity uses Y+ up.
    /// </summary>
    public class CoordinateConverterTests
    {
        private const float CellSize = 1f;
        private const int BoardWidth = 8;
        private const int BoardHeight = 8;
        private static readonly Vector2 Origin = Vector2.zero;

        #region GridToWorld Tests

        [Test]
        public void GridToWorld_TopRow_MapsToHighY()
        {
            // Grid Y=0 is top row, should map to highest Unity Y
            var gridPos = new SysVector2(0, 0);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, Origin, BoardHeight);

            // Expected: origin.y + (height-1-0) * cellSize + 0.5 = 0 + 7 + 0.5 = 7.5
            Assert.AreEqual(7.5f, world.y, 0.001f, "Top row (Grid Y=0) should map to high Unity Y");
        }

        [Test]
        public void GridToWorld_BottomRow_MapsToLowY()
        {
            // Grid Y=7 is bottom row, should map to lowest Unity Y
            var gridPos = new SysVector2(0, 7);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, Origin, BoardHeight);

            // Expected: origin.y + (height-1-7) * cellSize + 0.5 = 0 + 0 + 0.5 = 0.5
            Assert.AreEqual(0.5f, world.y, 0.001f, "Bottom row (Grid Y=7) should map to low Unity Y");
        }

        [Test]
        public void GridToWorld_XAxis_NotFlipped()
        {
            // X axis should not be flipped
            var gridPos = new SysVector2(3, 0);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, Origin, BoardHeight);

            // Expected: origin.x + 3 * cellSize + 0.5 = 0 + 3 + 0.5 = 3.5
            Assert.AreEqual(3.5f, world.x, 0.001f, "X axis should not be flipped");
        }

        [Test]
        public void GridToWorld_WithOriginOffset_AddsOffset()
        {
            var origin = new Vector2(10f, 5f);
            var gridPos = new SysVector2(0, 0);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, origin, BoardHeight);

            Assert.AreEqual(10.5f, world.x, 0.001f);
            Assert.AreEqual(12.5f, world.y, 0.001f); // 5 + 7 + 0.5
        }

        [Test]
        public void GridToWorld_WithCellSize2_ScalesCorrectly()
        {
            const float cellSize = 2f;
            var gridPos = new SysVector2(1, 1);
            var world = CoordinateConverter.GridToWorld(gridPos, cellSize, Origin, BoardHeight);

            // X: 0 + 1*2 + 1 = 3
            // Y: 0 + (7-1)*2 + 1 = 13
            Assert.AreEqual(3f, world.x, 0.001f);
            Assert.AreEqual(13f, world.y, 0.001f);
        }

        [Test]
        public void GridToWorld_FloatPosition_InterpolatesCorrectly()
        {
            // Tile falling between rows 2 and 3 (Grid Y = 2.5)
            var gridPos = new SysVector2(0, 2.5f);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, Origin, BoardHeight);

            // Y: 0 + (7-2.5)*1 + 0.5 = 5
            Assert.AreEqual(5f, world.y, 0.001f, "Float Y should interpolate correctly");
        }

        [Test]
        public void GridToWorld_Position_MatchesSysVector2()
        {
            // Both overloads should produce same result for integer positions
            var sysPos = new SysVector2(3, 4);
            var intPos = new Position(3, 4);

            var worldFromSys = CoordinateConverter.GridToWorld(sysPos, CellSize, Origin, BoardHeight);
            var worldFromInt = CoordinateConverter.GridToWorld(intPos, CellSize, Origin, BoardHeight);

            Assert.AreEqual(worldFromSys.x, worldFromInt.x, 0.001f);
            Assert.AreEqual(worldFromSys.y, worldFromInt.y, 0.001f);
        }

        #endregion

        #region WorldToGrid Tests

        [Test]
        public void WorldToGrid_HighY_MapsToTopRow()
        {
            // Click at top of board (high Unity Y) should map to Grid Y=0
            var worldPos = new Vector3(0.5f, 7.5f, 0f);
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(0, gridPos.X);
            Assert.AreEqual(0, gridPos.Y, "High Unity Y should map to Grid Y=0 (top row)");
        }

        [Test]
        public void WorldToGrid_LowY_MapsToBottomRow()
        {
            // Click at bottom of board (low Unity Y) should map to Grid Y=7
            var worldPos = new Vector3(0.5f, 0.5f, 0f);
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(0, gridPos.X);
            Assert.AreEqual(7, gridPos.Y, "Low Unity Y should map to Grid Y=7 (bottom row)");
        }

        [Test]
        public void WorldToGrid_OutOfBounds_ReturnsInvalid()
        {
            // Click outside board
            var worldPos = new Vector3(-1f, 0.5f, 0f);
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(Position.Invalid, gridPos);
        }

        [Test]
        public void WorldToGrid_RightEdge_StillValid()
        {
            // Click at right edge (X = 7.9, should be column 7)
            var worldPos = new Vector3(7.9f, 0.5f, 0f);
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(7, gridPos.X);
            Assert.IsTrue(gridPos.IsValid);
        }

        [Test]
        public void WorldToGrid_JustOutside_ReturnsInvalid()
        {
            // Click just outside board (X = 8.0)
            var worldPos = new Vector3(8.0f, 0.5f, 0f);
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(Position.Invalid, gridPos);
        }

        #endregion

        #region Round-trip Tests

        [Test]
        public void RoundTrip_GridToWorldToGrid_IsConsistent()
        {
            // For all valid grid positions, converting to world and back should be identity
            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    var original = new Position(x, y);
                    var world = CoordinateConverter.GridToWorld(original, CellSize, Origin, BoardHeight);
                    var back = CoordinateConverter.WorldToGrid(world, CellSize, Origin, BoardWidth, BoardHeight);

                    Assert.AreEqual(original, back, $"Round-trip failed for ({x}, {y})");
                }
            }
        }

        [Test]
        public void RoundTrip_WithOffset_IsConsistent()
        {
            var origin = new Vector2(-5f, 3f);

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    var original = new Position(x, y);
                    var world = CoordinateConverter.GridToWorld(original, CellSize, origin, BoardHeight);
                    var back = CoordinateConverter.WorldToGrid(world, CellSize, origin, BoardWidth, BoardHeight);

                    Assert.AreEqual(original, back, $"Round-trip with offset failed for ({x}, {y})");
                }
            }
        }

        [Test]
        public void RoundTrip_WithLargeCellSize_IsConsistent()
        {
            const float cellSize = 2.5f;

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    var original = new Position(x, y);
                    var world = CoordinateConverter.GridToWorld(original, cellSize, Origin, BoardHeight);
                    var back = CoordinateConverter.WorldToGrid(world, cellSize, Origin, BoardWidth, BoardHeight);

                    Assert.AreEqual(original, back, $"Round-trip with cellSize={cellSize} failed for ({x}, {y})");
                }
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GridToWorld_NegativeGridY_HandlesGracefully()
        {
            // Tiles spawning above board (negative Y in some systems)
            var gridPos = new SysVector2(0, -1);
            var world = CoordinateConverter.GridToWorld(gridPos, CellSize, Origin, BoardHeight);

            // Y: 0 + (7-(-1))*1 + 0.5 = 8.5
            Assert.AreEqual(8.5f, world.y, 0.001f);
        }

        [Test]
        public void WorldToGrid_ClickOnCellBoundary_ReturnsCorrectCell()
        {
            // Click exactly on boundary between cells (should floor to lower cell)
            var worldPos = new Vector3(2.0f, 4.0f, 0f); // Exactly on boundary
            var gridPos = CoordinateConverter.WorldToGrid(worldPos, CellSize, Origin, BoardWidth, BoardHeight);

            Assert.AreEqual(2, gridPos.X);
            // Y: height - 1 - Floor(4.0) = 7 - 4 = 3
            Assert.AreEqual(3, gridPos.Y);
        }

        [Test]
        public void GetBoardBounds_ReturnsCorrectRect()
        {
            var bounds = CoordinateConverter.GetBoardBounds(BoardWidth, BoardHeight, CellSize, Origin);

            Assert.AreEqual(0f, bounds.x);
            Assert.AreEqual(0f, bounds.y);
            Assert.AreEqual(8f, bounds.width);
            Assert.AreEqual(8f, bounds.height);
        }

        #endregion
    }
}
