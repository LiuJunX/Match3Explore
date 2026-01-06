using System;
using System.Collections.Generic;
using Match3.Core.Structs;
using Match3.Core.Logic;

namespace Match3.Core.AI;

/// <summary>
/// A RL-friendly environment wrapper using the new Data-Oriented Core (Structs + Logic).
/// </summary>
public class Match3Environment : IGameEnvironment<GameState, Move>
{
    private GameState _state;
    private readonly int _width;
    private readonly int _height;
    private readonly int _tileTypesCount;
    private readonly int _maxMoves;
    private int _stepsTaken;

    public Match3Environment(int width, int height, int tileTypesCount, int maxMoves = 100)
    {
        _width = width;
        _height = height;
        _tileTypesCount = tileTypesCount;
        _maxMoves = maxMoves;
    }

    public GameState Reset(int? seed = null)
    {
        var rng = new DefaultRandom(seed);
        _state = new GameState(_width, _height, _tileTypesCount, rng);
        GameRules.Initialize(ref _state);
        _stepsTaken = 0;
        return _state; // Returns a copy because GameState is a struct
    }

    public StepResult<GameState> Step(Move move)
    {
        if (_state.Grid == null)
            throw new InvalidOperationException("Environment not initialized. Call Reset() first.");

        if (_stepsTaken >= _maxMoves)
        {
            return new StepResult<GameState>(_state, 0, true, new Dictionary<string, object> { { "Reason", "MaxMovesReached" } });
        }

        _stepsTaken++;
        bool isDone = _stepsTaken >= _maxMoves;

        // Validate logic is now inside GameRules or handled here
        if (!GameRules.IsValidMove(in _state, move.From, move.To))
        {
             return new StepResult<GameState>(_state, -10.0, isDone, new Dictionary<string, object> { { "Error", "InvalidMove" } });
        }

        int cascades;
        int points;
        bool success = GameRules.ApplyMove(ref _state, move.From, move.To, out cascades, out points);

        if (!success)
        {
             return new StepResult<GameState>(_state, -1.0, isDone, new Dictionary<string, object> { { "Result", "NoMatch" } });
        }

        return new StepResult<GameState>(
            _state, 
            points, 
            isDone, 
            new Dictionary<string, object> 
            { 
                { "Score", _state.Score },
                { "Cascades", cascades },
                { "CurrentMove", _state.MoveCount },
                { "StepsTaken", _stepsTaken }
            }
        );
    }

    public GameState GetState()
    {
        return _state;
    }
}

