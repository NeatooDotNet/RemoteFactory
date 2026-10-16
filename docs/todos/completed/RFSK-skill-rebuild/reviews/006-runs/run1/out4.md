I've written the full feature below; none of it has been compiled. It follows the Neatoo and RemoteFactory patterns, and the namespaces in the `using` lines are my best guess, so check them against the packages.

There are four projects. **zCRM.Dal** holds the row types and a store interface, with no EF reference. **zCRM.Domain** references Dal and is loaded by both the browser and the server. **zCRM.Server** holds EF Core and the services only the server has. **zCRM.Client** is the Blazor WebAssembly app.

---

## 1. zCRM.Dal: rows and the store interface

This is here because the domain is downloaded to the browser. It needs the row shapes and a store to ask, but it can't drag EF Core into the WebAssembly bundle. The interface goes here and the EF implementation goes on the server.

```csharp
// zCRM.Dal/VisitRows.cs
namespace zCRM.Dal;

public class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly VisitDate { get; set; }
    public string Salesperson { get; set; } = "";
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

/// <summary>
/// What the Visit aggregate's server operations need from the store.
/// Implemented by the EF Core DbContext on the server; never registered on the client.
/// </summary>
public interface IVisitDbContext
{
    Task<VisitRow?> FindVisitForDayAsync(Guid contactId, DateOnly visitDate, CancellationToken cancellationToken = default);
    Task<VisitRow?> FindVisitAsync(Guid id, CancellationToken cancellationToken = default);
    void AddVisit(VisitRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## 2. zCRM.Domain: the Visit aggregate

### What the domain needs from the server

The domain declares that it needs the signed-in salesperson's name. Only the server implements this. It is used only inside a server-only operation, so the client never has to resolve it.

```csharp
// zCRM.Domain/Visits/ICurrentSalesperson.cs
namespace zCRM.Domain.Visits;

/// <summary>The salesperson making the current request. Server-only.</summary>
public interface ICurrentSalesperson
{
    string Name { get; }
}
```

### Interfaces

Each type has a public interface and an `internal` concrete class. `IVisit` extends `IEntityRoot`, so it has `Save()` and `IsSavable`. `IActionItem` extends `IEntityBase`, so the compiler won't let you save a single action item on its own.

```csharp
// zCRM.Domain/Visits/IVisitInterfaces.cs
using Neatoo;

namespace zCRM.Domain.Visits;

/// <summary>Visit aggregate root: one contact, one day.</summary>
public interface IVisit : IEntityRoot
{
    Guid Id { get; }
    Guid ContactId { get; }
    DateOnly VisitDate { get; }
    string? Salesperson { get; }
    string? Notes { get; set; }
    IActionItemList? ActionItems { get; }
}

/// <summary>Action item within a Visit. Saved through the Visit.</summary>
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

```csharp
// zCRM.Domain/Visits/Visit.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public Visit(IEntityBaseServices<Visit> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial Guid ContactId { get; set; }
    public partial DateOnly VisitDate { get; set; }
    public partial string? Salesperson { get; set; }

    [DisplayName("Notes")]
    [StringLength(4000)]
    public partial string? Notes { get; set; }

    public partial IActionItemList? ActionItems { get; set; }

    /// <summary>
    /// The Today tab's seam: the contact's visit for the day if it is stored,
    /// otherwise a new one. One round trip; both branches run on the server.
    /// </summary>
    [Remote, Execute]
    public static async Task<IVisit> OpenToday(
        Guid contactId,
        DateOnly today,
        [Service] IVisitFactory factory)
    {
        return await factory.FetchForDay(contactId, today)
            ?? factory.Create(contactId, today);
    }

    /// <summary>
    /// Starts a visit. Server-only (internal, no [Remote]): the salesperson
    /// comes from the request's identity, and only OpenToday starts a visit.
    /// </summary>
    [Create]
    internal void Create(
        Guid contactId,
        DateOnly visitDate,
        [Service] ICurrentSalesperson salesperson,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        ContactId = contactId;
        VisitDate = visitDate;
        Salesperson = salesperson.Name;
        ActionItems = actionItemListFactory.Create();
    }

    /// <summary>The stored visit for the contact and day; false when there is none.</summary>
    [Fetch]
    internal async Task<bool> FetchForDay(
        Guid contactId,
        DateOnly visitDate,
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        var row = await db.FindVisitForDayAsync(contactId, visitDate);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        ContactId = row.ContactId;
        VisitDate = row.VisitDate;
        Salesperson = row.Salesperson;
        Notes = row.Notes;
        ActionItems = actionItemListFactory.Fetch(row.ActionItems);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        // Server gate against a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.CreateVersion7();

        var row = new VisitRow();
        MapTo(row);
        db.AddVisit(row);

        actionItemListFactory.Save(ActionItems!, row.ActionItems);

        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindVisitAsync(Id)
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
        row.Id = Id;
        row.ContactId = ContactId;
        row.VisitDate = VisitDate;
        row.Salesperson = Salesperson!;
        row.Notes = Notes;
    }
}
```

Why it's shaped this way:
- **Load-or-start is one `[Remote, Execute]`.** `OpenToday` calls Fetch first, then Create. The `[Create]` never searches the database and the `[Fetch]` never creates anything, so a loaded visit is never mistaken for a new one and inserted twice.
- **`Create` is `internal` with no `[Remote]`.** Starting a visit needs the salesperson's identity, which only the server can trust. Making it server-only also means no client code can start a visit any way except `OpenToday`.
- **`FetchForDay` is `internal`.** The client never calls it directly; it only reaches it through `OpenToday`.
- **There is no `[Delete]`.** The Today tab has no way to delete a visit. Add one if another screen needs it.

### ActionItemList

The list coordinates the child rows. It loads each item through that item's own `[Fetch]`. On save it adds, finds or removes rows and hands each item its row. Its operations are `internal` because the Visit's operations call them on the server.

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
        // Ids are version-7 Guids, so ordering by Id is creation order.
        foreach (var row in rows.OrderBy(r => r.Id))
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

### ActionItem

A child entity. Its `[Create]` is `public` and runs locally, so the browser can add an item with no server call. Its other operations are `internal`, take the row the list hands them, and write only that row.

```csharp
// zCRM.Domain/Visits/ActionItem.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItem : EntityBase<ActionItem>, IActionItem
{
    public ActionItem(IEntityBaseServices<ActionItem> services) : base(services) { }

    public partial Guid Id { get; set; }

    [DisplayName("Action item")]
    [Required(ErrorMessage = "Describe the action item")]
    [StringLength(500)]
    public partial string? Description { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; set; }

    /// <summary>
    /// A blank item for the salesperson to fill in. Rules run so the empty
    /// description shows its message at once and the Visit is not savable
    /// until it is filled.
    /// </summary>
    [Create]
    public async Task Create()
    {
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
        Id = Guid.CreateVersion7();
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

## 3. zCRM.Server

### The DbContext

Your DbContext may differ. What matters is that it implements `IVisitDbContext` and has the unique index on (contact, day). Without that index, two tabs that both start a visit for the same day would produce two rows; with it, the second save fails.

```csharp
// zCRM.Server/Data/CrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Dal;

namespace zCRM.Server.Data;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options)
    : DbContext(options), IVisitDbContext
{
    public DbSet<VisitRow> Visits => Set<VisitRow>();
    public DbSet<ActionItemRow> ActionItems => Set<ActionItemRow>();

    public Task<VisitRow?> FindVisitForDayAsync(Guid contactId, DateOnly visitDate, CancellationToken cancellationToken = default) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.ContactId == contactId && v.VisitDate == visitDate, cancellationToken);

    public Task<VisitRow?> FindVisitAsync(Guid id, CancellationToken cancellationToken = default) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public void AddVisit(VisitRow row) => Visits.Add(row);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitRow>(e =>
        {
            e.ToTable("Visit");
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).ValueGeneratedNever();
            e.Property(v => v.Salesperson).HasMaxLength(200).IsRequired();
            e.Property(v => v.Notes).HasMaxLength(4000);
            e.HasIndex(v => new { v.ContactId, v.VisitDate }).IsUnique();
            e.HasMany(v => v.ActionItems)
             .WithOne()
             .HasForeignKey(a => a.VisitId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemRow>(e =>
        {
            e.ToTable("ActionItem");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.Description).HasMaxLength(500).IsRequired();
        });
    }
}
```

`VisitId` is a required foreign key, so when the list removes an item's row from `row.ActionItems`, EF deletes that orphan when it saves.

### The salesperson's name

This is on the server because the name has to come from the authenticated request, not from anything the browser sends.

```csharp
// zCRM.Server/HttpCurrentSalesperson.cs
using System.Security.Claims;
using zCRM.Domain.Visits;

namespace zCRM.Server;

internal sealed class HttpCurrentSalesperson(IHttpContextAccessor httpContextAccessor) : ICurrentSalesperson
{
    public string Name =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("name")
        ?? httpContextAccessor.HttpContext?.User.Identity?.Name
        ?? throw new InvalidOperationException("No signed-in salesperson on this request.");
}
```

### Program.cs

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;
using zCRM.Domain.Visits;
using zCRM.Server;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Neatoo in Server mode: the generated factories run operations here, and
// this registers what UseNeatoo() needs. Do not also call AddNeatooAspNetCore
// after it (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IVisit).Assembly);

// The server-only services the Visit operations take as [Service] parameters.
builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddScoped<IVisitDbContext>(sp => sp.GetRequiredService<CrmDbContext>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentSalesperson, HttpCurrentSalesperson>();

builder.Services.AddAuthentication(); // configure your cookie/OIDC scheme here
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Sign the caller in before the endpoint runs; ICurrentSalesperson reads HttpContext.User.
app.UseAuthentication();
app.UseAuthorization();

// One endpoint carries every factory call; no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## 4. zCRM.Client

### Program.cs

The client registers no store and no salesperson service. If client code ever tried to resolve them, that would be a bug, and it would fail loudly.

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.Visits;
using zCRM.Domain.Visits;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Neatoo in Remote mode: [Remote] operations cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IVisit).Assembly);

// The HttpClient the factory proxies call through.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

// The browser's clock: "today" is the salesperson's local day.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddTransient<TodayVisitViewModel>();

await builder.Build().RunAsync();
```

### The ViewModel

It holds the live Visit, turns button clicks into calls on the domain, and saves. It doesn't decide anything itself: whether Save is allowed comes from `IsSavable`, and which visit to show comes from `OpenToday`. The one thing it supplies is today's date, read from the browser's clock. A server running on UTC would give an evening salesperson tomorrow's date.

```csharp
// zCRM.Client/Visits/TodayVisitViewModel.cs
using zCRM.Domain.Visits;

namespace zCRM.Client.Visits;

/// <summary>ViewModel for a contact's Today tab: holds today's Visit, saves it.</summary>
internal sealed class TodayVisitViewModel
{
    private readonly IVisitFactory _visitFactory;
    private readonly IActionItemFactory _actionItemFactory;
    private readonly TimeProvider _clock;

    public TodayVisitViewModel(IVisitFactory visitFactory, IActionItemFactory actionItemFactory, TimeProvider clock)
    {
        _visitFactory = visitFactory;
        _actionItemFactory = actionItemFactory;
        _clock = clock;
    }

    /// <summary>The aggregate the tab binds to. Null until opened.</summary>
    public IVisit? Visit { get; private set; }

    /// <summary>Set while a save is in flight (Save does not mark the entity busy - Neatoo#103).</summary>
    public bool IsSaving { get; private set; }

    /// <summary>Today's visit for the contact: the stored one, or a new one. One server call.</summary>
    public async Task Open(Guid contactId)
    {
        var today = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
        Visit = await _visitFactory.OpenToday(contactId, today);
    }

    /// <summary>Adds a blank action item. Local; no server call.</summary>
    public async Task AddActionItem()
    {
        if (Visit?.ActionItems is null)
        {
            return;
        }

        Visit.ActionItems.Add(await _actionItemFactory.Create());
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
                return;  // invalid or unchanged; the validation summary shows why
            }

            Visit = (IVisit)await Visit.Save();  // reassign: Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### The Today tab

This component goes inside the contact page's `MudTabPanel`. Its markup is just bindings. The fields show the entity's own validation messages, and the "Not saved yet" chip shows the entity's `IsNew` flag unchanged.

```razor
@* zCRM.Client/Visits/TodayTab.razor *@
@using zCRM.Domain.Visits
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@implements IDisposable
@inject TodayVisitViewModel Vm

@if (Vm.Visit is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    @* @key: Save returns a new instance; keying on it rebuilds the bound
       components so each field subscribes to the new instance's wrappers. *@
    <div @key="Vm.Visit">
        <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-3">
            <MudText Typo="Typo.h6">@Vm.Visit.VisitDate.ToString("D") · @Vm.Visit.Salesperson</MudText>
            @if (Vm.Visit.IsNew)
            {
                <MudChip T="string" Size="Size.Small" Color="Color.Info">Not saved yet</MudChip>
            }
        </MudStack>

        <NeatooValidationSummary Entity="@Vm.Visit" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Visit[nameof(IVisit.Notes)]"
                            Lines="6"
                            Sizing="InputSizing.Auto"
                            MaxLines="30"
                            UserAttributes="@(new() { ["spellcheck"] = "true" })"
                            Variant="Variant.Outlined"
                            Class="mb-4" />

        <MudText Typo="Typo.subtitle1" Class="mb-2">Action items</MudText>

        @if (Vm.Visit.ActionItems is not null)
        {
            @foreach (var item in Vm.Visit.ActionItems)
            {
                <MudGrid Spacing="2" Class="mb-1">
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

    private Guid? _openedFor;
    private IVisit? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (_openedFor == ContactId)
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
    }

    // The fields subscribe to their own properties; the Save button and the
    // header read the Visit, so the tab re-renders on any change in the graph.
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

On the contact page: `<MudTabPanel Text="Today"><TodayTab ContactId="@ContactId" /></MudTabPanel>`.

---

## Decisions to check

- **One visit per contact per day**, whoever the salesperson is. That's how I read "today's Visit for that contact", and the unique index enforces it. If each salesperson should have their own visit, add `Salesperson` to the lookup and to the index.
- **"Today" comes from the browser.** The server accepts that date as sent. If you'd rather the client couldn't pick the day, give salespeople a time zone and compute the date inside `OpenToday` on the server.
- **Two tabs starting the same new visit:** the second save hits the unique index and throws a `DbUpdateException`. An `ErrorBoundary` will show it, or you can catch it and reopen the visit.
- **A new, untouched visit can be saved.** `IsSavable` counts a new object as savable, so Save is enabled straight away and would store an empty visit. If you'd rather Save stayed disabled until the salesperson types something, that needs a rule on Visit.
- **Unsaved edits and tab switches:** by default `MudTabs` throws away a panel's component when you switch tabs, so unsaved edits are lost and the visit reloads. Set `KeepPanelsAlive="true"` if edits should survive a tab switch.
