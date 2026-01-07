using System.Numerics;

namespace Match3.Core.Structs;

public struct Tile
{
    public TileType Type;
    public BombType Bomb;
    public Vector2 Position; // Visual position (World Space)

    public Tile(TileType type, int x, int y, BombType bomb = BombType.None)
    {
        Type = type;
        Bomb = bomb;
        Position = new Vector2(x, y);
    }
    
    public Tile(TileType type, Vector2 position, BombType bomb = BombType.None)
    {
        Type = type;
        Bomb = bomb;
        Position = position;
    }
}
