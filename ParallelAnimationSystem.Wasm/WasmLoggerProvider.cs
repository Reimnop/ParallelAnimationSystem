using Microsoft.Extensions.Logging;

namespace ParallelAnimationSystem.Wasm;

public class WasmLoggerProvider : ILoggerProvider
{
    private class WasmLogger(string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => throw new NotImplementedException();

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception!);
            
            var logLevelStr = logLevel switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Information => "Info",
                LogLevel.Warning => "Warn",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => "Unknown"
            };
            
            var categoryStr = $"{categoryName.Split('.')[^1]}";

            if (logLevel < LogLevel.Error)
                Console.WriteLine($"[{logLevelStr} - {categoryStr}] {message}");
            else
                Console.Error.WriteLine($"[{logLevelStr} - {categoryStr}] {message}");
        }
    }
    
    public ILogger CreateLogger(string categoryName)
        => new WasmLogger(categoryName);
    
    public void Dispose()
    {
    }
}