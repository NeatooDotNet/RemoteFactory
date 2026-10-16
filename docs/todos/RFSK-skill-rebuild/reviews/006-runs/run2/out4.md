I haven't compiled any of this; you asked for code in the reply, not files. It follows the Neatoo, RemoteFactory and MudNeatoo patterns.

**How it works:** opening the tab makes one server call, `OpenToday`. On the server it either loads today's visit for the contact or creates a new one. The salesperson's name comes from the signed-in user on the server, so the browser can't fill in someone else's. The tab edits the visit in the browser, and Save sends the whole visit back. The visit then writes its own row and each action item writes its own row.

## Solution layout

| Project | References | Why |
|---|---|---|
| `zCRM.Dal` | nothing | Table classes and the data-access interface, with no EF Core, so the browser app can reference the domain without pulling in EF |
| `zCRM.Domain` | Dal | The visit and action-item classes. Both tiers load this project. |
| `zCRM.Ef` | Dal | The EF Core `DbContext`. Only the server uses it. |
| `zCRM.Server` | Domain, Ef, Client | Hosts the API and the Blazor app |
| `zCRM.Client` | Domain | The Blazor WebAssembly app |

Packages: `Neatoo` and `Neatoo.RemoteFactory` (Domain), `Neatoo.RemoteFactory.AspNetCore` (Server), `MudBlazor` and `Neatoo.Blazor.MudNeatoo` (Client), and EF Core with your provider (Ef). I haven't pinned versions because I didn't look them up on NuGet.

---

## zCRM.Dal

**Why here:** the domain's server-side code needs the table classes and a small interface to the database. Neither may bring EF Core into the browser.

```csharp
// zCRM.Dal/VisitRow.cs
namespace zCRM.Dal;

public class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly VisitDate { get; set; }
    public string SalespersonName { get; set; } = "";
    public string? Notes { get; set; }
    public List<ActionItemRow> ActionItems { get; set; } = [];
}

public class ActionItemRow
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public string Description { get; set; } = "";
    public bool IsDone { get; set; }
}
```

```csharp
// zCRM.Dal/IVisitDb.cs
namespace zCRM.Dal;

/// <summary>
/// The Visit aggregate's store. Server-only; every query includes the action item rows.
/// </summary>
public interface IVisitDb
{
    Task<VisitRow?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VisitRow?> FindForDayAsync(Guid contactId, DateOnly visitDate, CancellationToken cancellationToken = default);
    void Add(VisitRow row);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## zCRM.Domain

### The signed-in salesperson

**Why here:** the open-the-visit code needs this, so the interface lives in the domain. Only the server implements it, and only server code asks for it.

```csharp
// zCRM.Domain/ICurrentSalesperson.cs
namespace zCRM.Domain;

/// <summary>
/// The signed-in salesperson. Server-only: read from the request's user.
/// </summary>
public interface ICurrentSalesperson
{
    string Name { get; }
}
```

### Interfaces

**Why here:** every reference uses the interface, and the classes stay `internal`. The visit's interface extends `IEntityRoot`, which has `Save()`. The action item's extends `IEntityBase`, which has no `Save()`. So calling `Save()` on an action item by itself won't compile.

```csharp
// zCRM.Domain/Visits/IVisit.cs
using Neatoo;

namespace zCRM.Domain.Visits;

/// <summary>
/// One salesperson-day with a contact. Date, contact and salesperson are set when
/// the visit starts and are not edited.
/// </summary>
public interface IVisit : IEntityRoot
{
    Guid Id { get; }
    Guid ContactId { get; }
    DateOnly VisitDate { get; }
    string? SalespersonName { get; }
    string? Notes { get; set; }
    IActionItemList? ActionItems { get; }
}

public interface IActionItem : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    bool IsDone { get; set; }
}

public interface IActionItemList : IEntityListBase<IActionItem>
{
}
```

### Visit

**Why here:** the visit decides how it is opened, loaded and saved:

- **`OpenToday` (load or create)** is a `[Remote, Execute]` the browser calls. On the server it asks for today's visit and creates one only if there is none.
- **`FetchForDay` and `Create` are `internal` with no `[Remote]`**, so only the server can call them. That is what guarantees the name comes from the signed-in user.
- **`Insert` and `Update`** write only the visit's own row. They hand the action item rows to the list and save to the database once.

```csharp
// zCRM.Domain/Visits/Visit.cs
using System.ComponentModel;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public partial Guid Id { get; set; }
    public partial Guid ContactId { get; set; }
    public partial DateOnly VisitDate { get; set; }

    [DisplayName("Salesperson")]
    public partial string? SalespersonName { get; set; }

    [DisplayName("Notes")]
    public partial string? Notes { get; set; }

    public partial IActionItemList? ActionItems { get; set; }

    public Visit(IEntityBaseServices<Visit> services) : base(services)
    {
    }

    /// <summary>
    /// The Today tab's seam: the contact's visit for the given day, or a new one
    /// started for the signed-in salesperson. One round trip.
    /// </summary>
    /// <remarks>
    /// The day comes from the caller because "today" is the salesperson's local
    /// date, which the browser knows and the server does not.
    /// </remarks>
    [Remote, Execute]
    public static async Task<IVisit> OpenToday(
        Guid contactId,
        DateOnly today,
        [Service] IVisitFactory factory,
        [Service] ICurrentSalesperson salesperson)
    {
        return await factory.FetchForDay(contactId, today)
            ?? factory.Create(contactId, today, salesperson.Name);
    }

    /// <summary>
    /// Starts a visit. Server-only: the salesperson's name is the signed-in
    /// user's, so the client cannot start one itself.
    /// </summary>
    [Create]
    internal void Create(
        Guid contactId,
        DateOnly visitDate,
        string salespersonName,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        ContactId = contactId;
        VisitDate = visitDate;
        SalespersonName = salespersonName;
        ActionItems = actionItemListFactory.Create();
    }

    /// <summary>
    /// Loads the contact's visit for the day; false when there is none. Server-only.
    /// </summary>
    [Fetch]
    internal async Task<bool> FetchForDay(
        Guid contactId,
        DateOnly visitDate,
        [Service] IVisitDb db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        var row = await db.FindForDayAsync(contactId, visitDate);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        ContactId = row.ContactId;
        VisitDate = row.VisitDate;
        SalespersonName = row.SalespersonName;
        Notes = row.Notes;
        ActionItems = actionItemListFactory.Fetch(row.ActionItems);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] IVisitDb db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        // Server gate against a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();
        var row = new VisitRow { Id = Id };
        MapTo(row);
        db.Add(row);

        actionItemListFactory.Save(ActionItems!, row.ActionItems);

        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] IVisitDb db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        actionItemListFactory.Save(ActionItems!, row.ActionItems);

        await db.SaveChangesAsync();
    }

    private void MapTo(VisitRow row)
    {
        row.ContactId = ContactId;
        row.VisitDate = VisitDate;
        row.SalespersonName = SalespersonName!;
        row.Notes = Notes;
    }
}
```

### ActionItem

**Why here:**

- **`Create` is `public` and runs locally**, so the browser can add an item without calling the server.
- **The other operations are `internal`, never `[Remote]`**, because they only run inside the visit's save, which is already on the server.
- **The "required" rule** lives on the item, so the message shows next to the field.
- **`Create` runs the rules**, so a blank new row shows its message right away and Save stays disabled until it's filled in.

```csharp
// zCRM.Domain/Visits/ActionItem.cs
using System.ComponentModel;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItem : EntityBase<ActionItem>, IActionItem
{
    public partial Guid Id { get; set; }

    [DisplayName("Action item")]
    public partial string? Description { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; set; }

    public ActionItem(IEntityBaseServices<ActionItem> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Description) ? "Describe the action item" : string.Empty,
            t => t.Description);
    }

    /// <summary>
    /// A blank action item. Local: the client adds one with no server call.
    /// </summary>
    [Create]
    public async Task Create()
    {
        // A person fills this row in: run the rules so the empty description
        // shows its message and the visit is unsavable until it is filled.
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(ActionItemRow row)
    {
        Id = row.Id;
        Description = row.Description;
        IsDone = row.IsDone;
    }

    [Insert]
    internal void Insert(ActionItemRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(ActionItemRow row)
    {
        MapTo(row);
    }

    private void MapTo(ActionItemRow row)
    {
        row.Id = Id;
        row.Description = Description!;
        row.IsDone = IsDone;
    }
}
```

### ActionItemList

**Why here:** the list keeps the visit's action item rows in step with what the screen holds. It removes rows for deleted items, makes rows for new ones and hands each item its own row. It never fills in an item's columns itself.

```csharp
// zCRM.Domain/Visits/ActionItemList.cs
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItemList : EntityListBase<IActionItem>, IActionItemList
{
    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<ActionItemRow> rows, [Service] IActionItemFactory actionItemFactory)
    {
        foreach (var row in rows)
        {
            Add(actionItemFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<ActionItemRow> rows, [Service] IActionItemFactory actionItemFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            if (item.IsDeleted)
            {
                if (!item.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == item.Id));
                }
            }
            else if (item.IsNew)
            {
                var row = new ActionItemRow();
                rows.Add(row);
                actionItemFactory.Save(item, row);
            }
            else if (item.IsModified)
            {
                actionItemFactory.Save(item, rows.Single(r => r.Id == item.Id));
            }
        }
    }
}
```

---

## zCRM.Ef

**Why here:** this is the only project that knows about EF Core.

- **`ValueGeneratedNever`**: the classes set their own ids, so EF must not generate them. EF then treats an action item row added to a visit's collection as a new row.
- **Removing a row from `ActionItems` deletes it**, because the relationship is required (EF deletes the orphan).
- **The unique index** on (contact, date) stops two open tabs from both inserting a visit for the same day.

```csharp
// zCRM.Ef/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Dal;

namespace zCRM.Ef;

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options), IVisitDb
{
    public DbSet<VisitRow> Visits => Set<VisitRow>();
    public DbSet<ActionItemRow> ActionItems => Set<ActionItemRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitRow>(visit =>
        {
            visit.HasKey(v => v.Id);
            visit.Property(v => v.Id).ValueGeneratedNever();
            visit.Property(v => v.SalespersonName).HasMaxLength(200).IsRequired();
            visit.HasIndex(v => new { v.ContactId, v.VisitDate }).IsUnique();
            visit.HasMany(v => v.ActionItems)
                 .WithOne()
                 .HasForeignKey(a => a.VisitId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemRow>(item =>
        {
            item.HasKey(a => a.Id);
            item.Property(a => a.Id).ValueGeneratedNever();
            item.Property(a => a.Description).HasMaxLength(500).IsRequired();
        });
    }

    public Task<VisitRow?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<VisitRow?> FindForDayAsync(Guid contactId, DateOnly visitDate, CancellationToken cancellationToken = default) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.ContactId == contactId && v.VisitDate == visitDate, cancellationToken);

    void IVisitDb.Add(VisitRow row) => Visits.Add(row);

    Task IVisitDb.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
```

---

## zCRM.Server

**Why here:** the services that only exist on the server (the database and the signed-in user) are registered here, and `UseNeatoo()` maps the single endpoint every call goes through. I use `AddNeatooServices(Server)` and not `AddNeatooAspNetCore`: registering both, in the wrong order, breaks every save from the browser (RemoteFactory#117).

```csharp
// zCRM.Server/HttpContextSalesperson.cs
using zCRM.Domain;

namespace zCRM.Server;

internal sealed class HttpContextSalesperson(IHttpContextAccessor accessor) : ICurrentSalesperson
{
    public string Name =>
        accessor.HttpContext?.User.Identity?.Name
        ?? throw new InvalidOperationException("No signed-in salesperson on this request.");
}
```

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Dal;
using zCRM.Domain;
using zCRM.Domain.Visits;
using zCRM.Ef;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IVisit).Assembly);

builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IVisitDb>(sp => sp.GetRequiredService<ZCrmDbContext>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentSalesperson, HttpContextSalesperson>();

builder.Services.AddAuthentication(); // zCRM's sign-in scheme goes here
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## zCRM.Client

### Program.cs

**Why here:** in the browser, calls marked `[Remote]` go to the server. `TimeProvider` is registered so the view model reads the salesperson's local date in a way tests can control. Nothing server-only is registered.

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.Visits;
using zCRM.Domain.Visits;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IVisit).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTransient<TodayVisitViewModel>();

await builder.Build().RunAsync();
```

### TodayVisitViewModel

**Why here:** this class only turns the salesperson's clicks into calls on the visit. The visit itself answers whether it's valid, changed, or ready to save.

- **Save** waits for any rules still running, asks the visit whether it can be saved, and keeps the copy that `Save()` returns.
- **The `IsSaving` flag** exists because `Save()` doesn't mark the visit busy, so a double-click would otherwise send two inserts (Neatoo#103).

```csharp
// zCRM.Client/Visits/TodayVisitViewModel.cs
using zCRM.Domain.Visits;

namespace zCRM.Client.Visits;

/// <summary>
/// ViewModel for a contact's Today tab: opens today's visit, adds and removes
/// action items, saves.
/// </summary>
internal sealed class TodayVisitViewModel(
    IVisitFactory visitFactory,
    IActionItemFactory actionItemFactory,
    TimeProvider clock)
{
    /// <summary>The visit the tab binds to. Null until opened.</summary>
    public IVisit? Visit { get; private set; }

    public bool IsSaving { get; private set; }

    public async Task Open(Guid contactId)
    {
        // The salesperson's local date; the browser's time zone, not the server's.
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        Visit = await visitFactory.OpenToday(contactId, today);
    }

    public async Task AddActionItem()
    {
        if (Visit?.ActionItems is null)
        {
            return;
        }

        Visit.ActionItems.Add(await actionItemFactory.Create());
    }

    public void RemoveActionItem(IActionItem item)
    {
        Visit?.ActionItems?.Remove(item);
    }

    public bool CanSave => !IsSaving && (Visit?.IsSavable ?? false);

    public async Task Save()
    {
        if (Visit is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Visit.WaitForTasks();

            if (!Visit.IsSavable)
            {
                return;
            }

            Visit = (IVisit)await Visit.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### _Imports.razor

```razor
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zCRM.Domain.Visits
@using zCRM.Client.Visits
```

### ContactTodayTab.razor

**Why here:** this is the tab's loading and saving code.

- **Loading:** it opens the visit when the contact changes.
- **Live updates:** it subscribes to the visit's change event, so the Save button updates as fields change, and re-subscribes after each save.
- **`@key`:** each save returns a new visit object, and keying on it rebuilds the fields so they bind to the new copy.
- **No page logic:** every field shows the visit's own validation messages, and the Save button follows `CanSave`.

```razor
@* zCRM.Client/Visits/ContactTodayTab.razor *@
@implements IDisposable
@inject TodayVisitViewModel Vm

@if (Vm.Visit is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    @* Save returns a new instance; keying on it rebuilds the bound fields. *@
    <div @key="Vm.Visit">
        <MudText Typo="Typo.h6">@Vm.Visit.VisitDate.ToString("D")</MudText>
        <MudText Typo="Typo.body2" Class="mb-3">Salesperson: @Vm.Visit.SalespersonName</MudText>

        <NeatooValidationSummary Entity="@Vm.Visit" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Visit[nameof(IVisit.Notes)]"
                            Variant="Variant.Outlined"
                            Lines="5"
                            Sizing="InputSizing.Auto"
                            MaxLines="20"
                            UserAttributes="@(new() { ["spellcheck"] = "true" })"
                            Class="mb-4" />

        <MudText Typo="Typo.subtitle1" Class="mb-2">Action items</MudText>

        @if (Vm.Visit.ActionItems is not null)
        {
            @foreach (var item in Vm.Visit.ActionItems)
            {
                <MudGrid @key="item" Spacing="2" Class="mb-1">
                    <MudItem xs="2" sm="1" Class="d-flex align-center">
                        <MudNeatooCheckBox T="bool" EntityProperty="@item[nameof(IActionItem.IsDone)]" />
                    </MudItem>
                    <MudItem xs="8" sm="10">
                        <MudNeatooTextField T="string" EntityProperty="@item[nameof(IActionItem.Description)]" />
                    </MudItem>
                    <MudItem xs="2" sm="1" Class="d-flex align-center">
                        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                       OnClick="@(() => Vm.RemoveActionItem(item))" />
                    </MudItem>
                </MudGrid>
            }
        }

        <MudButton Variant="Variant.Text"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="AddActionItemAsync"
                   Class="mb-4">
            Add action item
        </MudButton>

        <div>
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       Disabled="@(!Vm.CanSave)"
                       OnClick="SaveAsync">
                Save
            </MudButton>
        </div>
    </div>
}

@code {
    [Parameter, EditorRequired] public Guid ContactId { get; set; }

    private Guid _openedFor;
    private IVisit? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (ContactId == _openedFor)
        {
            return;
        }

        _openedFor = ContactId;
        await Vm.Open(ContactId);
        Resubscribe();
    }

    private async Task AddActionItemAsync()
    {
        await Vm.AddActionItem();
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    // The fields subscribe to their own properties; the Save button reads the
    // visit, so the tab re-renders on any change in the visit's graph.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnVisitChanged;
        }

        _subscribed = Vm.Visit;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnVisitChanged;
        }
    }

    private Task OnVisitChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnVisitChanged;
        }
    }
}
```

### ContactPage.razor (host)

**Why here:** `KeepPanelsAlive` should keep the Today tab mounted when the salesperson switches tabs, so unsaved notes survive. My understanding is that MudTabs otherwise unmounts panels that aren't showing; I haven't checked that.

```razor
@* zCRM.Client/Contacts/ContactPage.razor *@
@page "/contacts/{ContactId:guid}"

<MudTabs KeepPanelsAlive="true">
    <MudTabPanel Text="Details">
        @* contact details *@
    </MudTabPanel>
    <MudTabPanel Text="Today">
        <ContactTodayTab ContactId="ContactId" />
    </MudTabPanel>
</MudTabs>

@code {
    [Parameter] public Guid ContactId { get; set; }
}
```

---

## Decisions to check

- **One visit per contact per day, shared by all salespeople.** That's how I read "today's Visit for that contact", and the unique index enforces it. If each salesperson should have their own visit, add the salesperson to `FindForDayAsync` and the index.
- **The browser sends "today".** The salesperson's local date is the right meaning, and the server doesn't know their time zone. The catch is that a modified client could open any date's visit through this call.
- **Not covered:**
  - Sign-in itself, including `[AspAuthorize]` on these operations.
  - Ordering action items: rows load in whatever order the database returns, so add a sort column if order matters.
  - Showing a friendly message when a save fails, such as a unique-index clash between two tabs. Right now the exception reaches Blazor's error UI.
  - Deleting a visit.
  - The `App.razor` and layout from the standard template.
- **Unverified names:** the namespaces for `RunRulesFlag`, `SaveOperationException` and `NeatooPropertyChangedEventArgs` (I assumed `Neatoo`), and the MudTabs behaviour above, need confirming by the first build.
