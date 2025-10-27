using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace MiniUnit.Adapter.Reflection;

[Export(typeof(ITestExecutor))]
[ExtensionUri(AdapterConstants.ExecutorUriString)]
public sealed class MiniUnitExecutor : ITestExecutor
{
    private volatile bool _cancel;
    public void Cancel() => _cancel = true;

    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var sink = new MiniUnitDiscovererCollectingSink();
        new MiniUnitDiscoverer().DiscoverTests(sources, runContext, frameworkHandle, sink);
        RunTests(sink.Collected, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        foreach (var group in tests.GroupBy(t => t.Source))
        {
            Assembly asm;
            try { asm = Assembly.LoadFrom(group.Key); }
            catch (Exception e)
            {
                foreach (var tc in group)
                {
                    var tr = new TestResult(tc) { Outcome = TestOutcome.Failed, ErrorMessage = e.GetBaseException().Message };
                    frameworkHandle.RecordResult(tr);
                }
                continue;
            }

            foreach (var tc in group)
            {
                if (_cancel) return;
                var (typeName, methodName) = Split(tc.FullyQualifiedName);
                var t = asm.GetType(typeName);
                var m = t?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                var result = new TestResult(tc);
                frameworkHandle.RecordStart(tc);
                var sw = Stopwatch.StartNew();

                using var capture = new TestOutputCapture(line =>
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, $"[{tc.DisplayName}] {line}"));
                TestLog.Current.Value = capture;

                try
                {
                    if (t == null || m == null) throw new InvalidOperationException($"Test not found: {tc.FullyQualifiedName}");
                    var instance = Activator.CreateInstance(t);

                    var oneTimeSetUp = FindSingle(t, typeof(OneTimeSetUpAttribute));
                    var oneTimeTearDown = FindSingle(t, typeof(OneTimeTearDownAttribute));
                    var setUp = FindSingle(t, typeof(SetUpAttribute));
                    var tearDown = FindSingle(t, typeof(TearDownAttribute));

                    Invoke(instance, oneTimeSetUp);
                    Invoke(instance, setUp);
                    Invoke(instance, m);
                    Invoke(instance, tearDown);
                    Invoke(instance, oneTimeTearDown);

                    result.Outcome = TestOutcome.Passed;
                }
                catch (TargetInvocationException tie) when (tie.InnerException is AssertionException aex)
                {
                    result.Outcome = TestOutcome.Failed;
                    result.ErrorMessage = aex.Message;
                    result.ErrorStackTrace = aex.StackTrace;
                }
                catch (Exception e)
                {
                    result.Outcome = TestOutcome.Failed;
                    result.ErrorMessage = e.GetBaseException().Message;
                    result.ErrorStackTrace = e.ToString();
                }
                finally
                {
                    sw.Stop();
                    var all = capture.GetBufferedText();
                    if (!string.IsNullOrWhiteSpace(all))
                        result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, all));

                    result.Duration = sw.Elapsed;
                    frameworkHandle.RecordResult(result);
                    frameworkHandle.RecordEnd(tc, result.Outcome);
                    TestLog.Current.Value = null;
                }
            }
        }
    }

    private static MethodInfo? FindSingle(Type t, Type attr) =>
        t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(mi => mi.GetCustomAttribute(attr) != null);

    private static void Invoke(object? instance, MethodInfo? m)
    {
        if (m == null) return;
        var ret = m.Invoke(instance, null);
        if (ret is Task task) task.GetAwaiter().GetResult();
    }

    private static (string typeName, string methodName) Split(string fq)
    {
        var i = fq.LastIndexOf('.');
        return (fq.Substring(0, i), fq.Substring(i + 1));
    }

    private sealed class MiniUnitDiscovererCollectingSink : ITestCaseDiscoverySink, IMessageLogger, IFrameworkHandle
    {
        public readonly List<TestCase> Collected = new();
        public void SendTestCase(TestCase discoveredTest) => Collected.Add(discoveredTest);
        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message) { }
        public bool EnableShutdownAfterTestRun { get => false; set { } }
        public int LaunchProcessWithDebuggerAttached(string filePath, string? workingDirectory, string? arguments, IDictionary<string, string?>? environmentVariables) => -1;
        public void RecordAttachment(AttachmentSet attachmentSet) { }
        public void RecordAttachments(IList<AttachmentSet> attachmentSets) { }
        public void RecordEnd(TestCase testCase, TestOutcome outcome) { }
        public void RecordResult(TestResult testResult) { }
        public void RecordStart(TestCase testCase) { }
    }
}