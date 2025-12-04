using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
// ReSharper disable UnusedMember.Local

namespace MiniUnit.Adapter.MTP;

public class MiniUnitTestFramework(ITestFrameworkCapabilities capabilities, IServiceProvider serviceProvider)
    : ITestFramework, IDataProducer
{
    private readonly ITestFrameworkCapabilities _capabilities = capabilities;
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly TestDiscoverer _discoverer = new();
    private readonly TestExecutor _executor = new();
    private List<TestCase>? _discoveredTests;

    public string Uid => "MiniUnit";
    public string DisplayName => "MiniUnit Test Framework";
    public string Description => "A minimal unit testing framework";
    public string Version => "1.0.0";

    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    public Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        return context.Request switch
        {
            DiscoverTestExecutionRequest discoverReq => HandleDiscovery(discoverReq, context),
            RunTestExecutionRequest runReq => HandleExecution(runReq, context),
            _ => throw new InvalidOperationException($"Unknown request: {context.Request.GetType()}")
        };
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    private async Task HandleDiscovery(DiscoverTestExecutionRequest request, ExecuteRequestContext context)
    {
        var testAssembly = GetTestAssembly();
        _discoveredTests = _discoverer.DiscoverTests(testAssembly.Location);

        foreach (var test in _discoveredTests)
        {
            var node = new TestNode
            {
                Uid = new TestNodeUid(test.FullyQualifiedName),
                DisplayName = test.DisplayName,
                Properties = new PropertyBag(DiscoveredTestNodeStateProperty.CachedInstance)
            };

            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(request.Session.SessionUid, node));
        }

        context.Complete();
    }

    private async Task HandleExecution(RunTestExecutionRequest request, ExecuteRequestContext context)
    {
        var testsToRun = _discoveredTests ?? GetDiscoveredTests();

        foreach (var test in testsToRun)
        {
            var results = new List<TestResult>();
            void OnTestResult(TestResult result)
            {
                if (result.TestCase.FullyQualifiedName == test.FullyQualifiedName)
                    results.Add(result);
            }

            _executor.TestResultReceived += OnTestResult;
            _executor.ExecuteTests(new List<TestCase> { test });
            _executor.TestResultReceived -= OnTestResult;

            var result = results.FirstOrDefault();
            var stateProperty = GetStateProperty(result);

            var node = new TestNode
            {
                Uid = new TestNodeUid(test.FullyQualifiedName),
                DisplayName = test.DisplayName,
                Properties = new PropertyBag(stateProperty)
            };

            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(request.Session.SessionUid, node));
        }

        context.Complete();
    }

    private IProperty GetStateProperty(TestResult? result) =>
        result?.Outcome == TestOutcome.Passed ? PassedTestNodeStateProperty.CachedInstance :
        result?.Outcome == TestOutcome.Failed ? new FailedTestNodeStateProperty(new Exception(result.ErrorMessage)) :
        SkippedTestNodeStateProperty.CachedInstance;

    private List<TestCase> GetDiscoveredTests()
    {
        var testAssembly = GetTestAssembly();
        return _discoverer.DiscoverTests(testAssembly.Location);
    }

    private Assembly GetTestAssembly()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly?.GetName().Name == "testingplatform")
            throw new InvalidOperationException("Cannot determine test assembly from testing platform");
        return entryAssembly ?? throw new InvalidOperationException("Cannot find entry assembly");
    }
}
