using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MiniUnit.Generators;

[Generator]
public sealed class MiniUnitRegistryGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;

        // Find MiniUnit attributes by name (to avoid referencing assembly at generator time)
        var testFixtureAttr = compilation.GetTypeByMetadataName("MiniUnit.TestFixtureAttribute");
        var testAttr = compilation.GetTypeByMetadataName("MiniUnit.TestAttribute");

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("namespace MiniUnit.Generated {");
        sb.AppendLine("  internal static class Registry {");
        sb.AppendLine("    internal static IReadOnlyList<TestInfo> GetTests() {");
        sb.AppendLine("      var list = new List<TestInfo>();");

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetRoot();
            foreach (var classNode in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>())
            {
                var type = model.GetDeclaredSymbol(classNode) as INamedTypeSymbol;
                if (type is null) continue;

                if (!HasAttribute(type, testFixtureAttr)) continue;

                foreach (var m in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (m.MethodKind != MethodKind.Ordinary) continue;
                    if (!HasAttribute(m, testAttr)) continue;
                    if (m.Parameters.Length != 0) continue;

                    var displayName = m.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, testAttr))?
                        .NamedArguments.FirstOrDefault(kv => kv.Key == "Name").Value.Value?.ToString();

                    var displayNameLiteral = displayName is null ? "null" : $"\"{displayName.Replace("\"", "\\\"")}\"";
                    sb.AppendLine($"      list.Add(new TestInfo(@\"{type.ToDisplayString()}\", @\"{m.Name}\", {displayNameLiteral}));");
                }
            }
        }

        sb.AppendLine("      return list;");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("  internal sealed record TestInfo(string FixtureType, string MethodName, string? DisplayName);");
        sb.AppendLine("}");

        context.AddSource("MiniUnit.Generated.Registry.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attr)
    {
        if (attr is null) return false;
        foreach (var a in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attr))
                return true;
        }
        return false;
    }
}