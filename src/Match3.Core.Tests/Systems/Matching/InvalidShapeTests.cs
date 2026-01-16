using System.Collections.Generic;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Matching.Generation;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Matching;

/// <summary>
/// Tests for invalid shapes that should NOT be matched.
/// Core rule: Only horizontal or vertical lines of 3+ consecutive tiles should match.
/// </summary>
public class InvalidShapeTests
{
    private readonly BombGenerator _generator = new();

    #region L-Shape Variants (Should NOT Match)

    [Fact]
    public void LShape_2x2_Corner_ShouldNotMatch()
    {
        // 2x2 corner: only 3 cells, no valid line
        // B B
        // B .
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0),
            new(0, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void LShape_2x2_BottomRight_ShouldNotMatch()
    {
        // . B
        // B B
        var component = new HashSet<Position>
        {
            new(1, 0),
            new(0, 1), new(1, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void LShape_2x2_TopRight_ShouldNotMatch()
    {
        // B B
        // . B
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0),
            new(1, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void LShape_2x2_BottomLeft_ShouldNotMatch()
    {
        // B .
        // B B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(0, 1), new(1, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void LShape_Short_2Plus2_Horizontal_ShouldNotMatch()
    {
        // B B .
        // . B B
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0),
            new(1, 1), new(2, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void LShape_Short_2Plus2_Vertical_ShouldNotMatch()
    {
        // B .
        // B B
        // . B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(0, 1), new(1, 1),
            new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion

    #region Diagonal Shapes (Should NOT Match)

    [Fact]
    public void Diagonal_3Cells_ShouldNotMatch()
    {
        // B . .
        // . B .
        // . . B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(1, 1),
            new(2, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void Diagonal_4Cells_ShouldNotMatch()
    {
        // B . . .
        // . B . .
        // . . B .
        // . . . B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(1, 1),
            new(2, 2),
            new(3, 3)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void Diagonal_AntiDiagonal_ShouldNotMatch()
    {
        // . . B
        // . B .
        // B . .
        var component = new HashSet<Position>
        {
            new(2, 0),
            new(1, 1),
            new(0, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion

    #region Scattered Shapes (Should NOT Match)

    [Fact]
    public void Scattered_3Cells_NoLine_ShouldNotMatch()
    {
        // B . B
        // . B .
        var component = new HashSet<Position>
        {
            new(0, 0), new(2, 0),
            new(1, 1)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void Scattered_4Cells_Square_ShouldNotMatch()
    {
        // B . B
        // . . .
        // B . B
        var component = new HashSet<Position>
        {
            new(0, 0), new(2, 0),
            new(0, 2), new(2, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void Scattered_Cross_Disconnected_ShouldNotMatch()
    {
        // . B .
        // B . B
        // . B .
        // Note: This is NOT connected, just 4 separate cells
        var component = new HashSet<Position>
        {
            new(1, 0),
            new(0, 1), new(2, 1),
            new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion

    #region Staircase Shapes (Should NOT Match)

    [Fact]
    public void Staircase_3Steps_ShouldNotMatch()
    {
        // B . .
        // B B .
        // . B B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(0, 1), new(1, 1),
            new(1, 2), new(2, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    [Fact]
    public void Staircase_Reverse_ShouldNotMatch()
    {
        // . . B
        // . B B
        // B B .
        var component = new HashSet<Position>
        {
            new(2, 0),
            new(1, 1), new(2, 1),
            new(0, 2), new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion

    #region T-Shape Missing One Arm (Contains Valid Line - Should Match)

    [Fact]
    public void TShape_Missing_OneArm_4Cells_HasVerticalLine_ShouldMatch()
    {
        // . B .
        // B B .
        // . B .
        // Contains vertical line: (1,0), (1,1), (1,2) = valid 3-line
        var component = new HashSet<Position>
        {
            new(1, 0),
            new(0, 1), new(1, 1),
            new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.NotEmpty(results); // Has valid vertical line
    }

    [Fact]
    public void TShape_Missing_TopArm_4Cells_HasVerticalLine_ShouldMatch()
    {
        // B B .
        // . B .
        // . B .
        // Contains vertical line: (1,0), (1,1), (1,2) = valid 3-line
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0),
            new(1, 1),
            new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.NotEmpty(results); // Has valid vertical line
    }

    [Fact]
    public void TShape_BrokenArms_NoValidLine_ShouldNotMatch()
    {
        // True broken T: no arm has 3 consecutive
        // B . B
        // . B .
        // B . .
        var component = new HashSet<Position>
        {
            new(0, 0), new(2, 0),
            new(1, 1),
            new(0, 2)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion

    #region L-Shape With Valid Line (Should Match ONLY the Line)

    /// <summary>
    /// User-reported bug: L-shape with 3-horizontal + 1 extra below should only clear the 3-line.
    /// A A A
    /// B C A  → Only top 3 A's should be matched, not the bottom-right A.
    /// </summary>
    [Fact]
    public void LShape_HorizontalLine3_PlusOneBelow_ShouldMatchOnly3()
    {
        // A A A
        // . . A
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0), new(2, 0),  // Top row: valid 3-line
            new(2, 1)                          // Extra cell below - NOT part of a valid line
        };

        var results = _generator.Generate(component);

        // Should have exactly one match group
        Assert.Single(results);

        // The match group should contain exactly 3 positions (the horizontal line only)
        Assert.Equal(3, results[0].Positions.Count);

        // Verify the correct 3 positions are matched
        Assert.Contains(new Position(0, 0), results[0].Positions);
        Assert.Contains(new Position(1, 0), results[0].Positions);
        Assert.Contains(new Position(2, 0), results[0].Positions);

        // The stray cell should NOT be in the match
        Assert.DoesNotContain(new Position(2, 1), results[0].Positions);
    }

    [Fact]
    public void LShape_VerticalLine3_PlusOneRight_ShouldMatchOnly3()
    {
        // A .
        // A A
        // A .
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(0, 1), new(1, 1),  // Extra cell at (1,1)
            new(0, 2)
        };

        var results = _generator.Generate(component);

        Assert.Single(results);
        Assert.Equal(3, results[0].Positions.Count);

        // Only the vertical line should be matched
        Assert.Contains(new Position(0, 0), results[0].Positions);
        Assert.Contains(new Position(0, 1), results[0].Positions);
        Assert.Contains(new Position(0, 2), results[0].Positions);

        // Stray cell should NOT be matched
        Assert.DoesNotContain(new Position(1, 1), results[0].Positions);
    }

    [Fact]
    public void LShape_HorizontalLine4_PlusOneAbove_ShouldMatchOnly4()
    {
        // . . A .
        // A A A A
        // Pure lines (4-line) should NOT absorb stray cells.
        var component = new HashSet<Position>
        {
            new(2, 0),                              // Stray cell above - NOT absorbed
            new(0, 1), new(1, 1), new(2, 1), new(3, 1)  // Valid 4-line → generates rocket
        };

        var results = _generator.Generate(component);

        Assert.Single(results);
        // Pure 4-line should NOT absorb stray cell
        Assert.Equal(4, results[0].Positions.Count);
        Assert.DoesNotContain(new Position(2, 0), results[0].Positions);
    }

    [Fact]
    public void LShape_HorizontalLine5_PlusOneAbove_ShouldMatchOnly5()
    {
        // . . A . .
        // A A A A A
        // Pure 5-line should NOT absorb stray cells.
        var component = new HashSet<Position>
        {
            new(2, 0),                                      // Stray cell above - NOT absorbed
            new(0, 1), new(1, 1), new(2, 1), new(3, 1), new(4, 1)  // Valid 5-line → generates rainbow
        };

        var results = _generator.Generate(component);

        Assert.Single(results);
        // Pure 5-line should NOT absorb stray cell
        Assert.Equal(5, results[0].Positions.Count);
        Assert.DoesNotContain(new Position(2, 0), results[0].Positions);
    }

    #endregion

    #region Valid Shapes (Should Match) - Sanity Checks

    [Fact]
    public void Valid_Horizontal3_ShouldMatch()
    {
        // B B B
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0), new(2, 0)
        };

        var results = _generator.Generate(component);
        Assert.Single(results);
        Assert.Equal(3, results[0].Positions.Count);
    }

    [Fact]
    public void Valid_Vertical3_ShouldMatch()
    {
        // B
        // B
        // B
        var component = new HashSet<Position>
        {
            new(0, 0),
            new(0, 1),
            new(0, 2)
        };

        var results = _generator.Generate(component);
        Assert.Single(results);
        Assert.Equal(3, results[0].Positions.Count);
    }

    [Fact]
    public void Valid_LShape_3Plus3_ShouldMatch()
    {
        // Valid L-shape with 3+3 = 5 unique cells (corner shared)
        // B B B
        // B . .
        // B . .
        var component = new HashSet<Position>
        {
            new(0, 0), new(1, 0), new(2, 0),
            new(0, 1),
            new(0, 2)
        };

        var results = _generator.Generate(component);
        Assert.NotEmpty(results);
        // Should create area bomb for L-shape
    }

    [Fact]
    public void Valid_TShape_5Cells_ShouldMatch()
    {
        // . B .
        // B B B
        // . B .
        var component = new HashSet<Position>
        {
            new(1, 0),
            new(0, 1), new(1, 1), new(2, 1),
            new(1, 2)
        };

        var results = _generator.Generate(component);
        Assert.NotEmpty(results);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EdgeCase_ExactlyAt_GridBoundary_Horizontal_ShouldMatch()
    {
        // Horizontal line at y=0, x=5,6,7 (right edge of 8-wide grid)
        var component = new HashSet<Position>
        {
            new(5, 0), new(6, 0), new(7, 0)
        };

        var results = _generator.Generate(component);
        Assert.Single(results);
    }

    [Fact]
    public void EdgeCase_ExactlyAt_GridBoundary_Vertical_ShouldMatch()
    {
        // Vertical line at x=0, y=5,6,7 (bottom edge of 8-tall grid)
        var component = new HashSet<Position>
        {
            new(0, 5), new(0, 6), new(0, 7)
        };

        var results = _generator.Generate(component);
        Assert.Single(results);
    }

    [Fact]
    public void EdgeCase_LargeGap_SameRow_ShouldNotMatch()
    {
        // B . . . . B . . . B (3 cells in same row but not consecutive)
        var component = new HashSet<Position>
        {
            new(0, 0), new(5, 0), new(9, 0)
        };

        var results = _generator.Generate(component);
        Assert.Empty(results);
    }

    #endregion
}
