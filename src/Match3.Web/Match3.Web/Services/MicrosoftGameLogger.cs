using System;
using Match3.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Match3.Web.Services;

public class MicrosoftGameLogger : IGameLogger
{
    private readonly ILogger _logger;

    public MicrosoftGameLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInfo(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
        {
            _logger.LogError(ex, message);
        }
        else
        {
            _logger.LogError(message);
        }
    }
}
