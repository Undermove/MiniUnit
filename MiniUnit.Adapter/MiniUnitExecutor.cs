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
        if (sources != null && runContext != null && frameworkHandle != null)
        {
            new MiniUnitDiscoverer().DiscoverTests(sources, runContext, frameworkHandle, sink);
        }
        RunTests(sink.Collected, runContext, frameworkHandle);
    }

    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        if (tests == null || frameworkHandle == null) return;
        var groupByTestFile = tests.GroupBy(t => t.Source);
        foreach (var testFile in groupByTestFile)
        {
            Assembly asm;
            try { asm = Assembly.LoadFrom(testFile.Key); }
            catch (Exception e)
            {
                foreach (var tc in testFile)
                {
                    var tr = new TestResult(tc) { Outcome = TestOutcome.Failed, ErrorMessage = e.GetBaseException().Message };
                    frameworkHandle?.RecordResult(tr);
                }
                continue;
            }

            var groupByClass = testFile.GroupBy(tc => Split(tc.FullyQualifiedName).typeName);
            foreach (var testClassGroup in groupByClass)
            {
                var testClass = asm.GetType(testClassGroup.Key);
                if (testClass == null) continue;

                try
                {
                    var instance = Activator.CreateInstance(testClass);

                    var oneTimeSetUp = FindSingle(testClass, typeof(OneTimeSetUpAttribute));
                    var oneTimeTearDown = FindSingle(testClass, typeof(OneTimeTearDownAttribute));
                    var setUp = FindSingle(testClass, typeof(SetUpAttribute));
                    var tearDown = FindSingle(testClass, typeof(TearDownAttribute));

                    Invoke(instance, oneTimeSetUp);

                    foreach (var testCase in testClassGroup)
                    {
                        if (_cancel) return;

                        var (_, methodName) = Split(testCase.FullyQualifiedName);
                        var test = testClass.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        var result = new TestResult(testCase);
                        frameworkHandle?.RecordStart(testCase);
                        var sw = Stopwatch.StartNew();

                        using var capture = new TestOutputCapture(line =>
                            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"[{testCase.DisplayName}] {line}"));
                        TestLog.Current.Value = capture;

                        try
                        {
                            if (test == null) throw new InvalidOperationException($"Test not found: {testCase.FullyQualifiedName}");

                            Invoke(instance, setUp);
                            Invoke(instance, test);
                            Invoke(instance, tearDown);

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
                            {
                                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, all));
                            }

                            result.Duration = sw.Elapsed;
                            frameworkHandle?.RecordResult(result);
                            frameworkHandle?.RecordEnd(testCase, result.Outcome);
                            TestLog.Current.Value = null;
                        }
                    }

                    Invoke(instance, oneTimeTearDown);
                }
                catch (Exception e)
                {
                    foreach (var testCase in testClassGroup)
                    {
                        var tr = new TestResult(testCase) { Outcome = TestOutcome.Failed, ErrorMessage = e.GetBaseException().Message };
                        frameworkHandle?.RecordResult(tr);
                    }
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
        public readonly List<TestCase> Collected = [];
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