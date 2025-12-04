using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiniUnit.Adapter.MTP;

public class TestDiscoverer
{
    public List<TestCase> DiscoverTests(string assemblyPath)
    {
        var testCases = new List<TestCase>();
        
        Assembly? asm;
        try
        {
            asm = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception)
        {
            return testCases;
        }

        var allTestTypes = asm.GetTypes();
        foreach (var testType in allTestTypes)
        {
            var tests = testType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null && m.GetParameters().Length == 0);
            
            foreach (var m in tests)
            {
                var fullyQualifiedName = $"{testType.FullName}.{m.Name}";
                var display = m.GetCustomAttribute<TestAttribute>()?.Name ?? m.Name;
                var testCase = new TestCase(fullyQualifiedName, display, assemblyPath, testType, m);
                testCases.Add(testCase);
            }
        }

        return testCases;
    }
}
