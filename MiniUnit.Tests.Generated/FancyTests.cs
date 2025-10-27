using System.Threading.Tasks;

namespace MiniUnit.Tests.Generated;

[TestFixture]
public class FancyTests
{
    private ILogger _log = null!;

    [SetUp]
    public void SetUp()
    {
        _log = TestLog.CreateLoggerFactory().CreateLogger("Fancy");
        TestLog.WriteLine("SetUp (generated) ok");
    }

    [Test(Name = "Math is still math")]
    public void Math_Add() => Assert.AreEqual(10, 7 + 3);

    [Test]
    public async Task Async_Test()
    {
        await Task.Delay(5);
        Assert.IsTrue(true);
    }
}