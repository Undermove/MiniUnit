using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MiniUnit.Adapter.Reflection;
using MiniUnit.CalculatorLib;
// ReSharper disable All

namespace MiniUnit.Tests.Reflection;

public class CalculatorTests
{
    private Calculator _calc = null!;
    private ILoggerFactory _loggerFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddTestLogCapture()
        );
        var logger = _loggerFactory.CreateLogger<Calculator>();
        _calc = new Calculator(logger);
        TestLog.WriteLine("SetUp complete");
    }

    [Test(Name = "Addition works")]
    public void Add_Works()
    {
        TestLog.WriteLine("Testing addition: 2 + 3");
        var result = _calc.Add(2, 3);
        TestLog.WriteLine($"Result: {result}");
        Assert.AreEqual(5, result);
    }

    [Test]
    public void Div_ByZero_Throws()
    {
        TestLog.WriteLine("Testing division by zero");
        Assert.Throws<System.DivideByZeroException>(() => _calc.Div(1, 0));
    }

    [Test]
    public async Task Async_Add_Works()
    {
        TestLog.WriteLine("Testing async addition: 40 + 2");
        var v = await _calc.AddAsync(40, 2);
        TestLog.WriteLine($"Async result: {v}");
        Assert.AreEqual(42, v);
    }

    [Test(Name = "Calculator with TestLogCapture")]
    public void Calculator_WithCustomLogging()
    {
        TestLog.WriteLine("--- Demo: Creating calculator with test log capture ---");
        
        // Создаем логгер который выводит в TestLog
        var logger = LoggerFactory.Create(builder =>
            builder.AddTestLogCapture()
        ).CreateLogger<Calculator>();
        
        logger.LogInformation("Logging custom message from test");
        TestLog.WriteLine("Test logging demonstration complete");
    }
}