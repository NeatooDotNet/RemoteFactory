# FactoryGenerator.cs Split Plan

## Executive Summary

**Current Status**: The `FactoryGenerator.cs` file is approximately 2108 lines and contains all type definitions, transform methods, and generation methods in a single file. There are already partial class files (`FactoryGenerator.Types.cs` and `FactoryGenerator.Transform.cs`) that appear to contain duplicated content, suggesting a prior incomplete refactoring attempt.

**Critical Finding**: The main `FactoryGenerator.cs` declares `public class FactoryGenerator` (NOT partial), while the other files declare `public partial class FactoryGenerator`. This is inconsistent - if all files are being compiled, this would cause duplicate member definitions. The main file MUST be changed to `public partial class` for a proper split.

---

## 1. Current Structure Analysis

### 1.1 File Overview

| File | Lines | Purpose | Status |
|------|-------|---------|--------|
| `FactoryGenerator.cs` | 2108 | Main file - contains everything | Needs refactoring |
| `FactoryGenerator.Types.cs` | 1053 | Type definitions (duplicated) | Already exists |
| `FactoryGenerator.Transform.cs` | 449 | Transform methods (duplicated) | Already exists |
| `DiagnosticDescriptors.cs` | 122 | Diagnostic descriptors | Complete - separate file |
| `DiagnosticInfo.cs` | 110 | Diagnostic info record | Complete - separate file |

### 1.2 Type Definitions in FactoryGenerator.cs

| Type | Lines | Line Count | Description |
|------|-------|------------|-------------|
| `TypeInfo` (record) | 77-172 | 95 | Main type info collected from source |
| `TypeFactoryMethodInfo` (record) | 177-241 | 64 | Factory method information |
| `TypeAuthMethodInfo` (record) | 243-330 | 87 | Authorization method information |
| `MethodInfo` (record) | 332-376 | 44 | Base method information |
| `MethodParameterInfo` (sealed record) | 378-406 | 28 | Method parameter details |
| `AspAuthorizeInfo` (record) | 410-451 | 41 | ASP.NET Authorize attribute info |
| `WriteFactoryMethod` (class) | 459-483 | 24 | Write operation code generator |
| `SaveFactoryMethod` (class) | 485-649 | 164 | Save operation code generator |
| `ReadFactoryMethod` (class) | 651-739 | 88 | Read operation code generator |
| `InterfaceFactoryMethod` (class) | 741-781 | 40 | Interface factory code generator |
| `CanFactoryMethod` (class) | 783-847 | 64 | Authorization check code generator |
| `FactoryMethod` (abstract class) | 849-1058 | 209 | Abstract base for all factory methods |
| `FactoryText` (class) | 1061-1071 | 10 | StringBuilder container for generation |
| **Total Types** | | **958** | |

### 1.3 Methods in FactoryGenerator.cs

| Method | Lines | Line Count | Description | Dependencies |
|--------|-------|------------|-------------|--------------|
| `Initialize` | 16-56 | 40 | IIncrementalGenerator entry point | TransformClassFactory, TransformInterfaceFactory, GenerateFactory, GenerateExecute, GenerateInterfaceFactory |
| `ClassOrBaseClassHasAttribute` | 58-71 | 13 | Recursive attribute lookup | None |
| `TransformClassFactory` | 1073-1078 | 5 | Class to TypeInfo transformation | TypeInfo constructor |
| `GenerateFactory` | 1080-1327 | 247 | Class factory code generation | FactoryText, WriteFactoryMethod, ReadFactoryMethod, SaveFactoryMethod, CanFactoryMethod, ReportDiagnostic, WithStringBuilder |
| `GenerateExecute` | 1329-1500 | 171 | Execute delegate generation | ReportDiagnostic, WithStringBuilder |
| `GetDescriptor` | 1502-1516 | 14 | Diagnostic descriptor lookup | DiagnosticDescriptors |
| `ReportDiagnostic` | 1518-1533 | 15 | Report diagnostic to context | GetDescriptor |
| `TransformInterfaceFactory` | 1535-1538 | 3 | Interface to TypeInfo transformation | TypeInfo constructor |
| `GenerateInterfaceFactory` | 1540-1674 | 134 | Interface factory code generation | FactoryText, InterfaceFactoryMethod, CanFactoryMethod, WithStringBuilder |
| `GetMethodsRecursive` | 1675-1685 | 10 | Recursive method collection | None |
| `TypeFactoryMethods` | 1687-1869 | 182 | Extract factory methods from type | TypeFactoryMethodInfo, AspAuthorizeInfo, DiagnosticInfo |
| `TypeAuthMethods` | 1871-1986 | 115 | Extract auth methods from type | ClassOrBaseClassHasAttribute, GetMethodsRecursive, TypeAuthMethodInfo, DiagnosticInfo |
| `UsingStatements` | 1988-2019 | 31 | Collect using directives | GetBaseTypeDeclarationSyntax |
| `GetBaseTypeDeclarationSyntax` | 2021-2051 | 30 | Get base type syntax | None |
| `FindNamespace` | 2053-2071 | 18 | Find namespace from syntax | None |
| `WithStringBuilder` | 2073-2081 | 8 | Utility string builder | None |
| `SafeHintName` | 2083-2107 | 24 | Create safe output file name | None |
| **Total Methods** | | **1060** | | |

### 1.4 Fields and Properties

| Member | Line | Description |
|--------|------|-------------|
| `MaxHintNameLength` (static property) | 14 | Max length for hint names |
| `factorySaveOperationAttributes` (static field) | 74 | List of save operations |

### 1.5 Dependency Graph

```
Initialize
    |
    +-- TransformClassFactory --> TypeInfo
    |       |
    |       +-- GetMethodsRecursive
    |       +-- TypeFactoryMethods --> TypeFactoryMethodInfo, DiagnosticInfo
    |       +-- TypeAuthMethods --> TypeAuthMethodInfo, DiagnosticInfo
    |       |       |
    |       |       +-- ClassOrBaseClassHasAttribute
    |       |       +-- GetMethodsRecursive
    |       +-- UsingStatements
    |       |       |
    |       |       +-- GetBaseTypeDeclarationSyntax
    |       +-- SafeHintName
    |       +-- FindNamespace
    |
    +-- TransformInterfaceFactory --> TypeInfo
    |
    +-- GenerateFactory --> FactoryText
    |       |
    |       +-- ReportDiagnostic --> GetDescriptor
    |       +-- ReadFactoryMethod
    |       +-- WriteFactoryMethod
    |       +-- SaveFactoryMethod
    |       +-- CanFactoryMethod
    |       +-- WithStringBuilder
    |
    +-- GenerateExecute
    |       |
    |       +-- ReportDiagnostic --> GetDescriptor
    |       +-- WithStringBuilder
    |
    +-- GenerateInterfaceFactory --> FactoryText
            |
            +-- InterfaceFactoryMethod
            +-- CanFactoryMethod
            +-- WithStringBuilder
```

---

## 2. Proposed File Structure

### 2.1 Overview

The split will organize the code into logical partial class files:

| File | Purpose | Estimated Lines |
|------|---------|-----------------|
| `FactoryGenerator.cs` | Entry point and orchestration | ~80 |
| `FactoryGenerator.Types.cs` | All data model types | ~700 |
| `FactoryGenerator.FactoryMethods.cs` | Factory method code generators | ~400 |
| `FactoryGenerator.Transform.cs` | Transform phase methods | ~300 |
| `FactoryGenerator.Generate.cs` | Generation phase methods | ~600 |
| `FactoryGenerator.Utilities.cs` | Utility methods | ~100 |

### 2.2 File: FactoryGenerator.cs (Entry Point)

**Purpose**: Contains only the IIncrementalGenerator implementation and orchestration.

**Contents**:
- Class declaration with `[Generator]` attribute
- `MaxHintNameLength` property (line 14)
- `Initialize` method (lines 16-56)

**Using Statements**:
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

**Current Lines**: 14-56
**Estimated New Lines**: ~80

**Code to Include**:
```csharp
// Lines 1-10: Using statements and namespace (modified)
// Line 11: [Generator] attribute
// Line 12: public partial class FactoryGenerator : IIncrementalGenerator (CHANGED to partial)
// Lines 14: MaxHintNameLength property
// Lines 16-56: Initialize method
```

### 2.3 File: FactoryGenerator.Types.cs (Data Models)

**Purpose**: Contains all record and class type definitions that model the extracted source data.

**Contents** (with current line numbers):
- `factorySaveOperationAttributes` field (line 74)
- `TypeInfo` record (lines 77-172)
- `TypeFactoryMethodInfo` record (lines 177-241)
- `TypeAuthMethodInfo` record (lines 243-330)
- `MethodInfo` record (lines 332-376)
- `MethodParameterInfo` record (lines 378-406)
- `AspAuthorizeInfo` record (lines 410-451)
- `FactoryText` class (lines 1061-1071)

**Using Statements**:
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using static Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator;
```

**Current Lines**: 74, 77-451, 1061-1071
**Estimated New Lines**: ~400

**Note**: The existing `FactoryGenerator.Types.cs` file already contains this content with documentation comments added. It should be kept and the duplicate code removed from the main file.

### 2.4 File: FactoryGenerator.FactoryMethods.cs (Code Generators)

**Purpose**: Contains the factory method classes that generate source code for different operation types.

**Contents** (with current line numbers):
- `FactoryMethod` abstract class (lines 849-1058)
- `ReadFactoryMethod` class (lines 651-739)
- `WriteFactoryMethod` class (lines 459-483)
- `SaveFactoryMethod` class (lines 485-649)
- `InterfaceFactoryMethod` class (lines 741-781)
- `CanFactoryMethod` class (lines 783-847)

**Using Statements**:
```csharp
using System.Text;
```

**Current Lines**: 459-483, 485-649, 651-739, 741-781, 783-847, 849-1058
**Estimated New Lines**: ~600

**Order in File** (for readability):
1. `FactoryMethod` (abstract base)
2. `ReadFactoryMethod` (extends FactoryMethod)
3. `WriteFactoryMethod` (extends ReadFactoryMethod)
4. `SaveFactoryMethod` (extends FactoryMethod)
5. `InterfaceFactoryMethod` (extends ReadFactoryMethod)
6. `CanFactoryMethod` (extends FactoryMethod)

**Note**: The existing `FactoryGenerator.Types.cs` includes these classes. They should be moved to a dedicated `FactoryGenerator.FactoryMethods.cs` for better organization, or kept in Types.cs if preferred.

### 2.5 File: FactoryGenerator.Transform.cs (Transform Phase)

**Purpose**: Contains methods that transform Roslyn syntax trees into the data model.

**Contents** (with current line numbers):
- `ClassOrBaseClassHasAttribute` method (lines 58-71)
- `TransformClassFactory` method (lines 1073-1078)
- `TransformInterfaceFactory` method (lines 1535-1538)
- `GetMethodsRecursive` method (lines 1675-1685)
- `TypeFactoryMethods` method (lines 1687-1869)
- `TypeAuthMethods` method (lines 1871-1986)
- `UsingStatements` method (lines 1988-2019)
- `GetBaseTypeDeclarationSyntax` method (lines 2021-2051)

**Using Statements**:
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
```

**Current Lines**: 58-71, 1073-1078, 1535-1538, 1675-1685, 1687-1869, 1871-1986, 1988-2019, 2021-2051
**Estimated New Lines**: ~350

**Note**: The existing `FactoryGenerator.Transform.cs` already contains this content. It should be kept and the duplicate code removed from the main file.

### 2.6 File: FactoryGenerator.Generate.cs (Generation Phase)

**Purpose**: Contains methods that generate source code output.

**Contents** (with current line numbers):
- `GenerateFactory` method (lines 1080-1327)
- `GenerateExecute` method (lines 1329-1500)
- `GenerateInterfaceFactory` method (lines 1540-1674)
- `GetDescriptor` method (lines 1502-1516)
- `ReportDiagnostic` method (lines 1518-1533)

**Using Statements**:
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
```

**Current Lines**: 1080-1327, 1329-1500, 1502-1516, 1518-1533, 1540-1674
**Estimated New Lines**: ~570

### 2.7 File: FactoryGenerator.Utilities.cs (Utilities)

**Purpose**: Contains utility methods used across the generator.

**Contents** (with current line numbers):
- `FindNamespace` method (lines 2053-2071)
- `WithStringBuilder` method (lines 2073-2081)
- `SafeHintName` method (lines 2083-2107)

**Using Statements**:
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
```

**Current Lines**: 2053-2071, 2073-2081, 2083-2107
**Estimated New Lines**: ~60

---

## 3. Implementation Checklist

### Phase 1: Preparation

- [ ] **Step 1.1**: Verify project compiles successfully before changes
  ```bash
  dotnet build src/RemoteFactory.FactoryGenerator/RemoteFactory.FactoryGenerator.csproj
  ```

- [ ] **Step 1.2**: Create a backup branch
  ```bash
  git checkout -b refactor/factorygenerator-split
  ```

- [ ] **Step 1.3**: Verify existing partial class files match current content
  - Compare `FactoryGenerator.Types.cs` content with main file types
  - Compare `FactoryGenerator.Transform.cs` content with main file methods

### Phase 2: Fix Class Declaration

- [ ] **Step 2.1**: Change main file class declaration to partial
  - Line 12: Change `public class FactoryGenerator` to `public partial class FactoryGenerator`
  - **VERIFY**: Project still compiles (will show duplicate member errors - expected)

### Phase 3: Remove Duplicates from Main File

- [ ] **Step 3.1**: Remove types already in `FactoryGenerator.Types.cs` from main file
  - Remove lines 74, 77-451, 1061-1071 (types defined in Types.cs)
  - Remove lines 459-483, 485-649, 651-739, 741-781, 783-847, 849-1058 (factory method classes in Types.cs)
  - **VERIFY**: Project compiles after each removal

- [ ] **Step 3.2**: Remove methods already in `FactoryGenerator.Transform.cs` from main file
  - Remove lines 58-71 (ClassOrBaseClassHasAttribute)
  - Remove lines 1073-1078 (TransformClassFactory)
  - Remove lines 1535-1538 (TransformInterfaceFactory)
  - Remove lines 1675-1685 (GetMethodsRecursive)
  - Remove lines 1687-1869 (TypeFactoryMethods)
  - Remove lines 1871-1986 (TypeAuthMethods)
  - Remove lines 1988-2019 (UsingStatements)
  - Remove lines 2021-2051 (GetBaseTypeDeclarationSyntax)
  - **VERIFY**: Project compiles after each removal

### Phase 4: Create New Partial Files

- [ ] **Step 4.1**: Create `FactoryGenerator.Generate.cs`
  - Move lines 1080-1327 (GenerateFactory)
  - Move lines 1329-1500 (GenerateExecute)
  - Move lines 1502-1516 (GetDescriptor)
  - Move lines 1518-1533 (ReportDiagnostic)
  - Move lines 1540-1674 (GenerateInterfaceFactory)
  - Add appropriate using statements
  - **VERIFY**: Project compiles

- [ ] **Step 4.2**: Create `FactoryGenerator.Utilities.cs`
  - Move lines 2053-2071 (FindNamespace)
  - Move lines 2073-2081 (WithStringBuilder)
  - Move lines 2083-2107 (SafeHintName)
  - Add appropriate using statements
  - **VERIFY**: Project compiles

### Phase 5: Reorganize Existing Files (Optional)

- [ ] **Step 5.1**: Consider moving FactoryMethod classes to separate file
  - Create `FactoryGenerator.FactoryMethods.cs`
  - Move `FactoryMethod`, `ReadFactoryMethod`, `WriteFactoryMethod`, `SaveFactoryMethod`, `InterfaceFactoryMethod`, `CanFactoryMethod` from Types.cs
  - Keep data records (`TypeInfo`, `MethodInfo`, etc.) in Types.cs
  - **VERIFY**: Project compiles

### Phase 6: Verification

- [ ] **Step 6.1**: Full build verification
  ```bash
  dotnet build src/RemoteFactory.FactoryGenerator/RemoteFactory.FactoryGenerator.csproj
  ```

- [ ] **Step 6.2**: Run all tests
  ```bash
  dotnet test
  ```

- [ ] **Step 6.3**: Verify generated output matches original
  - Build a test project that uses the generator
  - Compare generated `.g.cs` files before and after refactoring

---

## 4. Verification Plan

### 4.1 Compilation Verification

After each step:
```bash
dotnet build src/RemoteFactory.FactoryGenerator/RemoteFactory.FactoryGenerator.csproj --no-incremental
```

### 4.2 Unit Test Verification

Run all generator tests:
```bash
dotnet test tests/RemoteFactory.FactoryGenerator.Tests/
```

### 4.3 Integration Test Verification

1. Build a consuming project that uses `[Factory]` attributes
2. Verify generated factory code is identical (or functionally equivalent) to before refactoring
3. Check these specific scenarios:
   - Class with `[Factory]` attribute and Create/Fetch methods
   - Class with Insert/Update/Delete methods (Save generation)
   - Static class with Execute methods
   - Interface with `[Factory]` attribute
   - Class with `[AuthorizeFactory<T>]` attribute

### 4.4 Generated Output Comparison

```bash
# Before refactoring
dotnet build tests/TestProject/ -o before/
cp tests/TestProject/obj/Debug/**/GeneratedFiles/*.g.cs before-generated/

# After refactoring
dotnet build tests/TestProject/ -o after/
cp tests/TestProject/obj/Debug/**/GeneratedFiles/*.g.cs after-generated/

# Compare
diff -r before-generated/ after-generated/
```

---

## 5. Potential Risks and Gotchas

### 5.1 Partial Class Requirements
- All partial class declarations must use `partial` keyword
- All must be in the same namespace
- All must have the same accessibility modifier

### 5.2 Static Using Statement
- Line 7 of original: `using static Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator;`
- This is a self-referencing static import - may need to be in each file that uses `FactoryOperation` and `AuthorizeFactoryOperation` enums

### 5.3 Cross-File Dependencies
- `FactoryMethod` classes reference `FactoryOperation` enum (from static using)
- `TypeInfo` constructor calls `TypeFactoryMethods`, `TypeAuthMethods`, etc.
- These methods must remain accessible after split

### 5.4 Line Number Shifts
- As code is removed/moved, line numbers will shift
- Always work from bottom to top when removing code to preserve line numbers
- Verify each step compiles before proceeding

### 5.5 Existing Partial Files
- `FactoryGenerator.Types.cs` and `FactoryGenerator.Transform.cs` already exist
- They appear to be duplicates of code in main file
- Verify they are currently NOT being compiled (excluded from .csproj) OR
- They are being compiled and causing issues (investigate)

---

## 6. Final File Structure

After completion, the project should have:

```
src/RemoteFactory.FactoryGenerator/
    FactoryGenerator.cs              (~80 lines)  - Entry point, Initialize
    FactoryGenerator.Types.cs        (~400 lines) - Data model records
    FactoryGenerator.FactoryMethods.cs (~600 lines) - Code generator classes
    FactoryGenerator.Transform.cs    (~350 lines) - Transform phase
    FactoryGenerator.Generate.cs     (~570 lines) - Generation phase
    FactoryGenerator.Utilities.cs    (~60 lines)  - Utility methods
    DiagnosticDescriptors.cs         (~122 lines) - Already separate
    DiagnosticInfo.cs                (~110 lines) - Already separate
    EquatableArray.cs                (existing)   - Helper type
    HashCode.cs                      (existing)   - Helper type
    MapperGenerator.cs               (existing)   - Separate generator
```

Total: ~2292 lines across 6 partial files + supporting files

---

## 7. Summary of Actions

1. **Immediate**: Change `FactoryGenerator.cs` line 12 to `public partial class`
2. **Remove duplicates**: Delete code from main file that exists in Types.cs and Transform.cs
3. **Create new files**: `FactoryGenerator.Generate.cs` and `FactoryGenerator.Utilities.cs`
4. **Optional reorganization**: Move FactoryMethod classes to dedicated file
5. **Verify**: Full build, tests, and generated output comparison
