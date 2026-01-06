using System;

namespace Match3.Core.Structs;

/// <summary>
/// A pure data representation of the game state.
/// This is the "Component" in our ECS-like architecture.
/// </summary>
public struct GameState
{
    /// <summary>
    /// The 1D array representing the 2D grid.
    /// Index = y * Width + x
    /// </summary>
    public TileType[] Grid;

    public int Width;
    public int Height;
    public int TileTypesCount;
    public long Score;
    public long MoveCount;
    
    // We store the seed or state of RNG to ensure determinism if we implement a custom PRNG struct.
    // For simplicity now, we'll keep the reference to IRandom, but in a pure ECS/DOTS, 
    // this would be a 'RandomComponent' struct with internal state.
    public IRandom Random;

    public GameState(int width, int height, int tileTypesCount, IRandom random)
    {
        Width = width;
        Height = height;
        TileTypesCount = tileTypesCount;
        Grid = new TileType[width * height];
        Score = 0;
        MoveCount = 0;
        Random = random;
    }

    public GameState Clone()
    {
        var clone = new GameState(Width, Height, TileTypesCount, Random);
        clone.Score = Score;
        clone.MoveCount = MoveCount;
        Array.Copy(Grid, clone.Grid, Grid.Length);
        // Note: IRandom is shared reference here. 
        // For true MCTS/branching, we would need a cloneable/struct RNG.
        return clone;
    }

    public TileType Get(int x, int y) => Grid[y * Width + x];
    
    public void Set(int x, int y, TileType type) => Grid[y * Width + x] = type;

    public int Index(int x, int y) => y * Width + x;
}
