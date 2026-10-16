Next I'm writing out the answer: a read model for the sidebar, an interface factory for the registry lookup, the server implementation, and both tiers' Program.cs.

# Recent contacts sidebar and tax-number lookup for zCRM

The two features use two different kinds of factory:

- **Recent contacts** is a **read model**: a `[Factory]` class with only a `[Remote, Fetch]`. The screen shows data from the server, and nobody creates or saves it.
- **Legal-name lookup** is an **interface factory**: a `[Factory]` interface in the shared domain library, implemented by a plain class that exists only on the server. The client gets a generated proxy, so the code that holds the secret key and calls the registry never reaches the browser.

## Assumed persistence (the EF Core DbContext you said to assume)

```csharp
// zCRM.Persistence/ZCrmDbContext.cs
// Assumed shape. The domain library references this project, because the read model's
// server-only Fetch queries it.
using Microsoft.EntityFrameworkCore;

namespace zCRM.Persistence;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public DateTime ModifiedUtc { get; set; }
}

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The sidebar sorts by this column on every load.
        modelBuilder.Entity<ContactRow>().HasIndex(c => c.ModifiedUtc);
    }
}
```

## 1. Recent contacts: a read model in the shared domain library

```csharp
// zCRM.Domain/Contacts/RecentContacts.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Persistence;

namespace zCRM.Domain.Contacts;

public interface IRecentContacts
{
    IReadOnlyList<RecentContact> Items { get; }
}

/// <summary>One sidebar row. A plain record: it crosses the wire as data, with no factory.</summary>
public sealed record RecentContact(Guid Id, string DisplayName, string? CompanyName, DateTime ModifiedUtc);

/// <summary>
/// Read model for the contact edit screen's "Recent contacts" sidebar: the most recently
/// modified contacts. Nothing creates or saves it.
/// </summary>
[Factory]
internal partial class RecentContacts : IRecentContacts
{
    private const int Count = 10;

    public IReadOnlyList<RecentContact> Items { get; set; } = [];

    /// <summary>
    /// Crosses to the server: the DbContext exists only there. The ten rows come back in one
    /// round trip. Writes nothing, so it reads the Contacts table directly.
    /// </summary>
    [Remote, Fetch]
    internal async Task Fetch([Service] ZCrmDbContext db, CancellationToken cancellationToken)
    {
        Items = await db.Contacts
            .AsNoTracking()
            .OrderByDescending(c => c.ModifiedUtc)
            .ThenBy(c => c.Id)
            .Take(Count)
            .Select(c => new RecentContact(c.Id, c.FirstName + " " + c.LastName, c.CompanyName, c.ModifiedUtc))
            .ToListAsync(cancellationToken);
    }
}
```

**Why it's here:** the class lives in the domain library, which both tiers load. The client needs the type to receive the result, and the server needs it to run `Fetch`.
- `Fetch` is `[Remote] internal`. The generator makes it `public` on the generated `IRecentContactsFactory`, and with IL trimming its body is removed from the client bundle.
- The count of 10 is a constant on the server, not a parameter, so the client can't ask for 10,000 rows.
- `Fetch` returns `Task` rather than `Task<bool>`. An empty list is a valid answer, not "not found".

## 2. Legal-name lookup: an interface factory in the shared domain library

```csharp
// zCRM.Domain/Companies/ICompanyRegistry.cs
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Companies;

/// <summary>
/// Looks up companies in the external company registry. Implemented on the server only;
/// the client resolves a generated proxy.
/// </summary>
[Factory]
public interface ICompanyRegistry
{
    /// <summary>The company registered under the tax number, or null when the registry has none.</summary>
    Task<RegisteredCompany?> FindByTaxNumberAsync(string taxNumber);
}

public sealed record RegisteredCompany(string TaxNumber, string LegalName);
```

**Why it's here:** the client needs the contract in order to call it, so the contract goes in the shared library.
- The methods have no attributes. `[Factory]` on the interface already makes each method a remote entry point, and adding `[Execute]` or `[Remote]` is error NF0106.
- It's named for what it does. It is not a repository.

## 3. The registry implementation, in the server project only

```csharp
// zCRM.Server/Companies/CompanyRegistry.cs
using System.ComponentModel.DataAnnotations;
using System.Net;
using zCRM.Domain.Companies;

namespace zCRM.Server.Companies;

public sealed class CompanyRegistryOptions
{
    public const string Section = "CompanyRegistry";

    [Required] public Uri BaseAddress { get; set; } = null!;

    /// <summary>From user-secrets, environment or Key Vault. Never in the client's wwwroot/appsettings.json.</summary>
    [Required] public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Server implementation of ICompanyRegistry. Not marked [Factory]: only the interface carries it.
/// The typed HttpClient arrives with the base address and the API key header already set.
/// </summary>
internal sealed class CompanyRegistry(HttpClient http, ILogger<CompanyRegistry> logger) : ICompanyRegistry
{
    public async Task<RegisteredCompany?> FindByTaxNumberAsync(string taxNumber)
    {
        var normalized = new string((taxNumber ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        if (normalized.Length == 0)
            throw new ArgumentException("A tax number is required.", nameof(taxNumber));

        using var response = await http.GetAsync($"companies/{Uri.EscapeDataString(normalized)}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Company registry returned {Status} for a tax number lookup", response.StatusCode);
            throw new InvalidOperationException("The company registry is unavailable. Try again later.");
        }

        var body = await response.Content.ReadFromJsonAsync<RegistryCompanyResponse>();
        return string.IsNullOrWhiteSpace(body?.LegalName)
            ? null
            : new RegisteredCompany(normalized, body.LegalName);
    }

    // The registry's response shape (assumed; match the real API).
    private sealed record RegistryCompanyResponse(string? LegalName);
}
```

**Why it's here:** the server project is the only one that has the HttpClient setup, the options and the key.
- Putting the class in the shared library would ship the registry-calling code to the browser.
- Putting the key anywhere the client can read it would leak it.
- The client cannot get to this class at all. It only reaches the generated endpoint, which calls it.

## 4. Server registration

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Companies;
using zCRM.Domain.Contacts;
using zCRM.Persistence;
using zCRM.Server.Companies;

var builder = WebApplication.CreateBuilder(args);

// RemoteFactory in Server mode: the generated factories, plus the endpoint's delegates for
// IRecentContactsFactory.Fetch and ICompanyRegistry's methods.
builder.Services.AddNeatooAspNetCore(typeof(IRecentContacts).Assembly);

// Server-only services that [Service] parameters and the interface factory resolve.
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));

builder.Services.AddOptions<CompanyRegistryOptions>()
    .Bind(builder.Configuration.GetSection(CompanyRegistryOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Registers ICompanyRegistry -> CompanyRegistry with a typed HttpClient carrying the secret key.
builder.Services.AddHttpClient<ICompanyRegistry, CompanyRegistry>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CompanyRegistryOptions>>().Value;
    client.BaseAddress = options.BaseAddress;
    client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
});

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// The single RemoteFactory endpoint; no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

**Why it's here:** the server registers the implementation and everything it depends on.
- `ICompanyRegistry` is registered explicitly. `RegisterMatchingName` only scans the domain assembly, and the implementation isn't in it.
- `UseSqlServer` is an assumption; use whichever provider you have.

## 5. Client registration

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

builder.Services.AddMudServices();

// Remote mode: IRecentContactsFactory.Fetch and every ICompanyRegistry method resolve to proxies that POST to the server.
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IRecentContacts).Assembly);

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Deliberately absent: ZCrmDbContext, CompanyRegistry, CompanyRegistryOptions. Nothing server-only is registered here.

await builder.Build().RunAsync();
```

## 6. The client components

```razor
@* zCRM.Client/Contacts/RecentContactsSidebar.razor
   Display only: one factory call, then binding. *@
@using zCRM.Domain.Contacts
@inject IRecentContactsFactory RecentContactsFactory

<MudPaper Class="pa-3" Elevation="1">
    <MudText Typo="Typo.h6" Class="mb-2">Recent contacts</MudText>

    @if (error is not null)
    {
        <MudAlert Severity="Severity.Warning" Dense="true">@error</MudAlert>
    }
    else if (recent is null)
    {
        <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
    }
    else if (recent.Items.Count == 0)
    {
        <MudText Typo="Typo.body2">No contacts yet.</MudText>
    }
    else
    {
        <MudList T="Guid" Dense="true">
            @foreach (var contact in recent.Items)
            {
                <MudListItem T="Guid" Value="@contact.Id" Href="@($"/contacts/{contact.Id}")"
                             Class="@(contact.Id == CurrentContactId ? "mud-selected-item" : null)">
                    <MudText Typo="Typo.body1">@contact.DisplayName</MudText>
                    <MudText Typo="Typo.caption">
                        @(contact.CompanyName ?? "-") · @DateTime.SpecifyKind(contact.ModifiedUtc, DateTimeKind.Utc).ToLocalTime().ToString("g")
                    </MudText>
                </MudListItem>
            }
        </MudList>
    }
</MudPaper>

@code {
    [Parameter] public Guid? CurrentContactId { get; set; }

    private IRecentContacts? recent;
    private string? error;

    protected override Task OnInitializedAsync() => RefreshAsync();

    /// <summary>The edit page calls this after a save so the list reflects the new ModifiedUtc.</summary>
    public async Task RefreshAsync()
    {
        try
        {
            error = null;
            recent = await RecentContactsFactory.Fetch(); // crosses to the server
        }
        catch (Exception)
        {
            error = "Recent contacts could not be loaded.";
        }
        StateHasChanged();
    }
}
```

```razor
@* zCRM.Client/Companies/CompanyLookupButton.razor
   Turns the click into one registry call and hands the result to the edit screen. It changes nothing itself. *@
@using zCRM.Domain.Companies
@inject ICompanyRegistry CompanyRegistry
@inject ISnackbar Snackbar

<MudButton Variant="Variant.Outlined"
           StartIcon="@Icons.Material.Filled.Search"
           Disabled="@(busy || string.IsNullOrWhiteSpace(TaxNumber))"
           OnClick="LookUpAsync">
    @(busy ? "Looking up..." : "Look up legal name")
</MudButton>

@code {
    [Parameter, EditorRequired] public string? TaxNumber { get; set; }
    [Parameter] public EventCallback<RegisteredCompany> OnFound { get; set; }

    private bool busy;

    private async Task LookUpAsync()
    {
        busy = true;
        try
        {
            var company = await CompanyRegistry.FindByTaxNumberAsync(TaxNumber!); // proxy: crosses to the server
            if (company is null)
                Snackbar.Add("No company is registered under that tax number.", Severity.Info);
            else
                await OnFound.InvokeAsync(company);
        }
        catch (Exception ex) // server exceptions are rethrown here at the await
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            busy = false;
        }
    }
}
```

How the edit screen uses them (a fragment; `Contact` is your existing contact entity):

```razor
<CompanyLookupButton TaxNumber="@Contact.TaxNumber"
                     OnFound="(RegisteredCompany c) => Contact.CompanyName = c.LegalName" />
...
<RecentContactsSidebar @ref="recentSidebar" CurrentContactId="@Contact.Id" />

@code {
    private RecentContactsSidebar recentSidebar = default!;
    // after: Contact = await ContactFactory.Save(Contact);
    //        await recentSidebar.RefreshAsync();
}
```

## Which calls cross to the server

| Method | Crosses? | Why |
|---|---|---|
| `IRecentContactsFactory.Fetch()` (generated; called by the sidebar) | **Yes**, one round trip | `[Remote]` on the `Fetch` operation. The `ZCrmDbContext` it needs exists only on the server. |
| `RecentContacts.Fetch(db, ct)` (the body) | Runs **only on the server** | It runs after the call has crossed. Because it's `internal`, its body can be trimmed from the client bundle. |
| `ICompanyRegistry.FindByTaxNumberAsync` (called by the button) | **Yes**, one round trip | Every interface-factory method is a remote entry point, and in Remote mode the client's `ICompanyRegistry` is the proxy. |
| `CompanyRegistry.FindByTaxNumberAsync` (implementation) | Runs **only on the server**; it is never on the client | The generated server delegate calls it. Its HttpClient, key and outbound call to the registry are all on the server. |
| `RecentContactsSidebar.RefreshAsync`, `CompanyLookupButton.LookUpAsync` | No, they run in the browser | Each makes exactly one of the calls above. |
| `OnFound` handler setting `Contact.CompanyName` | No | It's a local property change on the contact being edited. It reaches the database when the contact's own `Save` runs. |
| `RecentContact` and `RegisteredCompany` records | Not calls | They travel only as results and need no factory. |

## Things to know

- **I couldn't compile any of this.** You asked for no files, so nothing was built. The attribute names and the registration calls follow the RemoteFactory reference; the `using` namespaces are my best understanding, so check them against the package. The registry's URL path, the `X-Api-Key` header and the `{ legalName }` response shape are placeholders for the real API.
- **The lookup endpoint needs authorization.** Anyone who can reach `UseNeatoo()`'s endpoint can make the server call the registry with your key. Before shipping, add an `[AuthorizeFactory<ICompanyRegistryAuth>]` with an `Execute`-scoped check, or require an authenticated user in front of the endpoint.
- **EF Core is now a dependency of the shared library.** Because the domain library references the persistence project, enable IL trimming on the client so the server-only bodies and EF Core are removed from the WASM bundle. If you'd rather not have that reference at all, the domain library can declare a narrow query interface that the server implements, and `Fetch` takes that instead.
