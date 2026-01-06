using Xunit;
using Match3.Core;

namespace Match3.Tests;

public class GameBoardTests
{
    [Fact]
    public void Initialize_CreatesCorrectDimensions()
    {
        // Arrange
        int width = 8;
        int height = 8;
        int tileCount = 5;
        var rng = new TestRandomGenerator();

        // Act
        var board = new GameBoard(width, height, tileCount, rng);

        // Assert
        Assert.Equal(width, board.Width);
        Assert.Equal(height, board.Height);
    }

    [Fact]
    public void FindMatches_FoundHorizontalMatch()
    {
        // Arrange
        var board = CreateAndClearBoard(5, 5);
        // R R R G B
        board.Set(new Position(0, 0), TileType.Red);
        board.Set(new Position(1, 0), TileType.Red);
        board.Set(new Position(2, 0), TileType.Red);
        board.Set(new Position(3, 0), TileType.Green);
        board.Set(new Position(4, 0), TileType.Blue);

        // Act
        var matches = board.FindMatches();

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
        var board = CreateAndClearBoard(5, 5);
        // R
        // R
        // R
        board.Set(new Position(0, 0), TileType.Red);
        board.Set(new Position(0, 1), TileType.Red);
        board.Set(new Position(0, 2), TileType.Red);
        board.Set(new Position(0, 3), TileType.Green);

        // Act
        var matches = board.FindMatches();

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
        var board = CreateAndClearBoard(5, 5);
        var p1 = new Position(0, 0);
        var p2 = new Position(1, 0);
        board.Set(p1, TileType.Red);
        board.Set(p2, TileType.Blue);

        // Act
        board.Swap(p1, p2);

        // Assert
        Assert.Equal(TileType.Blue, board.Get(p1));
        Assert.Equal(TileType.Red, board.Get(p2));
    }

    [Fact]
    public void ApplyGravity_TilesFall()
    {
        // Arrange
        var board = CreateAndClearBoard(3, 5);
        // Col 0:
        // (4) .
        // (3) .
        // (2) R  <- This should fall to 4
        // (1) .
        // (0) .
        board.Set(new Position(0, 2), TileType.Red);

        // Act
        board.ApplyGravity();

        // Assert
        // The tile at (0,2) should fall to (0,4) (bottom)
        Assert.Equal(TileType.Red, board.Get(new Position(0, 4)));
        Assert.Equal(TileType.None, board.Get(new Position(0, 2)));
    }

    private GameBoard CreateAndClearBoard(int width, int height)
    {
        var board = new GameBoard(width, height, 5, new TestRandomGenerator());
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                board.Set(new Position(x, y), TileType.None);
            }
        }
        return board;
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