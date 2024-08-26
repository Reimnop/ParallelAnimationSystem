using Microsoft.Extensions.Logging;
using ILogger = OpenTK.Core.Utility.ILogger;
using LogLevel = OpenTK.Core.Utility.LogLevel;
using MelLogType = Microsoft.Extensions.Logging.LogLevel;

namespace ParallelAnimationSystem.Rendering;

public class MelLogger<T>(ILogger<T> melLogger, string prefix) : ILogger
{
    public LogLevel Filter { get; set; }
    
    public void LogInternal(string str, LogLevel level, string filePath, int lineNumber, string member)
    {
        if (level < Filter)
            return;
        
        var melLevel = level switch
        {
            LogLevel.Debug => MelLogType.Debug,
            LogLevel.Info => MelLogType.Information,
            LogLevel.Warning => MelLogType.Warning,
            LogLevel.Error => MelLogType.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
        melLogger.Log(melLevel, "{Prefix}{Content}", prefix, str);
    }

    public void Flush()
    {
    }
}