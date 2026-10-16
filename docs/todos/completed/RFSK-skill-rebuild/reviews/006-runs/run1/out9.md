I didn't create files or compile anything. The code below follows the RemoteFactory patterns, but it hasn't been built. A few names in it are guesses and are marked inline: the column names, the registry's URL and response shape, the API-key header, and the Npgsql provider.

## How it splits across projects

| Project | Contents | Ships to the browser? |
|---|---|---|
| `ZCrm.Domain` | The `[Factory]` types the client calls, plus the records that cross the wire | Yes, loaded on both tiers. It's marked `IsTrimmable` so server-only code bodies are removed when the client is published |
| `ZCrm.Persistence` | `ZCrmDbContext` | No. Domain references it with `PrivateAssets="all"` |
| `ZCrm.Server` | The registry client that holds the secret key, plus the server's service registrations | No |
| `ZCrm.Client` | The MudBlazor components | Yes |

```xml
<!-- ZCrm.Domain.csproj (fragment) -->
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Neatoo.RemoteFactory" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
  <ProjectReference Include="..\ZCrm.Persistence\ZCrm.Persistence.csproj" PrivateAssets="all" />
</ItemGroup>

<!-- ZCrm.Client.csproj (fragment) -->
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />
  <TrimmableAssembly Include="ZCrm.Persistence" />
</ItemGroup>
```

## The assumed DbContext (ZCrm.Persistence)

This only fixes the names the query uses. Your real context replaces it.

```csharp
using Microsoft.EntityFrameworkCore;

namespace ZCrm.Persistence;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyLegalName { get; set; }
    public string? TaxNumber { get; set; }
    public DateTimeOffset ModifiedUtc { get; set; }
}

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(c => c.Id);
            // The sidebar sorts by this column on every load
            e.HasIndex(c => c.ModifiedUtc);
        });
    }
}
```

## 1. Recent contacts: a read model (ZCrm.Domain)

**Why a read model:** the sidebar only displays data from the server. Nothing creates, edits or saves it. So it's a plain `[Factory]` class with a `[Fetch]` and nothing else: no DTO, no controller and no Neatoo base class. It reads the `Contacts` table directly. That's fine because it writes nothing, so it can't bypass the Contact aggregate's rules.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Persistence;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// One row of the Recent contacts sidebar.
/// </summary>
public record RecentContact(Guid Id, string DisplayName, string? CompanyLegalName, DateTimeOffset ModifiedUtc);

public interface IRecentContacts
{
    IReadOnlyList<RecentContact> Items { get; }
}

/// <summary>
/// Read model: the most recently modified contacts.
/// </summary>
[Factory]
internal partial class RecentContacts : IRecentContacts
{
    public const int Count = 10;

    public IReadOnlyList<RecentContact> Items { get; set; } = [];

    /// <summary>
    /// Loads the most recently modified contacts on the server.
    /// </summary>
    /// <remarks>
    /// [Remote] internal: the client's call crosses, and ZCrmDbContext is resolved
    /// on the server. An empty table is still a valid result, so this never returns false.
    /// </remarks>
    [Remote, Fetch]
    internal async Task<bool> Fetch(
        [Service] ZCrmDbContext db,
        CancellationToken cancellationToken)
    {
        Items = await db.Contacts
            .AsNoTracking()
            .OrderByDescending(c => c.ModifiedUtc)
            .Take(Count)
            .Select(c => new RecentContact(
                c.Id,
                c.FirstName + " " + c.LastName,
                c.CompanyLegalName,
                c.ModifiedUtc))
            .ToListAsync(cancellationToken);

        return true;
    }
}
```

`Items` has a public setter on purpose. The serializer only carries properties it can set on the other side.

## 2. Tax-number lookup: an interface factory

### The contract (ZCrm.Domain)

**Why an interface factory:** the client calls this service, gets an answer, and is done. The name says what it does. It is not a repository. The methods have no attributes, because `[Factory]` on the interface already makes every method a server entry point.

```csharp
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Companies;

/// <summary>
/// A company as the external registry records it.
/// </summary>
public record RegisteredCompany(string TaxNumber, string LegalName);

/// <summary>
/// Looks up companies in the external company registry. Runs on the server,
/// which holds the registry's API key.
/// </summary>
[Factory]
public interface ICompanyRegistryLookup
{
    /// <summary>
    /// The company registered under the tax number, or null when the registry has none.
    /// </summary>
    Task<RegisteredCompany?> FindByTaxNumberAsync(string taxNumber);
}
```

### The implementation (ZCrm.Server)

**Why it lives in the server project and not in Domain:** the `HttpClient` code, the options type and the key never reach the WASM bundle, so this doesn't depend on trimming. The skill notes that trimming of interface-factory implementations hasn't been verified. It's a plain class with no `[Factory]`. Putting `[Factory]` on it as well fails the build with CS0101.

```csharp
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using ZCrm.Domain.Companies;

namespace ZCrm.Server.Companies;

public sealed class CompanyRegistryOptions
{
    public const string Section = "CompanyRegistry";

    [Required]
    public Uri BaseAddress { get; set; } = null!;

    /// <summary>From user-secrets locally and Key Vault / app settings in Azure. Never in appsettings.json.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Server-side implementation of ICompanyRegistryLookup. The typed HttpClient
/// arrives with the base address and API key already applied (see Program.cs).
/// </summary>
public sealed class CompanyRegistryLookup(HttpClient http) : ICompanyRegistryLookup
{
    public async Task<RegisteredCompany?> FindByTaxNumberAsync(string taxNumber)
    {
        if (string.IsNullOrWhiteSpace(taxNumber))
            throw new ArgumentException("A tax number is required.", nameof(taxNumber));

        var normalized = taxNumber.Trim();

        // Assumed registry shape: GET companies/{taxNumber} -> { "legalName": "..." }, 404 when unknown
        using var response = await http.GetAsync($"companies/{Uri.EscapeDataString(normalized)}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RegistryCompanyResponse>();
        return string.IsNullOrWhiteSpace(body?.LegalName)
            ? null
            : new RegisteredCompany(normalized, body.LegalName);
    }

    private sealed record RegistryCompanyResponse(string? LegalName);
}
```

## 3. Registrations

### Server: ZCrm.Server/Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Domain.Contacts;
using ZCrm.Domain.Companies;
using ZCrm.Persistence;
using ZCrm.Server.Companies;

var builder = WebApplication.CreateBuilder(args);

// Server mode: one endpoint carries every factory call
builder.Services.AddNeatooAspNetCore(typeof(IRecentContacts).Assembly);

// Server-only services that [Service] parameters and interface-factory implementations use
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("ZCrm")));

builder.Services.AddOptions<CompanyRegistryOptions>()
    .Bind(builder.Configuration.GetSection(CompanyRegistryOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Registers ICompanyRegistryLookup -> CompanyRegistryLookup with a configured typed HttpClient.
// The key is attached here, on the server, and only here.
builder.Services.AddHttpClient<ICompanyRegistryLookup, CompanyRegistryLookup>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<CompanyRegistryOptions>>().Value;
    http.BaseAddress = options.BaseAddress;
    http.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
});

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

I registered everything explicitly instead of using `RegisterMatchingName`. That method scans the assembly with reflection, it would only scan Domain anyway, and the implementation lives in the server project.

### Client: ZCrm.Client/Program.cs

The client registers the factory proxies and nothing for either feature. In Remote mode, `IRecentContactsFactory` and `ICompanyRegistryLookup` both resolve to generated proxies.

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

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IRecentContacts).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

## 4. Client components (ZCrm.Client)

### RecentContactsSidebar.razor

```razor
@using ZCrm.Domain.Contacts
@inject IRecentContactsFactory RecentContactsFactory

<MudPaper Outlined="true" Class="pa-2">
    <MudText Typo="Typo.h6" Class="px-2">Recent contacts</MudText>

    @if (_recent is null)
    {
        <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
    }
    else if (_recent.Items.Count == 0)
    {
        <MudText Typo="Typo.body2" Class="pa-2">No contacts yet.</MudText>
    }
    else
    {
        <MudList T="Guid" Dense="true">
            @foreach (var contact in _recent.Items)
            {
                <MudListItem T="Guid" Value="@contact.Id" Href="@($"contacts/{contact.Id}")">
                    <MudText Typo="Typo.body2">@contact.DisplayName</MudText>
                    <MudText Typo="Typo.caption">@contact.CompanyLegalName</MudText>
                </MudListItem>
            }
        </MudList>
    }
</MudPaper>

@code {
    private IRecentContacts? _recent;

    protected override Task OnInitializedAsync() => RefreshAsync();

    /// <summary>Reloads the list. The edit page calls this after a save, since the saved contact is now the most recent.</summary>
    public async Task RefreshAsync()
    {
        _recent = await RecentContactsFactory.Fetch();
        StateHasChanged();
    }
}
```

### CompanyLegalNameLookupButton.razor

The button takes the tax number and passes the legal name back through a callback. It knows nothing about the Contact entity, and the edit page decides where the name goes.

```razor
@using ZCrm.Domain.Companies
@inject ICompanyRegistryLookup CompanyRegistry

<MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
    <MudButton Variant="Variant.Outlined"
               StartIcon="@Icons.Material.Filled.ManageSearch"
               Disabled="@(_busy || string.IsNullOrWhiteSpace(TaxNumber))"
               OnClick="LookUpAsync">
        Look up legal name
    </MudButton>
    @if (_busy)
    {
        <MudProgressCircular Indeterminate="true" Size="Size.Small" />
    }
    @if (_message is not null)
    {
        <MudText Typo="Typo.caption" Color="Color.Warning">@_message</MudText>
    }
</MudStack>

@code {
    [Parameter, EditorRequired] public string? TaxNumber { get; set; }
    [Parameter] public EventCallback<string> LegalNameFound { get; set; }

    private bool _busy;
    private string? _message;

    private async Task LookUpAsync()
    {
        _busy = true;
        _message = null;
        try
        {
            var company = await CompanyRegistry.FindByTaxNumberAsync(TaxNumber!);
            if (company is null)
                _message = "No company is registered under that tax number.";
            else
                await LegalNameFound.InvokeAsync(company.LegalName);
        }
        catch (Exception ex)
        {
            // A server-side failure (registry down, bad key) rethrows here at the await
            _message = $"The registry lookup failed: {ex.Message}";
        }
        finally
        {
            _busy = false;
        }
    }
}
```

On the contact edit page (assuming `TaxNumber` and `CompanyLegalName` properties on your Contact entity):

```razor
<CompanyLegalNameLookupButton TaxNumber="@_contact.TaxNumber"
                              LegalNameFound="@(name => _contact.CompanyLegalName = name)" />
...
<RecentContactsSidebar @ref="_recentSidebar" />
@* after: _contact = await ContactFactory.Save(_contact); *@
@* then:  await _recentSidebar.RefreshAsync(); *@
```

## Which calls go to the server

| Call | Goes to server? | Why |
|---|---|---|
| `IRecentContactsFactory.Fetch()` from the sidebar | **Yes**, one round trip | The `Fetch` is `[Remote] internal`. Its `[Service] ZCrmDbContext` only exists on the server, so the query runs there and only the 10 records come back. On the published client, the method body is trimmed away. |
| `RecentContacts.Fetch(db, ct)`, the method body | Already on the server | It's what the server endpoint runs. The client never runs this code. |
| `IRecentContacts.Items` reads in the sidebar | No | It's state the client already holds after the fetch. |
| `ICompanyRegistryLookup.FindByTaxNumberAsync` from the button | **Yes**, always | Every interface-factory method is a server entry point; on the client the interface is a generated proxy. It has to cross because the API key and the configured `HttpClient` only exist on the server. |
| `CompanyRegistryLookup.FindByTaxNumberAsync`, the implementation | Server only | It's in the server project, so it isn't in the client bundle at all. The outbound call to the registry goes server to registry, never browser to registry. |
| `LegalNameFound` callback / setting `CompanyLegalName` | No | It's a local property change on the contact being edited. It's persisted later by the contact's own `Save`. |

## Not covered here

- **Authorization on the lookup.** Any signed-in caller can spend your registry quota through this endpoint. You'll probably want an `[AuthorizeFactory<T>]` check or rate limiting before it ships.
- **Who can see which contacts.** If contact visibility is restricted per user or tenant, the recent-contacts query needs that filter too.
