namespace Match3.Core.Config;

/// <summary>
/// Provides access to game configuration.
/// Implementations handle platform-specific loading (file system, resources, etc.).
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    /// Get visual configuration (colors, etc.).
    /// </summary>
    VisualConfig GetVisualConfig();

    /// <summary>
    /// Get animation timing configuration.
    /// </summary>
    AnimationConfig GetAnimationConfig();

    /// <summary>
    /// Get extended game configuration.
    /// </summary>
    GameConfigExtended GetGameConfig();

    /// <summary>
    /// Get level configuration by ID.
    /// </summary>
    LevelConfig GetLevelConfig(string levelId);

    /// <summary>
    /// List all available level IDs.
    /// </summary>
    string[] GetLevelIds();
}
