using System.Reflection;

namespace MiniUnit.Basic;

public static class MiniUnitRunner
{
    public static async Task<int> RunAsync(Assembly asm, string? filter = null)
    {
        var fixtures = asm.GetTypes()
            .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
            .OrderBy(t => t.FullName)
            .ToArray();

        var total = 0;
        var passed = 0;
        var failed = 0;

        foreach (var fxType in fixtures)
        {
            var fxName = fxType.FullName ?? fxType.Name;
            var (oneTimeSetUp, oneTimeTearDown, setUp, tearDown, tests)
                = InspectFixture(fxType, filter);

            object? fxInstance;

            try
            {
                fxInstance = Activator.CreateInstance(fxType);
                await InvokeAsync(fxInstance, oneTimeSetUp);
            }
            catch (Exception e)
            {
                WriteRed($"[Fixture ERROR] {fxName}: {e.GetBaseException().Message}");
                failed += tests.Count;
                continue;
            }

            foreach (var test in tests)
            {
                total++;
                var display = test.GetCustomAttribute<TestAttribute>()?.Name ?? test.Name;

                try
                {
                    await InvokeAsync(fxInstance, setUp);
                    await InvokeAsync(fxInstance, test);
                    await InvokeAsync(fxInstance, tearDown);
                    WriteGreen($"[PASS] {fxName}.{display}");
                    passed++;
                }
                catch (TargetInvocationException tie)
                {
                    var ex = tie.InnerException ?? tie;
                    WriteRed($"[FAIL] {fxName}.{display}\n{ex.GetType().Name}: {ex.Message}");
                    failed++;
                }
                catch (Exception ex)
                {
                    WriteRed($"[FAIL] {fxName}.{display}\n{ex.GetType().Name}: {ex.Message}");
                    failed++;
                }
            }

            try
            {
                await InvokeAsync(fxInstance, oneTimeTearDown);
            }
            catch (Exception e)
            {
                WriteRed($"[Fixture TearDown ERROR] {fxName}: {e.GetBaseException().Message}");
            }
        }

        Console.WriteLine($"\nTotal: {total}, Passed: {passed}, Failed: {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static (MethodInfo? oneTimeSetUp, MethodInfo? oneTimeTearDown, MethodInfo? setUp, MethodInfo? tearDown, List<MethodInfo> tests)
        InspectFixture(Type fxType, string? filter)
    {
        MethodInfo? otsu = null;
        MethodInfo? otd = null;
        MethodInfo? su = null;
        MethodInfo? td = null;
        var tests = new List<MethodInfo>();

        foreach (var m in fxType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (m.GetCustomAttribute<OneTimeSetUpAttribute>() != null) otsu = m;
            else if (m.GetCustomAttribute<SetUpAttribute>() != null) su = m;
            else if (m.GetCustomAttribute<TearDownAttribute>() != null) td = m;
            else if (m.GetCustomAttribute<OneTimeTearDownAttribute>() != null) otd = m;
            else if (m.GetCustomAttribute<TestAttribute>() != null)
            {
                if (m.GetParameters().Length != 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(filter) || m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    tests.Add(m);
                }
            }
        }

        return (otsu, otd, su, td, tests);
    }

    private static async Task InvokeAsync(object? instance, MethodInfo? method)
    {
        if (method == null) return;
        var result = method.Invoke(instance, null);
        if (result is Task t) await t;
    }

    private static void WriteGreen(string s)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(s);
        Console.ForegroundColor = prev;
    }

    private static void WriteRed(string s)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(s);
        Console.ForegroundColor = prev;
    }
}