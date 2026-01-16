using System;
using Match3.Core.Events;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Core;

namespace Match3.Core.Systems.Swap;

/// <summary>
/// Swap context for Match3Engine with animation system integration.
/// Uses AnimationSystem to detect animation completion.
/// Does not emit events (AnimationSystem handles visual revert).
/// </summary>
public sealed class AnimatedSwapContext : ISwapContext
{
    private readonly IAnimationSystem _animationSystem;

    public AnimatedSwapContext(IAnimationSystem animationSystem)
    {
        _animationSystem = animationSystem ?? throw new ArgumentNullException(nameof(animationSystem));
    }

    /// <summary>
    /// Match3Engine keeps Position for animation, doesn't sync immediately.
    /// </summary>
    public bool SyncPositionOnSwap => false;

    /// <summary>
    /// Check animation completion via AnimationSystem.
    /// </summary>
    public bool IsSwapAnimationComplete(in GameState state, Position a, Position b, float animationTime)
    {
        return _animationSystem.IsVisualAtTarget(in state, a) &&
               _animationSystem.IsVisualAtTarget(in state, b);
    }

    /// <summary>
    /// Match3Engine doesn't emit events - AnimationSystem handles visual revert.
    /// </summary>
    public void EmitRevertEvent(
        in GameState state,
        Position from,
        Position to,
        long tick,
        float simTime,
        IEventCollector events)
    {
        // No-op: AnimationSystem handles the visual swap back
    }
}
