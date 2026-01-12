using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Core;
using Match3.Core.Systems.Physics;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Core;

public class AsyncGameLoopTests
{
    private class StubPhysics : IPhysicsSimulation
    {
        public void Update(ref GameState state, float deltaTime) { }
        public bool IsStable(in GameState state) => true;
    }

    private class StubTileGenerator : ITileGenerator
    {
        public TileType GenerateNonMatchingTile(ref GameState state, int x, int y) => TileType.Blue;
    }

    private class MockMatchFinder : IMatchFinder
    {
        public List<MatchGroup> GroupsToReturn = new();
        public List<MatchGroup> FindMatchGroups(in GameState state, IEnumerable<Position>? foci = null) => GroupsToReturn;
        public bool HasMatchAt(in GameState state, Position p) => false;
        public bool HasMatches(in GameState state) => false;
    }

    private class SpyMatchProcessor : IMatchProcessor
    {
        public bool ProcessMatchesCalled { get; private set; }
        public int ProcessMatches(ref GameState state, List<MatchGroup> groups)
        {
            ProcessMatchesCalled = true;
            return 0;
        }
    }
    
    private class StubPowerUp : IPowerUpHandler
    {
        public void ActivateBomb(ref GameState state, Position p) { }
        public void HandlePowerUp(ref GameState state, Position p, BombType bomb) { }
        public bool TryActivate(ref GameState state, Position p) => false;
        public void ProcessSpecialMove(ref GameState state, Position a, Position b, out int score) { score = 0; }
    }

    private class StubRandom : IRandom
    {
        public float NextFloat() => 0f;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    [Fact]
    public void Update_ShouldProcessMatches_WhenFinderReturnsGroups()
    {
        // Arrange
        var state = new GameState(8, 8, 5, new StubRandom());
        var physics = new StubPhysics();
        var generator = new StubTileGenerator();
        var refill = new RealtimeRefillSystem(generator);
        var finder = new MockMatchFinder();
        var processor = new SpyMatchProcessor();
        var powerUp = new StubPowerUp();
        
        var loop = new AsyncGameLoopSystem(physics, refill, finder, processor, powerUp);
        
        // Setup match
        var group = new MatchGroup { Type = TileType.Red };
        group.Positions.Add(new Position(0, 0));
        finder.GroupsToReturn.Add(group);
        
        // Pre-fill board to ensure stability
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        
        // Act
        loop.Update(ref state, 0.1f);
        
        // Assert
        Assert.False(state.GetTile(0,0).IsFalling, "Tile should not be falling");
        Assert.True(processor.ProcessMatchesCalled, "MatchProcessor should be called");
    }
}
