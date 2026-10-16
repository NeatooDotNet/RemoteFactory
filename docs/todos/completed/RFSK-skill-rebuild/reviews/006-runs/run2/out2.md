I haven't compiled or run any of this. As you asked, I created no files. I followed the patterns in the Neatoo, RemoteFactory and MudNeatoo skills, but a first build may still show small API mismatches.

## How it's split

| Piece | Kind | Project | Why |
|---|---|---|---|
| `ContactSearchCriteria` | Neatoo input model (`ValidateBase`) | Domain | A person edits it, and the 2-character message and the Search gate are rules on it. Nothing saves it, so it isn't an entity. |
| `ContactSearchFilter` | `record` | Domain | The cleaned-up criteria (trimmed, blanks become null) that cross the wire. One "can this be searched?" check here serves both the form and the server. |
| `ContactSearchResults` | Read model (plain `[Factory]`, `[Fetch]` only) | Domain | A screen's view of server data. It holds the EF query; the method is `[Remote] internal`, so the query runs only on the server and is trimmed out of the client. |
| `ZCrmDbContext`, `ContactRow` | EF Core | Persistence | Server only. The Domain references it with `PrivateAssets="all"`, so EF never reaches the browser. |
| `ContactSearchViewModel` | Plain class | Client | Holds the criteria and the results, and turns the click into a fetch. It makes no decisions. |
| `ContactSearch.razor` | Page | Client | Binding and layout only. |

---

## zCRM.Persistence (server only)

This is the DbContext you said to assume. Only its shape matters.

```cs
// zCRM.Persistence/ContactRow.cs
namespace zCRM.Persistence;

/// <summary>Row in the Contacts table.</summary>
public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastContactedOn { get; set; }
}
```

```cs
// zCRM.Persistence/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace zCRM.Persistence;

public sealed class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(contact =>
        {
            contact.ToTable("Contacts");
            contact.HasKey(c => c.Id);
            contact.Property(c => c.FirstName).HasMaxLength(100);
            contact.Property(c => c.LastName).HasMaxLength(100);
            contact.Property(c => c.City).HasMaxLength(100);
            contact.HasIndex(c => c.City);
        });
    }
}
```

## zCRM.Domain (loaded on both client and server)

The project file below is XML, not C#, but where these references go is what keeps EF out of the browser. Take package versions from nuget.org.

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
<ItemGroup>
  <!-- Direct references: the source generators only run where referenced directly -->
  <PackageReference Include="Neatoo" />
  <PackageReference Include="Neatoo.RemoteFactory" />
  <!-- Server-only code paths; never flows to the client -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
  <ProjectReference Include="..\zCRM.Persistence\zCRM.Persistence.csproj" PrivateAssets="all" />
</ItemGroup>
```

```cs
// zCRM.Domain/Contacts/ContactSearchFilter.cs
namespace zCRM.Domain.Contacts;

/// <summary>
/// A contact search, normalized: text trimmed, blank text null. The read model's
/// Fetch parameter, and the one definition of a searchable filter.
/// </summary>
public sealed record ContactSearchFilter(string? NameFragment, string? City, bool ActiveOnly)
{
    public const int MinNameLength = 2;

    public static ContactSearchFilter From(string? nameFragment, string? city, bool activeOnly)
        => new(Normalize(nameFragment), Normalize(city), activeOnly);

    /// <summary>A name was entered but is too short to search on.</summary>
    public bool IsNameTooShort => NameFragment is { Length: < MinNameLength };

    /// <summary>At least one criterion, and none of them unusable.</summary>
    public bool IsSearchable =>
        !IsNameTooShort && (NameFragment is not null || City is not null || ActiveOnly);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

```cs
// zCRM.Domain/Contacts/IContactSearchCriteria.cs
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Input model for the contact search form.</summary>
public interface IContactSearchCriteria : IValidateBase
{
    string? NameFragment { get; set; }
    string? City { get; set; }
    bool ActiveOnly { get; set; }

    /// <summary>Admission for the search: a usable criterion is entered.</summary>
    bool CanSearch { get; }

    ContactSearchFilter ToFilter();
}
```

```cs
// zCRM.Domain/Contacts/ContactSearchCriteria.cs
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

[Factory]
internal partial class ContactSearchCriteria : ValidateBase<ContactSearchCriteria>, IContactSearchCriteria
{
    public partial string? NameFragment { get; set; }
    public partial string? City { get; set; }
    public partial bool ActiveOnly { get; set; }
    public partial bool CanSearch { get; private set; }

    public ContactSearchCriteria(IValidateBaseServices<ContactSearchCriteria> services) : base(services)
    {
        // The message under the name field.
        RuleManager.AddValidation(
            t => t.ToFilter().IsNameTooShort
                ? $"Enter at least {ContactSearchFilter.MinNameLength} characters of the name."
                : string.Empty,
            t => t.NameFragment);

        // Admission fact the Search button binds to.
        RuleManager.AddAction(
            t => t.CanSearch = t.ToFilter().IsSearchable,
            t => t.NameFragment,
            t => t.City,
            t => t.ActiveOnly);
    }

    // Local: an empty form needs nothing from the server. The operation runs
    // paused, so CanSearch starts false - the baseline for an empty form.
    [Create]
    public void Create()
    {
    }

    public ContactSearchFilter ToFilter() => ContactSearchFilter.From(NameFragment, City, ActiveOnly);
}
```

```cs
// zCRM.Domain/Contacts/ContactSearchResults.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Persistence;

namespace zCRM.Domain.Contacts;

/// <summary>One row of the contact search results.</summary>
public sealed record ContactListItem(Guid Id, string Name, string? City, DateTime? LastContactedOn);

/// <summary>Read model for the contact search screen.</summary>
public interface IContactSearchResults
{
    IReadOnlyList<ContactListItem> Contacts { get; }
    bool HasMatches { get; }

    /// <summary>More contacts matched than were returned.</summary>
    bool IsTruncated { get; }
}

[Factory]
internal partial class ContactSearchResults : IContactSearchResults
{
    private const int MaxResults = 200;

    public IReadOnlyList<ContactListItem> Contacts { get; set; } = [];
    public bool HasMatches { get; set; }
    public bool IsTruncated { get; set; }

    /// <remarks>
    /// Reads the store directly: a read model writes nothing, so no aggregate's
    /// rules are bypassed. The CancellationToken is the request's, so a page that
    /// navigates away mid-search stops the query.
    /// </remarks>
    [Remote, Fetch]
    internal async Task Fetch(
        ContactSearchFilter filter,
        [Service] ZCrmDbContext db,
        CancellationToken cancellationToken)
    {
        // The server normalizes for itself rather than trusting the caller.
        filter = ContactSearchFilter.From(filter.NameFragment, filter.City, filter.ActiveOnly);

        // The screen never sends this; a caller that does disagrees with the
        // criteria's rules, and an unfiltered scan of Contacts is not a search.
        if (!filter.IsSearchable)
        {
            throw new InvalidOperationException("A contact search needs at least one usable criterion.");
        }

        var query = db.Contacts.AsNoTracking();

        if (filter.NameFragment is { } name)
        {
            query = query.Where(c => (c.FirstName + " " + c.LastName).Contains(name));
        }

        if (filter.City is { } city)
        {
            query = query.Where(c => c.City == city);
        }

        if (filter.ActiveOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var rows = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Take(MaxResults + 1)
            .Select(c => new ContactListItem(c.Id, c.FirstName + " " + c.LastName, c.City, c.LastContactedOn))
            .ToListAsync(cancellationToken);

        IsTruncated = rows.Count > MaxResults;
        Contacts = rows.Take(MaxResults).ToList();
        HasMatches = Contacts.Count > 0;
    }
}
```

## zCRM.Client (Blazor WebAssembly)

```cs
// zCRM.Client/Contacts/ContactSearchViewModel.cs
using zCRM.Domain.Contacts;

namespace zCRM.Client.Contacts;

/// <summary>
/// ViewModel for the contact search screen: holds the criteria and the last
/// results, and turns the Search click into a read-model fetch.
/// </summary>
internal sealed class ContactSearchViewModel
{
    private readonly IContactSearchResultsFactory _resultsFactory;

    public ContactSearchViewModel(
        IContactSearchCriteriaFactory criteriaFactory,
        IContactSearchResultsFactory resultsFactory)
    {
        _resultsFactory = resultsFactory;
        Criteria = criteriaFactory.Create();
    }

    public IContactSearchCriteria Criteria { get; }

    /// <summary>Null until the first search.</summary>
    public IContactSearchResults? Results { get; private set; }

    public bool IsSearching { get; private set; }

    /// <summary>
    /// The criteria's admission fact. Also off while a search is in flight,
    /// so a double click sends one request.
    /// </summary>
    public bool CanSearch => Criteria.CanSearch && !IsSearching;

    public async Task Search()
    {
        await Criteria.WaitForTasks();

        if (!CanSearch)
        {
            return;
        }

        IsSearching = true;
        try
        {
            Results = await _resultsFactory.Fetch(Criteria.ToFilter());
        }
        finally
        {
            IsSearching = false;
        }
    }
}
```

```razor
@* zCRM.Client/Pages/Contacts/ContactSearch.razor *@
@page "/contacts"
@inject ContactSearchViewModel Vm

<PageTitle>Contacts</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Contacts</MudText>

@* The criteria are an input model (ValidateBase). MudNeatoo fields take
   IEntityProperty only (Neatoo#95), so each field binds its property object by
   hand: the value, the messages, read-only, and SetValue so the rules run.
   Immediate: the rules are local and synchronous, so the button and the
   message follow each keystroke. *@
<MudPaper Class="pa-4 mb-4">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="4">
            @{ var name = Vm.Criteria[nameof(IContactSearchCriteria.NameFragment)]; }
            <MudTextField T="string"
                          Label="Name contains"
                          Value="@Vm.Criteria.NameFragment"
                          ValueChanged="@(async (string v) => await name.SetValue(v))"
                          Immediate="true"
                          Clearable="true"
                          Error="@(!name.IsValid)"
                          ErrorText="@string.Join(" ", name.PropertyMessages.Select(m => m.Message))"
                          Disabled="@name.IsReadOnly" />
        </MudItem>
        <MudItem xs="12" sm="4">
            @{ var city = Vm.Criteria[nameof(IContactSearchCriteria.City)]; }
            <MudTextField T="string"
                          Label="City"
                          Value="@Vm.Criteria.City"
                          ValueChanged="@(async (string v) => await city.SetValue(v))"
                          Immediate="true"
                          Clearable="true"
                          Error="@(!city.IsValid)"
                          ErrorText="@string.Join(" ", city.PropertyMessages.Select(m => m.Message))"
                          Disabled="@city.IsReadOnly" />
        </MudItem>
        <MudItem xs="6" sm="2" Class="d-flex align-center">
            @{ var activeOnly = Vm.Criteria[nameof(IContactSearchCriteria.ActiveOnly)]; }
            <MudCheckBox T="bool"
                         Label="Active only"
                         Value="@Vm.Criteria.ActiveOnly"
                         ValueChanged="@(async (bool v) => await activeOnly.SetValue(v))"
                         Disabled="@activeOnly.IsReadOnly" />
        </MudItem>
        <MudItem xs="6" sm="2" Class="d-flex align-center">
            @* Enabled by the criteria's admission fact; the page decides nothing. *@
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       FullWidth="true"
                       Disabled="@(!Vm.CanSearch)"
                       OnClick="@Vm.Search">
                Search
            </MudButton>
        </MudItem>
    </MudGrid>
</MudPaper>

@if (Vm.IsSearching)
{
    <MudProgressLinear Indeterminate="true" Class="mb-2" />
}

@if (Vm.Results is { } results)
{
    @if (results.HasMatches)
    {
        <MudTable T="ContactListItem" Items="@results.Contacts" Dense="true" Hover="true">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>City</MudTh>
                <MudTh>Last contacted</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel="Name">@context.Name</MudTd>
                <MudTd DataLabel="City">@context.City</MudTd>
                <MudTd DataLabel="Last contacted">@(context.LastContactedOn?.ToString("d") ?? "Never")</MudTd>
            </RowTemplate>
        </MudTable>

        @if (results.IsTruncated)
        {
            <MudText Typo="Typo.caption" Class="mt-2">More contacts match than are shown; narrow the search.</MudText>
        }
    }
    else
    {
        <MudAlert Severity="Severity.Info">No contacts match.</MudAlert>
    }
}
```

The page doesn't subscribe to change events. Each field commits inside its own `ValueChanged` callback and the rules run synchronously, so Blazor's re-render after that callback already shows the new `CanSearch` value and the name message. Add a subscription to the criteria's change event only if you later add an async rule.

```razor
@* zCRM.Client/_Imports.razor (additions) *@
@using MudBlazor
@using zCRM.Domain.Contacts
@using zCRM.Client.Contacts
```

```cs
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.Contacts;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Neatoo in Remote mode: the generated factories are proxies, and [Remote]
// operations (the results Fetch) cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContactSearchCriteria).Assembly);

// The HttpClient the proxies call through: the server hosting this client.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactSearchViewModel>();

await builder.Build().RunAsync();
```

Add this to the client project file so a published build strips the server-only bodies (the EF query):

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />
</ItemGroup>
```

## zCRM.Server (ASP.NET Core)

```cs
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Contacts;
using zCRM.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Neatoo in Server mode; this also registers what UseNeatoo() resolves.
// Do not also call AddNeatooAspNetCore after it (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContactSearchCriteria).Assembly);

// The [Service] the results Fetch takes. Registered here only, so it
// resolves only on the server.
builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The one endpoint every factory call goes through; no controllers, no DTOs.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

`App.razor` and `MainLayout.razor` (with MudBlazor's theme, popover, dialog and snackbar providers) are the standard templates and aren't repeated here.

---

**Decisions for you to confirm.** Each is a one-line change:
- **"Active only" counts as a criterion.** Checking it alone enables Search and lists every active contact. To require a name or city, drop `|| ActiveOnly` from `IsSearchable`.
- **A 1-character name blocks Search even when a city is filled in.** The form shows the message, and the server rejects that search too.
- **Name matches anywhere in "First Last"**, so "jo sm" finds John Smith. City is an exact match. Case sensitivity comes from the database collation (SQL Server's default ignores case).
- **Results are capped at 200**, with a "narrow the search" note when more match.
- **The table schema is assumed** (FirstName/LastName/City/IsActive/LastContactedOn), as is SQL Server.
- **There is no authorization yet.** Add RemoteFactory's authorization (`[AuthorizeFactory<T>]` or `[AspAuthorize]`) to the results fetch once zCRM has sign-in.

**Points that may need adjusting at first build:**
- **Fetch return type:** I'm not sure whether the generated `Fetch` on `IContactSearchResultsFactory` returns a nullable result when the method returns `Task` rather than `Task<bool>`. The view model handles either.
- **MudBlazor version:** `MudCheckBox` with `Value`/`ValueChanged` assumes MudBlazor 7 or later.
