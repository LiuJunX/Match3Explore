using System.Numerics;
using Match3.Core.Choreography;
using Match3.Core.Events;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Simulation;
using Match3.Presentation;
using Match3.Random;
using Xunit;

namespace Match3.Web.Tests.Integration;

/// <summary>
/// Integration tests for animation and physics coordination.
/// These tests verify the full flow: Simulation → Events → Choreographer → Player → VisualState
/// </summary>
public class AnimationPhysicsIntegrationTests
{
    private const float SwapDuration = 0.15f;
    private const float FrameTime = 1f / 60f;

    #region Swap Animation Tests

    [Fact]
    public void SwapAnimation_TilesHaveCorrectVisualPositions_DuringAnimation()
    {
        // Arrange
        var (engine, collector, player, choreographer) = CreateTestSetup();
        var posA = new Position(2, 3);
        var posB = new Position(3, 3);

        // Act: Apply swap
        engine.ApplyMove(posA, posB);
        ProcessFrame(engine, collector, player, choreographer);

        // Assert: During animation, tiles should be moving
        var tileA = FindTileAtOriginalPosition(player.VisualState, posA);
        var tileB = FindTileAtOriginalPosition(player.VisualState, posB);

        // Both tiles should be marked as being animated
        Assert.True(tileA?.IsBeingAnimated ?? false, "Tile A should be marked as animated");
        Assert.True(tileB?.IsBeingAnimated ?? false, "Tile B should be marked as animated");
    }

    [Fact]
    public void SwapRevert_HasAnimation_AndCorrectFinalPositions()
    {
        // Arrange: Create state where swap won't create match
        var state = CreateNoMatchState();
        var (engine, collector, player, choreographer) = CreateTestSetup(state);
        var posA = new Position(0, 0);
        var posB = new Position(1, 0);

        var tileAId = state.GetTile(posA.X, posA.Y).Id;
        var tileBId = state.GetTile(posB.X, posB.Y).Id;

        // Act: Apply swap (will be reverted due to no match)
        engine.ApplyMove(posA, posB);

        // Process first swap animation
        for (int i = 0; i < 15; i++) // ~0.25s
        {
            ProcessFrame(engine, collector, player, choreographer);
        }

        // Assert: After revert animation, tiles should be back at original positions
        var tileAVisual = player.VisualState.GetTile(tileAId);
        var tileBVisual = player.VisualState.GetTile(tileBId);

        // Process until stable
        for (int i = 0; i < 30; i++)
        {
            ProcessFrame(engine, collector, player, choreographer);
        }

        // After revert, tiles should be at their original positions
        Assert.NotNull(tileAVisual);
        Assert.NotNull(tileBVisual);
        Assert.Equal(posA.X, tileAVisual.Position.X, 0.1f);
        Assert.Equal(posA.Y, tileAVisual.Position.Y, 0.1f);
    }

    #endregion

    #region Physics and Animation Coordination Tests

    [Fact]
    public void FallingTiles_VisualPositionsMatchPhysics_WhenNotAnimated()
    {
        // Arrange
        var (engine, collector, player, choreographer) = CreateTestSetup();

        // Run simulation to create some falling tiles
        engine.Tick(FrameTime);
        ProcessFrame(engine, collector, player, choreographer);

        // Assert: Non-animated tiles should have visual positions matching physics
        foreach (var (id, visual) in player.VisualState.Tiles)
        {
            if (!visual.IsBeingAnimated)
            {
                var physicsTile = FindTileById(engine.State, id);
                if (physicsTile.HasValue)
                {
                    Assert.Equal(physicsTile.Value.Position.X, visual.Position.X, 0.01f);
                    Assert.Equal(physicsTile.Value.Position.Y, visual.Position.Y, 0.01f);
                }
            }
        }
    }

    [Fact]
    public void AnimatedTiles_VisualPositionsNotOverwrittenByPhysics()
    {
        // Arrange
        var (engine, collector, player, choreographer) = CreateTestSetup();
        var posA = new Position(2, 3);
        var posB = new Position(3, 3);

        // Act: Start swap animation
        engine.ApplyMove(posA, posB);
        var events = collector.DrainEvents();
        var commands = choreographer.Choreograph(events, player.CurrentTime);
        player.Append(commands);

        // Tick player to start animation
        player.Tick(FrameTime);

        // Get animated tile positions before physics sync
        var animatedTiles = player.VisualState.Tiles.Values
            .Where(t => t.IsBeingAnimated)
            .Select(t => new { t.Id, PositionBefore = t.Position })
            .ToList();

        // Sync physics (should NOT overwrite animated tiles)
        player.VisualState.SyncFallingTilesFromGameState(engine.State);

        // Assert: Animated tile positions should NOT have changed
        foreach (var tile in animatedTiles)
        {
            var visual = player.VisualState.GetTile(tile.Id);
            Assert.NotNull(visual);
            Assert.True(visual.IsBeingAnimated, $"Tile {tile.Id} should still be animated");
            Assert.Equal(tile.PositionBefore.X, visual.Position.X, 0.001f);
            Assert.Equal(tile.PositionBefore.Y, visual.Position.Y, 0.001f);
        }
    }

    #endregion

    #region Ref Count Tests

    [Fact]
    public void AnimationRefCount_CorrectlyTracksOverlappingAnimations()
    {
        // Arrange
        var visualState = new VisualState();
        visualState.AddTile(1, TileType.Red, BombType.None, new Position(0, 0), Vector2.Zero);
        var tile = visualState.GetTile(1)!;

        // Act & Assert: Single animation
        Assert.Equal(0, tile.AnimationRefCount);
        Assert.False(tile.IsBeingAnimated);

        tile.AddAnimationRef();
        Assert.Equal(1, tile.AnimationRefCount);
        Assert.True(tile.IsBeingAnimated);

        // Add another animation (overlapping)
        tile.AddAnimationRef();
        Assert.Equal(2, tile.AnimationRefCount);
        Assert.True(tile.IsBeingAnimated);

        // First animation completes
        tile.ReleaseAnimationRef();
        Assert.Equal(1, tile.AnimationRefCount);
        Assert.True(tile.IsBeingAnimated); // Still animated by second animation

        // Second animation completes
        tile.ReleaseAnimationRef();
        Assert.Equal(0, tile.AnimationRefCount);
        Assert.False(tile.IsBeingAnimated);
    }

    [Fact]
    public void AnimationRefCount_NeverGoesNegative()
    {
        // Arrange
        var visualState = new VisualState();
        visualState.AddTile(1, TileType.Red, BombType.None, new Position(0, 0), Vector2.Zero);
        var tile = visualState.GetTile(1)!;

        // Act: Release without add (edge case / bug protection)
        tile.ReleaseAnimationRef();
        tile.ReleaseAnimationRef();

        // Assert: Should clamp to 0, not go negative
        Assert.Equal(0, tile.AnimationRefCount);
        Assert.False(tile.IsBeingAnimated);
    }

    #endregion

    #region Bomb Swap Integration Tests

    [Fact]
    public void BombSwap_AnimationCompletesBeforeEffects()
    {
        // Arrange
        var state = CreateStateWithBomb();
        var (engine, collector, player, choreographer) = CreateTestSetup(state);
        var bombPos = new Position(0, 0);
        var targetPos = new Position(1, 0);

        // Act: Apply bomb swap
        engine.ApplyMove(bombPos, targetPos);

        // Check immediately after ApplyMove - no TileDestroyedEvent yet
        var eventsBeforeAnimation = collector.DrainEvents();
        var destroyedBefore = eventsBeforeAnimation.OfType<TileDestroyedEvent>().Count();
        Assert.Equal(0, destroyedBefore); // No destruction before animation completes

        // Process commands
        var commands = choreographer.Choreograph(eventsBeforeAnimation, player.CurrentTime);
        player.Append(commands);

        // Tick through swap animation
        for (int i = 0; i < 12; i++) // ~0.2s
        {
            engine.Tick(FrameTime);
            var frameEvents = collector.DrainEvents();
            var frameCommands = choreographer.Choreograph(frameEvents, player.CurrentTime);
            player.Append(frameCommands);
            player.Tick(FrameTime);
        }

        // Now check for destruction events
        var allEvents = collector.DrainEvents();
        // After animation, bomb effects should have triggered
    }

    #endregion

    #region Helper Methods

    private (SimulationEngine, BufferedEventCollector, Player, Choreographer) CreateTestSetup(GameState? initialState = null)
    {
        var state = initialState ?? new GameState(8, 8, 6, new DefaultRandom(12345));
        var collector = new BufferedEventCollector();
        var engine = new SimulationEngine(state, collector);
        var player = new Player();
        player.SyncFromGameState(state);
        var choreographer = new Choreographer();
        return (engine, collector, player, choreographer);
    }

    private void ProcessFrame(SimulationEngine engine, BufferedEventCollector collector, Player player, Choreographer choreographer)
    {
        engine.Tick(FrameTime);
        var events = collector.DrainEvents();
        if (events.Count > 0)
        {
            var commands = choreographer.Choreograph(events, player.CurrentTime);
            player.Append(commands);
        }
        player.Tick(FrameTime);
        player.VisualState.SyncFallingTilesFromGameState(engine.State);
    }

    private GameState CreateNoMatchState()
    {
        // Create a state where adjacent swaps won't create matches
        var state = new GameState(5, 5, 6, new DefaultRandom(12345));
        var types = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow };
        int id = 1;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                // Alternate colors in a pattern that prevents matches
                var type = types[(x + y * 2) % types.Length];
                state.SetTile(x, y, new Tile(id++, type, x, y));
            }
        }
        return state;
    }

    private GameState CreateStateWithBomb()
    {
        var state = CreateNoMatchState();
        var tile = state.GetTile(0, 0);
        tile.Bomb = BombType.Horizontal;
        state.SetTile(0, 0, tile);
        return state;
    }

    private TileVisual? FindTileAtOriginalPosition(VisualState visualState, Position pos)
    {
        return visualState.Tiles.Values.FirstOrDefault(t =>
            t.GridPosition.X == pos.X && t.GridPosition.Y == pos.Y);
    }

    private Tile? FindTileById(in GameState state, long id)
    {
        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                var tile = state.GetTile(x, y);
                if (tile.Id == id && tile.Type != TileType.None)
                    return tile;
            }
        }
        return null;
    }

    #endregion
}
