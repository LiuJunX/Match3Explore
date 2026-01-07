using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Match3.Core;
using Match3.Core.Logic;
using Match3.Core.Structs;
using Xunit;

namespace Match3.Tests.Scenarios
{
    public class ScenarioTests
    {
        public static IEnumerable<object[]> GetScenarios()
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Data");
            if (!Directory.Exists(dataDir))
            {
                // Fallback to searching relative to project if running in different context
                var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Scenarios/Data"));
                if (Directory.Exists(projectDir))
                    dataDir = projectDir;
                else
                    throw new DirectoryNotFoundException($"Scenarios directory not found at: {dataDir}");
            }

            var files = Directory.GetFiles(dataDir, "*.json", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                 throw new FileNotFoundException($"No JSON files found in: {dataDir}");
            }

            var scenarios = new List<object[]>();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var scenario = JsonSerializer.Deserialize<TestScenario>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (scenario != null)
                    {
                        scenarios.Add(new object[] { Path.GetFileName(file), scenario });
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load scenario {file}: {ex.Message}");
                }
            }

            return scenarios;
        }

        [Theory]
        [MemberData(nameof(GetScenarios))]
        public void RunScenario(string fileName, TestScenario scenario)
        {
            var rng = new DefaultRandom(42);
            var state = new GameState(scenario.Width, scenario.Height, 6, rng);
            
            // Parse layout
            for (int y = 0; y < scenario.Height; y++)
            {
                var row = scenario.Layout[y].Split(',', StringSplitOptions.TrimEntries);
                for (int x = 0; x < scenario.Width; x++)
                {
                    var type = ParseType(row[x]);
                    state.SetTile(x, y, new Tile(type, x, y));
                }
            }
            
            // Execute moves
            foreach (var move in scenario.Moves)
            {
                var p1 = ParsePos(move.From);
                var p2 = ParsePos(move.To);
                
                // We need to simulate the move logic. 
                // Since GameRules.ApplyMove might be internal or specific, we assume we use the public API.
                // If ApplyMove is not available, we might need to rely on Swap logic + Evaluation.
                
                // Assuming GameRules.ApplyMove is the entry point
                bool success = GameRules.ApplyMove(ref state, p1, p2, out var matches, out var effects);
                
                Assert.True(success, $"Scenario '{scenario.Name}' ({fileName}): Move {move.From}->{move.To} failed.");
            }
            
            // Verify expectations
            foreach (var exp in scenario.Expectations)
            {
                var tile = state.GetTile(exp.X, exp.Y);
                if (exp.Type != null)
                {
                    var expectedType = ParseType(exp.Type); // Using ParseType to handle string to Enum
                    Assert.Equal(expectedType, tile.Type);
                }
                if (exp.Bomb != null)
                {
                    var expectedBomb = ParseBombType(exp.Bomb);
                    Assert.Equal(expectedBomb, tile.Bomb);
                }
            }
        }

        private TileType ParseType(string code)
        {
            return code switch
            {
                "_" => TileType.None,
                "R" => TileType.Red,
                "G" => TileType.Green,
                "B" => TileType.Blue,
                "Y" => TileType.Yellow,
                "P" => TileType.Purple,
                "O" => TileType.Orange,
                "Rainbow" => TileType.Rainbow,
                _ => Enum.TryParse<TileType>(code, out var t) ? t : TileType.None
            };
        }

        private BombType ParseBombType(string code)
        {
            return Enum.TryParse<BombType>(code, true, out var b) ? b : BombType.None;
        }

        private Position ParsePos(string pos)
        {
            var parts = pos.Split(',');
            return new Position(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
}
