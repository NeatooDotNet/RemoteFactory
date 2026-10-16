I've read the Neatoo, RemoteFactory and MudNeatoo guidance, and the design below follows it. Nothing here has been compiled or run, because you asked for code in the reply rather than files. Treat the namespaces and the generated factory names (`IContactSearchCriteriaFactory`, `IContactSearchResultsFactory`) as unverified until the first build.

## Project layout

| Project | References | Runs on |
|---|---|---|
| `zCRM.Dal` | nothing (no EF Core) | both |
| `zCRM.Domain` | Neatoo, `zCRM.Dal` | both |
| `zCRM.Ef` | EF Core, `zCRM.Dal` | server |
| `zCRM.Server` | Domain, Ef, Client, `Neatoo.RemoteFactory.AspNetCore` | server |
| `zCRM.Client` | Domain, `Neatoo.Blazor.MudNeatoo`, MudBlazor | browser |

The browser and the server load the same domain assembly. The EF query sits behind an interface in `Dal`, so EF Core never ends up in the client's reference graph.

---

## 1. `zCRM.Dal`: the query contract

**Why here:** the read model's server method needs a parameter type that compiles on both tiers. This interface is that type. Its EF implementation lives only on the server.

```csharp
namespace zCRM.Dal.Contacts;

/// <summary>One contact row as the search query returns it.</summary>
public sealed record ContactSearchRow(
    Guid Id,
    string FirstName,
    string LastName,
    string? City,
    DateTime? LastContactedOn);

/// <summary>Server-side contact search. Implemented over EF Core in zCRM.Ef.</summary>
public interface IContactSearchQuery
{
    Task<IReadOnlyList<ContactSearchRow>> Search(
        string? nameFragment,
        string? city,
        bool activeOnly,
        CancellationToken cancellationToken);
}
```

---

## 2. `zCRM.Domain`: the search criteria (input model)

**Why here, and why `ValidateBase`:** someone edits the criteria, and the criteria have rules, but nothing is ever saved. That makes it an input model, not an entity.

- The minimum-length check is a validation rule, so its message lives on the `NameFragment` property.
- "At least one criterion" is a fact (`HasCriterion`) computed by a rule.
- `CanSearch` combines that fact with `IsValid` and `IsBusy`, the same way `IsSavable` does.

The page decides none of this.

The two static helpers are the only copy of the rule logic. The server guard in section 3 calls them too.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>Criteria for the contact search screen.</summary>
public interface IContactSearchCriteria : IValidateBase
{
    string? NameFragment { get; set; }
    string? City { get; set; }
    bool ActiveOnly { get; set; }

    /// <summary>At least one criterion is entered. Computed by a rule.</summary>
    bool HasCriterion { get; }

    /// <summary>A search may run: a criterion is entered, every rule passes, no rule is running.</summary>
    bool CanSearch { get; }
}

[Factory]
internal partial class ContactSearchCriteria : ValidateBase<ContactSearchCriteria>, IContactSearchCriteria
{
    internal const int MinNameFragmentLength = 2;

    public partial string? NameFragment { get; set; }
    public partial string? City { get; set; }
    public partial bool ActiveOnly { get; set; }
    public partial bool HasCriterion { get; private set; }

    public ContactSearchCriteria(IValidateBaseServices<ContactSearchCriteria> services) : base(services)
    {
        // The name is optional. When entered, it must be long enough to be selective.
        RuleManager.AddValidation(
            t => IsNameFragmentTooShort(t.NameFragment)
                ? $"Enter at least {MinNameFragmentLength} characters of the name"
                : string.Empty,
            t => t.NameFragment);

        RuleManager.AddAction(
            t => t.HasCriterion = HasAnyCriterion(t.NameFragment, t.City, t.ActiveOnly),
            t => t.NameFragment,
            t => t.City,
            t => t.ActiveOnly);
    }

    // Composes the object's own state, like IsSavable. HasCriterion and IsValid both raise PropertyChanged.
    public bool CanSearch => HasCriterion && IsValid && !IsBusy;

    // Local: an empty form needs nothing from the server. No rule runs here, so the
    // form starts with no message and HasCriterion == false (Search disabled).
    [Create]
    public void Create()
    {
    }

    internal static bool HasAnyCriterion(string? nameFragment, string? city, bool activeOnly) =>
        !string.IsNullOrWhiteSpace(nameFragment)
        || !string.IsNullOrWhiteSpace(city)
        || activeOnly;

    internal static bool IsNameFragmentTooShort(string? nameFragment) =>
        !string.IsNullOrWhiteSpace(nameFragment)
        && nameFragment.Trim().Length < MinNameFragmentLength;
}
```

---

## 3. `zCRM.Domain`: the results (read model)

**Why a plain `[Factory]` class:** the screen shows data from the server and never edits it, so it needs no Neatoo base class.

- **Why `[Remote, Fetch]`:** this is where the client's call crosses to the server. On the server it gets `IContactSearchQuery` as a `[Service]`.
- **Why plain parameters:** passing the three values instead of the criteria object means the trimmed client needs no extra configuration to keep a criteria type.
- **Why the guard throws:** this is a public endpoint. A caller that skipped `CanSearch` would otherwise run an unfiltered scan of the table. Such a call means the client and server disagree, which is an application error, not a validation message.

```csharp
using Neatoo.RemoteFactory;
using zCRM.Dal.Contacts;

namespace zCRM.Domain.Contacts;

/// <summary>One row of the contact search results.</summary>
public sealed record ContactSearchItem(Guid Id, string Name, string? City, DateTime? LastContactedOn);

/// <summary>Read model for the contact search screen.</summary>
public interface IContactSearchResults
{
    IReadOnlyList<ContactSearchItem> Contacts { get; }
}

[Factory]
internal partial class ContactSearchResults : IContactSearchResults
{
    public IReadOnlyList<ContactSearchItem> Contacts { get; set; } = [];

    [Remote, Fetch]
    internal async Task Fetch(
        string? nameFragment,
        string? city,
        bool activeOnly,
        [Service] IContactSearchQuery query,
        CancellationToken cancellationToken)
    {
        if (!ContactSearchCriteria.HasAnyCriterion(nameFragment, city, activeOnly)
            || ContactSearchCriteria.IsNameFragmentTooShort(nameFragment))
        {
            throw new ArgumentException(
                "Contact search requires at least one criterion and a name fragment of at least "
                + $"{ContactSearchCriteria.MinNameFragmentLength} characters.");
        }

        var rows = await query.Search(nameFragment, city, activeOnly, cancellationToken);

        Contacts = rows
            .Select(r => new ContactSearchItem(r.Id, $"{r.FirstName} {r.LastName}", r.City, r.LastContactedOn))
            .ToList();
    }
}
```

---

## 4. `zCRM.Ef`: the EF Core query

**Why here:** this is the only project that knows about EF Core, and only the server references it. The `Contact` entity and `CrmDbContext` below are the shape I assumed for your existing context; change the property names to match yours. Filters are added only when a criterion is set. The query doesn't track entities and loads only the five columns the list needs.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using zCRM.Dal.Contacts;

namespace zCRM.Ef;

// Assumed shape of your existing table and context; replace with yours.
public sealed class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastContactedOn { get; set; }
}

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
}

internal sealed class ContactSearchQuery(CrmDbContext db) : IContactSearchQuery
{
    public async Task<IReadOnlyList<ContactSearchRow>> Search(
        string? nameFragment,
        string? city,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        IQueryable<Contact> contacts = db.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nameFragment))
        {
            var fragment = nameFragment.Trim();
            contacts = contacts.Where(c => c.FirstName.Contains(fragment) || c.LastName.Contains(fragment));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityName = city.Trim();
            contacts = contacts.Where(c => c.City == cityName);
        }

        if (activeOnly)
        {
            contacts = contacts.Where(c => c.IsActive);
        }

        return await contacts
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new ContactSearchRow(c.Id, c.FirstName, c.LastName, c.City, c.LastContactedOn))
            .ToListAsync(cancellationToken);
    }
}

public static class EfRegistration
{
    /// <summary>Server-only: the implementations behind the Dal interfaces.</summary>
    public static IServiceCollection AddCrmEf(this IServiceCollection services)
    {
        services.AddScoped<IContactSearchQuery, ContactSearchQuery>();
        return services;
    }
}
```

---

## 5. `zCRM.Server`: Program.cs

**Why this shape:** use `AddNeatooServices(Server)` plus `UseNeatoo()`, and do **not** add `AddNeatooAspNetCore`. Registering that after `AddNeatooServices(Server)` breaks remote Saves (RemoteFactory#117). This search doesn't save anything, but later screens in zCRM will.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Contacts;
using zCRM.Ef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContactSearchCriteria).Assembly);

// Your existing DbContext registration (provider assumed).
builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddCrmEf();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The one endpoint every factory call crosses; no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## 6. `zCRM.Client`: Program.cs

**Why:** the client registers the factories in Remote mode, so `[Remote]` calls go to the server and `[Create]` runs locally. It also registers the keyed `HttpClient` those calls use, MudBlazor, and the view model. It registers nothing server-only.

```csharp
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

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContactSearchCriteria).Assembly);

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

builder.Services.AddTransient<ContactSearchViewModel>();

await builder.Build().RunAsync();
```

---

## 7. `zCRM.Client`: the view model

**Why:** it holds the criteria and the results and turns the click into a fetch. It makes no decision of its own. `IsSearching` is screen state: it disables the button while a request is in flight, so a double-click doesn't send two searches.

```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.Contacts;

/// <summary>ViewModel for the contact search screen: holds the criteria, fetches the results.</summary>
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

    /// <summary>The Search button binds to this: the criteria's own answer, plus not already searching.</summary>
    public bool CanSearch => Criteria.CanSearch && !IsSearching;

    public async Task Search()
    {
        await Criteria.WaitForTasks();

        if (!Criteria.CanSearch)
        {
            return;
        }

        IsSearching = true;
        try
        {
            Results = await _resultsFactory.Fetch(Criteria.NameFragment, Criteria.City, Criteria.ActiveOnly);
        }
        finally
        {
            IsSearching = false;
        }
    }
}
```

---

## 8. `zCRM.Client`: the page

**Why hand-written binding instead of MudNeatoo components:** MudNeatoo components take an `IEntityProperty`, but a `ValidateBase` object's properties are `IValidateProperty` (Neatoo#95). So each field binds its property object directly: the value, then `SetValue`, `IsValid` and the property's messages.

- `Immediate="true"` is safe because the rules are synchronous and local. The message under Name appears while typing, and the Search button turns on without the field having to lose focus first.
- Every handler is an `EventCallback` on this page, so Blazor re-renders after each change. No `PropertyChanged` subscription is needed.
- `MudTable`'s `NoRecordsContent` covers the empty result without the page adding a check of its own.

```razor
@page "/contacts/search"
@using Neatoo
@using zCRM.Domain.Contacts
@inject ContactSearchViewModel Vm

<PageTitle>Contact search</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Contacts</MudText>

<MudPaper Class="pa-4 mb-4">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="5">
            <MudTextField T="string"
                          Label="Name"
                          Value="@Vm.Criteria.NameFragment"
                          ValueChanged="@((string v) => NameFragment.SetValue(v))"
                          Immediate="true"
                          Error="@(!NameFragment.IsValid)"
                          ErrorText="@MessagesOf(NameFragment)"
                          Disabled="@NameFragment.IsReadOnly" />
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudTextField T="string"
                          Label="City"
                          Value="@Vm.Criteria.City"
                          ValueChanged="@((string v) => City.SetValue(v))"
                          Immediate="true"
                          Error="@(!City.IsValid)"
                          ErrorText="@MessagesOf(City)"
                          Disabled="@City.IsReadOnly" />
        </MudItem>
        <MudItem xs="12" sm="3" Class="d-flex align-center">
            <MudCheckBox T="bool"
                         Label="Active only"
                         Value="@Vm.Criteria.ActiveOnly"
                         ValueChanged="@((bool v) => ActiveOnly.SetValue(v))"
                         Disabled="@ActiveOnly.IsReadOnly" />
        </MudItem>
        <MudItem xs="12">
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       StartIcon="@Icons.Material.Filled.Search"
                       Disabled="@(!Vm.CanSearch)"
                       OnClick="Vm.Search">
                Search
            </MudButton>
        </MudItem>
    </MudGrid>
</MudPaper>

@if (Vm.Results is not null)
{
    <MudTable T="ContactSearchItem"
              Items="@Vm.Results.Contacts"
              Loading="@Vm.IsSearching"
              Dense="true"
              Hover="true">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>City</MudTh>
            <MudTh>Last contacted</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="City">@context.City</MudTd>
            <MudTd DataLabel="Last contacted">@context.LastContactedOn?.ToString("d")</MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText>No contacts match.</MudText>
        </NoRecordsContent>
    </MudTable>
}

@code {
    private IValidateProperty NameFragment => Vm.Criteria[nameof(IContactSearchCriteria.NameFragment)];
    private IValidateProperty City => Vm.Criteria[nameof(IContactSearchCriteria.City)];
    private IValidateProperty ActiveOnly => Vm.Criteria[nameof(IContactSearchCriteria.ActiveOnly)];

    private static string MessagesOf(IValidateProperty property) =>
        string.Join(" ", property.PropertyMessages.Select(m => m.Message));
}
```

---

## Assumptions to confirm

- **"Active only" counts as a criterion.** Ticking just that box enables Search and lists every active contact. If it should only narrow a name or city search, remove `|| activeOnly` from `HasAnyCriterion`; the client rule and the server guard both change with it.
- **Name matching:** the fragment matches anywhere in the first name or the last name. City must match exactly. Whether either is case-sensitive depends on your database's collation (SQL Server usually isn't; PostgreSQL is). Whitespace is trimmed, so "  a " counts as one character.
- **No row limit.** "Active only" on its own can return a very large list. I'd add a `Take(n)` plus a `HasMore` flag on the read model, but you didn't ask for it.
- **No tests yet.** The rules are easy to test without a browser:
  - an empty form can't search;
  - one character shows the message and blocks Search;
  - a city alone allows Search;
  - the server rejects a call with no criteria.
