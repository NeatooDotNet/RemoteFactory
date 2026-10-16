I didn't compile any of this, because you asked for code in the reply rather than files. The RemoteFactory calls follow the library's tested examples. The `using` namespaces for RemoteFactory (`Neatoo.RemoteFactory`, `Neatoo.RemoteFactory.AspNetCore`) are my best reading and haven't been checked against the packages. Keep `ImplicitUsings` enabled in all three projects, because the generated code relies on it.

## The design

| Service | What it is | How it reaches its code | Registered on |
|---|---|---|---|
| `IPhoneFormatter` | Pure computation, needed on both tiers | Through `Contact`'s **constructor**. RemoteFactory builds `Contact` through DI on whichever tier is deserializing it, so each tier passes in its own copy. | Client **and** server |
| `IAuditTrail` | Writes to the database, server only | As a **`[Service]` parameter** on `Contact`'s `[Remote] internal` Insert, Update and Delete, which only run on the server | Server only |

How the code is split across projects:
- **zCRM.Domain** is referenced by both tiers and depends on `Neatoo.RemoteFactory`. It holds `Contact`, the service interfaces, `PhoneFormatter` and `ContactRow`.
- **zCRM.Server** depends on `Neatoo.RemoteFactory.AspNetCore` and EF Core. It holds the EF context and `AuditTrail`.
- **zCRM.Client** is the Blazor WASM app with MudBlazor.

---

## zCRM.Domain

### `Phones/IPhoneFormatter.cs` and `Phones/PhoneFormatter.cs`
These live in the domain because both tiers need the same implementation. It has no dependencies, so a singleton is safe.

```csharp
using System.Globalization;

namespace zCRM.Domain.Phones;

/// <summary>
/// Formats a phone number for the current user's country. No I/O; available on both tiers.
/// </summary>
public interface IPhoneFormatter
{
    string Format(string? phone);
}

/// <summary>
/// Reads the user's country from CurrentCulture, which on the Blazor client is the browser's culture.
/// </summary>
public sealed class PhoneFormatter : IPhoneFormatter
{
    public string Format(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        var formatted = CurrentCountry() switch
        {
            "US" or "CA" => FormatNanp(digits),
            "GB" => FormatGb(digits),
            "FR" => FormatFr(digits),
            _ => null
        };

        return formatted ?? phone.Trim();
    }

    private static string? CurrentCountry()
    {
        try
        {
            return new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName;
        }
        catch (ArgumentException)
        {
            // Neutral or invariant culture: no country to format for.
            return null;
        }
    }

    // (555) 123-4567
    private static string? FormatNanp(string digits)
    {
        if (digits.Length == 11 && digits[0] == '1')
            digits = digits[1..];
        return digits.Length == 10
            ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
            : null;
    }

    // 01632 960123
    private static string? FormatGb(string digits)
    {
        if (digits.StartsWith("44") && digits.Length == 12)
            digits = "0" + digits[2..];
        return digits.Length == 11 && digits[0] == '0'
            ? $"{digits[..5]} {digits[5..]}"
            : null;
    }

    // 01 23 45 67 89
    private static string? FormatFr(string digits)
    {
        if (digits.StartsWith("33") && digits.Length == 11)
            digits = "0" + digits[2..];
        return digits.Length == 10
            ? string.Join(' ', Enumerable.Range(0, 5).Select(i => digits.Substring(i * 2, 2)))
            : null;
    }
}
```

### `Auditing/IAuditTrail.cs`
`Contact`'s server operations name this interface, so it has to be in the domain. Its implementation is server-only.

```csharp
namespace zCRM.Domain.Auditing;

/// <summary>
/// Records audit entries. Server-only. Joins the request's unit of work:
/// the caller's SaveChangesAsync commits the entry with the change it describes.
/// </summary>
public interface IAuditTrail
{
    void Record(string entityType, Guid entityId, string action, string summary);
}
```

### `Contacts/ContactRow.cs` and `Contacts/IContactDb.cs`
The row and the store interface are named by `Contact`'s server operations. The EF implementation lives on the server.

```csharp
namespace zCRM.Domain.Contacts;

public class ContactRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Contact persistence. Server-only; a [Service] parameter on Contact's remote operations.
/// </summary>
public interface IContactDb
{
    Task<ContactRow?> FindContactAsync(Guid id);
    void AddContact(ContactRow row);
    void RemoveContact(ContactRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### `Contacts/Contact.cs`
This is the aggregate root.
- **`IPhoneFormatter`** arrives through the constructor because `FormattedPhone` is read on the client, wherever the instance happens to be.
- **`IAuditTrail`** and `IContactDb` are `[Service]` parameters on the `[Remote] internal` write operations. Those operations run on the server, and their bodies are trimmed from the published client.
- **The audit entry** is added before the single `SaveChangesAsync`, so the contact row and its audit row commit together.

```csharp
using Neatoo.RemoteFactory;
using zCRM.Domain.Auditing;
using zCRM.Domain.Phones;

namespace zCRM.Domain.Contacts;

public interface IContact : IFactorySaveMeta
{
    Guid Id { get; }
    string Name { get; set; }
    string Phone { get; set; }
    string FormattedPhone { get; }
    void MarkDeleted();
}

/// <summary>
/// Contact aggregate root.
/// </summary>
[Factory]
internal partial class Contact : IContact
{
    // Registered on both tiers: whichever side deserializes a Contact builds it through DI.
    private readonly IPhoneFormatter _phoneFormatter;

    public Contact(IPhoneFormatter phoneFormatter)
    {
        _phoneFormatter = phoneFormatter;
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Computed on the tier that reads it; not serialized.
    public string FormattedPhone => _phoneFormatter.Format(Phone);

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    public void MarkDeleted() => IsDeleted = true;

    /// <summary>
    /// Creates a new Contact. Local: no server call.
    /// </summary>
    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Loads a Contact; false when there is none.
    /// </summary>
    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactDb db)
    {
        var row = await db.FindContactAsync(id);
        if (row == null)
            return false;

        Id = row.Id;
        Name = row.Name;
        Phone = row.Phone;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IContactDb db, [Service] IAuditTrail audit)
    {
        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.AddContact(row);
        audit.Record(nameof(Contact), Id, "Inserted", $"{Name} {Phone}");
        await db.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    internal async Task Update([Service] IContactDb db, [Service] IAuditTrail audit)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        audit.Record(nameof(Contact), Id, "Updated", $"{Name} {Phone}");
        await db.SaveChangesAsync();
    }

    [Remote, Delete]
    internal async Task Delete([Service] IContactDb db, [Service] IAuditTrail audit)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        db.RemoveContact(row);
        audit.Record(nameof(Contact), Id, "Deleted", Name);
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Name = Name;
        row.Phone = Phone;
    }
}
```

---

## zCRM.Server

### `Data/ZCrmDbContext.cs`
The EF context implements `IContactDb`. It is scoped, so `AuditTrail` and `Contact` share the same instance within one request.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts;

namespace zCRM.Server.Data;

public class AuditEntryRow
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options), IContactDb
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<AuditEntryRow> AuditEntries => Set<AuditEntryRow>();

    public Task<ContactRow?> FindContactAsync(Guid id) =>
        Contacts.SingleOrDefaultAsync(c => c.Id == id);

    public void AddContact(ContactRow row) => Contacts.Add(row);

    public void RemoveContact(ContactRow row) => Contacts.Remove(row);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<AuditEntryRow>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.Action).HasMaxLength(50);
            e.Property(a => a.UserName).HasMaxLength(200);
        });
    }
}
```

### `Auditing/AuditTrail.cs`
This is server-only because it needs EF and `HttpContext`. It only adds the entry; the calling operation commits it.

```csharp
using zCRM.Domain.Auditing;
using zCRM.Server.Data;

namespace zCRM.Server.Auditing;

public sealed class AuditTrail(ZCrmDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditTrail
{
    public void Record(string entityType, Guid entityId, string action, string summary)
    {
        db.AuditEntries.Add(new AuditEntryRow
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            UserName = httpContextAccessor.HttpContext?.User.Identity?.Name ?? "anonymous",
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
```

### `Program.cs` (server registrations)
`IPhoneFormatter` is registered here too. The server builds `Contact` whenever it runs Fetch, or deserializes a contact sent to `Save`. Without the registration, that construction fails, even though the server never displays a phone number.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Auditing;
using zCRM.Domain.Contacts;
using zCRM.Domain.Phones;
using zCRM.Server.Auditing;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// RemoteFactory in server mode: the factories and the one endpoint
builder.Services.AddNeatooAspNetCore(typeof(IContact).Assembly);

// Both tiers: Contact's constructor takes it
builder.Services.AddSingleton<IPhoneFormatter, PhoneFormatter>();

// Server only: [Service] parameters on Contact's remote operations
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IContactDb>(sp => sp.GetRequiredService<ZCrmDbContext>());
builder.Services.AddScoped<IAuditTrail, AuditTrail>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## zCRM.Client

### `Program.cs` (client registrations)
The client registers `IPhoneFormatter` because the client creates and holds the contact being displayed. It does **not** register `IAuditTrail` or `IContactDb`: no code that runs on the client resolves them.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Domain.Contacts;
using zCRM.Domain.Phones;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Factory proxies: [Remote] operations go to the server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Both tiers: Contact's constructor takes it
builder.Services.AddSingleton<IPhoneFormatter, PhoneFormatter>();

await builder.Build().RunAsync();
```

### `Pages/ContactEdit.razor`
The page only uses the generated `IContactFactory`. It never sees either service; the formatted number comes from `contact.FormattedPhone`. It keeps the instance that `Save` returns.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@using zCRM.Domain.Contacts
@inject IContactFactory ContactFactory

@if (loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (contact is null)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else
{
    <MudPaper Class="pa-4" MaxWidth="480px">
        <MudTextField @bind-Value="contact.Name" Label="Name" />
        <MudTextField @bind-Value="contact.Phone" Label="Phone" Immediate="true"
                      HelperText="@contact.FormattedPhone" />
        <MudButton Class="mt-4" Variant="Variant.Filled" Color="Color.Primary"
                   Disabled="saving" OnClick="SaveAsync">Save</MudButton>
    </MudPaper>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private IContact? contact;
    private bool loading = true;
    private bool saving;

    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        // Create is local; Fetch crosses to the server
        contact = Id is Guid id
            ? await ContactFactory.Fetch(id)
            : ContactFactory.Create();
        loading = false;
    }

    private async Task SaveAsync()
    {
        saving = true;
        try
        {
            // Save returns a new instance from the server: keep it
            contact = await ContactFactory.Save(contact!);
        }
        finally
        {
            saving = false;
        }
    }
}
```

---

## Notes
- **Country choice:** I used the user's browser culture as their country. If the country is a profile setting instead, that becomes another service registered on both tiers that the formatter takes as a parameter. The rest of the design stays the same.
- **Trimming:** to keep the bodies of Fetch, Insert, Update and Delete (and anything referencing `IAuditTrail`) out of the published WASM bundle, publish the client with RemoteFactory's `IsServerRuntime` switch set to `false`.
