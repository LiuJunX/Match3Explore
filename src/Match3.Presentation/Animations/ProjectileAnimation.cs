using System.Numerics;

namespace Match3.Presentation.Animations;

/// <summary>
/// Animation for projectile movement.
/// </summary>
public sealed class ProjectileAnimation : AnimationBase
{
    private readonly long _projectileId;
    private readonly Vector2 _fromPosition;
    private readonly Vector2 _toPosition;

    /// <summary>
    /// Creates a new projectile animation.
    /// </summary>
    public ProjectileAnimation(
        long animationId,
        long projectileId,
        Vector2 from,
        Vector2 to,
        float startTime,
        float duration)
        : base(animationId, startTime, duration)
    {
        _projectileId = projectileId;
        _fromPosition = from;
        _toPosition = to;
    }

    /// <inheritdoc />
    public override int Priority => 10; // Render on top of tiles

    /// <inheritdoc />
    protected override float ApplyEasing(float t)
    {
        // Linear for projectiles (physics-driven movement)
        return t;
    }

    /// <inheritdoc />
    protected override void UpdateVisual(float progress, IVisualState visualState)
    {
        var currentPos = Vector2.Lerp(_fromPosition, _toPosition, progress);
        visualState.SetProjectilePosition(_projectileId, currentPos);
    }

    /// <inheritdoc />
    public override void OnStart()
    {
        // Projectile becomes visible when animation starts
    }
}

/// <summary>
/// Animation for projectile launch (takeoff phase).
/// </summary>
public sealed class ProjectileLaunchAnimation : AnimationBase
{
    private readonly long _projectileId;
    private readonly Vector2 _startPosition;
    private readonly float _arcHeight;

    /// <summary>
    /// Creates a new projectile launch animation.
    /// </summary>
    public ProjectileLaunchAnimation(
        long animationId,
        long projectileId,
        Vector2 startPosition,
        float arcHeight,
        float startTime,
        float duration)
        : base(animationId, startTime, duration)
    {
        _projectileId = projectileId;
        _startPosition = startPosition;
        _arcHeight = arcHeight;
    }

    /// <inheritdoc />
    public override int Priority => 10;

    /// <inheritdoc />
    protected override float ApplyEasing(float t)
    {
        // Ease-out for takeoff
        return 1f - (1f - t) * (1f - t);
    }

    /// <inheritdoc />
    protected override void UpdateVisual(float progress, IVisualState visualState)
    {
        // Rise vertically with easing
        float height = progress * _arcHeight;
        var currentPos = new Vector2(_startPosition.X, _startPosition.Y - height);
        visualState.SetProjectilePosition(_projectileId, currentPos);
    }

    /// <inheritdoc />
    public override void OnStart()
    {
        // Show projectile
    }
}

/// <summary>
/// Animation for projectile impact.
/// </summary>
public sealed class ProjectileImpactAnimation : AnimationBase
{
    private readonly long _projectileId;
    private readonly Vector2 _impactPosition;
    private readonly string _effectType;

    /// <summary>
    /// Default impact duration.
    /// </summary>
    public const float DefaultDuration = 0.3f;

    /// <summary>
    /// Creates a new projectile impact animation.
    /// </summary>
    public ProjectileImpactAnimation(
        long animationId,
        long projectileId,
        Vector2 impactPosition,
        string effectType,
        float startTime,
        float duration = DefaultDuration)
        : base(animationId, startTime, duration)
    {
        _projectileId = projectileId;
        _impactPosition = impactPosition;
        _effectType = effectType;
    }

    /// <inheritdoc />
    public override int Priority => 15;

    /// <inheritdoc />
    protected override void UpdateVisual(float progress, IVisualState visualState)
    {
        // The projectile is no longer visible after impact
        // The effect is handled separately
    }

    /// <inheritdoc />
    public override void OnStart()
    {
        // Hide projectile, show impact effect
    }

    /// <inheritdoc />
    public override void OnComplete()
    {
        // Effect cleanup
    }
}
