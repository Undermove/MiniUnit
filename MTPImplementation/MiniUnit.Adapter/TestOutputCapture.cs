using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MiniUnit.Adapter.MTP;

internal sealed class TestOutputCapture : TextWriter, ITestLogSink, IDisposable
{
    private readonly StringBuilder _sb = new();
    private readonly TextWriter _origOut;
    private readonly TextWriter _origErr;
    private readonly TraceListener _traceListener;
    private readonly Action<string> _live;
    public override Encoding Encoding => Encoding.UTF8;

    public TestOutputCapture(Action<string> live)
    {
        _live = live;
        _origOut = Console.Out;
        _origErr = Console.Error;
        Console.SetOut(this);
        Console.SetError(this);

        _traceListener = new ForwardTrace(this);
        Trace.Listeners.Add(_traceListener);
    }

    public override void WriteLine(string? value)
    {
        value ??= string.Empty;
        lock (_sb) _sb.AppendLine(value);
        _live(value);
    }

    public override void Write(char value)
    {
        lock (_sb) _sb.Append(value);
    }

    public string GetBufferedText()
    {
        lock (_sb) return _sb.ToString();
    }

    public new void Dispose()
    {
        Console.SetOut(_origOut);
        Console.SetError(_origErr);
        Trace.Listeners.Remove(_traceListener);
        _traceListener.Dispose();
    }

    private sealed class ForwardTrace(TestOutputCapture owner) : TraceListener
    {
        public override void Write(string? message) => owner.Write(message ?? "");
        public override void WriteLine(string? message) => owner.WriteLine(message ?? "");
    }
}
