using System.Linq;
using Match3.Core.Config;
using Match3.Core.Events;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Simulation;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Matching.Generation;
using Match3.Core.Systems.Physics;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Scoring;
using Match3.Core.Systems.Spawning;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Simulation;

public class SimulationEngineTests
{
    private class StubRandom : IRandom
    {
        public float NextFloat() => 0f;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    private class StubScoreSystem : IScoreSystem
    {
        public int CalculateMatchScore(MatchGroup match) => 10;
        public int CalculateSpecialMoveScore(TileType t1, BombType b1, TileType t2, BombType b2) => 100;
    }

    private class StubSpawnModel : ISpawnModel
    {
        public TileType Predict(ref GameState state, int spawnX, in SpawnContext context) => TileType.Blue;
    }

    private SimulationEngine CreateEngine(GameState state, IEventCollector? eventCollector = null)
    {
        var random = new StubRandom();
        var config = new Match3Config();
        var physics = new RealtimeGravitySystem(config, random);
        var refill = new RealtimeRefillSystem(new StubSpawnModel());
        var bombGenerator = new BombGenerator();
        var matchFinder = new ClassicMatchFinder(bombGenerator);
        var scoreSystem = new StubScoreSystem();
        var matchProcessor = new StandardMatchProcessor(scoreSystem, BombEffectRegistry.CreateDefault());
        var powerUpHandler = new PowerUpHandler(scoreSystem);

        return new SimulationEngine(
            state,
            SimulationConfig.ForHumanPlay(),
            physics,
            refill,
            matchFinder,
            matchProcessor,
            powerUpHandler,
            null,
            eventCollector);
    }

    #region Basic Tick Tests

    [Fact]
    public void Tick_IncrementsTickCounter()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        Assert.Equal(0, engine.CurrentTick);

        engine.Tick();

        Assert.Equal(1, engine.CurrentTick);
    }

    [Fact]
    public void Tick_IncrementsElapsedTime()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        Assert.Equal(0f, engine.ElapsedTime);

        engine.Tick(0.016f);

        Assert.Equal(0.016f, engine.ElapsedTime, 0.001f);
    }

    [Fact]
    public void Tick_ReturnsTickResult()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var result = engine.Tick();

        Assert.Equal(1, result.CurrentTick);
        Assert.True(result.IsStable);
    }

    #endregion

    #region Stability Tests

    [Fact]
    public void IsStable_ReturnsTrueForStableBoard()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        Assert.True(engine.IsStable());
    }

    [Fact]
    public void IsStable_ReturnsFalseWithFallingTiles()
    {
        var state = CreateStateWithFallingTile();
        var engine = CreateEngine(state);

        Assert.False(engine.IsStable());
    }

    #endregion

    #region ApplyMove Tests

    [Fact]
    public void ApplyMove_SwapsTiles()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var tileABefore = engine.State.GetTile(0, 0).Type;
        var tileBBefore = engine.State.GetTile(1, 0).Type;

        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        var tileAAfter = engine.State.GetTile(0, 0).Type;
        var tileBAfter = engine.State.GetTile(1, 0).Type;

        Assert.Equal(tileABefore, tileBAfter);
        Assert.Equal(tileBBefore, tileAAfter);
    }

    [Fact]
    public void ApplyMove_ReturnsFalseForInvalidPosition()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var result = engine.ApplyMove(new Position(-1, 0), new Position(0, 0));

        Assert.False(result);
    }

    [Fact]
    public void ApplyMove_EmitsSwapEvent()
    {
        var state = CreateStableState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        var events = collector.GetEvents();
        Assert.Contains(events, e => e is TilesSwappedEvent);
    }

    #endregion

    #region RunUntilStable Tests

    [Fact]
    public void RunUntilStable_ReachesStability()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var result = engine.RunUntilStable();

        Assert.True(result.ReachedStability);
    }

    [Fact]
    public void RunUntilStable_DisablesEventCollection()
    {
        var state = CreateStableState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        // Initially events are enabled
        Assert.True(engine.EventCollector.IsEnabled);

        engine.RunUntilStable();

        // After RunUntilStable, original collector should be restored
        Assert.True(engine.EventCollector.IsEnabled);
    }

    [Fact]
    public void RunUntilStable_ReturnsSimulationResult()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var result = engine.RunUntilStable();

        Assert.True(result.FinalState.Width > 0); // FinalState is valid
        Assert.True(result.TickCount >= 0);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentEngine()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var cloned = engine.Clone();

        // Modify original
        engine.Tick();

        // Clone should not be affected
        Assert.Equal(0, cloned.CurrentTick);
    }

    [Fact]
    public void Clone_UsesNullEventCollector()
    {
        var state = CreateStableState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        var cloned = engine.Clone();

        Assert.False(cloned.EventCollector.IsEnabled);
    }

    [Fact]
    public void Clone_CanUseCustomRandom()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);
        var customRandom = new StubRandom();

        var cloned = engine.Clone(customRandom);

        Assert.NotNull(cloned);
        Assert.Equal(0, cloned.CurrentTick);
    }

    #endregion

    #region Event Collection Tests

    [Fact]
    public void SetEventCollector_ChangesCollector()
    {
        var state = CreateStableState();
        var engine = CreateEngine(state);

        var newCollector = new BufferedEventCollector();
        engine.SetEventCollector(newCollector);

        Assert.Same(newCollector, engine.EventCollector);
    }

    [Fact]
    public void SetEventCollector_NullDefaultsToNullCollector()
    {
        var state = CreateStableState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        engine.SetEventCollector(null!);

        Assert.False(engine.EventCollector.IsEnabled);
    }

    #endregion

    #region Invalid Swap Revert Tests

    [Fact]
    public void ApplyMove_InvalidSwap_IsNotStableUntilRevertComplete()
    {
        // Arrange: Create a board where swapping (0,0) and (1,0) creates no match
        var state = CreateNoMatchSwapState();
        var engine = CreateEngine(state);

        // Act: Apply invalid swap
        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        // Assert: Engine should not be stable (pending move validation)
        Assert.False(engine.IsStable());
    }

    [Fact]
    public void ApplyMove_InvalidSwap_RevertsAfterAnimationDuration()
    {
        // Arrange
        var state = CreateNoMatchSwapState();
        var engine = CreateEngine(state);

        var originalTileA = state.GetTile(0, 0).Type;
        var originalTileB = state.GetTile(1, 0).Type;

        // Act: Apply invalid swap
        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        // After swap, tiles are in swapped positions
        Assert.Equal(originalTileB, engine.State.GetTile(0, 0).Type);
        Assert.Equal(originalTileA, engine.State.GetTile(1, 0).Type);

        // Run enough ticks to complete the swap animation and trigger revert
        // SwapAnimationDuration = 0.15f, FixedDeltaTime = 0.016f
        // Need about 10 ticks to pass 0.15 seconds
        for (int i = 0; i < 15; i++)
        {
            engine.Tick();
        }

        // Assert: Tiles should be back in original positions
        Assert.Equal(originalTileA, engine.State.GetTile(0, 0).Type);
        Assert.Equal(originalTileB, engine.State.GetTile(1, 0).Type);
    }

    [Fact]
    public void ApplyMove_InvalidSwap_EmitsRevertEvent()
    {
        // Arrange
        var state = CreateNoMatchSwapState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        // Act: Apply invalid swap
        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        // Clear initial swap event
        collector.Clear();

        // Run enough ticks to trigger revert
        for (int i = 0; i < 15; i++)
        {
            engine.Tick();
        }

        // Assert: Should have emitted a revert event
        var events = collector.GetEvents();
        var revertEvent = events.OfType<TilesSwappedEvent>().FirstOrDefault(e => e.IsRevert);
        Assert.NotNull(revertEvent);
        Assert.True(revertEvent.IsRevert);
    }

    [Fact]
    public void ApplyMove_ValidSwap_DoesNotRevert()
    {
        // Arrange: Create a board where swapping creates a match
        var state = CreateMatchOnSwapState();
        var collector = new BufferedEventCollector();
        var engine = CreateEngine(state, collector);

        var originalTileA = state.GetTile(1, 0).Type; // Will become part of match

        // Act: Apply valid swap that creates a match
        engine.ApplyMove(new Position(0, 0), new Position(1, 0));

        // Clear initial swap event
        collector.Clear();

        // Run ticks
        for (int i = 0; i < 15; i++)
        {
            engine.Tick();
        }

        // Assert: Should NOT have emitted a revert event
        var events = collector.GetEvents();
        var revertEvent = events.OfType<TilesSwappedEvent>().FirstOrDefault(e => e.IsRevert);
        Assert.Null(revertEvent);
    }

    #endregion

    #region Helper Methods

    private GameState CreateStableState()
    {
        // Create a 5x5 board with no matches
        var state = new GameState(5, 5, 4, new StubRandom());
        var types = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow };

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int idx = y * 5 + x;
                // Pattern that avoids matches
                var type = types[(x + y * 2) % types.Length];
                state.SetTile(x, y, new Tile(idx + 1, type, x, y));
            }
        }

        return state;
    }

    private GameState CreateStateWithFallingTile()
    {
        var state = CreateStableState();
        var tile = state.GetTile(0, 0);
        tile.IsFalling = true;
        // Physics system considers velocity/position for stability, not IsFalling flag
        // Set velocity to make the tile actually unstable
        tile.Velocity = new System.Numerics.Vector2(0, 1.0f);
        state.SetTile(0, 0, tile);
        return state;
    }

    /// <summary>
    /// Creates a state where swapping (0,0) and (1,0) does NOT create a match.
    /// Layout:
    ///   R B G Y R
    ///   B G Y R B
    ///   ...
    /// Swapping R and B at row 0 won't create a match.
    /// </summary>
    private GameState CreateNoMatchSwapState()
    {
        var state = new GameState(5, 5, 4, new StubRandom());
        var types = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow };

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int idx = y * 5 + x;
                // Pattern that avoids matches even after any single swap
                var type = types[(x + y * 2) % types.Length];
                state.SetTile(x, y, new Tile(idx + 1, type, x, y));
            }
        }

        return state;
    }

    /// <summary>
    /// Creates a state where swapping (0,0) and (1,0) DOES create a match.
    /// Layout:
    ///   R B R R G
    ///   ...
    /// Swapping R(0,0) and B(1,0) results in: B R R R G
    /// Now position (1,0), (2,0), (3,0) are all R → match at position (1,0)!
    /// </summary>
    private GameState CreateMatchOnSwapState()
    {
        var state = new GameState(5, 5, 4, new StubRandom());

        // First row: R B R R G - swapping (0,0) R with (1,0) B creates R R R match at 1,2,3
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Blue, 1, 0));
        state.SetTile(2, 0, new Tile(3, TileType.Red, 2, 0));
        state.SetTile(3, 0, new Tile(4, TileType.Red, 3, 0));
        state.SetTile(4, 0, new Tile(5, TileType.Green, 4, 0));

        // Fill rest with non-matching pattern
        var types = new[] { TileType.Blue, TileType.Green, TileType.Yellow, TileType.Purple };
        for (int y = 1; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int idx = y * 5 + x;
                var type = types[(x + y) % types.Length];
                state.SetTile(x, y, new Tile(idx + 1, type, x, y));
            }
        }

        return state;
    }

    #endregion
}
