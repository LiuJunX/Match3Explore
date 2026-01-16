using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Matching.Generation;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Matching;

/// <summary>
/// Integration tests for ClassicMatchFinder.
/// Tests the full pipeline from GameState to match detection.
/// Verifies that only valid line shapes (3+ consecutive) are detected as matches.
/// </summary>
public class MatchFinderIntegrationTests
{
    private class StubRandom : IRandom
    {
        public float NextFloat() => 0f;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    private readonly ClassicMatchFinder _finder;

    public MatchFinderIntegrationTests()
    {
        _finder = new ClassicMatchFinder(new BombGenerator());
    }

    private GameState CreateState(int width, int height)
    {
        return new GameState(width, height, 6, new StubRandom());
    }

    #region User Reported Bug: L-Shape Should NOT Match

    [Fact]
    public void UserBug_LShape_3Cells_ShouldNotMatch()
    {
        // User reported scenario:
        // A A A   <- Should match (horizontal 3)
        // D E B
        // F B B   <- B forms L-shape, should NOT match
        //
        // B positions: (2,1), (1,2), (2,2) = L-shape
        var state = CreateState(3, 3);

        // Row 0: A A A
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));

        // Row 1: D E B
        state.SetTile(0, 1, new Tile(4, TileType.Green, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Yellow, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Blue, 2, 1));

        // Row 2: F B B
        state.SetTile(0, 2, new Tile(7, TileType.Purple, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Blue, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Blue, 2, 2));

        var matches = _finder.FindMatchGroups(in state);

        // Should only find 1 match: the Red horizontal line
        Assert.Single(matches);
        Assert.Equal(TileType.Red, matches[0].Type);
        Assert.Equal(3, matches[0].Positions.Count);

        // Verify Blue L-shape was NOT matched
        foreach (var match in matches)
        {
            Assert.NotEqual(TileType.Blue, match.Type);
        }

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    [Fact]
    public void UserBug_OriginalScenario_4x4_ShouldOnlyMatchPurple()
    {
        // Original user scenario (4x4):
        // Red  Green Red  Purple
        // Yellow Purple Purple Purple  <- Purple horizontal 3
        // Red  Blue Green Purple       <- Purple vertical continues
        // Purple Purple Blue Purple    <- Purple vertical ends
        //
        // Purple positions that form connected component:
        // (1,1), (2,1), (3,1), (3,0), (3,2), (3,3)
        // This is a valid T/L shape with 3+ in both directions
        var state = CreateState(4, 4);

        // Row 0
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Green, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));
        state.SetTile(3, 0, new Tile(4, TileType.Purple, 3, 0));

        // Row 1
        state.SetTile(0, 1, new Tile(5, TileType.Yellow, 0, 1));
        state.SetTile(1, 1, new Tile(6, TileType.Purple, 1, 1));
        state.SetTile(2, 1, new Tile(7, TileType.Purple, 2, 1));
        state.SetTile(3, 1, new Tile(8, TileType.Purple, 3, 1));

        // Row 2
        state.SetTile(0, 2, new Tile(9, TileType.Red, 0, 2));
        state.SetTile(1, 2, new Tile(10, TileType.Blue, 1, 2));
        state.SetTile(2, 2, new Tile(11, TileType.Green, 2, 2));
        state.SetTile(3, 2, new Tile(12, TileType.Purple, 3, 2));

        // Row 3
        state.SetTile(0, 3, new Tile(13, TileType.Purple, 0, 3));
        state.SetTile(1, 3, new Tile(14, TileType.Purple, 1, 3));
        state.SetTile(2, 3, new Tile(15, TileType.Blue, 2, 3));
        state.SetTile(3, 3, new Tile(16, TileType.Purple, 3, 3));

        var matches = _finder.FindMatchGroups(in state);

        // Should find Purple match (has valid lines: horizontal 3 at row 1, vertical 4 at col 3)
        Assert.Single(matches);
        Assert.Equal(TileType.Purple, matches[0].Type);

        // Purple at (0,3) and (1,3) should NOT be in the match
        // because they only form a 2-horizontal, not connected to the main purple group
        // Wait, let me reconsider...
        // (0,3) and (1,3) are Purple but not adjacent to the main Purple component
        // Main component: (3,0), (1,1), (2,1), (3,1), (3,2), (3,3)
        // Separate: (0,3), (1,3) - only 2 cells, won't match

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    #endregion

    #region No Match Scenarios

    [Fact]
    public void NoMatch_AllDifferentColors_ShouldReturnEmpty()
    {
        var state = CreateState(3, 3);

        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Blue, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Green, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Yellow, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Purple, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Orange, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Blue, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Red, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Green, 2, 2));

        var matches = _finder.FindMatchGroups(in state);
        Assert.Empty(matches);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    [Fact]
    public void NoMatch_Only2InARow_ShouldReturnEmpty()
    {
        var state = CreateState(3, 3);

        // R R B
        // G B R
        // B G G
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Blue, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Green, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Blue, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Red, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Blue, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Green, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Green, 2, 2));

        var matches = _finder.FindMatchGroups(in state);
        Assert.Empty(matches);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    [Fact]
    public void NoMatch_DiagonalOnly_ShouldReturnEmpty()
    {
        var state = CreateState(3, 3);

        // R B G
        // B R G
        // G B R
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Blue, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Green, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Blue, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Red, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Green, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Green, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Blue, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Red, 2, 2));

        // Red forms diagonal: (0,0), (1,1), (2,2) - should NOT match
        var matches = _finder.FindMatchGroups(in state);
        Assert.Empty(matches);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    #endregion

    #region Valid Match Scenarios

    [Fact]
    public void ValidMatch_Horizontal3_ShouldMatch()
    {
        var state = CreateState(3, 3);

        // R R R
        // B G B
        // G B G
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Blue, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Green, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Blue, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Green, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Blue, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Green, 2, 2));

        var matches = _finder.FindMatchGroups(in state);
        Assert.Single(matches);
        Assert.Equal(TileType.Red, matches[0].Type);
        Assert.Equal(3, matches[0].Positions.Count);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    [Fact]
    public void ValidMatch_Vertical3_ShouldMatch()
    {
        var state = CreateState(3, 3);

        // R B G
        // R G B
        // R B G
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Blue, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Green, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Red, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Green, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Blue, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Red, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Blue, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Green, 2, 2));

        var matches = _finder.FindMatchGroups(in state);
        Assert.Single(matches);
        Assert.Equal(TileType.Red, matches[0].Type);
        Assert.Equal(3, matches[0].Positions.Count);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    [Fact]
    public void ValidMatch_MultipleMatches_ShouldFindAll()
    {
        var state = CreateState(3, 3);

        // R R R  <- Red horizontal
        // B B B  <- Blue horizontal
        // G G G  <- Green horizontal
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Blue, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Blue, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Blue, 2, 1));
        state.SetTile(0, 2, new Tile(7, TileType.Green, 0, 2));
        state.SetTile(1, 2, new Tile(8, TileType.Green, 1, 2));
        state.SetTile(2, 2, new Tile(9, TileType.Green, 2, 2));

        var matches = _finder.FindMatchGroups(in state);
        Assert.Equal(3, matches.Count);

        ClassicMatchFinder.ReleaseGroups(matches);
    }

    #endregion

    #region HasMatchAt Tests

    [Fact]
    public void HasMatchAt_ValidHorizontal_ShouldReturnTrue()
    {
        var state = CreateState(3, 3);

        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));
        state.SetTile(0, 1, new Tile(4, TileType.Blue, 0, 1));
        state.SetTile(1, 1, new Tile(5, TileType.Green, 1, 1));
        state.SetTile(2, 1, new Tile(6, TileType.Yellow, 2, 1));

        Assert.True(_finder.HasMatchAt(in state, new Position(0, 0)));
        Assert.True(_finder.HasMatchAt(in state, new Position(1, 0)));
        Assert.True(_finder.HasMatchAt(in state, new Position(2, 0)));
        Assert.False(_finder.HasMatchAt(in state, new Position(0, 1)));
    }

    [Fact]
    public void HasMatchAt_Only2InRow_ShouldReturnFalse()
    {
        var state = CreateState(3, 3);

        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Blue, 2, 0));

        Assert.False(_finder.HasMatchAt(in state, new Position(0, 0)));
        Assert.False(_finder.HasMatchAt(in state, new Position(1, 0)));
    }

    #endregion
}
