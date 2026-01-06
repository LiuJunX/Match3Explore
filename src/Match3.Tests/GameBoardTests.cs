using Xunit;
using Match3.Core;
using Match3.Core.Structs;
using Match3.Core.Logic;

namespace Match3.Tests;

public class GameBoardTests
{
    // Refactoring Note:
    // GameBoard class is deprecated. We should test GameRules acting on GameState.
    
    [Fact]
    public void Initialize_CreatesCorrectDimensions()
    {
        // Arrange
        int width = 8;
        int height = 8;
        int tileCount = 5;
        var rng = new TestRandomGenerator();

        // Act
        var state = new GameState(width, height, tileCount, rng);
        GameRules.Initialize(ref state);

        // Assert
        Assert.Equal(width, state.Width);
        Assert.Equal(height, state.Height);
    }

    [Fact]
    public void FindMatches_FoundHorizontalMatch()
    {
        // Arrange
        var state = CreateAndClearState(5, 5);
        // R R R G B
        state.Set(0, 0, TileType.Red);
        state.Set(1, 0, TileType.Red);
        state.Set(2, 0, TileType.Red);
        state.Set(3, 0, TileType.Green);
        state.Set(4, 0, TileType.Blue);

        // Act
        var matches = GameRules.FindMatches(in state);

        // Assert
        Assert.Equal(3, matches.Count);
        Assert.Contains(new Position(0, 0), matches);
        Assert.Contains(new Position(1, 0), matches);
        Assert.Contains(new Position(2, 0), matches);
    }

    [Fact]
    public void FindMatches_FoundVerticalMatch()
    {
        // Arrange
        var state = CreateAndClearState(5, 5);
        // R
        // R
        // R
        state.Set(0, 0, TileType.Red);
        state.Set(0, 1, TileType.Red);
        state.Set(0, 2, TileType.Red);
        state.Set(0, 3, TileType.Green);

        // Act
        var matches = GameRules.FindMatches(in state);

        // Assert
        Assert.Equal(3, matches.Count);
        Assert.Contains(new Position(0, 0), matches);
        Assert.Contains(new Position(0, 1), matches);
        Assert.Contains(new Position(0, 2), matches);
    }

    [Fact]
    public void Swap_SwapsTwoTiles()
    {
        // Arrange
        var state = CreateAndClearState(5, 5);
        var p1 = new Position(0, 0);
        var p2 = new Position(1, 0);
        state.Set(p1.X, p1.Y, TileType.Red);
        state.Set(p2.X, p2.Y, TileType.Blue);

        // Act
        GameRules.Swap(ref state, p1, p2);

        // Assert
        Assert.Equal(TileType.Blue, state.Get(p1.X, p1.Y));
        Assert.Equal(TileType.Red, state.Get(p2.X, p2.Y));
    }

    [Fact]
    public void ApplyGravity_TilesFall()
    {
        // Arrange
        var state = CreateAndClearState(3, 5);
        // Col 0:
        // (4) .
        // (3) .
        // (2) R  <- This should fall to 4 (Bottom is height-1 ?? No, height-1 is bottom in most arrays if 0 is top)
        // Wait, let's check coordinate system.
        // In GameBoard.cs/GameRules.cs:
        // ApplyGravity: for (var y = _height - 1; y >= 0; y--) ... writeY = _height - 1
        // So y=Height-1 is the BOTTOM.
        // y=0 is TOP.
        
        // In this test:
        // We set (0, 2) to Red. 
        // 0, 1, 2, 3, 4 (Height=5)
        // 2 is middle.
        // It should fall to 4.
        
        state.Set(0, 2, TileType.Red);

        // Act
        GameRules.ApplyGravity(ref state);

        // Assert
        // The tile at (0,2) should fall to (0,4) (bottom)
        Assert.Equal(TileType.Red, state.Get(0, 4));
        Assert.Equal(TileType.None, state.Get(0, 2));
    }

    private GameState CreateAndClearState(int width, int height)
    {
        var state = new GameState(width, height, 5, new TestRandomGenerator());
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                state.Set(x, y, TileType.None);
            }
        }
        return state;
    }
}

public class TestRandomGenerator : IRandom
{
    private int _val = 0;
    public int Next(int min, int max) 
    {
        return min + (_val++ % (max - min));
    }
}