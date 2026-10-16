Here's the whole design. It uses three projects: **zCRM.Domain**, which both tiers reference; **zCRM.Server**, an ASP.NET Core app that hosts the client; and **zCRM.Client**, the Blazor WebAssembly app. I followed RemoteFactory's documented patterns but haven't compiled any of this, because you asked for code in the reply rather than files.

## How each service reaches its code

| Service | Registered on | How it reaches `Contact` | Why |
|---|---|---|---|
| `IPhoneFormatter` | client and server | Constructor injection | The formatted number is shown wherever the contact is, which is mostly the browser. When a `Contact` arrives from the server, RemoteFactory builds it through dependency injection (DI) on the receiving side. So the constructor gets that tier's formatter, which is why both tiers must register it. |
| `IAuditTrail` | server only | A `[Service]` parameter on `[Remote, Insert/Update/Delete]` | It writes to the database, so it has to stay off the client. A `[Service]` parameter is resolved on the tier where the method runs, and `[Remote]` methods run on the server. These method bodies are also trimmed out of the published client. |
| `IContactDbContext` | server only | A `[Service]` parameter, same as above | It's the persistence the save operations need. The client never calls it directly. |

## zCRM.Domain (loaded on both tiers)

**`IPhoneFormatter.cs` and `PhoneFormatter.cs`**: both the interface and the implementation live in the shared assembly because both tiers register the implementation. It does no I/O, so it's safe in the browser bundle.

```csharp
namespace zCRM.Domain;

/// <summary>
/// Formats a stored phone number for the current user's country. Pure computation;
/// registered on both tiers.
/// </summary>
public interface IPhoneFormatter
{
    string Format(string phoneNumber);
}
```

```csharp
using System.Globalization;

namespace zCRM.Domain;

/// <summary>
/// Reads the user's country from CultureInfo.CurrentCulture: the browser's language
/// on the client, the request culture on the server.
/// </summary>
public sealed class PhoneFormatter : IPhoneFormatter
{
    public string Format(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        return UserCountry() switch
        {
            "US" or "CA" when digits.Length == 10
                => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
            "US" or "CA" when digits.Length == 11 && digits[0] == '1'
                => $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}",
            "GB" when digits.Length == 11 && digits[0] == '0'
                => $"{digits[..5]} {digits[5..]}",
            _ => phoneNumber.Trim()
        };
    }

    private static string UserCountry()
    {
        var culture = CultureInfo.CurrentCulture;
        if (culture.Equals(CultureInfo.InvariantCulture) || culture.IsNeutralCulture)
            return string.Empty;

        return new RegionInfo(culture.Name).TwoLetterISORegionName;
    }
}
```

**`IAuditTrail.cs`**: only the interface is in the domain assembly. `Contact`'s `[Service]` parameter needs the type, but the implementation (EF Core plus the database) is server-only.

```csharp
namespace zCRM.Domain;

/// <summary>
/// Records who changed what. Server only. Entries join the operation's unit of work
/// and are written by the operation's SaveChangesAsync.
/// </summary>
public interface IAuditTrail
{
    void Record(string entityType, Guid entityId, string action);
}
```

**`IContactDbContext.cs` and `ContactRow.cs`**: the persistence contract the save operations take. It's an interface so the domain assembly, and the client with it, never references EF Core.

```csharp
namespace zCRM.Domain;

public interface IContactDbContext
{
    Task<ContactRow?> FindContactAsync(Guid id);
    void AddContact(ContactRow row);
    void RemoveContact(ContactRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class ContactRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
```

**`Contact.cs`**: a plain `[Factory]` aggregate root, with no Neatoo base class. The class is `internal` and the interface is public, so the generated `IContactFactory` hands out `IContact`.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface IContact : IFactorySaveMeta
{
    Guid Id { get; }
    string Name { get; set; }
    string Phone { get; set; }
    string PhoneDisplay { get; }
    void MarkDeleted();
}

/// <summary>
/// Contact aggregate root.
/// </summary>
[Factory]
internal partial class Contact : IContact
{
    private readonly IPhoneFormatter _phoneFormatter;

    /// <remarks>
    /// IPhoneFormatter is registered on both tiers, so it can be a constructor
    /// dependency: a Contact arriving over the wire is built through DI with the
    /// receiving tier's formatter.
    /// </remarks>
    public Contact(IPhoneFormatter phoneFormatter)
    {
        _phoneFormatter = phoneFormatter;
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Computed on whichever tier holds the contact; not serialized.
    /// </summary>
    public string PhoneDisplay => _phoneFormatter.Format(Phone);

    public void MarkDeleted() => IsDeleted = true;

    /// <summary>
    /// A new contact. Local: the client creates it with no server call.
    /// </summary>
    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactDbContext db)
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
    internal async Task Insert([Service] IContactDbContext db, [Service] IAuditTrail audit)
    {
        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.AddContact(row);
        audit.Record(nameof(Contact), Id, "Inserted");
        await db.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    internal async Task Update([Service] IContactDbContext db, [Service] IAuditTrail audit)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        audit.Record(nameof(Contact), Id, "Updated");
        await db.SaveChangesAsync();
    }

    [Remote, Delete]
    internal async Task Delete([Service] IContactDbContext db, [Service] IAuditTrail audit)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        db.RemoveContact(row);
        audit.Record(nameof(Contact), Id, "Deleted");
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Name = Name;
        row.Phone = Phone;
    }
}
```

## zCRM.Server

**`CrmDbContext.cs`, `AuditEntryRow.cs` and `AuditTrail.cs`**: all server-only. `AuditTrail` adds its entry to the same scoped `CrmDbContext` that `Contact`'s operation flushes. The contact row and its audit entry are therefore written in one `SaveChangesAsync` call: one transaction, so neither can be saved without the other.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain;

namespace zCRM.Server;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options)
    : DbContext(options), IContactDbContext
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<AuditEntryRow> AuditEntries => Set<AuditEntryRow>();

    public Task<ContactRow?> FindContactAsync(Guid id) => Contacts.FindAsync(id).AsTask();
    public void AddContact(ContactRow row) => Contacts.Add(row);
    public void RemoveContact(ContactRow row) => Contacts.Remove(row);
}

public class AuditEntryRow
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
```

```csharp
using zCRM.Domain;

namespace zCRM.Server;

internal sealed class AuditTrail(
    CrmDbContext db,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider clock) : IAuditTrail
{
    public void Record(string entityType, Guid entityId, string action)
    {
        db.AuditEntries.Add(new AuditEntryRow
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserName = httpContextAccessor.HttpContext?.User.Identity?.Name,
            OccurredAt = clock.GetUtcNow()
        });
    }
}
```

**`Program.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

// RemoteFactory in Server mode for every [Factory] type in the domain assembly
builder.Services.AddNeatooAspNetCore(typeof(IContact).Assembly);

// Both tiers: Contact's constructor dependency
builder.Services.AddSingleton<IPhoneFormatter, PhoneFormatter>();

// Server only: what Contact's [Service] parameters resolve
builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddScoped<IContactDbContext>(sp => sp.GetRequiredService<CrmDbContext>());
builder.Services.AddScoped<IAuditTrail, AuditTrail>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// The one endpoint every factory call goes through
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

`IContactDbContext` is registered by forwarding to the scoped `CrmDbContext`, not as its own `AddScoped<IContactDbContext, CrmDbContext>()`. That forwarding is what gives `Contact` and `AuditTrail` the same instance, and so one flush.

## zCRM.Client

**`Program.cs`**: the client registers the formatter and nothing server-only. `IAuditTrail` and `IContactDbContext` don't appear here, which is correct: their only callers are `[Remote]` methods, which never run in the browser.

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

// Factory proxies: [Remote] operations go to the server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Both tiers: a Contact deserialized here is built with this formatter
builder.Services.AddSingleton<IPhoneFormatter, PhoneFormatter>();

await builder.Build().RunAsync();
```

**`Pages/ContactEdit.razor`**: shows the formatted number and saves. `Save` returns a new instance from the server, so the page reassigns `contact`.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@using zCRM.Domain
@inject IContactFactory ContactFactory
@inject ISnackbar Snackbar

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
    <MudCard>
        <MudCardContent>
            <MudTextField @bind-Value="contact.Name" Label="Name" />
            <MudTextField @bind-Value="contact.Phone" Label="Phone" Immediate="true" />
            <MudText Typo="Typo.body2">@contact.PhoneDisplay</MudText>
        </MudCardContent>
        <MudCardActions>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveAsync">Save</MudButton>
        </MudCardActions>
    </MudCard>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private IContact? contact;
    private bool loading = true;

    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        contact = Id is Guid id
            ? await ContactFactory.Fetch(id)
            : ContactFactory.Create();
        loading = false;
    }

    private async Task SaveAsync()
    {
        contact = await ContactFactory.Save(contact!);
        Snackbar.Add("Contact saved", Severity.Success);
    }
}
```

## Choices and caveats

- **What counts as a save:** an insert, an update and a delete all go through `Save`, so all three write an audit entry. If deletes shouldn't count, drop the `Record` call in `Delete`.
- **How the user's country is found:** it comes from `CultureInfo.CurrentCulture`. In the browser that's the browser's language. If the server ever reads `PhoneDisplay`, it needs `UseRequestLocalization` to get the user's culture. If the country should instead come from a user profile, pass it into `Format` from a service registered on both tiers.
- **Phone rules:** `PhoneFormatter` only handles US, Canadian and UK numbers; anything else is shown as stored. For real international formatting, swap in `libphonenumber-csharp` behind the same interface.
- **One phone per contact:** I modelled a single number. Several numbers per contact would need a child list of phone numbers whose own insert and update operations write their rows.
- **Not shown:** MudBlazor's providers in `MainLayout` and its stylesheet and script in `index.html`.
