using System;
using System.Collections.Generic;
using System.Numerics;
using Match3.Core.Structs;
using Match3.Core.Logic;

namespace Match3.Core;

/// <summary>
/// Orchestrates the game logic, handling moves, matches, and cascading effects.
/// Acts as the bridge between the GameState data and the IGameView presentation.
/// </summary>
public sealed class Match3Controller
{
    private GameState _state;
    private readonly IGameView _view;
    
    // Animation constants
    private const float SwapSpeed = 10.0f; // Tiles per second
    private const float GravitySpeed = 20.0f; // Tiles per second
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

    public Match3Controller(int width, int height, int tileTypesCount, IRandom rng, IGameView view)
    {
        _view = view;
        _state = new GameState(width, height, tileTypesCount, rng);
        GameRules.Initialize(ref _state);
    }

    /// <summary>
    /// Advances the simulation by dt seconds.
    /// MUST be called by the game loop (e.g. from the View).
    /// </summary>
    public void Update(float dt)
    {
        bool isStable = AnimateTiles(dt);

        if (!isStable) return;

        switch (_currentState)
        {
            case ControllerState.AnimateSwap:
                if (GameRules.HasMatches(in _state))
                {
                    _view.ShowSwap(_swapA, _swapB, true);
                    _currentState = ControllerState.Resolving;
                    ResolveStep();
                }
                else
                {
                    // Invalid move, revert
                    GameRules.Swap(ref _state, _swapA, _swapB);
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
                // Do nothing
                break;
        }
    }

    private bool ResolveStep()
    {
        var matches = GameRules.FindMatches(in _state);
        if (matches.Count == 0) return false;

        _view.ShowMatches(matches);
        
        // 1. Clear matches
        foreach (var p in matches)
        {
             _state.SetTile(p.X, p.Y, new Tile(TileType.None, p.X, p.Y));
        }

        // 2. Gravity & Refill (Logic)
        // These methods now move Tile structs, preserving their old visual positions
        // so the animation system will see them as "out of place" and move them.
        var gravityMoves = GameRules.ApplyGravity(ref _state);
        _view.ShowGravity(gravityMoves);
        
        var refillMoves = GameRules.Refill(ref _state);
        _view.ShowRefill(refillMoves);
        
        return true;
    }

    private bool AnimateTiles(float dt)
    {
        bool allStable = true;
        for (int i = 0; i < _state.Grid.Length; i++)
        {
            // Use ref to modify struct in array directly
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
                float move = GravitySpeed * dt;
                
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

    public bool TrySwap(Position a, Position b)
    {
        if (_currentState != ControllerState.Idle) return false;
        
        if (!GameRules.IsValidMove(in _state, a, b)) return false;

        _swapA = a;
        _swapB = b;
        
        GameRules.Swap(ref _state, a, b);
        // After swap, tile at A has VisPos of B, tile at B has VisPos of A.
        // Animation loop will fix this.
        
        _currentState = ControllerState.AnimateSwap;
        return true;
    }

    // Helper for tests/debug
    public void DebugSetTile(Position p, TileType t)
    {
        _state.SetTile(p.X, p.Y, new Tile(t, p.X, p.Y));
    }
}
