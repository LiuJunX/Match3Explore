using System.Collections.Generic;
using Match3.Core;
using Xunit;

namespace Match3.Tests;

public class Match3ControllerTests
{
    [Fact]
    public void TrySwap_ValidSwap_UpdatesViewAndResolves()
    {
        // Arrange
        // Note: TestRandomGenerator is defined in GameBoardTests.cs and is accessible here
        var rng = new TestRandomGenerator(); 
        var board = new GameBoard(4, 4, 5, rng);
        var view = new MockGameView();
        var controller = new Match3Controller(board, view);

        // Setup a specific board state
        // Row 0: R R G R  (Swap G<->R at x=2,3 to make R R R R)
        ClearBoard(board);
        board.Set(new Position(0, 0), TileType.Red);
        board.Set(new Position(1, 0), TileType.Red);
        board.Set(new Position(2, 0), TileType.Green);
        board.Set(new Position(3, 0), TileType.Red);
        
        // Act
        // Swap (2,0) <-> (3,0)
        var result = controller.TrySwap(new Position(2, 0), new Position(3, 0));

        // Assert
        Assert.True(result, "Swap should be valid and result in a match");
        Assert.True(view.RenderCalled, "View should be re-rendered");
        Assert.True(view.SwapSuccess, "Swap should be reported as successful");
        
        // Verify matches were detected
        Assert.Single(view.AllMatches); // Should be one set of matches
        var matchSet = view.AllMatches[0];
        Assert.Equal(3, matchSet.Count);
        Assert.Contains(new Position(0, 0), matchSet);
        Assert.Contains(new Position(1, 0), matchSet);
        Assert.Contains(new Position(2, 0), matchSet);
        // (3,0) is Green, so it shouldn't be matched
    }

    [Fact]
    public void TrySwap_InvalidSwap_Reverts()
    {
        // Arrange
        var rng = new TestRandomGenerator();
        var board = new GameBoard(4, 4, 5, rng);
        var view = new MockGameView();
        var controller = new Match3Controller(board, view);

        ClearBoard(board);
        // R G
        board.Set(new Position(0, 0), TileType.Red);
        board.Set(new Position(1, 0), TileType.Green);
        
        // Act
        // Swap (0,0) <-> (1,0) -> G R. No match.
        var result = controller.TrySwap(new Position(0, 0), new Position(1, 0));

        // Assert
        Assert.False(result, "Swap should be invalid (no match created)");
        Assert.False(view.SwapSuccess, "Swap should be reported as rejected");
        
        // Should be reverted to R G
        Assert.Equal(TileType.Red, board.Get(new Position(0, 0)));
        Assert.Equal(TileType.Green, board.Get(new Position(1, 0)));
    }

    private void ClearBoard(GameBoard board)
    {
        for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                board.Set(new Position(x, y), TileType.None);
    }
}

public class MockGameView : IGameView
{
    public bool RenderCalled { get; private set; }
    public bool SwapSuccess { get; private set; }
    public List<List<Position>> AllMatches { get; } = new();

    public void RenderBoard(TileType[,] board) => RenderCalled = true;
    public void ShowSwap(Position a, Position b, bool success) => SwapSuccess = success;
    public void ShowMatches(IReadOnlyCollection<Position> matched) 
    {
        AllMatches.Add(new List<Position>(matched));
    }
    public void ShowGravity() { }
    public void ShowRefill() { }
}
