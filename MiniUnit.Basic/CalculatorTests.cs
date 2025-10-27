namespace MiniUnit.Basic;

[TestFixture]
public class CalculatorTests
{
    private Calculator _calc = null!;

    [OneTimeSetUp]
    private void OneTimeSetUp() => Console.WriteLine("Fixture init once.");

    [SetUp]
    private void SetUp() => _calc = new Calculator();

    [TearDown]
    private void TearDown() {}

    [Test(Name = "Addition works")]
    public void Add_Works() => Assert.AreEqual(5, _calc.Add(2, 3));

    [Test]
    public void Div_ByZero_Throws() => Assert.Throws<DivideByZeroException>(() => _calc.Div(1, 0));

    [Test]
    public async Task Async_Works()
    {
        var v = await _calc.AddAsync(10, 32);
        Assert.AreEqual(42, v);
    }
}

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Div(int a, int b) => a / b;
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}