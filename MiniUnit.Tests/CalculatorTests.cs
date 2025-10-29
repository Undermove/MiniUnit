using System.Threading.Tasks;
using MiniUnit.Adapter.Reflection;
using MiniUnit.CalculatorLib;

namespace MiniUnit.Tests.Reflection;

[Attributes]
public class CalculatorTests
{
    private Calculator _calc = null!;

    [SetUp]
    public void SetUp()
    {
        _calc = new Calculator();
        TestLog.WriteLine("SetUp complete");
    }

    [Test(Name = "Addition works")]
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