using System.Numerics;
using Match3.Core.Config;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Physics;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Physics;

public class RealtimeGravityTests
{
    private class StubRandom : IRandom
    {
        public float NextFloat() => 0f;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    [Fact]
    public void Update_ShouldMoveFloatingTileDown()
    {
        // Arrange
        var state = new GameState(1, 10, 5, new StubRandom());
        // Clear board
        for(int y=0; y<10; y++) state.SetTile(0, y, new Tile(y, TileType.None, 0, y));
        
        // Place a tile at (0, 0) - Top of board
        var tile = new Tile(100, TileType.Red, 0, 0);
        state.SetTile(0, 0, tile);
        
        var physics = new RealtimeGravitySystem(new Match3Config());
        
        // Act
        // Simulate 0.05s (Reduced dt to prevent logical swap at high speed)
        physics.Update(ref state, 0.05f);
        
        // Assert
        var newTile = state.GetTile(0, 0);
        
        // It should have moved down (Y increases)
        // Gravity = 35. 0.1s -> v = 3.5. pos += 3.5 * 0.1 = 0.35.
        // So it is still at Grid[0,0] but Position.Y should be ~0.35.
        
        Assert.True(newTile.Position.Y > 0, $"Tile should have moved down. Pos: {newTile.Position.Y}");
        Assert.True(newTile.IsFalling, "Tile should be in Falling state");
        Assert.True(newTile.Velocity.Y > 0, "Tile should have downward velocity");
    }

    [Fact]
    public void Update_ShouldStopAtFloor()
    {
        // Arrange
        var state = new GameState(1, 2, 5, new StubRandom());
        // (0,0) = Red, (0,1) = None
        // Tile at (0,0) will fall to (0,1)
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(0, 1, new Tile(2, TileType.None, 0, 1));
        
        var physics = new RealtimeGravitySystem(new Match3Config());
        
        // Act
        // Simulate enough time to fall 1 unit.
        // s = 0.5 * g * t^2. 1 = 0.5 * 35 * t^2 => t^2 = 1/17.5 = 0.057 => t = 0.24s.
        // Let's run for 0.5s to be sure.
        
        float dt = 0.016f; // 60fps
        for(int i=0; i<30; i++) // ~0.5s
        {
            physics.Update(ref state, dt);
        }
        
        // Assert
        // Tile should be at (0,1)
        var tileAt0 = state.GetTile(0, 0);
        var tileAt1 = state.GetTile(0, 1);
        
        Assert.Equal(TileType.None, tileAt0.Type); // Should be empty
        Assert.Equal(TileType.Red, tileAt1.Type); // Should have moved here
        Assert.Equal(1.0f, tileAt1.Position.Y, 0.001f); // Should be exactly at 1.0
        Assert.False(tileAt1.IsFalling, "Tile should have stopped");
        Assert.Equal(0f, tileAt1.Velocity.Y);
    }
}
