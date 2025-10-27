using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace MiniUnit.Adapter.Reflection;

[Export(typeof(ITestDiscoverer))]
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
                logger.SendMessage(TestMessageLevel.Warning, $"MiniUnit.Reflection: can't load {source}: {e.GetBaseException().Message}");
                continue;
            }

            foreach (var t in asm.GetTypes().Where(t => t.GetCustomAttribute<Attributes>() != null))
            {
                var tests = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null && m.GetParameters().Length == 0);
                foreach (var m in tests)
                {
                    var fq = $"{t.FullName}.{m.Name}";
                    var display = m.GetCustomAttribute<TestAttribute>()?.Name ?? m.Name;
                    var tc = new TestCase(fq, AdapterConstants.ExecutorUri, source) { DisplayName = display };
                    discoverySink.SendTestCase(tc);
                }
            }
        }
    }
}