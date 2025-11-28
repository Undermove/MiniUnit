using System.Collections.Generic;
using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace MiniUnit.Adapter.MTP;

public class MiniUnitTestFrameworkCapabilities : ITestFrameworkCapabilities
{
    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [];
}
