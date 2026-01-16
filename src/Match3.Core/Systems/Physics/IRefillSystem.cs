using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Physics;

/// <summary>
/// Interface for refill systems that spawn new tiles at spawn points.
/// </summary>
public interface IRefillSystem
{
    /// <summary>
    /// Update the refill system, spawning new tiles as needed.
    /// </summary>
    /// <param name="state">The game state to modify.</param>
    void Update(ref GameState state);
}
