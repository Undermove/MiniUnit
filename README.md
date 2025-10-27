# MiniUnitPlayground

Three variants of a tiny test framework.
One without dotnet test integration:
- **Basic**:  implementation 

## Build & run
`cd MiniUnit.Basic`
`dotnet run`

And two wired into `dotnet test`:
- **Reflection path**: `MiniUnit` + `MiniUnit.Adapter.Reflection` + `MiniUnit.Tests.Reflection`
- **Source-generator path**: `MiniUnit` + `MiniUnit.Generators` + `MiniUnit.Adapter.Generated` + `MiniUnit.Tests.Generated`

## Build & run

```bash
dotnet restore
dotnet build

# Reflection-based tests
dotnet test MiniUnit.Tests.Reflection

# Source-generator-based tests
dotnet test MiniUnit.Tests.Generated
```