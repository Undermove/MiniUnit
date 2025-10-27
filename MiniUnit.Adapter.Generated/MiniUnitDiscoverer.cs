using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace MiniUnit.Adapter.Generated;

[FileExtension(".dll")]
[DefaultExecutorUri(AdapterConstants.ExecutorUriString)]
public sealed class MiniUnitDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        foreach (var source in sources)
        {
            Assembly? asm;
            try { asm = Assembly.LoadFrom(source); }
            catch (Exception e)
            {
                logger.SendMessage(TestMessageLevel.Warning, $"MiniUnit.Generated: can't load {source}: {e.GetBaseException().Message}");
                continue;
            }

            // Look for generated registry type
            var regType = asm.GetType("MiniUnit.Generated.Registry");
            var getMethod = regType?.GetMethod("GetTests", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (getMethod == null)
            {
                logger.SendMessage(TestMessageLevel.Informational, $"MiniUnit.Generated: no registry found in {source}");
                continue;
            }

            var list = (System.Collections.IEnumerable)getMethod.Invoke(null, null)!;
            foreach (var item in list)
            {
                var t = (string)item.GetType().GetProperty("FixtureType")!.GetValue(item)!;
                var m = (string)item.GetType().GetProperty("MethodName")!.GetValue(item)!;
                var display = (string?)item.GetType().GetProperty("DisplayName")!.GetValue(item);

                var fq = $"{t}.{m}";
                var tc = new TestCase(fq, AdapterConstants.ExecutorUri, source) { DisplayName = display ?? m };
                discoverySink.SendTestCase(tc);
            }
        }
    }
}