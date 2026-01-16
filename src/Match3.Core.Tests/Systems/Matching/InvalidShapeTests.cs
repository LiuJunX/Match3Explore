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
