using System.Collections.Generic;
using System.Numerics;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Random;

namespace Match3.Core.Systems.Physics;

/// <summary>
/// Default implementation of IGravityTargetResolver.
/// Resolves gravity targets for falling tiles with diagonal slide support.
/// </summary>
public sealed class GravityTargetResolver : IGravityTargetResolver
{
    private readonly IRandom _random;
    private readonly HashSet<int> _reservedSlots;

    public GravityTargetResolver(IRandom random)
    {
        _random = random;
        _reservedSlots = new HashSet<int>();
    }

    /// <inheritdoc />
    public void ClearReservations()
    {
        _reservedSlots.Clear();
    }

    /// <inheritdoc />
    public IGravityTargetResolver.TargetInfo DetermineTarget(ref GameState state, int x, int y)
    {
        int checkY = y + 1;

        // 1. Try Vertical Move
        if (checkY < state.Height)
        {
            if (CanMoveTo(ref state, x, checkY))
            {
                return FindLowestVerticalTarget(ref state, x, checkY);
            }

            // 2. Try Diagonal Slide
            var tileBelow = state.GetTile(x, checkY);
            if (tileBelow.IsSuspended)
            {
                return FindDiagonalTarget(ref state, x, checkY, y);
            }

            // If blocked by a normal tile, stay put.
            return new IGravityTargetResolver.TargetInfo(new Vector2(x, y), 0f, false);
        }

        // Bottom of grid
        return new IGravityTargetResolver.TargetInfo(new Vector2(x, y), 0f, false);
    }

    private IGravityTargetResolver.TargetInfo FindLowestVerticalTarget(ref GameState state, int x, int startY)
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
                break;
            }
        }

        ReserveSlot(x, floorY, state.Width);
        return new IGravityTargetResolver.TargetInfo(new Vector2(x, floorY), 0f, false);
    }

    private IGravityTargetResolver.TargetInfo FindDiagonalTarget(ref GameState state, int x, int checkY, int originalY)
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
            return new IGravityTargetResolver.TargetInfo(new Vector2(targetX, checkY), 0f, false);
        }

        return new IGravityTargetResolver.TargetInfo(new Vector2(x, originalY), 0f, false);
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
