I checked the RemoteFactory skill and its compiled Design samples so the attributes, visibility and setup calls below match the library's actual API. Here is the answer.

## What RemoteFactory is for

RemoteFactory is a Roslyn source generator that lets one domain assembly run in both the Blazor WebAssembly client and the ASP.NET Core server, so we write no DTOs and no controllers. You put `[Factory]` on a class and attributes such as `[Create]` or `[Fetch]` on its methods. At compile time it generates a factory (`IContactFactory`) and the serialization. `[Remote]` marks the operations whose call goes from the browser to the server, through one endpoint (`UseNeatoo()`). Operations without `[Remote]` run where they are called, with no round trip. A `[Service]` parameter is resolved from DI on the tier where the method runs. That is how server-only things like the database reach domain code without ever being sent to the browser.

## Projects

- **zCRM.Domain** (class library, `Neatoo.RemoteFactory`, `ImplicitUsings` on): `Contact`, `Note`, `IContactDefaults`. Both the client and the server reference it.
- **zCRM.Server** (ASP.NET Core, `Neatoo.RemoteFactory.AspNetCore`, EF Core): hosts the client and the endpoint, and holds `CrmDbContext` plus anything that touches it.
- **zCRM.Client** (Blazor WASM, `Neatoo.RemoteFactory`, MudBlazor): the pages.

---

## Domain (zCRM.Domain)

**`IContactDefaults.cs`**: only the interface is in the shared assembly. Its EF Core implementation stays on the server, so EF Core and the DbContext never go into the browser bundle.

```csharp
namespace zCRM.Domain;

/// <summary>
/// Server-side source of defaults for new contacts. Implemented in zCRM.Server over the Settings table.
/// </summary>
public interface IContactDefaults
{
    Task<string?> DefaultRegionAsync();
}
```

**`Contact.cs`**: `Create` is `[Remote]` because building a new contact needs the server's Settings table. It is still a `[Create]`: reading defaults doesn't change the fact that this contact isn't stored yet. The method is `internal`, and `[Remote]` promotes it to `public` on the generated `IContactFactory`. Because it is `internal` + `[Remote]`, its body only runs on the server, so `IContactDefaults` only needs to be registered there.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface IContact
{
    Guid Id { get; set; }
    string Name { get; set; }
    string Region { get; set; }
}

/// <summary>
/// Contact aggregate root.
/// </summary>
[Factory]
internal partial class Contact : IContact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new Contact with the region default from Settings.
    /// One round trip: the default lives on the server.
    /// </summary>
    [Remote, Create]
    internal async Task Create([Service] IContactDefaults defaults)
    {
        Id = Guid.NewGuid();
        Region = await defaults.DefaultRegionAsync() ?? string.Empty;
    }
}
```

**`Note.cs`**: `Create` is `public` with no `[Remote]`, so it runs in the browser with no server call. It needs nothing from the server, so it takes no `[Service]`. A note refers to its contact by id and is its own root, so you can add a note without loading the contact first.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface INote
{
    Guid Id { get; set; }
    Guid ContactId { get; set; }
    string Text { get; set; }
}

/// <summary>
/// Note on an existing contact. References the contact by id.
/// </summary>
[Factory]
internal partial class Note : INote
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new Note for a contact. Local: no server call.
    /// </summary>
    [Create]
    public void Create(Guid contactId)
    {
        Id = Guid.NewGuid();
        ContactId = contactId;
    }
}
```

The classes are `internal` behind `public` interfaces, so the generated factories return `IContact` and `INote`. All properties have public setters because only settable properties are serialized.

---

## Server (zCRM.Server)

**`ContactDefaults.cs`**: this is the only code that knows about EF Core and the Settings table, and it is only registered on the server. I've assumed `CrmDbContext.Settings` has rows with `Key` and `Value` columns.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain;

namespace zCRM.Server;

/// <summary>
/// Reads contact defaults from the Settings table.
/// </summary>
internal sealed class ContactDefaults(CrmDbContext db) : IContactDefaults
{
    public Task<string?> DefaultRegionAsync() =>
        db.Settings
            .Where(s => s.Key == "DefaultRegion")
            .Select(s => s.Value)
            .SingleOrDefaultAsync();
}
```

**`Program.cs`**: `AddNeatooAspNetCore` registers the factories in server mode, and `UseNeatoo()` maps the one endpoint that every `[Remote]` call uses.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

// Use your own provider here
builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));

builder.Services.AddNeatooAspNetCore(typeof(IContact).Assembly);

// Server-only: resolved for [Service] parameters on methods that run here
builder.Services.AddScoped<IContactDefaults, ContactDefaults>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## Client (zCRM.Client)

**`Program.cs`**: Remote mode sends `[Remote]` calls over the keyed `HttpClient`. Nothing server-only is registered here: the client never resolves `IContactDefaults`, because `Contact.Create` runs on the server.

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

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IContact).Assembly);

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

**`_Imports.razor`** (additions). Your layout also needs the usual `<MudThemeProvider />`, `<MudPopoverProvider />`, `<MudDialogProvider />` and `<MudSnackbarProvider />`.

```razor
@using MudBlazor
@using zCRM.Domain
```

**`Pages/NewContact.razor`**: the page injects the generated factory. `Create()` is awaited because it crosses to the server and back in one call.

```razor
@page "/contacts/new"
@inject IContactFactory ContactFactory

<MudText Typo="Typo.h5">New contact</MudText>

@if (contact is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudTextField @bind-Value="contact.Name" Label="Name" />
    <MudTextField @bind-Value="contact.Region" Label="Region" />
}

@code {
    private IContact? contact;

    protected override async Task OnInitializedAsync()
    {
        // [Remote] Create: the server fills Region from Settings
        contact = await ContactFactory.Create();
    }
}
```

**`Pages/NewNote.razor`**: `Create` is synchronous because it runs in the browser with no round trip.

```razor
@page "/contacts/{ContactId:guid}/notes/new"
@inject INoteFactory NoteFactory

<MudText Typo="Typo.h5">New note</MudText>

<MudTextField @bind-Value="note.Text" Label="Note" Lines="5" />

@code {
    [Parameter] public Guid ContactId { get; set; }

    private INote note = default!;

    protected override void OnParametersSet()
    {
        // Local Create: no server call
        if (note is null || note.ContactId != ContactId)
            note = NoteFactory.Create(ContactId);
    }
}
```

---

## Notes

- **Why the two creates differ:** the region default is a server fact, so the contact's `Create` crosses to the server. A note only needs an id the client already has, so its `Create` stays local. Don't use `new Contact()` or `new Note()`. Always go through the factory.
- **Saving isn't shown**, because you asked only for the creates. To add it, implement `IFactorySaveMeta` (`IsNew`, `IsDeleted`) on each class and add a `[Remote, Insert] internal` method that takes `[Service] CrmDbContext`. Then the client calls `entity = await factory.Save(entity)` and must keep the returned instance, which is a new object from the server.
- **Trimming (optional):** because `Contact.Create` is `internal`, its body can be stripped from the published browser bundle by turning on IL trimming with the `IsServerRuntime` switch set to `false`. That isn't configured here.
- **Not compiled:** I wrote this from the library's tested samples but haven't built it. The database provider (`UseSqlServer`), the `DefaultRegion` key name, and the `Key`/`Value` columns on the Settings table are assumptions, so change them to match your schema.
