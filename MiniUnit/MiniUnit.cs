namespace MiniUnit;

// === Attributes ===
[AttributeUsage(AttributeTargets.Class)]
public sealed class TestFixtureAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SetUpAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TearDownAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OneTimeSetUpAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OneTimeTearDownAttribute : Attribute;

// === Assert ===
public static class Assert
{
    public static void IsTrue(bool condition, string? message = null)
    {
        if (!condition) throw new AssertionException(message ?? "Expected true but was false.");
    }

    public static void AreEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"Expected: {expected}; Actual: {actual}");
    }

    public static TException Throws<TException>(Action action, string? message = null) where TException : Exception
    {
        try { action(); }
        catch (TException ex) { return ex; }
        catch (Exception ex) { throw new AssertionException(message ?? $"Expected {typeof(TException).Name}, but got {ex.GetType().Name}"); }
        throw new AssertionException(message ?? $"Expected {typeof(TException).Name} but no exception was thrown.");
    }
}

public sealed class AssertionException(string msg) : Exception(msg);

// === TestLog surface ===
public interface ITestLogSink
{
    void WriteLine(string line);
}

public static class TestLog
{
    public static readonly AsyncLocal<ITestLogSink?> Current = new();

    public static void WriteLine(string message) => Current.Value?.WriteLine(message);

    public static ILoggerFactory CreateLoggerFactory() => new MiniUnitLoggerFactory();
}

// Simple built-in logger interfaces that don't depend on external packages
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}

public interface ILogger
{
    void Log(LogLevel logLevel, string message, Exception? exception = null);
    void LogInformation(string message, params object?[] args);
}

public interface ILoggerFactory
{
    ILogger CreateLogger(string categoryName);
}

public sealed class MiniUnitLogger(string category) : ILogger
{
    public void Log(LogLevel logLevel, string message, Exception? exception = null)
    {
        var sink = TestLog.Current.Value;
        if (sink == null) return;
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {logLevel,-11} {category}: {message}";
        if (exception != null) line += Environment.NewLine + exception;
        sink.WriteLine(line);
    }

    public void LogInformation(string message, params object?[] args)
    {
        var formattedMessage = string.Format(message, args);
        Log(LogLevel.Information, formattedMessage);
    }
}

public sealed class MiniUnitLoggerFactory : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName) => new MiniUnitLogger(categoryName);
}