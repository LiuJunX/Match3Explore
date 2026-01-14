using System;
using Match3.Core.Models.Enums;

namespace Match3.Core.Config;

[Serializable]
public class LevelConfig
{
    public int Width { get; set; } = 8;
    public int Height { get; set; } = 8;
    public TileType[] Grid { get; set; }
    public BombType[] Bombs { get; set; }
    public int MoveLimit { get; set; } = 20;

    /// <summary>
    /// Target difficulty for spawn model (0.0 = easy, 1.0 = hard).
    /// Default 0.5 for medium difficulty.
    /// </summary>
    public float TargetDifficulty { get; set; } = 0.5f;

    public LevelConfig()
    {
        Grid = new TileType[Width * Height];
        Bombs = new BombType[Width * Height];
    }

    public LevelConfig(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new TileType[width * height];
        Bombs = new BombType[width * height];
    }
}
