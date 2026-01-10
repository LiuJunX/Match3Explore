using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Models.Grid;
using Match3.Core.Utility;
using Match3.Core.Interfaces;
using System;

namespace Match3.Core.Systems.Input;

/// <summary>
/// Handles user input interactions (Taps, Swipes) and manages selection state.
/// </summary>
public class InteractionSystem
{
    private readonly IInputSystem _inputSystem;
    private readonly IGameLogger _logger;

    public Position SelectedPosition { get; private set; } = Position.Invalid;
    public string StatusMessage { get; private set; } = "Ready";

    public InteractionSystem(IInputSystem inputSystem, IGameLogger logger)
    {
        _inputSystem = inputSystem;
        _logger = logger;
    }

    /// <summary>
    /// Handles a tap interaction. Returns a Move if a valid move was initiated.
    /// </summary>
    public bool TryHandleTap(in GameState state, Position p, bool isBoardInteractive, out Move? move)
    {
        move = null;

        if (!_inputSystem.IsValidPosition(in state, p)) return false;
        if (!isBoardInteractive) return false;

        _logger.LogInfo($"OnTap: {p}");

        if (SelectedPosition == Position.Invalid)
        {
            SelectedPosition = p;
            StatusMessage = "Select destination";
            return false;
        }
        else
        {
            if (SelectedPosition == p)
            {
                SelectedPosition = Position.Invalid;
                StatusMessage = "Selection Cleared";
                return false;
            }
            else
            {
                if (IsNeighbor(SelectedPosition, p))
                {
                    move = new Move(SelectedPosition, p);
                    SelectedPosition = Position.Invalid;
                    StatusMessage = "Swapping...";
                    return true;
                }
                else
                {
                    // Select the new position instead
                    SelectedPosition = p;
                    StatusMessage = "Select destination";
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Handles a swipe interaction. Returns a Move if a valid move was initiated.
    /// </summary>
    public bool TryHandleSwipe(in GameState state, Position from, Direction direction, bool isBoardInteractive, out Move? move)
    {
        move = null;

        if (!_inputSystem.IsValidPosition(in state, from)) return false;
        if (!isBoardInteractive) return false;

        Position to = _inputSystem.GetSwipeTarget(from, direction);
        if (!_inputSystem.IsValidPosition(in state, to)) return false;
        
        // Swipe doesn't use selection state, but clears it if any
        SelectedPosition = Position.Invalid;

        move = new Move(from, to);
        StatusMessage = "Swapping...";
        return true;
    }

    private bool IsNeighbor(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
    }
}
