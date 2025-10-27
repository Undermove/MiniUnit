using System.Threading.Tasks;

namespace MiniUnit.Tests.Reflection;

[TestFixture]
public class CalculatorTests
{
    private Calculator _calc = null!;
    private ILogger _log = null!;

    [SetUp]
    public void SetUp()
    {
        var lf = TestLog.CreateLoggerFactory();
        _log = lf.CreateLogger("Calc");
        _calc = new Calculator(_log);
        TestLog.WriteLine("SetUp complete");
    }

    [Test(Name = "Сложение работает")]
    public void Add_Works() => Assert.AreEqual(5, _calc.Add(2, 3));

    [Test]
    public void Div_ByZero_Throws() =>
        Assert.Throws<System.DivideByZeroException>(() => _calc.Div(1, 0));

    [Test]
    public async Task Async_Add_Works()
    {
        var v = await _calc.AddAsync(40, 2);
        Assert.AreEqual(42, v);
    }
}

public sealed class Calculator(ILogger log)
{
    public int Add(int a, int b)
    {
        log.LogInformation("Add({A},{B})", a, b);
        return a + b;
    }
    public int Div(int a, int b) => a / b;
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}