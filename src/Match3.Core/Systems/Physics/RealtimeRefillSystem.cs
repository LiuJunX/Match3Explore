using System.Numerics;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Physics;

public class RealtimeRefillSystem
{
    private readonly ITileGenerator _tileGenerator;

    public RealtimeRefillSystem(ITileGenerator tileGenerator)
    {
        _tileGenerator = tileGenerator;
    }

    public void Update(ref GameState state)
    {
        for (int x = 0; x < state.Width; x++)
        {
            // Check the logical top slot (0)
            var topTile = state.GetTile(x, 0);
            
            // If it's empty, we can spawn.
            // But we must ensure we don't spawn if there's already a tile visually falling into it?
            // In our Physics system, if a tile moves from -1 to 0, it occupies 0 immediately when > -0.5.
            // So if slot 0 is None, it means it's truly free.
            
            if (topTile.Type == TileType.None)
            {
                var type = _tileGenerator.GenerateNonMatchingTile(ref state, x, 0);
                
                // Spawn logic
                var tile = new Tile(state.NextTileId++, type, x, 0);
                // Set initial visual position above the board
                tile.Position = new Vector2(x, -1.0f);
                tile.Velocity = new Vector2(0, 2.0f); // Initial downward velocity
                tile.IsFalling = true;

                state.SetTile(x, 0, tile);
            }
        }
    }
}
