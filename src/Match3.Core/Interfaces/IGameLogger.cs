using System;

namespace Match3.Core.Interfaces;

/// <summary>
/// A platform-agnostic logger interface optimized for zero-allocation.
/// </summary>
/// <remarks>
/// @ai-usage-note:
/// - Use ZString.Format for structured logging to avoid string allocations.
/// - Prefer generic overloads (LogInfo&lt;T&gt;) to pass structs without boxing.
/// </remarks>
public interface IGameLogger
{
    void LogInfo(string message);
    void LogInfo<T>(string template, T arg1);
    void LogInfo<T1, T2>(string template, T1 arg1, T2 arg2);
    void LogInfo<T1, T2, T3>(string template, T1 arg1, T2 arg2, T3 arg3);
    
    void LogWarning(string message);
    void LogWarning<T>(string template, T arg1);
    
    void LogError(string message, Exception? ex = null);
}
