using System.Collections.Generic;
using Match3.Core.Interfaces;
using Match3.Core.Structs;

namespace Match3.Core.Logic;

public class PowerUpHandler : IPowerUpHandler
{
    public void ProcessSpecialMove(ref GameState state, Position p1, Position p2, out int points)
    {
        points = 0;
        var t1 = state.GetTile(p1.X, p1.Y);
        var t2 = state.GetTile(p2.X, p2.Y);

        // 1. Rainbow + Rainbow
        if (t1.Type == TileType.Rainbow && t2.Type == TileType.Rainbow)
        {
            ClearAll(ref state);
            points += 5000;
            return;
        }

        // 2. Rainbow + Any
        if (t1.Type == TileType.Rainbow || t2.Type == TileType.Rainbow)
        {
            var colorTile = t1.Type == TileType.Rainbow ? t2 : t1;
            
            // If the other tile is not a valid color target (e.g. None), ignore
            if (colorTile.Type == TileType.None || colorTile.Type == TileType.Rainbow) return;

            if (colorTile.Bomb != BombType.None)
            {
                // Rainbow + Bomb: Transform all of that color to that BombType
                ReplaceColorWithBomb(ref state, colorTile.Type, colorTile.Bomb);
                
                // Then Explode all of them. Pass the bomb type to ensure they explode even if cleared by others.
                ExplodeAllByType(ref state, colorTile.Type, colorTile.Bomb);
            }
            else
            {
                // Rainbow + Normal: Clear all of that color
                ClearColor(ref state, colorTile.Type);
            }
            
            // Clear the Rainbow and the source tile (ensure they are gone)
            state.SetTile(p1.X, p1.Y, new Tile(0, TileType.None, p1.X, p1.Y));
            state.SetTile(p2.X, p2.Y, new Tile(0, TileType.None, p2.X, p2.Y));
            
            points += 2000;
            return;
        }

        // 3. Bomb + Bomb
        if (t1.Bomb != BombType.None && t2.Bomb != BombType.None)
        {
             if ((t1.Bomb == BombType.Horizontal || t1.Bomb == BombType.Vertical) &&
                 (t2.Bomb == BombType.Horizontal || t2.Bomb == BombType.Vertical))
             {
                 ExplodeRow(ref state, p2.Y);
                 ExplodeCol(ref state, p2.X);
             }
             else
             {
                 ExplodeArea(ref state, p2.X, p2.Y, 2);
             }
             
             state.SetTile(p1.X, p1.Y, new Tile(0, TileType.None, p1.X, p1.Y));
             state.SetTile(p2.X, p2.Y, new Tile(0, TileType.None, p2.X, p2.Y));
             
             points += 1000;
             return;
        }
    }

    private void ClearAll(ref GameState state)
    {
        for(int i=0; i<state.Grid.Length; i++)
        {
            ref var t = ref state.Grid[i];
            t = new Tile(0, TileType.None, t.Position);
        }
    }
    
    private void ClearColor(ref GameState state, TileType color)
    {
        for(int i=0; i<state.Grid.Length; i++)
        {
            if (state.Grid[i].Type == color)
            {
                ref var t = ref state.Grid[i];
                t = new Tile(0, TileType.None, t.Position);
            }
        }
    }
    
    private void ReplaceColorWithBomb(ref GameState state, TileType color, BombType bomb)
    {
        for(int i=0; i<state.Grid.Length; i++)
        {
            if (state.Grid[i].Type == color)
            {
                ref var t = ref state.Grid[i];
                t.Bomb = bomb;
            }
        }
    }
    
    private void ExplodeAllByType(ref GameState state, TileType type, BombType forcedBombType = BombType.None)
    {
        // Collect positions first
        var positions = new List<Position>();
        for(int i=0; i<state.Grid.Length; i++)
        {
            if (state.Grid[i].Type == type)
            {
                int x = i % state.Width;
                int y = i / state.Width;
                positions.Add(new Position(x, y));
            }
        }

        // Explode each
        foreach(var p in positions)
        {
            if (forcedBombType != BombType.None)
            {
                ExplodeBomb(ref state, p.X, p.Y, forcedBombType);
            }
            else
            {
                var t = state.GetTile(p.X, p.Y);
                if (t.Bomb != BombType.None)
                    ExplodeBomb(ref state, p.X, p.Y, t.Bomb);
                else
                    state.SetTile(p.X, p.Y, new Tile(0, TileType.None, p.X, p.Y));
            }
        }
    }

    private void ExplodeBomb(ref GameState state, int cx, int cy, BombType type)
    {
        switch (type)
        {
            case BombType.Horizontal:
                ExplodeRow(ref state, cy);
                break;
            case BombType.Vertical:
                ExplodeCol(ref state, cx);
                break;
            case BombType.SmallCross:
                ExplodeArea(ref state, cx, cy, 1);
                break;
            case BombType.Square9x9:
                ExplodeArea(ref state, cx, cy, 1); // 3x3
                break;
            case BombType.Color:
                ClearAll(ref state);
                break;
            default:
                state.SetTile(cx, cy, new Tile(0, TileType.None, cx, cy));
                break;
        }
    }

    private void ExplodeRow(ref GameState state, int y)
    {
        for(int x=0; x<state.Width; x++) 
            state.SetTile(x, y, new Tile(0, TileType.None, x, y));
    }
    
    private void ExplodeCol(ref GameState state, int x)
    {
        for(int y=0; y<state.Height; y++) 
            state.SetTile(x, y, new Tile(0, TileType.None, x, y));
    }
    
    private void ExplodeArea(ref GameState state, int cx, int cy, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx >= 0 && nx < state.Width && ny >= 0 && ny < state.Height)
                     state.SetTile(nx, ny, new Tile(0, TileType.None, nx, ny));
            }
        }
    }
}
