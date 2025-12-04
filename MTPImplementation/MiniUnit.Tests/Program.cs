using Microsoft.Testing.Platform.Builder;
using MiniUnit.Adapter.MTP;

var builder = await TestApplication.CreateBuilderAsync(args);
builder.RegisterTestFramework(
    _ => new MiniUnitTestFrameworkCapabilities(),
    (capabilities, serviceProvider) => new MiniUnitTestFramework(capabilities, serviceProvider));
using var testApplication = await builder.BuildAsync();
return await testApplication.RunAsync();
