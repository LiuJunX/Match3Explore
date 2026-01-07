using System;

namespace Match3.Core.Interfaces;

/// <summary>
/// A platform-agnostic logger interface.
/// </summary>
public interface IGameLogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
}
