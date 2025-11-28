using System;

namespace MiniUnit.Adapter.MTP;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SetUpAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TearDownAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OneTimeSetUpAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OneTimeTearDownAttribute : Attribute;
