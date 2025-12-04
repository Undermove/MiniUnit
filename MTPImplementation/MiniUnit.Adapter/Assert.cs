using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniUnit.Adapter.MTP;

public sealed class AssertionException(string msg) : Exception(msg);

public static class Assert
{
    public static void IsTrue(bool condition, string? message = null)
    {
        if (!condition) throw new AssertionException(message ?? "Expected true but was false.");
    }

    public static void IsFalse(bool condition, string? message = null)
    {
        if (condition) throw new AssertionException(message ?? "Expected false but was true.");
    }

    public static void AreEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"Expected: {expected}\nBut was:  {actual}");
    }

    public static void AreNotEqual<T>(T notExpected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            throw new AssertionException(message ?? $"Did not expect: {notExpected}");
    }

    public static TException Throws<TException>(Action action, string? message = null) where TException : Exception
    {
        try { action(); }
        catch (TException ex) { return ex; }
        catch (Exception ex) { throw new AssertionException(message ?? $"Expected {typeof(TException).Name}, but {ex.GetType().Name} was thrown."); }
        throw new AssertionException(message ?? $"Expected {typeof(TException).Name} to be thrown, but no exception was thrown.");
    }

    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string? message = null) where TException : Exception
    {
        try { await action(); }
        catch (TException ex) { return ex; }
        catch (Exception ex) { throw new AssertionException(message ?? $"Expected {typeof(TException).Name}, but {ex.GetType().Name} was thrown."); }
        throw new AssertionException(message ?? $"Expected {typeof(TException).Name} to be thrown, but no exception was thrown.");
    }
}
