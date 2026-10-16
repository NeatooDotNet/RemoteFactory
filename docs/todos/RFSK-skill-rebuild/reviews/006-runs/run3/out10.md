I checked the RemoteFactory reference and have written the code below. Nothing here has been compiled. The `using` namespaces for the RemoteFactory registration calls (`AddNeatooAspNetCore`, `NeatooFactory`, `RemoteFactoryServices`) are from memory, so check them against the package.

## What RemoteFactory is for

zCRM has one domain assembly, `zCRM.Domain`, and both the browser and the server load it. You put `[Factory]` on a class and mark its operations: `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]` and `[Execute]`. A Roslyn source generator then writes a factory interface (`IContactFactory`) and the serialization for the class. Blazor pages inject that factory and call it. If an operation is marked `[Remote]`, the call goes over the wire to one server endpoint. That endpoint runs the same method there, with server services such as the `DbContext` passed in as `[Service]` parameters. Without `[Remote]`, the method runs in the browser. So you don't write controllers or DTOs. The thing to decide for each operation is whether it needs the server.

## The two creates, side by side

| | Contact | Note |
|---|---|---|
| Needs the server to build it? | Yes, it reads `Settings` | No |
| Create attribute | `[Remote, Create] internal`: one round trip | `[Create] public`: runs in the browser, no round trip |
| Factory call | `await ContactFactory.Create()` | `NoteFactory.Create(contactId)` |

Both still save through `[Remote, Insert]`, because writing a row always needs the server.

---

## 1. Domain project (`zCRM.Domain`, referenced by both Client and Server)

**Why here:** client and server must run the same compiled classes. The row classes and the `IZCrmDbContext` interface live here too, because the `[Remote]` method bodies map to them. Those bodies only ever run on the server.

```csharp
// zCRM.Domain/Persistence/Rows.cs
namespace zCRM.Domain.Persistence;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public List<NoteRow> Notes { get; set; } = [];
}

public class NoteRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class SettingRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public static class SettingKeys
{
    public const string DefaultContactRegion = "DefaultContactRegion";
}
```

```csharp
// zCRM.Domain/Persistence/IZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace zCRM.Domain.Persistence;

/// <summary>
/// The store the [Remote] operations write. Implemented and registered on the server only.
/// </summary>
public interface IZCrmDbContext
{
    DbSet<ContactRow> Contacts { get; }
    DbSet<NoteRow> Notes { get; }
    DbSet<SettingRow> Settings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Contact:** the create reads the database, so it has to cross to the server.

```csharp
// zCRM.Domain/Contacts/Contact.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Contacts;

public interface IContact
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string Email { get; set; }
    string Region { get; set; }
}

/// <summary>
/// Contact aggregate root.
/// </summary>
[Factory]
internal partial class Contact : IContact, IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Contact with the region from the Settings table.
    /// </summary>
    /// <remarks>
    /// [Remote]: the default region is server data, so the client's call crosses.
    /// It is still a Create: nothing is stored until the first Save inserts.
    /// </remarks>
    [Remote, Create]
    internal async Task Create([Service] IZCrmDbContext db)
    {
        var defaultRegion = await db.Settings
            .Where(s => s.Key == SettingKeys.DefaultContactRegion)
            .Select(s => s.Value)
            .SingleOrDefaultAsync();

        Id = Guid.NewGuid();
        Region = defaultRegion ?? string.Empty;
        IsNew = true;
    }

    /// <summary>
    /// Inserts the Contact's row.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert([Service] IZCrmDbContext db)
    {
        db.Contacts.Add(new ContactRow
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Region = Region
        });
        await db.SaveChangesAsync();
        IsNew = false;
    }
}
```

**Note:** creating it needs only the contact id, which the client already has. So the create runs locally with no round trip. A note is its own aggregate root with a `ContactId`, not a child of `Contact`. That way, adding a note doesn't load and re-save the whole contact.

```csharp
// zCRM.Domain/Notes/Note.cs
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Notes;

public interface INote
{
    Guid Id { get; }
    Guid ContactId { get; }
    string Text { get; set; }
    DateTimeOffset CreatedAt { get; }
}

/// <summary>
/// Note aggregate root; references its Contact by id.
/// </summary>
[Factory]
internal partial class Note : INote, IFactorySaveMeta
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Note for an existing Contact. Public and local: no server call.
    /// </summary>
    [Create]
    public void Create(Guid contactId)
    {
        Id = Guid.NewGuid();
        ContactId = contactId;
        IsNew = true;
    }

    /// <summary>
    /// Inserts the Note's row, stamped with the server's clock.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert([Service] IZCrmDbContext db)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        db.Notes.Add(new NoteRow
        {
            Id = Id,
            ContactId = ContactId,
            Text = Text,
            CreatedAt = CreatedAt
        });
        await db.SaveChangesAsync();
        IsNew = false;
    }
}
```

The classes are `internal` and the interfaces `public`. Pages see `IContact` and `INote` and can't `new` the classes. `[Remote] internal` methods are promoted to `public` on the generated factory. `[Fetch]`, `[Update]` and `[Delete]` are left out because the question only asks for the creates. `Save` routes a new object to `[Insert]`.

## 2. Server project (`zCRM.Server`)

**Why here:** the EF Core provider, the connection string and the `IZCrmDbContext` registration exist only on the server. The client never registers the DbContext, which is why only `[Remote]` methods take it as a parameter.

```csharp
// zCRM.Server/Data/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Persistence;

namespace zCRM.Server.Data;

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options)
    : DbContext(options), IZCrmDbContext
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<NoteRow> Notes => Set<NoteRow>();
    public DbSet<SettingRow> Settings => Set<SettingRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>().HasKey(c => c.Id);
        modelBuilder.Entity<ContactRow>()
            .HasMany(c => c.Notes)
            .WithOne()
            .HasForeignKey(n => n.ContactId);
        modelBuilder.Entity<NoteRow>().HasKey(n => n.Id);
        modelBuilder.Entity<SettingRow>().HasKey(s => s.Key);
    }
}
```

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Contacts;
using zCRM.Domain.Persistence;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// RemoteFactory in server mode for every [Factory] type in the domain assembly
builder.Services.AddNeatooAspNetCore(typeof(IContact).Assembly);

// What the [Service] parameters of the [Remote] operations resolve
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IZCrmDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());

builder.Services.RegisterMatchingName(typeof(IContact).Assembly);

var app = builder.Build();

// Host the Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// The one endpoint every factory call goes through
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## 3. Client project (`zCRM.Client`, Blazor WASM + MudBlazor)

**Why here:** the client registers RemoteFactory in Remote mode plus the `HttpClient` it uses to reach the server. It registers nothing server-only.

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Factory proxies: [Remote] calls go to the server, everything else runs here
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IContact).Assembly);

// The HttpClient RemoteFactory sends [Remote] calls with: the server hosting this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

await builder.Build().RunAsync();
```

```razor
@* zCRM.Client/Layout/MainLayout.razor — MudBlazor's providers, needed for Snackbar *@
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent Class="pa-4">
        @Body
    </MudMainContent>
</MudLayout>
```

**New contact page:** one round trip to create the contact, a second to save it.

```razor
@* zCRM.Client/Pages/NewContact.razor *@
@page "/contacts/new"
@using zCRM.Domain.Contacts
@inject IContactFactory ContactFactory
@inject NavigationManager Navigation
@inject ISnackbar Snackbar

<MudText Typo="Typo.h5" Class="mb-4">New contact</MudText>

@if (contact is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-4">
        <MudTextField @bind-Value="contact.FirstName" Label="First name" />
        <MudTextField @bind-Value="contact.LastName" Label="Last name" />
        <MudTextField @bind-Value="contact.Email" Label="Email" InputType="InputType.Email" />
        <MudTextField @bind-Value="contact.Region" Label="Region"
                      HelperText="Defaulted from the server's settings" />
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
                   Disabled="saving" OnClick="SaveAsync">Save</MudButton>
    </MudPaper>
}

@code {
    private IContact? contact;
    private bool saving;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // [Remote] Create: one round trip, the server reads the default region
            contact = await ContactFactory.Create();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Could not start a new contact: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveAsync()
    {
        saving = true;
        try
        {
            // Save returns a new instance from the server: reassign it
            contact = await ContactFactory.Save(contact!);
            Snackbar.Add("Contact saved.", Severity.Success);
            Navigation.NavigateTo($"/contacts/{contact!.Id}/notes/new");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            saving = false;
        }
    }
}
```

**Add note page:** creating the note is local. Only the save goes to the server.

```razor
@* zCRM.Client/Pages/AddNote.razor *@
@page "/contacts/{ContactId:guid}/notes/new"
@using zCRM.Domain.Notes
@inject INoteFactory NoteFactory
@inject ISnackbar Snackbar

<MudText Typo="Typo.h5" Class="mb-4">New note</MudText>

<MudPaper Class="pa-4">
    <MudTextField @bind-Value="note.Text" Label="Note" Lines="5" Variant="Variant.Outlined" />
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
               Disabled="saving" OnClick="SaveAsync">Save note</MudButton>
</MudPaper>

@code {
    [Parameter] public Guid ContactId { get; set; }

    private INote note = default!;
    private bool saving;

    protected override void OnParametersSet()
    {
        // Local Create: runs in the browser, no server call
        note = NoteFactory.Create(ContactId);
    }

    private async Task SaveAsync()
    {
        saving = true;
        try
        {
            // [Remote] Insert: the note crosses once, here
            note = (await NoteFactory.Save(note))!;
            Snackbar.Add("Note saved.", Severity.Success);
            note = NoteFactory.Create(ContactId); // ready for the next one
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            saving = false;
        }
    }
}
```

---

**Points that are easy to get wrong:**

- **`[Remote, Create]` is still a create.** Reading `Settings` for defaults doesn't make it a fetch: the contact isn't in the database until its first `Save` inserts it. Never use a `[Create]` that looks up an existing row. For "load it if it exists, otherwise create it," use an `[Execute]` that calls Fetch and then Create.
- **Don't add `[Remote]` to `Note.Create`** "to be safe." It would add a round trip for nothing.
- **Always reassign what `Save` returns.** It comes back from the server as a new instance, and the object you passed in still reports `IsNew == true`.
- **Missing setting:** if `DefaultContactRegion` isn't in `Settings`, the region falls back to an empty string. Throwing instead would be equally valid; it depends on whether a missing setting should block creating contacts.
- **Project files:** the domain project needs `Microsoft.EntityFrameworkCore` (only the base package, not a provider) for `DbSet`. Reference it with `PrivateAssets="all"` and set `<IsTrimmable>true</IsTrimmable>`. In the client project, add `<RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />`. With these, the published WASM bundle drops the bodies of the server-only methods and the EF dependency.
