using System.Reflection;

namespace MiniUnit.Basic;

public static class MiniUnitRunner
{
    public static async Task<int> RunAsync(Assembly asm, string? filter = null)
    {
        // I. Ищем тесты в сборке - то есть все классы с атрибутом TestFixtureAttribute
        IEnumerable<Type> fixtures = asm.GetTypes()
            .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
            .OrderBy(t => t.FullName)
            .ToArray();

        // Инициализируем счетчики статистики
        var total = 0;
        var passed = 0;
        var failed = 0;

        // II. Запускаем тесты - проходимся по всем классам
        foreach (var fixture in fixtures)
        {
            var fixtureName = fixture.FullName ?? fixture.Name;
            // 1. Находим все тестовые методы, сетапы и тирдауны
            var (oneTimeSetUp, oneTimeTearDown, setUp, tearDown, tests)
                = InspectFixture(fixture, filter);

            object? fxInstance;

            // 2. Создаем класс нашего теста, чтобы было у кого вызывать методы
            // и запускаем OneTimeSetup первым
            try
            {
                fxInstance = Activator.CreateInstance(fixture);
                await InvokeAsync(fxInstance, oneTimeSetUp);
            }
            catch (Exception e)
            {
                // Если он не прошел, то мы можем пометить все тесты как упавшие и завершить тестирование
                WriteRed($"[Fixture ERROR] {fixtureName}: {e.GetBaseException().Message}");
                failed += tests.Count;
                continue;
            }

            // 3. Запускаем тестовые методы по очереди
            foreach (var test in tests)
            {
                total++;
                var display = test.GetCustomAttribute<TestAttribute>()?.Name ?? test.Name;

                try
                {
                    // 4. Перед запуском теста прогоняем Setup
                    await InvokeAsync(fxInstance, setUp);
                    // 5. Потом сам тест
                    await InvokeAsync(fxInstance, test);
                    // 6. Ну и делаем тир-даун
                    await InvokeAsync(fxInstance, tearDown);
                    WriteGreen($"[PASS] {fixtureName}.{display}");
                    passed++;
                }
                catch (TargetInvocationException tie)
                {
                    // Если не получилось инициализировать тест, то пишем что не удалось и помечаем тест красным
                    var ex = tie.InnerException ?? tie;
                    WriteRed($"[FAIL] {fixtureName}.{display}\n{ex.GetType().Name}: {ex.Message}");
                    failed++;
                }
                catch (Exception ex)
                {
                    // Если тест не прошел по внутренней причине, тоже помечаем его красным
                    WriteRed($"[FAIL] {fixtureName}.{display}\n{ex.GetType().Name}: {ex.Message}");
                    failed++;
                }
            }

            // 7. Ну и напоследок после всех тестов запускаем OneTimeTearDown
            try
            {
                await InvokeAsync(fxInstance, oneTimeTearDown);
            }
            catch (Exception e)
            {
                WriteRed($"[Fixture TearDown ERROR] {fixtureName}: {e.GetBaseException().Message}");
            }
        }

        // Пишем итог
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