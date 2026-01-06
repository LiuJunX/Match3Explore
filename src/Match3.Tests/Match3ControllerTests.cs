using System.Collections.Generic;
using Match3.Core;
using Xunit;

namespace Match3.Tests;

public class Match3ControllerTests
{
    // Need a way to inject specific state into the controller for testing, 
    // since we can no longer access the private GameState directly or inject a GameBoard.
    // However, Match3Controller now creates its own state.
    // For unit testing, we might need to expose a way to setup the board, or use a "TestableMatch3Controller" 
    // or keep the tests high level.
    // Given the strict requirement to use the new architecture, we should update the tests to use the new constructor.
    // BUT: The tests heavily rely on `ClearBoard` and `board.Set` to setup scenarios.
    // `Match3Controller` does not expose `Set` method anymore (encapsulation).
    // To support testing, we should probably add a debug/test method or use a subclass.
    // For now, let's assume we can't easily modify the internal state without exposing it.
    // I will add a `SetTile` method to Match3Controller purely for testing/debug purposes? 
    // Or better, I will assume we should trust the random generator injection.
    
    // Actually, to make this work without breaking encapsulation too much, 
    // I'll add a temporary "SetTile" method to Match3Controller or rely on a deterministic seed.
    // But specific setups like "R R G R" are hard with just a seed.
    
    // Let's modify Match3Controller to allow setting state for tests? 
    // Or better, since this is a "Core" rewrite, maybe I should expose a way to load a state.
    
    // For this refactoring to be successful and strictly followed, tests must pass.
    // I will use reflection in tests to set the state if necessary, or better, 
    // I will modify Match3Controller to accept an initial state factory or similar?
    // No, keep it simple. I'll add a `SetTileForTest` method marked internal, 
    // and make InternalsVisibleTo Match3.Tests.
    
    // Wait, Match3.Core doesn't have InternalsVisibleTo set up yet.
    // I'll assume for now I can add a method `DebugSetTile` to Match3Controller.
    
    [Fact]
    public void TrySwap_ValidSwap_UpdatesViewAndResolves()
    {
        // Arrange
        var rng = new TestRandomGenerator(); 
        var view = new MockGameView();
        var controller = new Match3Controller(4, 4, 5, rng, view);

        // Setup a specific board state
        // Row 0: R R G R  (Swap G<->R at x=2,3 to make R R R R)
        // We need a way to set the board.
        // Since we are rewriting, I'll use a local helper to access the private state via reflection?
        // No, that's brittle.
        // I'll add `SetTile` to Match3Controller as it is a controller, maybe it's fine for it to have "God mode" or cheat methods?
        // No, `Match3Controller` handles user input.
        
        // Let's look at how I updated Match3Controller. 
        // I removed `Board` property.
        
        // Strategy: 
        // I will add `public void DebugSetTile(Position p, TileType t)` to Match3Controller.
        // It's useful for level editors too.
        
        controller.DebugSetTile(new Position(0, 0), TileType.Red);
        controller.DebugSetTile(new Position(1, 0), TileType.Red);
        controller.DebugSetTile(new Position(2, 0), TileType.Green);
        controller.DebugSetTile(new Position(3, 0), TileType.Red);
        
        // Clear others to avoid accidental matches
        for(int y=0; y<4; y++) 
            for(int x=0; x<4; x++) 
                if(y > 0) controller.DebugSetTile(new Position(x, y), TileType.None);
        
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
        // 0,0 1,0 2,0 should match (after swap 2,0 becomes Red)
        // 3,0 becomes Green.
        Assert.Contains(new Position(0, 0), matchSet);
        Assert.Contains(new Position(1, 0), matchSet);
        Assert.Contains(new Position(2, 0), matchSet);
    }

    [Fact]
    public void TrySwap_InvalidSwap_Reverts()
    {
        // Arrange
        var rng = new TestRandomGenerator();
        var view = new MockGameView();
        var controller = new Match3Controller(4, 4, 5, rng, view);

        // Clear
        for(int y=0; y<4; y++) for(int x=0; x<4; x++) controller.DebugSetTile(new Position(x, y), TileType.None);

        // R G
        controller.DebugSetTile(new Position(0, 0), TileType.Red);
        controller.DebugSetTile(new Position(1, 0), TileType.Green);
        
        // Act
        // Swap (0,0) <-> (1,0) -> G R. No match.
        var result = controller.TrySwap(new Position(0, 0), new Position(1, 0));

        // Assert
        Assert.False(result, "Swap should be invalid (no match created)");
        Assert.False(view.SwapSuccess, "Swap should be reported as rejected");
        
        // Should be reverted to R G
        // We need a way to check state.
        // `GetTile` ?
        Assert.Equal(TileType.Red, controller.DebugGetTile(new Position(0, 0)));
        Assert.Equal(TileType.Green, controller.DebugGetTile(new Position(1, 0)));
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
