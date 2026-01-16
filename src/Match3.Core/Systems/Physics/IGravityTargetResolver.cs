using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Physics;

/// <summary>
/// Interface for resolving gravity targets for falling tiles.
/// Allows different gravity behaviors to be plugged in.
/// </summary>
public interface IGravityTargetResolver
{
    /// <summary>
    /// Target information for a tile's movement.
    /// </summary>
    public readonly struct TargetInfo
    {
        /// <summary>
        /// Target position for the tile.
        /// </summary>
        public readonly System.Numerics.Vector2 Position;

        /// <summary>
        /// Inherited velocity from a falling tile below.
        /// </summary>
        public readonly float InheritedVelocityY;

        /// <summary>
        /// Whether the target was found dynamically (following a falling tile).
        /// </summary>
        public readonly bool FoundDynamicTarget;

        public TargetInfo(System.Numerics.Vector2 position, float inheritedVelocityY, bool foundDynamicTarget)
        {
            Position = position;
            InheritedVelocityY = inheritedVelocityY;
            FoundDynamicTarget = foundDynamicTarget;
        }
    }

    /// <summary>
    /// Determine the target position for a tile at the given position.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="x">X coordinate of the tile.</param>
    /// <param name="y">Y coordinate of the tile.</param>
    /// <returns>Target information for the tile.</returns>
    TargetInfo DetermineTarget(ref GameState state, int x, int y);

    /// <summary>
    /// Clear any reserved slots at the start of a new frame.
    /// </summary>
    void ClearReservations();
}
