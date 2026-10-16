I wrote this from the RemoteFactory skill's compiled examples and haven't compiled it, since you asked for no files. The best-supported parts are the factory attributes, visibility, `[Remote]` placement and the save chain from parent to child. The rest is my own inference and should get a first build:
- whether the child list's factory needs `IFactorySaveMeta` on the list;
- the `using` namespaces.

## Project layout

| Project | Contains | Why |
|---|---|---|
| `zCRM.Domain` (references `Neatoo.RemoteFactory`; referenced by both tiers) | `Company`, `Address`, `AddressList`, `AddressKind`, `CompanyRow`, `AddressRow`, `ICompanyDbContext` | One assembly runs on both tiers, so you need no DTOs and no controllers. The row classes live here because the child operations take them as parameters. The DbContext is behind an interface so EF Core stays out of the WebAssembly bundle. |
| `zCRM.Server` | `ZCrmDbContext`, `Program.cs` | EF Core and the database only exist on the server. |
| `zCRM.Client` | `Program.cs`, `MainLayout.razor`, `CompanyEdit.razor` | The UI uses the generated factories. |

Keep `ImplicitUsings` turned on in `zCRM.Domain`, because the generated code relies on it.

---

## zCRM.Domain

### Persistence contract and rows

These are plain classes with no EF types, so the domain assembly stays safe to ship to the browser. The server implements the interface.

```csharp
namespace zCRM.Domain;

public sealed class CompanyRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<AddressRow> Addresses { get; set; } = [];
}

public sealed class AddressRow
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public AddressKind Kind { get; set; }
}

/// <summary>
/// Store for the Company aggregate. Implemented on the server only; reached
/// only through [Service] parameters on server-side operations.
/// </summary>
public interface ICompanyDbContext
{
    Task<CompanyRow?> FindCompanyAsync(Guid id);
    void Add(CompanyRow row);
    void Remove(CompanyRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Company (aggregate root)

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface ICompany : IFactorySaveMeta
{
    Guid Id { get; set; }
    string Name { get; set; }
    IAddressList Addresses { get; set; }
    void AddAddress(IAddress address);
    void RemoveAddress(IAddress address);
    void MarkDeleted();
}

/// <summary>
/// Company aggregate root. Owns its Addresses.
/// </summary>
[Factory]
internal partial class Company : ICompany
{
    // Public setters: the serializer only carries what it can set on the other side.
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IAddressList Addresses { get; set; } = null!;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    public void AddAddress(IAddress address) => Addresses.AddAddress(address);

    public void RemoveAddress(IAddress address) => Addresses.RemoveAddress(address);

    public void MarkDeleted() => IsDeleted = true;

    /// <summary>
    /// A new company. Local: nothing here needs the server.
    /// </summary>
    [Create]
    public void Create(string name, [Service] IAddressListFactory addressListFactory)
    {
        Id = Guid.NewGuid();
        Name = name;
        Addresses = addressListFactory.Create();
        IsNew = true;
    }

    /// <summary>
    /// Loads a stored company and its addresses; false (null from the factory) when there is none.
    /// </summary>
    [Remote, Fetch]
    internal async Task<bool> Fetch(
        Guid id,
        [Service] ICompanyDbContext db,
        [Service] IAddressListFactory addressListFactory)
    {
        var row = await db.FindCompanyAsync(id);
        if (row == null)
            return false;

        Id = row.Id;
        Name = row.Name;
        Addresses = addressListFactory.Fetch(row.Addresses);
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] ICompanyDbContext db,
        [Service] IAddressListFactory addressListFactory)
    {
        var row = new CompanyRow { Id = Id };
        MapTo(row);
        db.Add(row);
        addressListFactory.Save(Addresses, row.Addresses);
        await db.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    internal async Task Update(
        [Service] ICompanyDbContext db,
        [Service] IAddressListFactory addressListFactory)
    {
        var row = await db.FindCompanyAsync(Id)
            ?? throw new KeyNotFoundException($"Company {Id} not found");
        MapTo(row);
        addressListFactory.Save(Addresses, row.Addresses);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes the company; its address rows go with it (cascade).
    /// </summary>
    [Remote, Delete]
    internal async Task Delete([Service] ICompanyDbContext db)
    {
        var row = await db.FindCompanyAsync(Id)
            ?? throw new KeyNotFoundException($"Company {Id} not found");
        db.Remove(row);
        await db.SaveChangesAsync();
    }

    // Maps only the company's own columns; each address maps its own row.
    private void MapTo(CompanyRow row)
    {
        row.Name = Name;
    }
}
```

### Address (child entity)

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public enum AddressKind
{
    Billing,
    Shipping
}

public interface IAddress : IFactorySaveMeta
{
    Guid Id { get; set; }
    string Street { get; set; }
    string City { get; set; }
    AddressKind Kind { get; set; }
}

/// <summary>
/// Address entity, a child of Company. Writes its own AddressRow.
/// </summary>
[Factory]
internal partial class Address : IAddress
{
    public Guid Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public AddressKind Kind { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// A new address. Public and local: the client builds it, then adds it to the company.
    /// </summary>
    [Create]
    public void Create(string street, string city, AddressKind kind)
    {
        Id = Guid.NewGuid();
        Street = street;
        City = city;
        Kind = kind;
        IsNew = true;
    }

    /// <summary>
    /// Loads from the row the list hands it during Company.Fetch.
    /// </summary>
    [Fetch]
    internal void Fetch(AddressRow row)
    {
        Id = row.Id;
        Street = row.Street;
        City = row.City;
        Kind = row.Kind;
        IsNew = false;
    }

    /// <summary>
    /// Maps a new address into the row the list made for it.
    /// </summary>
    [Insert]
    internal void Insert(AddressRow row)
    {
        row.Id = Id;
        MapTo(row);
        IsNew = false;
    }

    /// <summary>
    /// Maps an existing address into the row the list found for it.
    /// </summary>
    [Update]
    internal void Update(AddressRow row)
    {
        MapTo(row);
    }

    private void MapTo(AddressRow row)
    {
        row.Street = Street;
        row.City = City;
        row.Kind = Kind;
    }
}
```

### AddressList (child collection)

This is where a removed address gets handled. An address that was never saved is dropped from the list. A stored address is marked deleted and kept in the list until the save, so the list knows to remove its row.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface IAddressList : IReadOnlyCollection<IAddress>, IFactorySaveMeta
{
    void AddAddress(IAddress address);
    void RemoveAddress(IAddress address);
}

/// <summary>
/// The Company's addresses. Makes, finds or removes each address's row during save.
/// </summary>
[Factory]
internal partial class AddressList : List<Address>, IAddressList
{
    // Never new or deleted itself: Save always routes to Update, which walks the children.
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Empty list. Public and local, so Company.Create works on the client.
    /// </summary>
    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<AddressRow> rows, [Service] IAddressFactory addressFactory)
    {
        foreach (var row in rows)
        {
            Add((Address)addressFactory.Fetch(row));
        }
    }

    /// <summary>
    /// Called by Company.Insert/Update with the company row's address collection.
    /// The list handles the rows; each address maps its own row.
    /// </summary>
    [Update]
    internal void Update(ICollection<AddressRow> rows, [Service] IAddressFactory addressFactory)
    {
        foreach (var address in this)
        {
            if (address.IsDeleted)
            {
                rows.Remove(rows.Single(r => r.Id == address.Id));
                continue;
            }

            AddressRow row;
            if (address.IsNew)
            {
                row = new AddressRow();
                rows.Add(row);
            }
            else
            {
                row = rows.Single(r => r.Id == address.Id);
            }

            addressFactory.Save(address, row);
        }

        // The removed addresses' rows are gone; the saved list holds live addresses only.
        RemoveAll(a => a.IsDeleted);
    }

    public void AddAddress(IAddress address) => Add((Address)address);

    public void RemoveAddress(IAddress address)
    {
        var item = (Address)address;
        if (item.IsNew)
            Remove(item);           // never stored: nothing to delete
        else
            item.IsDeleted = true;  // stored: the save removes its row
    }

    IEnumerator<IAddress> IEnumerable<IAddress>.GetEnumerator()
        => ((IEnumerable<Address>)this).GetEnumerator();
}
```

---

## zCRM.Server

### ZCrmDbContext

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain;

namespace zCRM.Server;

public sealed class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options)
    : DbContext(options), ICompanyDbContext
{
    public DbSet<CompanyRow> Companies => Set<CompanyRow>();
    public DbSet<AddressRow> Addresses => Set<AddressRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyRow>(e =>
        {
            e.ToTable("CompanyRow");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();   // the domain assigns Guids
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasMany(c => c.Addresses)
                .WithOne()
                .HasForeignKey(a => a.CompanyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AddressRow>(e =>
        {
            e.ToTable("AddressRow");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.Street).HasMaxLength(200).IsRequired();
            e.Property(a => a.City).HasMaxLength(100).IsRequired();
        });
    }

    public Task<CompanyRow?> FindCompanyAsync(Guid id)
        => Companies.Include(c => c.Addresses).SingleOrDefaultAsync(c => c.Id == id);

    void ICompanyDbContext.Add(CompanyRow row) => Companies.Add(row);

    void ICompanyDbContext.Remove(CompanyRow row) => Companies.Remove(row);
}
```

Three of these settings matter for the save:
- **`ValueGeneratedNever`:** a new `AddressRow` added to a tracked collection is inserted with the Guid the domain gave it, and EF doesn't mistake it for an existing row.
- **Required foreign key:** a row removed from `row.Addresses` is deleted as an orphan.
- **One `SaveChangesAsync` in the root:** the whole save runs in one transaction.

### Program.cs (server)

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooAspNetCore(typeof(ICompany).Assembly);
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<ICompanyDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The single endpoint every factory call goes through
app.UseNeatoo();

app.MapFallbackToFile("index.html");
await app.RunAsync();
```

Swap `UseSqlServer` for whatever database provider you use.

---

## zCRM.Client

### Program.cs (client)

The client registers no database. Every method that takes `ICompanyDbContext` runs only on the server.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(ICompany).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

### MainLayout.razor

MudBlazor's providers need to be here: `MudSelect` needs the popover provider. Add `@using MudBlazor` to `_Imports.razor`.

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent>@Body</MudMainContent>
</MudLayout>
```

### CompanyEdit.razor

This is the whole scenario: one round trip to load, one to save, and every edit in between happens in the browser.

```razor
@page "/companies/{Id:guid}"
@using zCRM.Domain
@inject ICompanyFactory CompanyFactory
@inject IAddressFactory AddressFactory
@inject ISnackbar Snackbar

@if (company is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudTextField @bind-Value="company.Name" Label="Company name" />

    @foreach (var address in company.Addresses.Where(a => !a.IsDeleted))
    {
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudTextField @bind-Value="address.Street" Label="Street" />
            <MudTextField @bind-Value="address.City" Label="City" />
            <MudSelect T="AddressKind" @bind-Value="address.Kind" Label="Kind">
                <MudSelectItem Value="AddressKind.Billing">Billing</MudSelectItem>
                <MudSelectItem Value="AddressKind.Shipping">Shipping</MudSelectItem>
            </MudSelect>
            <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="() => Remove(address)" />
        </MudStack>
    }

    <MudButton OnClick="AddAddress">Add address</MudButton>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveAsync">Save</MudButton>
}

@code {
    [Parameter] public Guid Id { get; set; }

    private ICompany? company;

    protected override async Task OnParametersSetAsync()
    {
        company = await CompanyFactory.Fetch(Id);   // [Remote]: one round trip
        if (company is null)
            Snackbar.Add("Company not found.", Severity.Error);
    }

    // Built on the client by the child's local Create; no server call.
    private void AddAddress()
        => company!.AddAddress(AddressFactory.Create(string.Empty, string.Empty, AddressKind.Shipping));

    private void Remove(IAddress address) => company!.RemoveAddress(address);

    private async Task SaveAsync()
    {
        try
        {
            // Save returns a new instance from the server: reassign it.
            company = await CompanyFactory.Save(company!);
            Snackbar.Add("Saved.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
    }
}
```

---

## Which factory methods are `[Remote]`, and why

| Method | `[Remote]`? | Why |
|---|---|---|
| `Company.Create` | No, `public` | A new company needs nothing that only the server has. The address-list factory exists on the client, so there's no round trip. |
| `Company.Fetch(Guid)` | **Yes**, `internal` | The client loads it, and it needs `ICompanyDbContext`, which only the server has. The generator makes it `public` on `ICompanyFactory`. |
| `Company.Insert` / `Update` / `Delete` | **Yes**, `internal` | The client calls `Save`, which crosses to the server and picks one of these from `IsNew`/`IsDeleted`. They aren't on the factory interface by name. |
| `AddressList.Create` | No, `public` | `Company.Create` calls it on whichever side it runs, usually the client. |
| `AddressList.Fetch` / `Update` | No, `internal` | They only run inside `Company`'s server operations, which have already crossed. `[Remote]` here wouldn't add a round trip, but it would give the client a way in that bypasses the company. |
| `Address.Create` | No, `public` | The client builds a new address and then calls `company.AddAddress`. |
| `Address.Fetch` / `Insert` / `Update` | No, `internal` | They're already on the server. Each one maps only its own `AddressRow`. |

## What happens in your scenario

1. **Open:** `Fetch` makes one call to the server. The company comes back with its addresses, each built by its own `[Fetch]` from its row.
2. **Rename, edit an address, add one, remove one:** all of this happens in the browser.
   - The edits are plain property setters.
   - The new address is created on the client and has `IsNew = true`.
   - The removed address was stored, so it gets `IsDeleted = true` and stays in the list.
3. **Save:** `Save` makes one call to the server. Because the company has `IsNew = false`, it runs `Company.Update`:
   - It maps the name onto the company row and hands `row.Addresses` to the list.
   - The list removes the deleted address's row, adds a row for the new address, and finds the existing rows. Each address maps itself.
   - One `SaveChangesAsync` writes it all in one transaction.
4. **After the save:** the page keeps the returned company, so the new address now has `IsNew = false` and the removed address is gone.

Without Neatoo there's no change tracking, so every surviving address is mapped on each save. EF only sends UPDATEs for columns that actually changed, so unchanged addresses cost nothing in SQL. There's also no validation framework: an empty name would save. A check would belong in `Company`, not in the page or the factories.
