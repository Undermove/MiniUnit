using System;

namespace MiniUnit.Adapter.MTP;

public enum TestOutcome
{
    Passed,
    Failed,
    Skipped,
    NotRun,
    Error
}

public class TestResult
{
    public TestCase TestCase { get; }
    public TestOutcome Outcome { get; set; } = TestOutcome.NotRun;
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public TimeSpan Duration { get; set; }
    public string? StandardOutput { get; set; }

    public TestResult(TestCase testCase)
    {
        TestCase = testCase;
    }
}
