using Microsoft.Extensions.Logging;

namespace MiniUnit.CalculatorLib;

public sealed class Calculator
{
    private readonly ILogger<Calculator> _logger;

    public Calculator(ILogger<Calculator>? logger = null)
    {
        _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Calculator>();
    }

    public int Add(int a, int b)
    {
        _logger.LogInformation("Making sum of {a} and {b}", a, b);
        return a + b;
    }

    public int Div(int a, int b)
    {
        _logger.LogInformation("Making division of {a} and {b}", a, b);
        return a / b;
    }

    public Task<int> AddAsync(int a, int b)
    {
        _logger.LogInformation("Making async sum of {a} and {b}", a, b);
        return Task.FromResult(a + b);
    }
}