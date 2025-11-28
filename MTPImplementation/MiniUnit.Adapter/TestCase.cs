using System;
using System.Reflection;

namespace MiniUnit.Adapter.MTP;

public class TestCase
{
    public string FullyQualifiedName { get; }
    public string DisplayName { get; }
    public string Source { get; }
    public Type TestType { get; }
    public MethodInfo TestMethod { get; }

    public TestCase(string fullyQualifiedName, string displayName, string source, Type testType, MethodInfo testMethod)
    {
        FullyQualifiedName = fullyQualifiedName;
        DisplayName = displayName;
        Source = source;
        TestType = testType;
        TestMethod = testMethod;
    }
}
