using System.Numerics;

namespace Match3.Core.Structs;

public struct Tile
{
    public TileType Type;
    public Vector2 Position; // Visual position (World Space)

    public Tile(TileType type, int x, int y)
    {
        Type = type;
        Position = new Vector2(x, y);
    }
    
    public Tile(TileType type, Vector2 position)
    {
        Type = type;
        Position = position;
    }
}
