using System;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Input;

/// <summary>
/// Handles user input interactions and validation.
/// </summary>
public interface IInputSystem
{
    event Action<Position> TapDetected;
    event Action<Position, Direction> SwipeDetected;

    /// <summary>
    /// Configures the input system with platform-specific values.
    /// </summary>
    /// <param name="cellSize">The size of a grid cell in pixels/units.</param>
    void Configure(double cellSize);

    void OnPointerDown(int gridX, int gridY, double screenX, double screenY);
    void OnPointerUp(double screenX, double screenY);
    
    // Optional: For real-time drag feedback
    void OnPointerMove(double screenX, double screenY);

    /// <summary>
    /// Checks if a position is valid for selection/interaction.
    /// </summary>
    bool IsValidPosition(in GameState state, Position p);

    /// <summary>
    /// Determines the target position for a swipe action.
    /// </summary>
    Position GetSwipeTarget(Position from, Direction direction);
}
