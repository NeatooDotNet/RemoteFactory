---
layout: default
title: "Common Patterns"
description: "Reusable patterns and recipes for RemoteFactory applications"
parent: Examples
nav_order: 3
---

# Common Patterns

This guide presents reusable patterns and recipes for common scenarios in RemoteFactory applications.

## Upsert Pattern

Combine Insert and Update into a single method to reduce code duplication.

### Domain Model

```csharp
[Factory]
public partial class ProductModel : IProductModel, IFactorySaveMeta
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Partial method for generated mapper
    public partial void MapTo(ProductEntity entity);

    [Remote]
    [Insert][Update]  // Both attributes on same method
    public async Task Upsert([Service] IProductContext ctx)
    {
        ProductEntity entity;

        if (IsNew)
        {
            entity = new ProductEntity();
            ctx.Products.Add(entity);
        }
        else
        {
            entity = await ctx.Products.FindAsync(Id)
                ?? throw new InvalidOperationException($"Product {Id} not found");
        }

        MapTo(entity);
        await ctx.SaveChangesAsync();

        // Update ID from database-generated value
        Id = entity.Id;
        IsNew = false;
    }
}
```

### Generated Factory Behavior

The factory routes to Upsert based on `IsNew`:

```csharp
// LocalSave checks IsNew to determine Insert vs Update
if (target.IsNew)
{
    return await LocalUpsert1(target);  // Calls Upsert with Insert operation
}
else
{
    return await LocalUpsert(target);   // Calls Upsert with Update operation
}
```

## Soft Delete Pattern

Implement logical deletion instead of physical deletion.

### Domain Model

```csharp
[Factory]
public partial class CustomerModel : ICustomerModel, IFactorySaveMeta
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;  // Soft delete flag
    public DateTime? DeletedAt { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }  // From IFactorySaveMeta

    // Partial method for generated mapper
    public partial void MapFrom(CustomerEntity entity);

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] ICustomerContext ctx)
    {
        // Only fetch active customers
        var entity = await ctx.Customers
            .Where(c => c.Id == id && c.IsActive)
            .FirstOrDefaultAsync();

        if (entity == null) return false;

        MapFrom(entity);
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert][Update]
    public async Task Upsert([Service] ICustomerContext ctx)
    {
        // Standard upsert logic
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] ICustomerContext ctx)
    {
        var entity = await ctx.Customers.FindAsync(Id);
        if (entity != null)
        {
            // Soft delete: mark as inactive
            entity.IsActive = false;
            entity.DeletedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
```

### Fetch All Active

```csharp
[Factory]
public interface ICustomerQueries
{
    [Execute]
    Task<List<CustomerSummary>> GetActiveCustomers();

    [Execute]
    Task<List<CustomerSummary>> GetDeletedCustomers();
}

public class CustomerQueries : ICustomerQueries
{
    private readonly ICustomerContext _ctx;

    public async Task<List<CustomerSummary>> GetActiveCustomers()
    {
        return await _ctx.Customers
            .Where(c => c.IsActive)
            .Select(c => new CustomerSummary { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<List<CustomerSummary>> GetDeletedCustomers()
    {
        return await _ctx.Customers
            .Where(c => !c.IsActive)
            .Select(c => new CustomerSummary
            {
                Id = c.Id,
                Name = c.Name,
                DeletedAt = c.DeletedAt
            })
            .ToListAsync();
    }
}
```

## Optimistic Concurrency

Handle concurrent modifications with row versioning.

### Entity with RowVersion

```csharp
public class OrderEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Total { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
```

### Domain Model

```csharp
[Factory]
public partial class OrderModel : IOrderModel, IFactorySaveMeta
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Total { get; set; }
    public byte[]? RowVersion { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Partial method for generated mapper
    public partial void MapTo(OrderEntity entity);

    [Remote]
    [Update]
    public async Task Update([Service] IOrderContext ctx)
    {
        var entity = await ctx.Orders.FindAsync(Id)
            ?? throw new InvalidOperationException($"Order {Id} not found");

        // Check concurrency
        if (RowVersion != null && !entity.RowVersion.SequenceEqual(RowVersion))
        {
            throw new ConcurrencyException(
                "This record was modified by another user. Please reload and try again.");
        }

        MapTo(entity);
        await ctx.SaveChangesAsync();

        // Update with new row version
        RowVersion = entity.RowVersion;
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
```

### UI Handling

```razor
@code {
    private async Task Save()
    {
        try
        {
            _order = await _factory.Save(_order);
            _message = "Saved successfully";
        }
        catch (ConcurrencyException ex)
        {
            _message = ex.Message;
            // Offer to reload
            _showReloadButton = true;
        }
    }

    private async Task Reload()
    {
        _order = await _factory.Fetch(_order.Id);
        _showReloadButton = false;
    }
}
```

## Parent-Child Relationships

Handle aggregate roots with child collections.

### Models

```csharp
[Factory]
public partial class InvoiceModel : IInvoiceModel, IFactorySaveMeta
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public List<InvoiceLineModel> Lines { get; set; } = new();

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Partial methods for generated mapper
    public partial void MapFrom(InvoiceEntity entity);
    public partial void MapTo(InvoiceEntity entity);

    [Create]
    public InvoiceModel()
    {
        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IInvoiceContext ctx)
    {
        var entity = await ctx.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (entity == null) return false;

        MapFrom(entity);
        Lines = entity.Lines.Select(l => new InvoiceLineModel
        {
            Id = l.Id,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            IsNew = false
        }).ToList();

        IsNew = false;
        return true;
    }

    [Remote]
    [Insert][Update]
    public async Task Upsert([Service] IInvoiceContext ctx)
    {
        InvoiceEntity entity;

        if (IsNew)
        {
            entity = new InvoiceEntity();
            ctx.Invoices.Add(entity);
        }
        else
        {
            entity = await ctx.Invoices
                .Include(i => i.Lines)
                .FirstAsync(i => i.Id == Id);
        }

        MapTo(entity);

        // Handle lines
        SyncLines(entity, ctx);

        await ctx.SaveChangesAsync();
        Id = entity.Id;
        IsNew = false;
    }

    private void SyncLines(InvoiceEntity entity, IInvoiceContext ctx)
    {
        // Remove deleted lines
        var linesToRemove = entity.Lines
            .Where(e => !Lines.Any(m => m.Id == e.Id && !m.IsDeleted))
            .ToList();

        foreach (var line in linesToRemove)
        {
            ctx.InvoiceLines.Remove(line);
        }

        // Add/update lines
        foreach (var lineModel in Lines.Where(l => !l.IsDeleted))
        {
            InvoiceLineEntity lineEntity;

            if (lineModel.IsNew)
            {
                lineEntity = new InvoiceLineEntity { InvoiceId = Id };
                entity.Lines.Add(lineEntity);
            }
            else
            {
                lineEntity = entity.Lines.First(l => l.Id == lineModel.Id);
            }

            lineEntity.Description = lineModel.Description;
            lineEntity.Quantity = lineModel.Quantity;
            lineEntity.UnitPrice = lineModel.UnitPrice;
        }
    }
}

// Child model (not a [Factory] - managed by parent)
public class InvoiceLineModel
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }
}
```

### UI for Child Collection

```razor
<h4>Invoice Lines</h4>

@foreach (var line in _invoice.Lines.Where(l => !l.IsDeleted))
{
    <div class="row">
        <input @bind="line.Description" />
        <input type="number" @bind="line.Quantity" />
        <input type="number" @bind="line.UnitPrice" />
        <span>@line.LineTotal.ToString("C")</span>
        <button @onclick="() => RemoveLine(line)">Remove</button>
    </div>
}

<button @onclick="AddLine">Add Line</button>
<div>Total: @_invoice.Total.ToString("C")</div>

@code {
    private void AddLine()
    {
        _invoice.Lines.Add(new InvoiceLineModel());
    }

    private void RemoveLine(InvoiceLineModel line)
    {
        if (line.IsNew)
        {
            _invoice.Lines.Remove(line);
        }
        else
        {
            line.IsDeleted = true;
        }
    }
}
```

## Lookup/Reference Data

Load reference data through Execute operations.

### Interface Factory for Lookups

```csharp
[Factory]
public interface ILookupService
{
    [Execute]
    Task<List<CountryDto>> GetCountries();

    [Execute]
    Task<List<StateDto>> GetStates(string countryCode);

    [Execute]
    Task<List<CategoryDto>> GetCategories();
}

public class LookupService : ILookupService
{
    private readonly ILookupContext _ctx;

    public LookupService(ILookupContext ctx) => _ctx = ctx;

    public async Task<List<CountryDto>> GetCountries()
    {
        return await _ctx.Countries
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto { Code = c.Code, Name = c.Name })
            .ToListAsync();
    }

    public async Task<List<StateDto>> GetStates(string countryCode)
    {
        return await _ctx.States
            .Where(s => s.CountryCode == countryCode)
            .OrderBy(s => s.Name)
            .Select(s => new StateDto { Code = s.Code, Name = s.Name })
            .ToListAsync();
    }

    public async Task<List<CategoryDto>> GetCategories()
    {
        return await _ctx.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}
```

### Usage in Component

```razor
@inject ILookupServiceFactory LookupFactory

<select @bind="_selectedCountry" @bind:after="OnCountryChanged">
    <option value="">Select Country...</option>
    @foreach (var country in _countries)
    {
        <option value="@country.Code">@country.Name</option>
    }
</select>

<select @bind="_selectedState">
    <option value="">Select State...</option>
    @foreach (var state in _states)
    {
        <option value="@state.Code">@state.Name</option>
    }
</select>

@code {
    private List<CountryDto> _countries = new();
    private List<StateDto> _states = new();
    private string _selectedCountry = "";
    private string _selectedState = "";

    protected override async Task OnInitializedAsync()
    {
        _countries = await LookupFactory.GetCountries();
    }

    private async Task OnCountryChanged()
    {
        _states = string.IsNullOrEmpty(_selectedCountry)
            ? new()
            : await LookupFactory.GetStates(_selectedCountry);
        _selectedState = "";
    }
}
```

## Paged Data Loading

Implement pagination for large datasets.

### Page Request/Response DTOs

```csharp
public class PageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public string? SearchTerm { get; set; }
}

public class PageResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

### Interface Factory

```csharp
[Factory]
public interface IProductSearch
{
    [Execute]
    Task<PageResponse<ProductSummary>> SearchProducts(PageRequest request);
}

public class ProductSearch : IProductSearch
{
    private readonly IProductContext _ctx;

    public async Task<PageResponse<ProductSummary>> SearchProducts(PageRequest request)
    {
        var query = _ctx.Products.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(request.SearchTerm) ||
                p.Description.Contains(request.SearchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = request.SortBy switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "price" => request.SortDescending
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price),
            _ => query.OrderBy(p => p.Id)
        };

        // Apply paging
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductSummary
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            })
            .ToListAsync();

        return new PageResponse<ProductSummary>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

## Validation Pattern

Validate before saving with detailed error messages.

### Validation Result

```csharp
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; } = new();

    public void AddError(string property, string message)
    {
        Errors.Add(new ValidationError(property, message));
    }
}

public record ValidationError(string Property, string Message);
```

### Domain Model with Validation

```csharp
[Factory]
public class ContactModel : IContactModel, IFactorySaveMeta, IFactoryOnStart
{
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Validate before save operations
    public void FactoryStart(FactoryOperation operation)
    {
        if (operation is FactoryOperation.Insert or FactoryOperation.Update)
        {
            var validation = Validate();
            if (!validation.IsValid)
            {
                throw new ValidationException(validation);
            }
        }
    }

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(Email))
        {
            result.AddError(nameof(Email), "Email is required");
        }
        else if (!Email.Contains('@'))
        {
            result.AddError(nameof(Email), "Email must be valid");
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            result.AddError(nameof(Phone), "Phone is required");
        }

        return result;
    }
}

public class ValidationException : Exception
{
    public ValidationResult Validation { get; }

    public ValidationException(ValidationResult validation)
        : base(string.Join("; ", validation.Errors.Select(e => $"{e.Property}: {e.Message}")))
    {
        Validation = validation;
    }
}
```

### UI Error Display

```razor
@code {
    private Dictionary<string, string> _errors = new();

    private async Task Save()
    {
        _errors.Clear();

        // Client-side validation first
        var validation = _contact.Validate();
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                _errors[error.Property] = error.Message;
            }
            return;
        }

        try
        {
            _contact = await _factory.Save(_contact);
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Validation.Errors)
            {
                _errors[error.Property] = error.Message;
            }
        }
    }
}

<div class="form-group">
    <label>Email</label>
    <input @bind="_contact.Email" class="@(_errors.ContainsKey("Email") ? "is-invalid" : "")" />
    @if (_errors.TryGetValue("Email", out var emailError))
    {
        <div class="invalid-feedback">@emailError</div>
    }
</div>
```

## Caching Pattern

Cache frequently accessed data.

### With IFactoryOnComplete

```csharp
[Factory]
public class SettingsModel : ISettingsModel, IFactoryOnCompleteAsync
{
    private readonly ICacheService _cache;

    public SettingsModel(ICacheService cache) => _cache = cache;

    public string Key { get; set; } = "";
    public string Value { get; set; } = "";

    public async Task FactoryCompleteAsync(FactoryOperation operation)
    {
        if (operation is FactoryOperation.Update or FactoryOperation.Delete)
        {
            // Invalidate cache after modifications
            await _cache.RemoveAsync($"settings:{Key}");
            await _cache.RemoveAsync("settings:all");
        }
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(string key, [Service] ISettingsContext ctx)
    {
        // Try cache first
        var cached = await _cache.GetAsync<SettingsDto>($"settings:{key}");
        if (cached != null)
        {
            Key = cached.Key;
            Value = cached.Value;
            return true;
        }

        // Load from database
        var entity = await ctx.Settings.FindAsync(key);
        if (entity == null) return false;

        Key = entity.Key;
        Value = entity.Value;

        // Cache for future requests
        await _cache.SetAsync($"settings:{key}",
            new SettingsDto { Key = Key, Value = Value },
            TimeSpan.FromMinutes(30));

        return true;
    }
}
```

## Next Steps

- **[Blazor Application](blazor-app.md)**: Complete Blazor example
- **[WPF Application](wpf-app.md)**: Desktop application example
- **[Factory Lifecycle](../advanced/factory-lifecycle.md)**: Lifecycle hooks for cross-cutting concerns
