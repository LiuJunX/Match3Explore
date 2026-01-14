namespace Match3.Core.Config;

/// <summary>
/// Configuration settings for the Match3 game logic.
/// </summary>
public class Match3Config
{
    public int Width { get; set; } = 8;
    public int Height { get; set; } = 8;
    public int TileTypesCount { get; set; } = 6;
    
    // Animation speeds (visual/logical update speeds)
    public float SwapSpeed { get; set; } = 15.0f;

    // Gravity settings
    public float InitialFallSpeed { get; set; } = 8.0f;  // 初始掉落速度，解决"前半格慢"问题
    public float GravitySpeed { get; set; } = 20.0f;     // 重力加速度
    public float MaxFallSpeed { get; set; } = 25.0f;     // 最大掉落速度

    // Logic Flags
    public bool IsGravityEnabled { get; set; } = true;

    public Match3Config(int width, int height, int tileTypesCount)
    {
        Width = width;
        Height = height;
        TileTypesCount = tileTypesCount;
    }

    public Match3Config() { }
}
