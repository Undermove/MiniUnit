using System;
using System.Collections.Generic;

namespace MiniUnit.Adapter.Reflection;

public sealed class AssertionException(string msg) : Exception(msg);

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