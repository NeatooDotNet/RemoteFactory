The code below follows the `Order` / `OrderLine` / `OrderLineList` pattern from RemoteFactory's own `src/Design` sample. I wrote it in this reply only, as you asked, so it has not been compiled or run.

## Where things live

| Project | Contents | Why |
|---|---|---|
| `ZCrm.Domain` (referenced by client and server) | `Company`, `Address`, `AddressList`, `AddressKind`, the plain `CompanyRow` / `AddressRow` classes, `ICompanyDbContext` | Both tiers run the same classes, so no DTOs or controllers are needed. The factory methods take `AddressRow` as a parameter, so the row classes have to be here. They are plain classes and EF Core is not referenced, so EF never reaches the WASM bundle. |
| `ZCrm.Server` | `ZCrmDbContext` (EF mapping), registration | Only the server has the database. `ICompanyDbContext` is a `[Service]` parameter on server-only methods. |
| `ZCrm.Client` | Registration, the page | The page calls `ICompanyFactory` and `IAddressFactory` and never calls a repository. |

## Domain: rows and the persistence contract

```csharp
// ZCrm.Domain/Persistence/Rows.cs
namespace ZCrm.Domain.Persistence;

/// Stored row for a Company. Mapped by ZCrmDbContext on the server.
public class CompanyRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<AddressRow> Addresses { get; set; } = [];
}

/// Stored row for one Address, held in its CompanyRow's Addresses.
public class AddressRow
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public AddressKind Kind { get; set; }
}

/// What Company's server operations need from the store. Implemented by ZCrmDbContext.
public interface ICompanyDbContext
{
    /// The company row with its address rows, tracked.
    Task<CompanyRow?> FindCompanyAsync(Guid id);
    void AddCompany(CompanyRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// ZCrm.Domain/AddressKind.cs
namespace ZCrm.Domain;

public enum AddressKind
{
    Billing,
    Shipping
}
```

## Domain: Address (child)

```csharp
// ZCrm.Domain/Address.cs
using Neatoo.RemoteFactory;
using ZCrm.Domain.Persistence;

namespace ZCrm.Domain;

public interface IAddress : IFactorySaveMeta
{
    Guid Id { get; }
    string Street { get; set; }
    string City { get; set; }
    AddressKind Kind { get; set; }
}

/// Address child of Company. Maps itself into the row its list hands it.
[Factory]
internal partial class Address : IAddress
{
    public Guid Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public AddressKind Kind { get; set; }

    public bool IsNew { get; set; } = true;
    // Only the list's items cross the wire, not the list's own state, so the removal flag lives here.
    public bool IsDeleted { get; set; }

    /// New address. Public and local: the client builds it before adding it.
    [Create]
    public void Create(string street, string city, AddressKind kind)
    {
        Id = Guid.NewGuid();
        Street = street;
        City = city;
        Kind = kind;
        IsNew = true;
    }

    /// Loads from the row handed over by AddressList.Fetch.
    [Fetch]
    internal void Fetch(AddressRow row)
    {
        Id = row.Id;
        Street = row.Street;
        City = row.City;
        Kind = row.Kind;
        IsNew = false;
    }

    /// Maps a new address into the row the list made for it.
    [Insert]
    internal void Insert(AddressRow row)
    {
        row.Id = Id;
        MapTo(row);
        IsNew = false;
    }

    /// Maps an existing address into the row the list found for it.
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

## Domain: AddressList (the cascade)

```csharp
// ZCrm.Domain/AddressList.cs
using Neatoo.RemoteFactory;
using ZCrm.Domain.Persistence;

namespace ZCrm.Domain;

/// Enumerates live addresses only; a removed saved address stays flagged until the next save.
public interface IAddressList : IReadOnlyList<IAddress>, IFactorySaveMeta
{
    void AddAddress(IAddress address);
    void RemoveAddress(IAddress address);
}

/// Company's addresses. Never new or deleted itself, so its Save always routes to Update.
[Factory]
internal partial class AddressList : List<Address>, IAddressList
{
    public bool IsNew => false;
    public bool IsDeleted => false;

    private IEnumerable<Address> Live => ((IEnumerable<Address>)this).Where(a => !a.IsDeleted);

    IAddress IReadOnlyList<IAddress>.this[int index] => Live.ElementAt(index);
    int IReadOnlyCollection<IAddress>.Count => Live.Count();
    IEnumerator<IAddress> IEnumerable<IAddress>.GetEnumerator() => Live.GetEnumerator();

    /// Empty list. Public and local, so Company.Create works on the client.
    [Create]
    public void Create()
    {
    }

    /// Loads each address from its row, through the address's own Fetch.
    [Fetch]
    internal void Fetch(IEnumerable<AddressRow> rows, [Service] IAddressFactory addressFactory)
    {
        foreach (var row in rows)
        {
            Add((Address)addressFactory.Fetch(row));
        }
    }

    /// Removes the rows of removed addresses, makes or finds each live address's row,
    /// and hands it to the address's own Insert/Update through IAddressFactory.Save.
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

        RemoveAll(a => a.IsDeleted);
    }

    public void AddAddress(IAddress address)
    {
        Add((Address)address);
    }

    /// An unsaved address is dropped; a saved one is flagged so the save deletes its row.
    public void RemoveAddress(IAddress address)
    {
        var item = (Address)address;
        if (item.IsNew)
            Remove(item);
        else
            item.IsDeleted = true;
    }
}
```

## Domain: Company (aggregate root)

```csharp
// ZCrm.Domain/Company.cs
using Neatoo.RemoteFactory;
using ZCrm.Domain.Persistence;

namespace ZCrm.Domain;

public interface ICompany : IFactorySaveMeta
{
    Guid Id { get; }
    string Name { get; set; }
    IAddressList Addresses { get; }
}

/// Company aggregate root. Writes its own row and hands its address rows to AddressList.
[Factory]
internal partial class Company : ICompany
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IAddressList Addresses { get; set; } = null!;

    public bool IsNew { get; set; } = true;
    // No [Delete] yet; add one, and a settable IsDeleted, when zCRM deletes companies.
    public bool IsDeleted => false;

    /// New company. Local: nothing here needs the server.
    [Create]
    public void Create(string name, [Service] IAddressListFactory addressListFactory)
    {
        Id = Guid.NewGuid();
        Name = name;
        Addresses = addressListFactory.Create();
        IsNew = true;
    }

    /// Opens a stored company with its addresses; null from the factory when there is none.
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
        db.AddCompany(row);
        addressListFactory.Save(Addresses, row.Addresses);
        await db.SaveChangesAsync();
        IsNew = false;
    }

    /// Rename, address edits, adds and removes: one flush, one transaction.
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

    private void MapTo(CompanyRow row)
    {
        row.Name = Name;
    }
}
```

## Server: EF Core mapping and registration

```csharp
// ZCrm.Server/Data/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using ZCrm.Domain.Persistence;

namespace ZCrm.Server.Data;

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options)
    : DbContext(options), ICompanyDbContext
{
    public DbSet<CompanyRow> Companies => Set<CompanyRow>();
    public DbSet<AddressRow> Addresses => Set<AddressRow>();

    public Task<CompanyRow?> FindCompanyAsync(Guid id) =>
        Companies.Include(c => c.Addresses).SingleOrDefaultAsync(c => c.Id == id);

    public void AddCompany(CompanyRow row) => Companies.Add(row);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyRow>(e =>
        {
            e.ToTable("Company");
            e.HasKey(c => c.Id);
            // Ids come from the domain's Create; EF must treat a new keyed row as Added.
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasMany(c => c.Addresses)
                .WithOne()
                .HasForeignKey(a => a.CompanyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AddressRow>(e =>
        {
            e.ToTable("Address");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.Street).HasMaxLength(200).IsRequired();
            e.Property(a => a.City).HasMaxLength(100).IsRequired();
            e.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
        });
    }
}
```

```csharp
// ZCrm.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Domain;
using ZCrm.Domain.Persistence;
using ZCrm.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("ZCrm")));   // your provider
builder.Services.AddScoped<ICompanyDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());
builder.Services.AddNeatooAspNetCore(typeof(ICompany).Assembly);

var app = builder.Build();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();                 // the one endpoint every factory call goes through
app.MapFallbackToFile("index.html");
await app.RunAsync();
```

Two EF details are what make the save work:
- **`ValueGeneratedNever()`.** Without it, EF assumes a new `AddressRow` that already has a key is an existing row. It then issues an `UPDATE` and fails with a concurrency exception.
- **The required foreign key.** Taking an `AddressRow` out of `row.Addresses` then counts as an orphan, so EF issues a `DELETE`. EF also fills in `CompanyId` on new rows from the navigation, so `Address` never needs to know which company it belongs to.

## Client: registration and the scenario

```csharp
// ZCrm.Client/Program.cs (relevant lines)
builder.Services.AddMudServices();
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(ICompany).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

The user's flow takes two round trips: one for Fetch and one for Save. Everything in between runs on the client. MudBlazor fields bind to `Name`, `Street`, `City` and `Kind` with `@bind-Value`.

```csharp
company = await CompanyFactory.Fetch(companyId)                        // round trip 1
    ?? throw new InvalidOperationException("Company not found.");

company.Name = "Acme Holdings";                                        // rename
var billing = company.Addresses.Single(a => a.Id == billingId);
billing.Street = "200 Main St";                                        // edit one
company.Addresses.AddAddress(
    AddressFactory.Create("1 Dock Rd", "Rotterdam", AddressKind.Shipping)); // add: local
company.Addresses.RemoveAddress(
    company.Addresses.Single(a => a.Id == oldShippingId));            // remove: flag

company = await CompanyFactory.Save(company)                           // round trip 2
    ?? throw new InvalidOperationException("Saving the company returned nothing.");
```

You have to reassign the result of `Save`. It returns a new instance from the server, and the one you passed in still has its old state.

## Each factory method: `[Remote]` or not

| Method | `[Remote]`? | Why |
|---|---|---|
| `Company.Create` | **No**, `public` | Creating a company needs nothing from the server: the Guid is made locally, and `IAddressListFactory` is registered on both tiers. A `[Remote]` here would cost a round trip for what amounts to `new`. |
| `Company.Fetch` | **Yes**, `internal` | The client opens the company here, so the call has to cross. It needs `ICompanyDbContext`, which exists only on the server. `internal` lets the body be trimmed from the client. |
| `Company.Insert` / `Update` | **Yes**, `internal` | The client's `Save` crosses here with the whole graph. The DbContext and the single `SaveChangesAsync` transaction are on the server. They are not on `ICompanyFactory`; `Save` picks one based on `IsNew`. |
| `AddressList.Create` | **No**, `public` | It is called from `Company.Create` on whichever tier is creating the company. |
| `AddressList.Fetch` / `Update` | **No**, `internal` | They only run inside `Company.Fetch` / `Update`, which are already on the server. Marking them `[Remote]` would put them on the client's factory interface and give the client a way around the root. |
| `Address.Create` | **No**, `public` | The client builds the new address before adding it. |
| `Address.Fetch` / `Insert` / `Update` | **No**, `internal` | Same reason as the list: they are reached through the root's operation on the server. Each one maps only its own row; `Company` never maps an address row. |

There is no `Address.Delete`. `AddressList.Update` deletes a removed address by taking its row out of the collection, and EF turns that orphan into a `DELETE`.
