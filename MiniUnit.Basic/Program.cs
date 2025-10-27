using System.Reflection;
using MiniUnit.Basic;

// Опциональный фильтр по имени метода: dotnet run -- add
var filter = args.FirstOrDefault();
return await MiniUnitRunner.RunAsync(Assembly.GetExecutingAssembly(), filter);