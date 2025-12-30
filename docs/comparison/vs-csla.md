---
layout: default
title: "RemoteFactory vs CSLA"
description: "Detailed comparison of RemoteFactory and CSLA frameworks"
parent: Comparison
nav_order: 2
---

# RemoteFactory vs CSLA

CSLA (Component-based Scalable Logical Architecture) is a mature business object framework that has been around for over 20 years. RemoteFactory shares some goals with CSLA but takes a different approach. This document provides a detailed comparison.

## Overview

### CSLA

- Created by Rockford Lhotka in the late 1990s
- Comprehensive framework for building business objects
- Supports .NET Framework and .NET Core/.NET 5+
- Uses inheritance-based patterns
- Includes built-in business rules engine
- Extensive documentation and books available

### RemoteFactory

- Modern framework using Roslyn Source Generators
- Focused on factory generation and remote invocation
- Requires .NET 8.0 or later
- Uses attribute-based patterns
- Delegates business rules to external systems
- Minimal runtime overhead due to compile-time generation

## Feature Comparison

| Feature | RemoteFactory | CSLA |
|---------|--------------|------|
| **Foundation** | Roslyn Source Generators | Runtime reflection + optional codegen |
| **Base Class** | None required | Required (BusinessBase, etc.) |
| **Data Portal** | Single `/api/neatoo` endpoint | Data Portal pattern |
| **Authorization** | `[AuthorizeFactory<T>]` + `[AspAuthorize]` | Rule-based + AuthorizationRules |
| **Validation** | DataAnnotations (external) | Built-in IDataErrorInfo + rules |
| **Business Rules** | External/manual | Built-in rules engine |
| **State Tracking** | `IFactorySaveMeta` (IsNew, IsDeleted) | Full tracking (IsDirty, IsValid, etc.) |
| **Data Binding** | INotifyPropertyChanged (manual) | Full MVVM support built-in |
| **Serialization** | System.Text.Json | MobileFormatter/custom |
| **Compile-Time Safety** | High (generated code) | Moderate |
| **Learning Curve** | Lower | Higher |
| **Maturity** | New | 20+ years |

## Architecture Comparison

### CSLA Data Portal

```csharp
// CSLA Domain Object
[Serializable]
public class PersonEdit : BusinessBase<PersonEdit>
{
    public static readonly PropertyInfo<int> IdProperty =
        RegisterProperty<int>(c => c.Id);
    public int Id
    {
        get => GetProperty(IdProperty);
        private set => SetProperty(IdProperty, value);
    }

    public static readonly PropertyInfo<string> NameProperty =
        RegisterProperty<string>(c => c.Name);
    public string Name
    {
        get => GetProperty(NameProperty);
        set => SetProperty(NameProperty, value);
    }

    [Fetch]
    private void Fetch(int id, [Inject] IPersonDal dal)
    {
        var data = dal.Fetch(id);
        using (BypassPropertyChecks)
        {
            Id = data.Id;
            Name = data.Name;
        }
    }

    [Insert]
    private void Insert([Inject] IPersonDal dal)
    {
        using (BypassPropertyChecks)
        {
            var data = new PersonData { Name = Name };
            data = dal.Insert(data);
            Id = data.Id;
        }
    }
}

// Usage
var person = await DataPortal.FetchAsync<PersonEdit>(123);
person.Name = "Updated";
person = await person.SaveAsync();
```

### RemoteFactory Approach

```csharp
// RemoteFactory Domain Object
[Factory]
public partial class PersonModel : IPersonModel
{
    public int Id { get; private set; }
    public string? Name { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonModel() { }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.Name;
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonContext context)
    {
        var entity = new PersonEntity();
        entity.Name = Name;
        context.Persons.Add(entity);
        await context.SaveChangesAsync();
        Id = entity.Id;
        IsNew = false;
    }
}

// Usage
var factory = serviceProvider.GetRequiredService<IPersonModelFactory>();
var person = await factory.Fetch(123);
person.Name = "Updated";
person = await factory.Save(person);
```

## Code Volume Comparison

### CSLA Example (Complete)

```csharp
// 1. Domain Object (50-100+ lines typically)
[Serializable]
public class PersonEdit : BusinessBase<PersonEdit>
{
    // Property registrations
    public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
    public static readonly PropertyInfo<string> FirstNameProperty = RegisterProperty<string>(c => c.FirstName);
    public static readonly PropertyInfo<string> LastNameProperty = RegisterProperty<string>(c => c.LastName);
    public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);

    // Properties with full get/set
    public int Id { get => GetProperty(IdProperty); private set => SetProperty(IdProperty, value); }
    public string FirstName { get => GetProperty(FirstNameProperty); set => SetProperty(FirstNameProperty, value); }
    public string LastName { get => GetProperty(LastNameProperty); set => SetProperty(LastNameProperty, value); }
    public string Email { get => GetProperty(EmailProperty); set => SetProperty(EmailProperty, value); }

    // Business rules
    protected override void AddBusinessRules()
    {
        base.AddBusinessRules();
        BusinessRules.AddRule(new Required(FirstNameProperty));
        BusinessRules.AddRule(new Required(LastNameProperty));
        BusinessRules.AddRule(new EmailFormat(EmailProperty));
    }

    // Authorization rules
    [ObjectAuthorizationRules]
    private static void AddObjectAuthorizationRules(ObjectAuthorizationRules rules)
    {
        rules.AllowCreate("Admin", "Manager");
        rules.AllowEdit("Admin", "Manager");
        rules.AllowDelete("Admin");
    }

    // Data access
    [Create]
    private void Create() { }

    [Fetch]
    private void Fetch(int id, [Inject] IPersonDal dal) { ... }

    [Insert]
    private void Insert([Inject] IPersonDal dal) { ... }

    [Update]
    private void Update([Inject] IPersonDal dal) { ... }

    [DeleteSelf]
    private void DeleteSelf([Inject] IPersonDal dal) { ... }
}

// 2. DAL Interface
public interface IPersonDal
{
    PersonData Fetch(int id);
    PersonData Insert(PersonData data);
    void Update(PersonData data);
    void Delete(int id);
}

// 3. DAL Implementation (20-50 lines)
public class PersonDal : IPersonDal { ... }

// 4. Data object (DTO-like)
public class PersonData
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}
```

### RemoteFactory Example (Complete)

```csharp
// 1. Domain Object (30-50 lines)
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public partial class PersonModel : IPersonModel, IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonModel() { }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context) { ... }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext context) { ... }
}

// 2. Authorization (15-25 lines)
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();
    // ... other methods
}

internal class PersonModelAuth : IPersonModelAuth
{
    public PersonModelAuth(IUser user) => _user = user;
    public bool CanAccess() => _user.IsAuthenticated;
    // ... other methods
}

// Entity class is required anyway for EF Core
// Factory interface and implementation are GENERATED
```

**Line Count Comparison:**

| Component | CSLA | RemoteFactory |
|-----------|------|---------------|
| Domain object | 80-120 lines | 40-60 lines |
| DAL interface | 10-15 lines | N/A (direct EF) |
| DAL implementation | 30-50 lines | N/A |
| Data object | 10-20 lines | N/A |
| Authorization | 10-15 lines | 15-25 lines |
| Factory | N/A | Generated |
| **Total** | **140-220 lines** | **55-85 lines** |

## Business Rules Comparison

### CSLA Built-in Rules

CSLA provides a comprehensive rules engine:

```csharp
protected override void AddBusinessRules()
{
    BusinessRules.AddRule(new Required(FirstNameProperty));
    BusinessRules.AddRule(new MaxLength(FirstNameProperty, 50));
    BusinessRules.AddRule(new MinValue<int>(AgeProperty, 0));
    BusinessRules.AddRule(new AsyncRule(ValidateEmailAsync));
}
```

**Advantages:**
- Declarative rule definition
- Automatic re-evaluation
- Dependency tracking
- Built-in common rules

### RemoteFactory External Validation

RemoteFactory delegates to standard .NET validation:

```csharp
[Factory]
public class PersonModel
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

// Validation in Blazor
<EditForm Model="_person" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <ValidationSummary />
</EditForm>
```

**Advantages:**
- Standard .NET patterns
- Works with Blazor validation
- FluentValidation compatible
- No framework-specific learning

## State Tracking Comparison

### CSLA Full Tracking

```csharp
if (person.IsDirty)           // Any changes?
if (person.IsNew)             // Never saved?
if (person.IsDeleted)         // Marked for delete?
if (person.IsValid)           // All rules pass?
if (person.IsSavable)         // Can save?
if (person.IsSelfDirty)       // This object dirty?
if (person.IsChild)           // Part of parent?
```

### RemoteFactory Minimal Tracking

```csharp
if (person.IsNew)             // Never saved?
if (person.IsDeleted)         // Marked for delete?
// Validation is external (DataAnnotations, FluentValidation)
```

## When to Choose CSLA

1. **Existing CSLA investment**: Codebase or team expertise
2. **Complex business rules**: Need the rules engine
3. **Full state tracking**: IsDirty, IsValid, etc. needed throughout
4. **Enterprise requirements**: Commercial support, established ecosystem
5. **Complete MVVM**: Full data binding support required
6. **Gradual migration**: From .NET Framework

## When to Choose RemoteFactory

1. **New .NET 8+ project**: No legacy constraints
2. **Minimal boilerplate**: Want generated factories
3. **Source generators**: Comfortable with compile-time generation
4. **External validation**: Using DataAnnotations or FluentValidation
5. **Simple authorization**: Attribute-based is sufficient
6. **Blazor/WPF focus**: Building these types of clients

## Migration Considerations

### From CSLA to RemoteFactory

**Challenges:**
- Different paradigm for business rules
- State tracking must be simplified or replicated
- Authorization patterns differ significantly
- Significant refactoring required

**Recommendation:**
- Consider for new modules/features
- Keep existing CSLA code working
- Don't attempt wholesale migration

### Coexistence

Both frameworks can coexist in the same solution:
- Legacy modules stay on CSLA
- New modules use RemoteFactory
- Shared services layer between them

## Summary

| Aspect | CSLA Better | RemoteFactory Better |
|--------|-------------|---------------------|
| Boilerplate | | X |
| Learning curve | | X |
| Business rules engine | X | |
| State tracking | X | |
| Compile-time safety | | X |
| Modern .NET patterns | | X |
| Documentation | X | |
| Community support | X | |
| Runtime performance | | X |
| Flexibility | Depends | Depends |
