using System.Collections.Generic;

namespace Match3.Core;

/// <summary>
/// Represents the game grid containing tiles.
/// Handles grid manipulation, matching logic, gravity, and refilling.
/// </summary>
public sealed class GameBoard
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _tileTypesCount;
    private readonly IRandom _rng;
    private readonly TileType[,] _grid;

    /// <summary>
    /// Gets the width of the board.
    /// </summary>
    public int Width => _width;

    /// <summary>
    /// Gets the height of the board.
    /// </summary>
    public int Height => _height;

    /// <summary>
    /// Creates a deep copy of the current board state.
    /// </summary>
    /// <returns>A 2D array of TileType representing the current grid.</returns>
    public TileType[,] Snapshot()
    {
        var copy = new TileType[_width, _height];
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                copy[x, y] = _grid[x, y];
            }
        }
        return copy;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameBoard"/> class.
    /// </summary>
    /// <param name="width">Width of the grid.</param>
    /// <param name="height">Height of the grid.</param>
    /// <param name="tileTypesCount">Number of distinct tile colors.</param>
    /// <param name="rng">Random number generator.</param>
    public GameBoard(int width, int height, int tileTypesCount, IRandom rng)
    {
        _width = width;
        _height = height;
        _tileTypesCount = tileTypesCount;
        _rng = rng;
        _grid = new TileType[width, height];
        InitializeBoard();
    }

    /// <summary>
    /// Checks if a position is within the grid boundaries.
    /// </summary>
    /// <param name="p">The position to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool InBounds(Position p)
    {
        return p.X >= 0 && p.X < _width && p.Y >= 0 && p.Y < _height;
    }

    /// <summary>
    /// Gets the tile at the specified position.
    /// </summary>
    public TileType Get(Position p)
    {
        return _grid[p.X, p.Y];
    }

    /// <summary>
    /// Sets the tile at the specified position.
    /// </summary>
    public void Set(Position p, TileType t)
    {
        _grid[p.X, p.Y] = t;
    }

    /// <summary>
    /// Swaps the tiles at two positions. 
    /// Does NOT check for validity or matches.
    /// </summary>
    public void Swap(Position a, Position b)
    {
        var tmp = _grid[a.X, a.Y];
        _grid[a.X, a.Y] = _grid[b.X, b.Y];
        _grid[b.X, b.Y] = tmp;
    }

    /// <summary>
    /// Scans the entire board for horizontal and vertical matches of 3 or more tiles.
    /// </summary>
    /// <returns>A set of unique positions involved in matches.</returns>
    /// <remarks>
    /// <b>Why:</b> We need to identify all tiles that should be cleared in the current state.
    /// <br/>
    /// <b>How:</b>
    /// 1. Scan rows: Iterate X from 1 to Width. Keep a running count (`run`) of identical adjacent tiles.
    ///    When the chain breaks or row ends, if `run >= 3`, add those positions to the result.
    /// 2. Scan columns: Similar logic, iterating Y from 1 to Height.
    /// 
    /// <code>
    /// Scanning Process:
    /// [A] [A] [A] [B] ...
    ///  ^   ^   ^
    ///  |   |   |
    /// run=1 run=2 run=3 -> Match found!
    /// </code>
    /// </remarks>
    public HashSet<Position> FindMatches()
    {
        var result = new HashSet<Position>();

        // 1. Horizontal Scans
        for (var y = 0; y < _height; y++)
        {
            var run = 1;
            for (var x = 1; x < _width; x++)
            {
                var curr = _grid[x, y];
                var prev = _grid[x - 1, y];

                if (curr != TileType.None && curr == prev)
                {
                    run++;
                }
                else
                {
                    if (run >= 3)
                    {
                        // Add previous run positions
                        for (var k = x - run; k < x; k++)
                        {
                            result.Add(new Position(k, y));
                        }
                    }
                    run = 1;
                }
            }
            // Handle run at the end of the row
            if (run >= 3)
            {
                for (var k = _width - run; k < _width; k++)
                {
                    result.Add(new Position(k, y));
                }
            }
        }

        // 2. Vertical Scans
        for (var x = 0; x < _width; x++)
        {
            var run = 1;
            for (var y = 1; y < _height; y++)
            {
                var curr = _grid[x, y];
                var prev = _grid[x, y - 1];

                if (curr != TileType.None && curr == prev)
                {
                    run++;
                }
                else
                {
                    if (run >= 3)
                    {
                        for (var k = y - run; k < y; k++)
                        {
                            result.Add(new Position(x, k));
                        }
                    }
                    run = 1;
                }
            }
            // Handle run at the end of the column
            if (run >= 3)
            {
                for (var k = _height - run; k < _height; k++)
                {
                    result.Add(new Position(x, k));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if there are any matches on the board.
    /// </summary>
    public bool HasAnyMatches()
    {
        return FindMatches().Count > 0;
    }

    /// <summary>
    /// Clears the specified positions by setting them to TileType.None.
    /// </summary>
    public void Clear(HashSet<Position> positions)
    {
        foreach (var p in positions)
        {
            _grid[p.X, p.Y] = TileType.None;
        }
    }

    /// <summary>
    /// Makes tiles fall down to fill empty spaces (Gravity).
    /// </summary>
    /// <remarks>
    /// <b>Why:</b> After clearing matches, gaps appear. Tiles above must fall to fill them.
    /// <br/>
    /// <b>How:</b>
    /// Process each column from bottom to top.
    /// Maintain a `writeY` pointer that tracks the lowest empty position.
    /// When a non-empty tile is found, move it to `writeY` and decrement `writeY`.
    /// 
    /// <code>
    /// Before:      After:
    /// [A]          [ ] (None)
    /// [ ]          [ ] (None)
    /// [B]    =>    [A]
    /// [ ]          [B]
    /// </code>
    /// </remarks>
    public void ApplyGravity()
    {
        for (var x = 0; x < _width; x++)
        {
            var writeY = _height - 1;
            for (var y = _height - 1; y >= 0; y--)
            {
                var t = _grid[x, y];
                if (t == TileType.None) continue;

                // Move tile down
                _grid[x, writeY] = t;
                
                // Clear original position if we moved it
                if (writeY != y)
                {
                    _grid[x, y] = TileType.None;
                }
                writeY--;
            }
        }
    }

    /// <summary>
    /// Refills empty spaces (None) with new random tiles.
    /// </summary>
    public void Refill()
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                if (_grid[x, y] == TileType.None)
                {
                    _grid[x, y] = RandomTileAvoidingImmediateRun(x, y);
                }
            }
        }
    }

    private void InitializeBoard()
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _grid[x, y] = RandomTileAvoidingImmediateRun(x, y);
            }
        }
    }

    /// <summary>
    /// Generates a random tile but attempts to avoid creating an immediate match-3.
    /// </summary>
    private TileType RandomTileAvoidingImmediateRun(int x, int y)
    {
        // Try up to 10 times to pick a tile that doesn't create a match
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var v = _rng.Next(1, _tileTypesCount + 1);
            var t = (TileType)v;
            if (CreatesImmediateRun(x, y, t)) continue;
            return t;
        }
        // Fallback
        return (TileType)_rng.Next(1, _tileTypesCount + 1);
    }

    private bool CreatesImmediateRun(int x, int y, TileType t)
    {
        // Check horizontal: [x-2][x-1][t]
        if (x >= 2)
        {
            var a = _grid[x - 1, y];
            var b = _grid[x - 2, y];
            if (a == t && b == t) return true;
        }
        // Check vertical: [y-2]
        //                 [y-1]
        //                 [t]
        if (y >= 2)
        {
            var a = _grid[x, y - 1];
            var b = _grid[x, y - 2];
            if (a == t && b == t) return true;
        }
        return false;
    }
}
