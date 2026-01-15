using System.Collections.Generic;
using Match3.Core.Events;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Projectiles;

/// <summary>
/// Interface for managing projectiles in the game.
/// </summary>
public interface IProjectileSystem
{
    /// <summary>
    /// Gets all currently active projectiles.
    /// </summary>
    IReadOnlyList<Projectile> ActiveProjectiles { get; }

    /// <summary>
    /// Whether there are any active projectiles.
    /// </summary>
    bool HasActiveProjectiles { get; }

    /// <summary>
    /// Launch a new projectile.
    /// </summary>
    /// <param name="projectile">The projectile to launch.</param>
    /// <param name="tick">Current simulation tick.</param>
    /// <param name="simTime">Current simulation time.</param>
    /// <param name="events">Event collector.</param>
    void Launch(Projectile projectile, long tick, float simTime, IEventCollector events);

    /// <summary>
    /// Update all active projectiles.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="deltaTime">Time step in seconds.</param>
    /// <param name="tick">Current simulation tick.</param>
    /// <param name="simTime">Current simulation time.</param>
    /// <param name="events">Event collector.</param>
    /// <returns>Set of positions affected by projectiles that reached their targets.</returns>
    HashSet<Position> Update(
        ref GameState state,
        float deltaTime,
        long tick,
        float simTime,
        IEventCollector events);

    /// <summary>
    /// Clear all active projectiles.
    /// </summary>
    void Clear();

    /// <summary>
    /// Generate a unique projectile ID.
    /// </summary>
    long GenerateProjectileId();
}
