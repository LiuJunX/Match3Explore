using System.Numerics;
using Match3.Core.Models.Enums;

namespace Match3.Core.Models.Grid;

public struct Tile
{
    public TileType Type;
    public BombType Bomb;
    public Vector2 Position; // Logic position (World Space), e.g. (3, 4.5)
    public Vector2 Velocity; // Physics velocity
    public bool IsSuspended; // If true, gravity ignores this tile (e.g. matched and exploding)
    public bool IsFalling; // If true, currently moving down
    public long Id;

    public Tile(long id, TileType type, int x, int y, BombType bomb = BombType.None)
    {
        Id = id;
        Type = type;
        Bomb = bomb;
        Position = new Vector2(x, y);
        Velocity = Vector2.Zero;
        IsSuspended = false;
        IsFalling = false;
    }
    
    public Tile(long id, TileType type, Vector2 position, BombType bomb = BombType.None)
    {
        Id = id;
        Type = type;
        Bomb = bomb;
        Position = position;
        Velocity = Vector2.Zero;
        IsSuspended = false;
        IsFalling = false;
    }
}
