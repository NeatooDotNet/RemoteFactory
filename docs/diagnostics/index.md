# RemoteFactory Diagnostics Reference

This page lists all diagnostics reported by the RemoteFactory source generator.

## Error Diagnostics (NF01xx)

Errors indicate problems that prevent factory generation from proceeding correctly. These must be fixed.

| ID | Description |
|----|-------------|
| [NF0101](NF0101.md) | Class must be partial for factory generation |
| [NF0102](NF0102.md) | Execute method must return Task |
| [NF0103](NF0103.md) | Execute method requires static class |
| [NF0104](NF0104.md) | Hint name truncated - potential collision risk |

## Warning Diagnostics (NF02xx)

Warnings indicate potential issues that may cause unexpected behavior. Review and fix or suppress as appropriate.

| ID | Description |
|----|-------------|
| [NF0201](NF0201.md) | Factory method must be static |
| [NF0202](NF0202.md) | Authorization method has invalid return type |
| [NF0203](NF0203.md) | Ambiguous save operations |
| [NF0204](NF0204.md) | Write operation should not return target type |

## Info Diagnostics (NF03xx)

Info diagnostics are opt-in and help debug "why didn't my method generate?" scenarios.

| ID | Description |
|----|-------------|
| [NF0301](NF0301.md) | Method has no factory operation attribute (opt-in) |

## Configuring Diagnostics

Use `.editorconfig` to customize diagnostic behavior:

```ini
[*.cs]
# Treat all RemoteFactory warnings as errors
dotnet_analyzer_diagnostic.category-RemoteFactory.Usage.severity = error

# Enable verbose mode for debugging
dotnet_diagnostic.NF0301.severity = suggestion

# Suppress specific warning
dotnet_diagnostic.NF0203.severity = none
```

### Suppressing Diagnostics in Code

You can also suppress specific diagnostics inline using pragma directives:

```csharp
#pragma warning disable NF0201 // Factory method must be static
public MyClass CreateInstance()
{
    return new MyClass();
}
#pragma warning restore NF0201
```

Or using the `[SuppressMessage]` attribute:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RemoteFactory.Usage",
    "NF0201:Factory method must be static")]
public MyClass CreateInstance()
{
    return new MyClass();
}
```

## Diagnostic Categories

The RemoteFactory source generator uses two diagnostic categories:

- **RemoteFactory.Usage** - Issues related to incorrect usage of factory attributes and patterns
- **RemoteFactory.Configuration** - Issues related to factory configuration and method signatures
