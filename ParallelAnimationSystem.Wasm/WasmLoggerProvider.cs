using Microsoft.Extensions.Logging;

namespace ParallelAnimationSystem.Wasm;

public class WasmLoggerProvider : ILoggerProvider
{
    private class WasmLogger(string categoryName) : ILogger
    {
#pragma warning disable CS8633
        public IDisposable BeginScope<TState>(TState state)
#pragma warning restore CS8633
            => null!;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception!);
            Console.WriteLine($"[{logLevel}] {message}\n{categoryName}");
        }
    }
    
    public ILogger CreateLogger(string categoryName)
        => new WasmLogger(categoryName);
    
    public void Dispose()
    {
    }
}