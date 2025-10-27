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
}