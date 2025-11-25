using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace MiniUnit.Adapter.Reflection;

public interface ITestLogSink
{
    void WriteLine(string line);
}

public static class TestLog
{
    public static readonly AsyncLocal<ITestLogSink?> Current = new();

    public static void WriteLine(string message) => Current.Value?.WriteLine(message);
}

/// <summary>
/// Расширение для перенаправления логов ILogger в TestLog для тестирования
/// </summary>
public static class TestLogProviderExtensions
{
    public static ILoggingBuilder AddTestLogCapture(this ILoggingBuilder builder)
    {
        return builder.AddProvider(new TestLogProvider());
    }

    private sealed class TestLogProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestLogger();

        public void Dispose() { }

        private sealed class TestLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                
                var message = formatter(state, exception);
                TestLog.WriteLine($"[{logLevel}] {message}");
                
                if (exception != null)
                    TestLog.WriteLine($"[Exception] {exception}");
            }
        }
    }
}