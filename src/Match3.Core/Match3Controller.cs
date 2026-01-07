using System;
using System.Collections.Generic;
using System.Numerics;
using Match3.Core.Config;
using Match3.Core.Interfaces;
using Match3.Core.Logic;
using Match3.Core.Structs;

namespace Match3.Core;

/// <summary>
/// Orchestrates the game logic, handling moves, matches, and cascading effects.
/// Acts as the bridge between the GameState data and the IGameView presentation.
/// </summary>
public sealed class Match3Controller
{
    private GameState _state;
    private readonly Match3Config _config;
    private readonly IGameView _view;
    
    private readonly IMatchFinder _matchFinder;
    private readonly IMatchProcessor _matchProcessor;
    private readonly IGravitySystem _gravitySystem;
    private readonly IPowerUpHandler _powerUpHandler;
    private readonly ITileGenerator _tileGenerator;
    private readonly IGameLogger _logger;

    // Animation constants
    private const float Epsilon = 0.01f;

    private enum ControllerState
    {
        Idle,
        AnimateSwap,
        Resolving,
        AnimateRevert
    }
    
    private ControllerState _currentState = ControllerState.Idle;
    private Position _swapA;
    private Position _swapB;

    public GameState State => _state; // Expose state for View
    public bool IsIdle => _currentState == ControllerState.Idle;
    public Position SelectedPosition { get; private set; } = Position.Invalid;
    public string StatusMessage { get; private set; } = "Ready";

    public Match3Controller(
        Match3Config config,
        IRandom rng, 
        IGameView view,
        IMatchFinder matchFinder,
        IMatchProcessor matchProcessor,
        IGravitySystem gravitySystem,
        IPowerUpHandler powerUpHandler,
        ITileGenerator tileGenerator,
        IGameLogger logger)
    {
        _config = config;
        _view = view;
        _matchFinder = matchFinder;
        _matchProcessor = matchProcessor;
        _gravitySystem = gravitySystem;
        _powerUpHandler = powerUpHandler;
        _tileGenerator = tileGenerator;
        _logger = logger;

        _state = new GameState(_config.Width, _config.Height, _config.TileTypesCount, rng);
        InitializeBoard();
        _logger.LogInfo($"Match3Controller initialized with size {_config.Width}x{_config.Height}");
    }

    private void InitializeBoard()
    {
        for (int y = 0; y < _state.Height; y++)
        {
            for (int x = 0; x < _state.Width; x++)
            {
                var type = _tileGenerator.GenerateNonMatchingTile(ref _state, x, y);
                _state.SetTile(x, y, new Tile(_state.NextTileId++, type, x, y));
            }
        }
    }

    /// <summary>
    /// Handles a tap/click interaction on a specific tile.
    /// </summary>
    public void OnTap(Position p)
    {
        if (!IsIdle) return;
        if (!IsValidPosition(p)) return;
        
        _logger.LogInfo($"OnTap: {p}");

        if (SelectedPosition == Position.Invalid)
        {
            // Select first tile
            SelectedPosition = p;
            StatusMessage = "Select destination";
        }
        else
        {
            if (SelectedPosition == p)
            {
                // Deselect
                SelectedPosition = Position.Invalid;
                StatusMessage = "Selection Cleared";
            }
            else
            {
                // Try swap
                bool success = TrySwapInternal(SelectedPosition, p);
                if (success)
                {
                     SelectedPosition = Position.Invalid;
                     StatusMessage = "Swapping...";
                }
                else
                {
                    // If neighbors but invalid move -> Invalid Move
                    // If not neighbors -> Select new tile
                    if (IsNeighbor(SelectedPosition, p))
                    {
                        StatusMessage = "Invalid Move";
                        SelectedPosition = Position.Invalid;
                    }
                    else
                    {
                        SelectedPosition = p;
                        StatusMessage = "Select destination";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles a swipe interaction originating from a specific tile.
    /// </summary>
    public void OnSwipe(Position from, Direction direction)
    {
        if (!IsIdle) return;
        if (!IsValidPosition(from)) return;

        Position to = GetNeighbor(from, direction);
        if (!IsValidPosition(to)) return;

        // Swipe overrides selection
        SelectedPosition = Position.Invalid;

        bool success = TrySwapInternal(from, to);
        if (success)
        {
            StatusMessage = "Swapping...";
        }
        else
        {
            StatusMessage = "Invalid Move";
        }
    }

    private bool IsValidPosition(Position p)
    {
        return p.X >= 0 && p.X < _state.Width && p.Y >= 0 && p.Y < _state.Height;
    }

    private bool IsNeighbor(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
    }

    private Position GetNeighbor(Position p, Direction dir)
    {
        return dir switch
        {
            Direction.Up => new Position(p.X, p.Y - 1),
            Direction.Down => new Position(p.X, p.Y + 1),
            Direction.Left => new Position(p.X - 1, p.Y),
            Direction.Right => new Position(p.X + 1, p.Y),
            _ => p
        };
    }

    public void Update(float dt)
    {
        bool isStable = AnimateTiles(dt);

        if (!isStable) return;

        switch (_currentState)
        {
            case ControllerState.AnimateSwap:
                // Check if the move results in a match or if it was a special move
                bool hasMatch = _matchFinder.HasMatches(in _state);
                bool isSpecial = IsSpecialMove(_swapA, _swapB);

                if (hasMatch || isSpecial)
                {
                    _view.ShowSwap(_swapA, _swapB, true);
                    _currentState = ControllerState.Resolving;
                    
                    if (isSpecial)
                    {
                         _powerUpHandler.ProcessSpecialMove(ref _state, _swapA, _swapB, out int points);
                         _state.Score += points;
                         // Special moves usually result in cleared tiles, so we should continue resolving
                         ResolveStep(null); 
                    }
                    else
                    {
                        ResolveStep(_swapB);
                    }
                }
                else
                {
                    // Invalid move, revert
                    Swap(ref _state, _swapA, _swapB);
                    _currentState = ControllerState.AnimateRevert;
                }
                break;

            case ControllerState.AnimateRevert:
                _view.ShowSwap(_swapA, _swapB, false);
                _currentState = ControllerState.Idle;
                break;

            case ControllerState.Resolving:
                if (!ResolveStep())
                {
                    _currentState = ControllerState.Idle;
                }
                break;
                
            case ControllerState.Idle:
                break;
        }
    }

    private bool IsSpecialMove(Position a, Position b)
    {
        var t1 = _state.GetTile(a.X, a.Y);
        var t2 = _state.GetTile(b.X, b.Y);
        
        // Rainbow swap is always valid
        if (t1.Type == TileType.Rainbow || t2.Type == TileType.Rainbow) return true;
        
        bool isBombCombo = t1.Bomb != BombType.None && t2.Bomb != BombType.None;
        
        return isBombCombo;
    }

    private bool ResolveStep(Position? focus = null)
    {
        var groups = _matchFinder.FindMatchGroups(in _state, focus);
        
        // Also check if we have any pending explosions/cleared tiles that need gravity?
        // If groups count is 0, we might still have holes from PowerUpHandler
        // But PowerUpHandler is called before this loop.
        
        // If we processed a special move, we might have cleared tiles but no "matches".
        // In that case, we need to apply gravity.
        // How do we detect holes?
        bool hasHoles = HasHoles();

        if (groups.Count == 0 && !hasHoles) return false;

        // Flatten for View
        var allPositions = new HashSet<Position>();
        foreach(var g in groups) 
            foreach(var p in g.Positions) allPositions.Add(p);

        if (allPositions.Count > 0)
            _view.ShowMatches(allPositions);
        
        // 1. Process matches (Clear + Spawn Bombs)
        int points = _matchProcessor.ProcessMatches(ref _state, groups);
        _state.Score += points;

        // 2. Gravity & Refill (Logic)
        var gravityMoves = _gravitySystem.ApplyGravity(ref _state);
        _view.ShowGravity(gravityMoves);
        
        var refillMoves = _gravitySystem.Refill(ref _state);
        _view.ShowRefill(refillMoves);
        
        return true;
    }

    private bool HasHoles()
    {
        for (int i = 0; i < _state.Grid.Length; i++)
        {
            if (_state.Grid[i].Type == TileType.None) return true;
        }
        return false;
    }

    private bool AnimateTiles(float dt)
    {
        bool allStable = true;
        for (int i = 0; i < _state.Grid.Length; i++)
        {
            ref var tile = ref _state.Grid[i];
            if (tile.Type == TileType.None) continue;

            int x = i % _state.Width;
            int y = i / _state.Width;
            var targetPos = new Vector2(x, y);

            if (Vector2.DistanceSquared(tile.Position, targetPos) > Epsilon * Epsilon)
            {
                allStable = false;
                var dir = targetPos - tile.Position;
                float dist = dir.Length();
                float move = _config.GravitySpeed * dt;
                
                if (move >= dist)
                {
                    tile.Position = targetPos;
                }
                else
                {
                    tile.Position += Vector2.Normalize(dir) * move;
                }
            }
            else
            {
                tile.Position = targetPos; // Snap
            }
        }
        return allStable;
    }

    private bool TrySwapInternal(Position a, Position b)
    {
        if (_currentState != ControllerState.Idle) return false;
        
        if (!IsValidMove(a, b)) return false;

        _swapA = a;
        _swapB = b;
        
        Swap(ref _state, a, b);
        
        _currentState = ControllerState.AnimateSwap;
        return true;
    }

    private bool IsValidMove(Position from, Position to)
    {
        if (from.X < 0 || from.X >= _state.Width || from.Y < 0 || from.Y >= _state.Height) return false;
        if (to.X < 0 || to.X >= _state.Width || to.Y < 0 || to.Y >= _state.Height) return false;
        if (Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y) != 1) return false;
        return true;
    }

    private void Swap(ref GameState state, Position a, Position b)
    {
        var idxA = state.Index(a.X, a.Y);
        var idxB = state.Index(b.X, b.Y);
        var temp = state.Grid[idxA];
        state.Grid[idxA] = state.Grid[idxB];
        state.Grid[idxB] = temp;
    }

    // Helper for tests/debug
    public void DebugSetTile(Position p, TileType t)
    {
        _state.SetTile(p.X, p.Y, new Tile(_state.NextTileId++, t, p.X, p.Y));
    }

    public bool TryMakeRandomMove()
    {
        if (!IsIdle) return false;

        for (int y = 0; y < _state.Height; y++)
        {
            for (int x = 0; x < _state.Width; x++)
            {
                var p = new Position(x, y);

                // Try Right
                var right = new Position(x + 1, y);
                if (IsValidPosition(right))
                {
                    if (CheckAndPerformMove(p, right)) return true;
                }

                // Try Down
                var down = new Position(x, y + 1);
                if (IsValidPosition(down))
                {
                    if (CheckAndPerformMove(p, down)) return true;
                }
            }
        }
        
        return false;
    }

    private bool CheckAndPerformMove(Position a, Position b)
    {
        Swap(ref _state, a, b);
        bool hasMatch = _matchFinder.HasMatches(in _state);
        bool isSpecial = IsSpecialMove(a, b); // Note: a and b are now swapped in grid
        
        Swap(ref _state, a, b); // Swap back

        if (hasMatch || isSpecial)
        {
            TrySwapInternal(a, b);
            StatusMessage = "Auto Move...";
            return true;
        }
        return false;
    }
}
