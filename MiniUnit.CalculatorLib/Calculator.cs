namespace MiniUnit.CalculatorLib;

public sealed class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Div(int a, int b) => a / b;
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}