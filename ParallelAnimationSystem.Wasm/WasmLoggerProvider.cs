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
            Console.WriteLine($"[{logLevel}] {message}\n{categoryName}");
        }
    }
    
    public ILogger CreateLogger(string categoryName)
        => new WasmLogger(categoryName);
    
    public void Dispose()
    {
    }
}