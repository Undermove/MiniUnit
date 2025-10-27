using System.Threading;

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