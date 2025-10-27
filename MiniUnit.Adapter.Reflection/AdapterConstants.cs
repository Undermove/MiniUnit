using System;

namespace MiniUnit.Adapter.Reflection;

internal static class AdapterConstants
{
    public const string ExecutorUriString = "executor://miniunit-reflection/v1";
    public static readonly Uri ExecutorUri = new(ExecutorUriString);
}