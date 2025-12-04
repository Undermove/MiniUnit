using System;
using System.Reflection;

namespace MiniUnit.Adapter.MTP;

public class TestCase(
    string fullyQualifiedName,
    string displayName,
    string source,
    Type testType,
    MethodInfo testMethod)
{
    public string FullyQualifiedName { get; } = fullyQualifiedName;
    public string DisplayName { get; } = displayName;
    public string Source { get; } = source;
    public Type TestType { get; } = testType;
    public MethodInfo TestMethod { get; } = testMethod;
}
