using System;
using System.Numerics;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Spawning;

namespace Match3.Core.Systems.Physics;

public class RealtimeRefillSystem
{
    private readonly ISpawnModel _spawnModel;

    public RealtimeRefillSystem(ISpawnModel spawnModel)
    {
        _spawnModel = spawnModel;
    }

    public void Update(ref GameState state)
    {
        // Build SpawnContext from current game state
        var context = new SpawnContext
        {
            TargetDifficulty = state.TargetDifficulty,
            RemainingMoves = Math.Max(0, state.MoveLimit - (int)state.MoveCount),
            GoalProgress = 0f,      // TODO: Integrate with goal system
            FailedAttempts = 0,     // TODO: Track from session
            InFlowState = false     // Reserved for Phase 2
        };

        for (int x = 0; x < state.Width; x++)
        {
            // Only spawn if the spawn point (0) is empty
            if (state.GetTile(x, 0).Type == TileType.None)
            {
                // Spawn a new tile at the top using the spawn model
                var type = _spawnModel.Predict(ref state, x, in context);
                var tile = new Tile(state.NextTileId++, type, x, 0);
                
                // Calculate start position
                // Default: Just above the board (-1.0f)
                float startY = -1.0f;

                // Optimization: If there's a falling tile immediately below, spawn relative to it
                // to create a continuous stream without gaps.
                if (state.Height > 1)
                {
                    var tileBelow = state.GetTile(x, 1);
                    if (tileBelow.Type != TileType.None && tileBelow.IsFalling)
                    {
                        // Maintain 1.0 distance
                        startY = tileBelow.Position.Y - 1.0f;
                    }
                }

                tile.Position = new Vector2(x, startY);
                tile.Velocity = new Vector2(0, 2.0f); // Initial downward velocity
                tile.IsFalling = true;

                state.SetTile(x, 0, tile);
            }
        }
    }
}