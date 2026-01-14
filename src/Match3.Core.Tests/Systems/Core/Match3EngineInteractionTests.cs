using System;
using System.Collections.Generic;
using System.Reflection;
using Match3.Core;
using Match3.Core.Config;
using Match3.Core.Systems.Core;
using Match3.Core.Systems.Generation;
using Match3.Core.Systems.Input;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Physics;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Scoring;
using Match3.Core.View;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Models.Gameplay;
using Match3.Core.Systems.Generation;
using Match3.Core.Systems.Input;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Scoring;
using Match3.Core.Utility;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Core;

public class Match3EngineInteractionTests
{
    private class StubInputSystem : IInputSystem
    {
        public event Action<Position>? TapDetected;
        public event Action<Position, Direction>? SwipeDetected;
        public void Configure(double cellSize) { }
        public void OnPointerDown(int gx, int gy, double sx, double sy) { }
        public void OnPointerUp(double sx, double sy) { }
        public void OnPointerMove(double sx, double sy) { }
        public bool IsValidPosition(in GameState state, Position p) => p.X >= 0 && p.X < state.Width && p.Y >= 0 && p.Y < state.Height;
        public Position GetSwipeTarget(Position from, Direction direction)
        {
            return direction switch
            {
                Direction.Up => new Position(from.X, from.Y + 1),
                Direction.Down => new Position(from.X, from.Y - 1),
                Direction.Left => new Position(from.X - 1, from.Y),
                Direction.Right => new Position(from.X + 1, from.Y),
                _ => from
            };
        }
        public void TriggerTap(Position p) => TapDetected?.Invoke(p);
        public void TriggerSwipe(Position p, Direction d) => SwipeDetected?.Invoke(p, d);
    }

    private class StubAsyncGameLoopSystem : IAsyncGameLoopSystem
    {
        public bool ActivateBombCalled { get; private set; }
        public Position? LastBombPosition { get; private set; }

        public void Update(ref GameState state, float dt) { }

        public void ActivateBomb(ref GameState state, Position p)
        {
            ActivateBombCalled = true;
            LastBombPosition = p;
        }
    }

    private class StubInteractionSystem : IInteractionSystem
    {
        public string StatusMessage => "Stub";
        public bool ShouldSucceed { get; set; } = true;
        public Move? MoveToReturn { get; set; } = null;

        public bool TryHandleTap(ref GameState state, Position p, bool isInteractive, out Move? move)
        {
            move = MoveToReturn;
            return ShouldSucceed;
        }

        public bool TryHandleSwipe(ref GameState state, Position from, Direction direction, bool isInteractive, out Move? move)
        {
            move = MoveToReturn;
            return ShouldSucceed;
        }
    }

    private class StubAnimationSystem : IAnimationSystem
    {
        public bool IsVisuallyStable => true;
        public bool Animate(ref GameState state, float dt) => true;
        public bool IsVisualAtTarget(in GameState state, Position p) => true;
    }

    private class StubBoardInitializer : IBoardInitializer
    {
        public void Initialize(ref GameState state, LevelConfig? levelConfig) { }
    }

    private class StubGameView : IGameView
    {
        public void RenderBoard(TileType[,] board) { }
        public void ShowSwap(Position a, Position b, bool success) { }
        public void ShowMatches(IReadOnlyCollection<Position> matched) { }
        public void ShowGravity(IEnumerable<TileMove> moves) { }
        public void ShowRefill(IEnumerable<TileMove> moves) { }
    }

    private class StubLogger : IGameLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInfo<T>(string message, T args) { }
        public void LogInfo<T1, T2>(string message, T1 arg1, T2 arg2) { }
        public void LogInfo<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3) { }
        public void LogWarning<T>(string message, T args) { }
    }

    private class StubRandom : IRandom
    {
        public int Next() => 0;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public float NextFloat() => 0f;
        public bool NextBool() => false;
        public T PickRandom<T>(IList<T> items) => items[0];
        public void Shuffle<T>(IList<T> list) { }
    }

    private class StubMatchFinder : IMatchFinder
    {
        public bool HasMatchResult { get; set; } = true;
        public List<MatchGroup> FindMatchGroups(in GameState state, IEnumerable<Position>? foci = null) => new List<MatchGroup>();
        public bool HasMatchAt(in GameState state, Position p) => HasMatchResult;
        public bool HasMatches(in GameState state) => HasMatchResult;
    }

    private class StubBotSystem : IBotSystem
    {
        public bool TryGetRandomMove(ref GameState state, IInteractionSystem interactionSystem, out Move move)
        {
            move = default;
            return false;
        }
    }

    [Fact]
    public void OnTap_WithBomb_ShouldActivateBomb()
    {
        // Arrange
        var config = new Match3Config(8, 8, 5);
        var inputStub = new StubInputSystem();
        var gameLoopStub = new StubAsyncGameLoopSystem();
        var interactionStub = new StubInteractionSystem();
        var animationStub = new StubAnimationSystem();
        var boardInitStub = new StubBoardInitializer();
        var matchFinderStub = new StubMatchFinder();
        var botSystemStub = new StubBotSystem();
        
        var random = new StubRandom();
        var view = new StubGameView();
        var logger = new StubLogger();
        
        var engine = new Match3Engine(
            config, random, view, logger, inputStub, 
            gameLoopStub, interactionStub, animationStub, boardInitStub, matchFinderStub, botSystemStub
        );

        // Inject a Bomb into State using Reflection
        var stateField = typeof(Match3Engine).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        var state = (GameState)stateField!.GetValue(engine)!;
        
        // Place a bomb at (0,0)
        var bombTile = new Tile(1, TileType.Red, 0, 0);
        bombTile.Bomb = BombType.Horizontal;
        state.SetTile(0, 0, bombTile);

        // Act
        engine.OnTap(new Position(0, 0));

        // Assert
        Assert.True(gameLoopStub.ActivateBombCalled, "ActivateBomb should be called when tapping a bomb");
        Assert.Equal(new Position(0, 0), gameLoopStub.LastBombPosition);
    }

    [Fact]
    public void OnTap_WithNormalTile_ShouldNotActivateBomb()
    {
        // Arrange
        var config = new Match3Config(8, 8, 5);
        var inputStub = new StubInputSystem();
        var gameLoopStub = new StubAsyncGameLoopSystem();
        var interactionStub = new StubInteractionSystem();
        var animationStub = new StubAnimationSystem();
        var boardInitStub = new StubBoardInitializer();
        var matchFinderStub = new StubMatchFinder();
        var botSystemStub = new StubBotSystem();
        
        var random = new StubRandom();
        var view = new StubGameView();
        var logger = new StubLogger();
        
        var engine = new Match3Engine(
            config, random, view, logger, inputStub, 
            gameLoopStub, interactionStub, animationStub, boardInitStub, matchFinderStub, botSystemStub
        );

        // Inject a Normal Tile at (0,0)
        var stateField = typeof(Match3Engine).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        var state = (GameState)stateField!.GetValue(engine)!;
        
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));

        // Act
        engine.OnTap(new Position(0, 0));

        // Assert
        Assert.False(gameLoopStub.ActivateBombCalled, "ActivateBomb should NOT be called when tapping a normal tile");
    }

    [Fact]
    public void OnSwipe_ShouldSwapTiles_WhenMatchFound()
    {
        // Arrange
        var config = new Match3Config(8, 8, 5);
        var inputStub = new StubInputSystem();
        var gameLoopStub = new StubAsyncGameLoopSystem();
        var interactionStub = new StubInteractionSystem();
        var animationStub = new StubAnimationSystem();
        var boardInitStub = new StubBoardInitializer();
        var matchFinderStub = new StubMatchFinder { HasMatchResult = true }; // Simulate valid match
        var botSystemStub = new StubBotSystem();
        
        var random = new StubRandom();
        var view = new StubGameView();
        var logger = new StubLogger();
        
        // Setup Interaction to return a valid move
        interactionStub.MoveToReturn = new Move(new Position(0, 0), new Position(1, 0));
        
        var engine = new Match3Engine(
            config, random, view, logger, inputStub, 
            gameLoopStub, interactionStub, animationStub, boardInitStub, matchFinderStub, botSystemStub
        );

        // Inject tiles
        var stateField = typeof(Match3Engine).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        var state = (GameState)stateField!.GetValue(engine)!;
        
        var t1 = new Tile(1, TileType.Red, 0, 0);
        var t2 = new Tile(2, TileType.Blue, 1, 0);
        state.SetTile(0, 0, t1);
        state.SetTile(1, 0, t2);

        // Act
        // Trigger swipe. InteractionSystem.TryHandleSwipe will return true and the Move.
        inputStub.TriggerSwipe(new Position(0, 0), Direction.Right);

        // Assert
        var tileAt00 = engine.State.GetTile(0, 0);
        var tileAt10 = engine.State.GetTile(1, 0);
        
        // Tiles should be swapped (Blue at 0,0, Red at 1,0)
        Assert.Equal(TileType.Blue, tileAt00.Type);
        Assert.Equal(TileType.Red, tileAt10.Type);
    }
}
