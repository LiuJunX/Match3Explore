using System;
using Match3.Core.Interfaces;

namespace Match3.Core.Logic;

public class ConsoleGameLogger : IGameLogger
{
    public void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    public void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARN] {message}");
        Console.ResetColor();
    }

    public void LogError(string message, Exception? ex = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        if (ex != null)
        {
            Console.WriteLine(ex.ToString());
        }
        Console.ResetColor();
    }
}
