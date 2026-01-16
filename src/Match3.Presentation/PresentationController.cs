using System;
using System.Collections.Generic;
using Match3.Core.Events;
using Match3.Core.Models.Grid;

namespace Match3.Presentation;

/// <summary>
/// Coordinates all presentation layer components: events, animations, and visual state.
/// This is the main entry point for View layer to interact with Presentation layer.
/// </summary>
public sealed class PresentationController
{
    private readonly VisualState _visualState;
    private readonly AnimationTimeline _animationTimeline;
    private readonly EventInterpreter _eventInterpreter;
    private readonly VisualStateSynchronizer _synchronizer;

    /// <summary>
    /// Visual state for rendering.
    /// </summary>
    public VisualState VisualState => _visualState;

    /// <summary>
    /// Animation timeline.
    /// </summary>
    public AnimationTimeline Timeline => _animationTimeline;

    /// <summary>
    /// Whether there are active animations.
    /// </summary>
    public bool HasActiveAnimations => _animationTimeline.HasActiveAnimations;

    /// <summary>
    /// Current animation timeline time.
    /// </summary>
    public float CurrentTime => _animationTimeline.CurrentTime;

    public PresentationController()
    {
        _visualState = new VisualState();
        _animationTimeline = new AnimationTimeline();
        _eventInterpreter = new EventInterpreter(_animationTimeline, _visualState);
        _synchronizer = new VisualStateSynchronizer(_visualState, _animationTimeline);
    }

    /// <summary>
    /// Initialize visual state from game state (call once at game start).
    /// </summary>
    public void Initialize(in GameState state)
    {
        _visualState.SyncFromGameState(in state);
        _animationTimeline.Reset();
    }

    /// <summary>
    /// Process a frame update.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    /// <param name="events">Events from simulation to process.</param>
    /// <param name="currentState">Current game state for synchronization.</param>
    public void Update(float deltaTime, IReadOnlyList<GameEvent> events, in GameState currentState)
    {
        // 1. Process events into animations
        if (events.Count > 0)
        {
            _eventInterpreter.InterpretEvents(events);
        }

        // 2. Update animation timeline
        _animationTimeline.Update(deltaTime, _visualState);

        // 3. Sync visual state from game state (handles spawned/falling tiles)
        _synchronizer.SyncFromGameState(in currentState);
    }

    /// <summary>
    /// Reset presentation state and reinitialize from game state.
    /// </summary>
    public void Reset(in GameState state)
    {
        _animationTimeline.Reset();
        _visualState.SyncFromGameState(in state);
    }
}
