using System.Collections.Generic;
using System.Numerics;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Random;

namespace Match3.Core.Systems.Physics;

/// <summary>
/// Resolves gravity targets for falling tiles.
/// Extracted from RealtimeGravitySystem to reduce class size.
/// </summary>
internal sealed class GravityTargetResolver
{
    private const float FallingFollowDistance = 1.0f;

    private readonly IRandom _random;
    private readonly HashSet<int> _reservedSlots;

    public GravityTargetResolver(IRandom random, HashSet<int> reservedSlots)
    {
        _random = random;
        _reservedSlots = reservedSlots;
    }

    /// <summary>
    /// Target information for a tile's movement.
    /// </summary>
    public readonly struct TargetInfo
    {
        public readonly Vector2 Position;
        public readonly float InheritedVelocityY;
        public readonly bool FoundDynamicTarget;

        public TargetInfo(Vector2 position, float inheritedVelocityY, bool foundDynamicTarget)
        {
            Position = position;
            InheritedVelocityY = inheritedVelocityY;
            FoundDynamicTarget = foundDynamicTarget;
        }
    }

    /// <summary>
    /// Determine the target position for a tile.
    /// </summary>
    public TargetInfo DetermineTarget(ref GameState state, int x, int y)
    {
        int checkY = y + 1;

        // 1. Try Vertical Move
        if (checkY < state.Height)
        {
            if (CanMoveTo(ref state, x, checkY))
            {
                return FindLowestVerticalTarget(ref state, x, checkY);
            }

            // 2. Try Follow Falling Blocker
            var below = state.GetTile(x, checkY);
            if (below.Type != TileType.None && below.IsFalling && below.Velocity.Y > 0)
            {
                return new TargetInfo(
                    new Vector2(x, below.Position.Y - FallingFollowDistance),
                    below.Velocity.Y,
                    true
                );
            }

            // 3. Try Diagonal Slide
            var tileBelow = state.GetTile(x, checkY);
            if (tileBelow.IsSuspended)
            {
                return FindDiagonalTarget(ref state, x, checkY, y);
            }

            // If blocked by a normal tile, stay put.
            return new TargetInfo(new Vector2(x, y), 0f, false);
        }

        // Bottom of grid
        return new TargetInfo(new Vector2(x, y), 0f, false);
    }

    private TargetInfo FindLowestVerticalTarget(ref GameState state, int x, int startY)
    {
        int floorY = startY;

        for (int k = startY + 1; k < state.Height; k++)
        {
            if (CanMoveTo(ref state, x, k))
            {
                floorY = k;
            }
            else
            {
                var below = state.GetTile(x, k);
                if (below.IsFalling)
                {
                    ReserveSlot(x, floorY, state.Width);
                    return new TargetInfo(
                        new Vector2(x, below.Position.Y - FallingFollowDistance),
                        below.Velocity.Y,
                        true
                    );
                }
                break;
            }
        }

        ReserveSlot(x, floorY, state.Width);
        return new TargetInfo(new Vector2(x, floorY), 0f, false);
    }

    private TargetInfo FindDiagonalTarget(ref GameState state, int x, int checkY, int originalY)
    {
        bool canLeft = x > 0 && CanMoveTo(ref state, x - 1, checkY) && IsOverheadClear(ref state, x - 1, originalY);
        bool canRight = x < state.Width - 1 && CanMoveTo(ref state, x + 1, checkY) && IsOverheadClear(ref state, x + 1, originalY);

        int targetX = -1;

        if (canLeft && canRight)
        {
            targetX = _random.Next(0, 2) == 0 ? x - 1 : x + 1;
        }
        else if (canLeft)
        {
            targetX = x - 1;
        }
        else if (canRight)
        {
            targetX = x + 1;
        }

        if (targetX != -1)
        {
            ReserveSlot(targetX, checkY, state.Width);
            return new TargetInfo(new Vector2(targetX, checkY), 0f, false);
        }

        return new TargetInfo(new Vector2(x, originalY), 0f, false);
    }

    private bool CanMoveTo(ref GameState state, int x, int y)
    {
        return IsInsideGrid(state, x, y) &&
               state.GetTile(x, y).Type == TileType.None &&
               !IsReserved(x, y, state.Width);
    }

    private bool IsOverheadClear(ref GameState state, int targetX, int targetY)
    {
        return state.GetTile(targetX, targetY).Type == TileType.None;
    }

    private static bool IsInsideGrid(GameState state, int x, int y)
    {
        return x >= 0 && x < state.Width && y >= 0 && y < state.Height;
    }

    private void ReserveSlot(int x, int y, int width)
    {
        _reservedSlots.Add(y * width + x);
    }

    private bool IsReserved(int x, int y, int width)
    {
        return _reservedSlots.Contains(y * width + x);
    }
}
