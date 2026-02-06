using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Spawning;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Spawning;

/// <summary>
/// RuleBasedSpawnModel 单元测试
/// 测试规则驱动的生成点模型
/// </summary>
public class RuleBasedSpawnModelTests
{
    private class StubRandom : IRandom
    {
        private int _value;
        public StubRandom(int value = 0) => _value = value;
        public void SetValue(int value) => _value = value;
        public int Next(int min, int max) => min + (_value % (max - min));
    }

    private class SequentialRandom : IRandom
    {
        private int _counter = 0;
        public int Next(int min, int max) => min + (_counter++ % (max - min));
    }

    private GameState CreateState(int width = 8, int height = 8)
    {
        var state = new GameState(width, height, 6, new SequentialRandom());
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                state.SetTile(x, y, new Tile(y * width + x, TileType.None, x, y));
            }
        }
        return state;
    }

    #region Basic Prediction Tests

    [Fact]
    public void Predict_EmptyBoard_ReturnsValidColor()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState();
        var context = SpawnContext.Default;

        var type = model.Predict(ref state, 0, in context);

        Assert.NotEqual(TileType.None, type);
    }

    [Fact]
    public void Predict_ZeroTileTypes_ReturnsNone()
    {
        var model = new RuleBasedSpawnModel();
        var state = new GameState(3, 3, 0, new StubRandom());
        var context = SpawnContext.Default;

        var type = model.Predict(ref state, 0, in context);

        Assert.Equal(TileType.None, type);
    }

    #endregion

    #region Strategy Tests - Help Mode

    [Fact]
    public void Predict_FailedAttempts_TriggersHelpMode()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup a board where Red would create a match
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.5f,
            RemainingMoves = 10,
            GoalProgress = 0.5f,
            FailedAttempts = 3, // Trigger help mode
            InFlowState = false
        };

        var type = model.Predict(ref state, 2, in context);

        // Should spawn Red to create a match (helping the player)
        Assert.Equal(TileType.Red, type);
    }

    [Fact]
    public void Predict_LastFewMoves_TriggersHelpMode()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup a board where Blue would create a match
        state.SetTile(0, 0, new Tile(1, TileType.Blue, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Blue, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.5f,
            RemainingMoves = 2, // Very few moves left
            GoalProgress = 0.5f, // Not close to goal
            FailedAttempts = 0,
            InFlowState = true
        };

        var type = model.Predict(ref state, 2, in context);

        // Should spawn Blue to create a match
        Assert.Equal(TileType.Blue, type);
    }

    [Fact]
    public void Predict_LowDifficulty_TriggersHelpMode()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup a board where Green would create a match
        state.SetTile(0, 0, new Tile(1, TileType.Green, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Green, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.2f, // Very easy
            RemainingMoves = 20,
            GoalProgress = 0.5f,
            FailedAttempts = 0,
            InFlowState = true
        };

        var type = model.Predict(ref state, 2, in context);

        // Should spawn Green to create a match
        Assert.Equal(TileType.Green, type);
    }

    #endregion

    #region Strategy Tests - Challenge Mode

    [Fact]
    public void Predict_HighDifficulty_AvoidsMatches()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup a board where Red would create a match
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.9f, // Very hard
            RemainingMoves = 20,
            GoalProgress = 0.5f,
            FailedAttempts = 0,
            InFlowState = true
        };

        var type = model.Predict(ref state, 2, in context);

        // Should NOT spawn Red (avoid creating match)
        Assert.NotEqual(TileType.Red, type);
    }

    [Fact]
    public void Predict_PlayerDoingWell_AddsChallenges()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup a board where Yellow would create a match
        state.SetTile(0, 0, new Tile(1, TileType.Yellow, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Yellow, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.5f,
            RemainingMoves = 15, // Plenty of moves
            GoalProgress = 0.8f, // Almost done
            FailedAttempts = 0,
            InFlowState = true
        };

        var type = model.Predict(ref state, 2, in context);

        // Should NOT spawn Yellow (challenge the player)
        Assert.NotEqual(TileType.Yellow, type);
    }

    #endregion

    #region Diversity Guard Tests

    [Fact]
    public void Predict_DominantColor_BiasesTowardRare()
    {
        var state = CreateState(5, 5);

        // Red dominates: 8 Red + 1 each of 5 others = 13 total, Red=61%
        int id = 1;
        for (int x = 0; x < 5; x++)
            state.SetTile(x, 4, new Tile(id++, TileType.Red, x, 4));
        state.SetTile(0, 3, new Tile(id++, TileType.Red, 0, 3));
        state.SetTile(1, 3, new Tile(id++, TileType.Red, 1, 3));
        state.SetTile(2, 3, new Tile(id++, TileType.Red, 2, 3));
        state.SetTile(3, 3, new Tile(id++, TileType.Green, 3, 3));
        state.SetTile(4, 3, new Tile(id++, TileType.Blue, 4, 3));
        state.SetTile(0, 2, new Tile(id++, TileType.Yellow, 0, 2));
        state.SetTile(1, 2, new Tile(id++, TileType.Purple, 1, 2));
        state.SetTile(2, 2, new Tile(id++, TileType.Orange, 2, 2));

        var context = SpawnContext.Default;
        int redCount = 0;
        const int samples = 100;

        for (int i = 0; i < samples; i++)
        {
            var model = new RuleBasedSpawnModel(new StubRandom(i));
            var type = model.Predict(ref state, 0, in context);
            if (type == TileType.Red) redCount++;
        }

        // Red is 61% of board but guard triggers Balance weighting:
        // Red weight=11/261≈4%, so expect ≤15 out of 100
        Assert.True(redCount < 20,
            $"Red spawned {redCount}/{samples} times; expected <20 with diversity guard active");
    }

    [Fact]
    public void Predict_BalancedBoard_DoesNotTriggerGuard()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(6, 3);

        // Place 2 of each color = perfectly balanced, 12 total
        int id = 1;
        var colors = new[] {
            TileType.Red, TileType.Green, TileType.Blue,
            TileType.Yellow, TileType.Purple, TileType.Orange
        };
        for (int i = 0; i < 6; i++)
        {
            state.SetTile(i, 2, new Tile(id++, colors[i], i, 2));
            state.SetTile(i, 1, new Tile(id++, colors[i], i, 1));
        }

        var context = SpawnContext.Default;

        // Should NOT trigger diversity guard — normal strategy runs
        var type = model.Predict(ref state, 0, in context);
        Assert.NotEqual(TileType.None, type);
    }

    [Fact]
    public void Predict_FewTilesOnBoard_DoesNotTriggerGuard()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Only 2 tiles of same color — too few to judge
        state.SetTile(0, 0, new Tile(1, TileType.Red, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Red, 1, 0));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.2f,
            RemainingMoves = 20,
            GoalProgress = 0.5f,
            FailedAttempts = 0,
            InFlowState = false
        };

        var type = model.Predict(ref state, 2, in context);

        // Guard should NOT fire — Help strategy spawns Red to match
        Assert.Equal(TileType.Red, type);
    }

    #endregion

    #region Challenge Feedback Fix Tests

    [Fact]
    public void Predict_ChallengeMode_DoesNotSpawnMostCommon()
    {
        var model = new RuleBasedSpawnModel(new StubRandom(0));
        var state = CreateState(5, 5);

        // Red is most common (4 tiles), others have 1 each = 9 total
        // Red at 44% > threshold, but let's keep it under threshold
        // so diversity guard doesn't fire and we test Challenge logic directly
        // Use 3 Red + 2 each of Green/Blue = 7 total, Red=42% > 33% — guard fires
        // Need: just above colorCount threshold but Red not dominant
        // 2 Red + 1 Green + 1 Blue + 1 Yellow + 1 Purple + 1 Orange = 7 total
        // Red = 2/7 = 28% < 33% — guard won't fire
        int id = 1;
        state.SetTile(0, 4, new Tile(id++, TileType.Red, 0, 4));
        state.SetTile(1, 4, new Tile(id++, TileType.Red, 1, 4));
        state.SetTile(2, 4, new Tile(id++, TileType.Green, 2, 4));
        state.SetTile(3, 4, new Tile(id++, TileType.Blue, 3, 4));
        state.SetTile(4, 4, new Tile(id++, TileType.Yellow, 4, 4));
        state.SetTile(0, 3, new Tile(id++, TileType.Purple, 0, 3));
        state.SetTile(1, 3, new Tile(id++, TileType.Orange, 1, 3));

        var context = new SpawnContext
        {
            TargetDifficulty = 0.9f,
            RemainingMoves = 20,
            GoalProgress = 0.5f,
            FailedAttempts = 0,
            InFlowState = false
        };

        var type = model.Predict(ref state, 2, in context);

        // Challenge should NOT prefer the most common color (Red)
        // It should prefer the rarest non-matching color
        Assert.NotEqual(TileType.Red, type);
    }

    #endregion

    #region Adapter Tests

    [Fact]
    public void SpawnModelAdapter_WrapsModelCorrectly()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var adapter = new SpawnModelAdapter(model);
        var state = CreateState();

        var type = adapter.GenerateNonMatchingTile(ref state, 0, 0);

        Assert.NotEqual(TileType.None, type);
    }

    [Fact]
    public void SpawnModelAdapter_UsesProvidedContext()
    {
        var model = new RuleBasedSpawnModel(new SequentialRandom());
        var state = CreateState(5, 5);

        // Setup for match
        state.SetTile(0, 0, new Tile(1, TileType.Purple, 0, 0));
        state.SetTile(1, 0, new Tile(2, TileType.Purple, 1, 0));

        // Context that triggers help mode
        var helpContext = new SpawnContext
        {
            TargetDifficulty = 0.1f,
            RemainingMoves = 20,
            GoalProgress = 0f,
            FailedAttempts = 5,
            InFlowState = false
        };

        var adapter = new SpawnModelAdapter(model, helpContext);
        var type = adapter.GenerateNonMatchingTile(ref state, 2, 0);

        // Should create match in help mode
        Assert.Equal(TileType.Purple, type);
    }

    #endregion

    #region Legacy Adapter Tests

    [Fact]
    public void LegacySpawnModel_WrapsGeneratorCorrectly()
    {
        var generator = new Match3.Core.Systems.Generation.StandardTileGenerator(new SequentialRandom());
        var legacyModel = new LegacySpawnModel(generator);
        var state = CreateState();
        var context = SpawnContext.Default;

        var type = legacyModel.Predict(ref state, 0, in context);

        Assert.NotEqual(TileType.None, type);
    }

    #endregion
}
