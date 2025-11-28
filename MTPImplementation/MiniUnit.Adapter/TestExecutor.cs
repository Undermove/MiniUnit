using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiniUnit.Adapter.MTP;

public delegate void TestResultHandler(TestResult result);

public class TestExecutor
{
    private volatile bool _cancel;
    public event TestResultHandler? TestResultReceived;

    public void Cancel() => _cancel = true;

    public void ExecuteTests(IEnumerable<TestCase> testCases)
    {
        var groupByTestFile = testCases.GroupBy(t => t.Source);
        foreach (var testFile in groupByTestFile)
        {
            if (_cancel) return;

            var groupByClass = testFile.GroupBy(tc => tc.TestType.FullName);
            foreach (var testClassGroup in groupByClass)
            {
                if (_cancel) return;

                try
                {
                    var testType = testClassGroup.First().TestType;
                    var instance = Activator.CreateInstance(testType);

                    var oneTimeSetUp = FindSingle(testType, typeof(OneTimeSetUpAttribute));
                    var oneTimeTearDown = FindSingle(testType, typeof(OneTimeTearDownAttribute));
                    var setUp = FindSingle(testType, typeof(SetUpAttribute));
                    var tearDown = FindSingle(testType, typeof(TearDownAttribute));

                    Invoke(instance, oneTimeSetUp);

                    foreach (var testCase in testClassGroup)
                    {
                        if (_cancel) return;

                        var result = new TestResult(testCase);
                        var sw = Stopwatch.StartNew();

                        using var capture = new TestOutputCapture(line => { });
                        TestLog.Current.Value = capture;

                        try
                        {
                            Invoke(instance, setUp);
                            Invoke(instance, testCase.TestMethod);
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
                                result.StandardOutput = all;
                            }

                            result.Duration = sw.Elapsed;
                            TestLog.Current.Value = null;
                            TestResultReceived?.Invoke(result);
                        }
                    }

                    Invoke(instance, oneTimeTearDown);
                }
                catch (Exception e)
                {
                    foreach (var testCase in testClassGroup)
                    {
                        var result = new TestResult(testCase)
                        {
                            Outcome = TestOutcome.Failed,
                            ErrorMessage = e.GetBaseException().Message
                        };
                        TestResultReceived?.Invoke(result);
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
}
