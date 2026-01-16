using System;
using Match3.Core.Events;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Physics;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Projectiles;
using Match3.Core.Utility.Pools;

namespace Match3.Core.Simulation;

/// <summary>
/// Central simulation coordinator with tick-based updates.
/// Provides event sourcing for presentation layer and high-speed simulation for AI.
/// </summary>
public sealed class SimulationEngine : IDisposable
{
    private readonly SimulationConfig _config;
    private readonly IPhysicsSimulation _physics;
    private readonly RealtimeRefillSystem _refill;
    private readonly IMatchFinder _matchFinder;
    private readonly IMatchProcessor _matchProcessor;
    private readonly IPowerUpHandler _powerUpHandler;
    private readonly IProjectileSystem _projectileSystem;
    private readonly IExplosionSystem _explosionSystem;
    private readonly SimulationMatchHandler _matchHandler;

    private IEventCollector _eventCollector;
    private long _currentTick;
    private float _elapsedTime;
    private int _cascadeDepth;
    private int _tilesCleared;
    private int _matchesProcessed;
    private int _bombsActivated;

    /// <summary>
    /// Current game state.
    /// </summary>
    public GameState State { get; private set; }

    /// <summary>
    /// Current tick number.
    /// </summary>
    public long CurrentTick => _currentTick;

    /// <summary>
    /// Total elapsed simulation time in seconds.
    /// </summary>
    public float ElapsedTime => _elapsedTime;

    /// <summary>
    /// Event collector for the simulation.
    /// </summary>
    public IEventCollector EventCollector => _eventCollector;

    /// <summary>
    /// Creates a new simulation engine.
    /// </summary>
    public SimulationEngine(
        GameState initialState,
        SimulationConfig config,
        IPhysicsSimulation physics,
        RealtimeRefillSystem refill,
        IMatchFinder matchFinder,
        IMatchProcessor matchProcessor,
        IPowerUpHandler powerUpHandler,
        IProjectileSystem? projectileSystem = null,
        IEventCollector? eventCollector = null,
        IExplosionSystem? explosionSystem = null)
    {
        State = initialState;
        _config = config ?? new SimulationConfig();
        _physics = physics ?? throw new ArgumentNullException(nameof(physics));
        _refill = refill ?? throw new ArgumentNullException(nameof(refill));
        _matchFinder = matchFinder ?? throw new ArgumentNullException(nameof(matchFinder));
        _matchProcessor = matchProcessor ?? throw new ArgumentNullException(nameof(matchProcessor));
        _powerUpHandler = powerUpHandler ?? throw new ArgumentNullException(nameof(powerUpHandler));
        _projectileSystem = projectileSystem ?? new ProjectileSystem();
        _eventCollector = eventCollector ?? NullEventCollector.Instance;
        _explosionSystem = explosionSystem ?? new ExplosionSystem();
        _matchHandler = new SimulationMatchHandler(_matchFinder, _matchProcessor);

        _currentTick = 0;
        _elapsedTime = 0f;
    }

    /// <summary>
    /// Execute a single simulation tick.
    /// </summary>
    public TickResult Tick()
    {
        return Tick(_config.FixedDeltaTime);
    }

    /// <summary>
    /// Execute a single simulation tick with custom delta time.
    /// </summary>
    public TickResult Tick(float deltaTime)
    {
        var state = State;

        // 1. Refill empty columns
        _refill.Update(ref state);

        // 2. Update projectiles
        var projectileAffected = _projectileSystem.Update(
            ref state,
            deltaTime,
            _currentTick,
            _elapsedTime,
            _eventCollector);

        // Process tiles affected by projectile impacts
        if (projectileAffected.Count > 0)
        {
            _matchHandler.ProcessProjectileImpacts(ref state, projectileAffected, _currentTick, _elapsedTime, _eventCollector);
            _tilesCleared += projectileAffected.Count;
        }
        Pools.Release(projectileAffected);

        // Update explosions
        var triggeredBombs = Pools.ObtainList<Position>();
        try
        {
            _explosionSystem.Update(
                ref state,
                deltaTime,
                _currentTick,
                _elapsedTime,
                _eventCollector,
                triggeredBombs);

            // Handle triggered bombs
            foreach (var pos in triggeredBombs)
            {
                _powerUpHandler.ActivateBomb(ref state, pos);
                _bombsActivated++;
            }
        }
        finally
        {
            Pools.Release(triggeredBombs);
        }

        // 3. Physics (gravity)
        _physics.Update(ref state, deltaTime);

        // 4. Process stable matches
        var matchCount = _matchHandler.ProcessStableMatches(ref state, _currentTick, _elapsedTime, _eventCollector);
        if (matchCount > 0)
        {
            _matchesProcessed += matchCount;
            _cascadeDepth++;
        }

        // 5. Update tick counter
        _currentTick++;
        _elapsedTime += deltaTime;

        State = state;

        var isStable = IsStable();

        return new TickResult
        {
            CurrentTick = _currentTick,
            ElapsedTime = _elapsedTime,
            IsStable = isStable,
            HasActiveProjectiles = _projectileSystem.HasActiveProjectiles,
            HasFallingTiles = !_physics.IsStable(in state),
            HasPendingMatches = HasPendingMatches(),
            DeltaTime = deltaTime
        };
    }

    /// <summary>
    /// Run simulation until stable state.
    /// Optimized for AI - disables event collection.
    /// </summary>
    public SimulationResult RunUntilStable()
    {
        // Store original collector and disable events for performance
        var originalCollector = _eventCollector;
        _eventCollector = NullEventCollector.Instance;

        var initialScore = State.Score;
        _tilesCleared = 0;
        _matchesProcessed = 0;
        _bombsActivated = 0;
        _cascadeDepth = 0;

        int tickCount = 0;

        try
        {
            while (!IsStable() && tickCount < _config.MaxTicksPerRun)
            {
                Tick(_config.FixedDeltaTime);
                tickCount++;
            }
        }
        finally
        {
            _eventCollector = originalCollector;
        }

        return new SimulationResult
        {
            TickCount = tickCount,
            FinalState = State.Clone(),
            ReachedStability = IsStable(),
            ElapsedTime = _elapsedTime,
            ScoreGained = State.Score - initialScore,
            TilesCleared = _tilesCleared,
            MatchesProcessed = _matchesProcessed,
            BombsActivated = _bombsActivated,
            MaxCascadeDepth = _cascadeDepth
        };
    }

    /// <summary>
    /// Apply a move (swap two tiles).
    /// </summary>
    public bool ApplyMove(Position from, Position to)
    {
        var state = State;

        if (!state.IsValid(from) || !state.IsValid(to))
            return false;

        // Swap tiles in grid
        SwapTiles(ref state, from, to);

        // Emit swap event
        if (_eventCollector.IsEnabled)
        {
            var tileA = state.GetTile(from.X, from.Y);
            var tileB = state.GetTile(to.X, to.Y);

            _eventCollector.Emit(new TilesSwappedEvent
            {
                Tick = _currentTick,
                SimulationTime = _elapsedTime,
                TileAId = tileA.Id,
                TileBId = tileB.Id,
                PositionA = from,
                PositionB = to,
                IsRevert = false
            });
        }

        State = state;
        return true;
    }

    /// <summary>
    /// Activate a bomb at the specified position.
    /// </summary>
    public void ActivateBomb(Position position)
    {
        var state = State;
        _powerUpHandler.ActivateBomb(ref state, position);
        _bombsActivated++;
        State = state;
    }

    /// <summary>
    /// Check if simulation is in stable state.
    /// </summary>
    public bool IsStable()
    {
        var state = State;
        return _physics.IsStable(in state)
            && !_projectileSystem.HasActiveProjectiles
            && !_explosionSystem.HasActiveExplosions
            && !HasPendingMatches();
    }

    /// <summary>
    /// Clone the engine for parallel simulation (AI branching).
    /// </summary>
    public SimulationEngine Clone(Match3.Random.IRandom? newRandom = null)
    {
        var clonedState = State.Clone();
        if (newRandom != null)
        {
            clonedState.Random = newRandom;
        }

        return new SimulationEngine(
            clonedState,
            _config,
            _physics,
            _refill,
            _matchFinder,
            _matchProcessor,
            _powerUpHandler,
            new ProjectileSystem(), // Each clone gets its own projectile system
            NullEventCollector.Instance, // Clones always use null collector
            new ExplosionSystem() // Each clone gets its own explosion system
        );
    }

    /// <summary>
    /// Launch a projectile into the simulation.
    /// </summary>
    public void LaunchProjectile(Projectile projectile)
    {
        _projectileSystem.Launch(projectile, _currentTick, _elapsedTime, _eventCollector);
    }

    /// <summary>
    /// Gets the projectile system for advanced usage.
    /// </summary>
    public IProjectileSystem ProjectileSystem => _projectileSystem;

    /// <summary>
    /// Set a new event collector.
    /// </summary>
    public void SetEventCollector(IEventCollector collector)
    {
        _eventCollector = collector ?? NullEventCollector.Instance;
    }

    /// <summary>
    /// Reset simulation counters.
    /// </summary>
    public void ResetCounters()
    {
        _currentTick = 0;
        _elapsedTime = 0f;
        _tilesCleared = 0;
        _matchesProcessed = 0;
        _bombsActivated = 0;
        _cascadeDepth = 0;
    }

    private bool HasPendingMatches()
    {
        var state = State;
        return _matchHandler.HasPendingMatches(in state);
    }

    private void SwapTiles(ref GameState state, Position a, Position b)
    {
        var idxA = a.Y * state.Width + a.X;
        var idxB = b.Y * state.Width + b.X;
        var temp = state.Grid[idxA];
        state.Grid[idxA] = state.Grid[idxB];
        state.Grid[idxB] = temp;

        // Update positions
        state.Grid[idxA].Position = new System.Numerics.Vector2(a.X, a.Y);
        state.Grid[idxB].Position = new System.Numerics.Vector2(b.X, b.Y);
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
}
