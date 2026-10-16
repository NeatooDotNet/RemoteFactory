**What RemoteFactory is for.** zCRM runs the same domain classes in the browser and on the server. RemoteFactory is a Roslyn source generator that lets those classes cross between the two without DTOs or controllers. You put `[Factory]` on a class and mark its operations: `[Create]` for a new object, `[Fetch]` to load one, and `[Insert]`/`[Update]`/`[Delete]` for writes, which you never call directly because `Save` picks one. The generator writes a factory interface (`IContactFactory`) that you inject anywhere. An operation marked `[Remote]` runs on the server: the client's call is serialized to one endpoint, the method runs there with server services such as the DbContext passed in as `[Service]` parameters, and the object comes back. An operation without `[Remote]` runs on the client with no network call. The attribute says what the operation means. `[Create]` means "this object isn't stored yet, and its first `Save` inserts it", even if building it reads the database.

I used the RemoteFactory skill's reference docs for the API. I haven't compiled any of this. Two namespaces are guesses, and package versions are left out because I didn't look them up on nuget.org (details at the end).

---

## Solution layout

| Project | Contains | Why there |
|---|---|---|
| `ZCrm.Persistence` | EF Core `ZCrmDbContext` and row classes | Server-only. Kept separate so EF Core doesn't ship to the browser. |
| `ZCrm.Domain` | `Contact` and `Note` with their factory operations | Loaded by **both** client and server, which is why no DTOs are needed. |
| `ZCrm.Server` | ASP.NET Core host and the one RemoteFactory endpoint | Registers the DbContext that `[Service]` parameters receive. |
| `ZCrm.Client` | Blazor WASM, MudBlazor pages | Injects the generated factories. |

### Project files

**`ZCrm.Persistence/ZCrm.Persistence.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
  </ItemGroup>
</Project>
```

**`ZCrm.Domain/ZCrm.Domain.csproj`**: this is where server code is kept out of the browser.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <!-- Generated files rely on implicit usings -->
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Lets the client publish trim the [Remote] bodies out of this assembly -->
    <IsTrimmable>true</IsTrimmable>
  </PropertyGroup>
  <ItemGroup>
    <!-- Direct reference required: the source generator only runs where it is referenced directly -->
    <PackageReference Include="Neatoo.RemoteFactory" />
    <!-- Server-only dependencies: PrivateAssets keeps them from flowing to the client -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
    <ProjectReference Include="..\ZCrm.Persistence\ZCrm.Persistence.csproj" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

**`ZCrm.Server/ZCrm.Server.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" />
    <PackageReference Include="Neatoo.RemoteFactory.AspNetCore" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ZCrm.Domain\ZCrm.Domain.csproj" />
    <ProjectReference Include="..\ZCrm.Persistence\ZCrm.Persistence.csproj" />
    <ProjectReference Include="..\ZCrm.Client\ZCrm.Client.csproj" />
  </ItemGroup>
</Project>
```

**`ZCrm.Client/ZCrm.Client.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
    <PackageReference Include="MudBlazor" />
    <PackageReference Include="Neatoo.RemoteFactory" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ZCrm.Domain\ZCrm.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Tells the trimmer this is not the server, so server-only factory paths are dead code -->
    <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                    Value="false"
                                    Trim="true" />
    <TrimmableAssembly Include="ZCrm.Persistence" />
  </ItemGroup>
</Project>
```

---

## Persistence (server-only)

**`ZCrm.Persistence/Rows.cs`**: plain EF row classes, one per table.
```csharp
namespace ZCrm.Persistence;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
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
```

**`ZCrm.Persistence/ZCrmDbContext.cs`**
```csharp
using Microsoft.EntityFrameworkCore;

namespace ZCrm.Persistence;

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<NoteRow> Notes => Set<NoteRow>();
    public DbSet<SettingRow> Settings => Set<SettingRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(c => c.Id);
            // Ids are assigned by the domain object's Create, not by the database
            e.Property(c => c.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<NoteRow>(e =>
        {
            e.ToTable("Notes");
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).ValueGeneratedNever();
            // A note for a contact that doesn't exist fails here, at the database
            e.HasOne<ContactRow>().WithMany().HasForeignKey(n => n.ContactId);
        });

        modelBuilder.Entity<SettingRow>(e =>
        {
            e.ToTable("Settings");
            e.HasKey(s => s.Key);
        });
    }
}
```

---

## Domain (loaded on both tiers)

**`ZCrm.Domain/Contacts/Contact.cs`**: the Create needs the server, so it is `[Remote]`.
```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Persistence;

namespace ZCrm.Domain.Contacts;

public interface IContact : IFactorySaveMeta
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
internal partial class Contact : IContact
{
    private const string DefaultRegionSettingKey = "Contacts.DefaultRegion";

    // Public setters: the serializer only carries what it can set on the other side
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    // IFactorySaveMeta: Save routes on these, on the server
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Contact whose region defaults from the Settings table.
    /// </summary>
    /// <remarks>
    /// [Remote] because the default lives in the server's database: the client
    /// crosses once and gets the new contact back. It is still a Create; nothing
    /// is stored until Save routes to Insert.
    /// </remarks>
    [Remote, Create]
    internal async Task Create([Service] ZCrmDbContext db)
    {
        Id = Guid.NewGuid();
        Region = await db.Settings
                .Where(s => s.Key == DefaultRegionSettingKey)
                .Select(s => s.Value)
                .SingleOrDefaultAsync()
            ?? throw new InvalidOperationException(
                $"Setting '{DefaultRegionSettingKey}' is missing.");
        IsNew = true;
    }

    /// <summary>
    /// Inserts a new Contact. Reached only through IContactFactory.Save.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
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
Why it's built this way: the class is `internal` and the factory returns the public `IContact`, so pages bind to the interface. The operations are `[Remote] internal`. Writing `[Remote] public` is a compile error (NF0105). With `internal`, the generator makes the factory method public and trims its body out of the published client. The DbContext is a method parameter, not a constructor parameter, because only the server has it.

**`ZCrm.Domain/Notes/Note.cs`**: the Create is local, with no round trip.
```csharp
using Neatoo.RemoteFactory;
using ZCrm.Persistence;

namespace ZCrm.Domain.Notes;

public interface INote : IFactorySaveMeta
{
    Guid Id { get; }
    Guid ContactId { get; }
    string Text { get; set; }
    DateTimeOffset CreatedAt { get; }
}

/// <summary>
/// Note aggregate root. References its Contact by id.
/// </summary>
[Factory]
internal partial class Note : INote
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Note for a contact. Public and local: runs on the client.
    /// </summary>
    [Create]
    public void Create(Guid contactId)
    {
        Id = Guid.NewGuid();
        ContactId = contactId;
        IsNew = true;
    }

    /// <summary>
    /// Inserts a new Note. The timestamp is the server's clock.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
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
Why it's built this way: `Note` is its own aggregate root that points to its contact by id, not a child of `Contact`. A new note doesn't change any rule about the contact. If notes were children, adding one would mean fetching the whole contact from the server first, and you said a note needs nothing from the server. Its `[Create]` is `public` without `[Remote]`, so it runs in the browser with no network call. Only `Save` crosses.

---

## Server

**`ZCrm.Server/Program.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Domain.Contacts;
using ZCrm.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The server-only service that [Service] ZCrmDbContext parameters receive
builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZCrm")));

// Server mode: runs [Remote] operations for every factory in the domain assembly
builder.Services.AddNeatooAspNetCore(typeof(IContact).Assembly);

var app = builder.Build();

// Serve the hosted Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The one endpoint every factory call goes through; no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
I left out authentication. When you add it, `UseAuthentication()` and `UseAuthorization()` go before `UseNeatoo()`.

---

## Client

**`ZCrm.Client/Program.cs`**
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using ZCrm.Client;
using ZCrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Remote mode: [Remote] operations go to the server, everything else runs here
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IContact).Assembly);

// The HttpClient RemoteFactory sends with: the server hosting this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```
The client registers no DbContext. Nothing that runs in the browser needs one.

**`ZCrm.Client/Layout/MainLayout.razor`**: MudBlazor's providers. `App.razor` and `index.html` are the unchanged template files plus MudBlazor's CSS and JS links.
```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Medium" Class="mt-6">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

**`ZCrm.Client/Pages/NewContact.razor`**: calls the remote Create.
```razor
@page "/contacts/new"
@using ZCrm.Domain.Contacts
@inject IContactFactory ContactFactory
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<MudText Typo="Typo.h4" Class="mb-4">New contact</MudText>

@if (contact is null)
{
    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-4">
        <MudTextField @bind-Value="contact.FirstName" Label="First name" />
        <MudTextField @bind-Value="contact.LastName" Label="Last name" />
        <MudTextField @bind-Value="contact.Email" Label="Email" InputType="InputType.Email" />
        <MudTextField @bind-Value="contact.Region" Label="Region" HelperText="Default from Settings" />
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
                   Disabled="saving" OnClick="SaveAsync">Save</MudButton>
    </MudPaper>
}

@code {
    private IContact? contact;
    private bool saving;

    protected override async Task OnInitializedAsync()
    {
        // One round trip: the server builds the contact and reads the default region
        contact = await ContactFactory.Create();
    }

    private async Task SaveAsync()
    {
        saving = true;
        try
        {
            // IsNew, so Save routes to Insert. Save returns the server's instance: keep it.
            contact = await ContactFactory.Save(contact!);
            Snackbar.Add("Contact saved.", Severity.Success);
            Navigation.NavigateTo($"/contacts/{contact!.Id}/notes/new");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Saving failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            saving = false;
        }
    }
}
```

**`ZCrm.Client/Pages/NewNote.razor`**: calls the local Create.
```razor
@page "/contacts/{ContactId:guid}/notes/new"
@using ZCrm.Domain.Notes
@inject INoteFactory NoteFactory
@inject ISnackbar Snackbar

<MudText Typo="Typo.h4" Class="mb-4">New note</MudText>

<MudPaper Class="pa-4">
    <MudTextField @bind-Value="note.Text" Label="Note" Lines="5" Immediate="true" />
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
               Disabled="@(saving || string.IsNullOrWhiteSpace(note.Text))"
               OnClick="SaveAsync">Save</MudButton>
</MudPaper>

@code {
    [Parameter] public Guid ContactId { get; set; }

    private INote note = null!;
    private bool saving;

    protected override void OnParametersSet()
    {
        // Local: no server call. A new note needs only the contact's id.
        if (note is null || note.ContactId != ContactId)
            note = NoteFactory.Create(ContactId);
    }

    private async Task SaveAsync()
    {
        saving = true;
        try
        {
            // The only round trip for a note: Save routes to Insert on the server
            var saved = await NoteFactory.Save(note);
            Snackbar.Add($"Note saved at {saved!.CreatedAt:g}.", Severity.Success);

            // Start the next note, again with no server call
            note = NoteFactory.Create(ContactId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Saving failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            saving = false;
        }
    }
}
```

---

## Things to check

- **Nothing here has been compiled.** I'm not sure of two namespaces: `Neatoo.RemoteFactory.AspNetCore` for `AddNeatooAspNetCore`, and `Neatoo.RemoteFactory` for `RemoteFactoryServices`.
- **Package versions are missing.** Put them in `Directory.Packages.props` with `ManagePackageVersionsCentrally` turned on, taking the versions from nuget.org.
- **Only Create and Insert exist.** Saving an already-saved contact or note would route to `[Update]`, which isn't written yet, so the contact page navigates away after its first save. Add `[Remote, Fetch]`, `[Remote, Update]` and `[Remote, Delete]` when you build the edit screens.
- **Missing setting:** if the `Contacts.DefaultRegion` row doesn't exist, `Contact.Create` throws instead of silently giving a blank region. That's my choice; change it if a blank region is acceptable.
- **Server code in the browser:** it is removed only by `dotnet publish`, not by `dotnet build`. To confirm, search the published `ZCrm.Domain.wasm` for `ZCrmDbContext`.
