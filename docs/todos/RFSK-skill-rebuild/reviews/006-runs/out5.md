I haven't compiled any of this, because you asked for code in the reply rather than files. The design follows the Neatoo and RemoteFactory skill references. The `using` namespaces (`Neatoo`, `Neatoo.RemoteFactory`, `Neatoo.Rules`) are my best guess, since those references never show them. Check them on the first build.

**Assumptions:** a `ZCrm.Data` project holds the EF `CrmDbContext`, with `Contact { Guid Id; string Name; bool IsKeyAccount; bool IsDormant }` and `Visit { Guid ContactId; DateTime VisitedOn /* UTC */ }`. Projects: `ZCrm.Domain` (loaded on both tiers; it references `ZCrm.Data`, which is trimmed off the client), `ZCrm.Server` and `ZCrm.Client`.

## How it works

There is **one rule**: a key account cannot be dormant. It lives on the Contact entity and runs whenever `IsKeyAccount` or `IsDormant` changes. On screen, a person ticking either box sees the message and the Save button stays disabled. In the job, `MarkDormant()` changes `IsDormant`, the same rule runs, and the job reads `IsSavable`. A contact that can be saved is saved. One that fails validation is reported with the rule's messages, not saved.

---

## 1. Domain (`ZCrm.Domain`): runs on both tiers

### `Contacts/DormancyPolicy.cs`
The 180 days is a business number, so it lives in the domain. The job's query reads it from here instead of hard-coding it.

```csharp
namespace ZCrm.Domain.Contacts;

/// <summary>Dormancy policy for contacts.</summary>
public static class DormancyPolicy
{
    /// <summary>A contact with no visit in this period is marked dormant by the nightly job.</summary>
    public static readonly TimeSpan InactivityPeriod = TimeSpan.FromDays(180);
}
```

### `Contacts/KeyAccountCannotBeDormantRule.cs`
`AddValidation` accepts only one trigger, and this check depends on two properties, so it is a `RuleBase<T>` class. The message goes on `IsDormant`, so a person who ticks "Key account" on a dormant contact sees it next to the Dormant box.

```csharp
using Neatoo.Rules;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// A key account cannot be dormant. Runs when either flag changes, so the nightly
/// job (through MarkDormant) and a person editing either checkbox get the same answer.
/// </summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule() : base(t => t.IsKeyAccount, t => t.IsDormant) { }

    protected override IRuleMessages Execute(Contact target)
    {
        if (target.IsKeyAccount && target.IsDormant)
        {
            return (nameof(Contact.IsDormant), "A key account cannot be dormant.").AsRuleMessages();
        }

        return None;
    }
}
```

### `Contacts/Contact.cs`
This is the aggregate root, shared by the screen and the job. `MarkDormant()` is a verb: it sets the flag and does no check and no throw, because deciding is the rule's job. `[Update]` re-runs the rules and refuses an invalid contact. That is the server's safety net against a caller that skips `IsSavable`; callers don't copy it. `[Fetch]` and `[Update]` take the DbContext as a `[Service]` parameter, so it is resolved only on the server.

```csharp
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Data;

namespace ZCrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }

    /// <summary>Marks the contact dormant. Whether that is allowed is the rules' answer.</summary>
    void MarkDormant();
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    // Public setter: a private setter is left out of serialization.
    public partial Guid Id { get; set; }

    [DisplayName("Name")]
    public partial string? Name { get; set; }

    [DisplayName("Key account")]
    public partial bool IsKeyAccount { get; set; }

    [DisplayName("Dormant")]
    public partial bool IsDormant { get; set; }

    public void MarkDormant()
    {
        IsDormant = true;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] CrmDbContext db)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (row is null)
        {
            return false;
        }

        // Paused inside the operation: a plain assignment is the clean baseline.
        Id = row.Id;
        Name = row.Name;
        IsKeyAccount = row.IsKeyAccount;
        IsDormant = row.IsDormant;
        return true;
    }

    [Remote, Update]
    internal async Task Update([Service] CrmDbContext db)
    {
        // Server gate: re-run every rule and refuse an invalid contact before writing.
        // Throw, never return: after [Update] returns the entity is marked saved.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.SingleOrDefaultAsync(c => c.Id == Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        row.Name = Name ?? string.Empty;
        row.IsKeyAccount = IsKeyAccount;
        row.IsDormant = IsDormant;

        await db.SaveChangesAsync();
    }
}
```

`[Create]`, `[Insert]` and `[Delete]` are left out because this feature doesn't touch them.

---

## 2. The job (`ZCrm.Server/Jobs`): server project only

The job is a plain class in the server project, not a `[Factory]` command in the domain. Only the server starts it and no call crosses tiers, so it doesn't need a factory. Making it one would either expose a `[Remote]` endpoint any client could trigger, or ship the job's code to the browser.

### `Jobs/DormancyJob.cs`
It finds candidates with one query, then loads each one through the Contact aggregate: verb, `WaitForTasks()`, then `IsSavable` decides. It doesn't call `RunRules` and doesn't catch validation; failures come back as messages, not exceptions. Each batch of contacts gets its own DI scope, so the DbContext's change tracker doesn't keep growing.

```csharp
using Microsoft.EntityFrameworkCore;
using ZCrm.Data;
using ZCrm.Domain.Contacts;

namespace ZCrm.Server.Jobs;

/// <summary>What one run did: data, not a success flag.</summary>
public sealed record DormancyReport(
    DateTimeOffset RanAt,
    IReadOnlyList<Guid> MarkedDormant,
    IReadOnlyList<RejectedContact> Rejected,
    IReadOnlyList<Guid> Unchanged,
    IReadOnlyList<Guid> NotFound);

/// <summary>A contact the rules refused, with the messages a screen would have shown.</summary>
public sealed record RejectedContact(Guid ContactId, string Name, IReadOnlyList<string> Messages);

/// <summary>Marks contacts with no visit within DormancyPolicy.InactivityPeriod as dormant.</summary>
internal sealed class DormancyJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<DormancyJob> logger)
{
    private const int BatchSize = 200;

    public async Task<DormancyReport> RunAsync(CancellationToken cancellationToken)
    {
        var ranAt = clock.GetUtcNow();
        var cutoff = ranAt.UtcDateTime - DormancyPolicy.InactivityPeriod;

        var candidateIds = await FindCandidatesAsync(cutoff, cancellationToken);

        var marked = new List<Guid>();
        var rejected = new List<RejectedContact>();
        var unchanged = new List<Guid>();
        var notFound = new List<Guid>();

        foreach (var batch in candidateIds.Chunk(BatchSize))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var contactFactory = scope.ServiceProvider.GetRequiredService<IContactFactory>();

            foreach (var id in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var contact = await contactFactory.Fetch(id);
                if (contact is null)
                {
                    notFound.Add(id); // deleted between the query and the load
                    continue;
                }

                // The verb sets IsDormant; KeyAccountCannotBeDormantRule runs now.
                contact.MarkDormant();
                await contact.WaitForTasks();

                if (contact.IsSavable)
                {
                    contact = (IContact)await contact.Save(); // Save returns the saved instance
                    marked.Add(contact.Id);
                }
                else if (!contact.IsValid)
                {
                    rejected.Add(new RejectedContact(
                        id,
                        contact.Name ?? string.Empty,
                        contact.PropertyMessages.Select(m => $"{m.Property.Name}: {m.Message}").ToList()));
                }
                else
                {
                    unchanged.Add(id); // valid, nothing changed
                }
            }
        }

        foreach (var r in rejected)
        {
            logger.LogWarning(
                "Dormancy: contact {ContactId} ({Name}) not saved: {Messages}",
                r.ContactId, r.Name, string.Join("; ", r.Messages));
        }

        logger.LogInformation(
            "Dormancy run at {RanAt}: {Marked} marked dormant, {Rejected} rejected, {Unchanged} unchanged, {NotFound} not found",
            ranAt, marked.Count, rejected.Count, unchanged.Count, notFound.Count);

        return new DormancyReport(ranAt, marked, rejected, unchanged, notFound);
    }

    /// <summary>
    /// Contacts not already dormant with no visit on or after the cutoff.
    /// Key accounts are NOT filtered here: the rule decides, and they come back as Rejected.
    /// </summary>
    private async Task<List<Guid>> FindCandidatesAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        return await db.Contacts
            .AsNoTracking()
            .Where(c => !c.IsDormant)
            .Where(c => !db.Visits.Any(v => v.ContactId == c.Id && v.VisitedOn >= cutoffUtc))
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }
}
```

### `Jobs/DormancyJobScheduler.cs`: how it is started
This is a `BackgroundService` that sleeps until the configured time each night, then runs the job. It catches a failed run so one bad night doesn't stop the host. Contacts saved before the failure stay saved, and the next run picks up the rest.

```csharp
using Microsoft.Extensions.Options;

namespace ZCrm.Server.Jobs;

public sealed class DormancyJobOptions
{
    /// <summary>Local server time the job runs each night.</summary>
    public TimeOnly RunAt { get; set; } = new(2, 0);
}

internal sealed class DormancyJobScheduler(
    DormancyJob job,
    IOptions<DormancyJobOptions> options,
    TimeProvider clock,
    ILogger<DormancyJobScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = UntilNextRun(clock.GetLocalNow(), options.Value.RunAt);
            logger.LogInformation("Dormancy job next runs in {Delay}", delay);

            try
            {
                await Task.Delay(delay, clock, stoppingToken);
                await job.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dormancy job failed; it runs again at the next scheduled time");
            }
        }
    }

    private static TimeSpan UntilNextRun(DateTimeOffset nowLocal, TimeOnly runAt)
    {
        var next = nowLocal.Date + runAt.ToTimeSpan();
        if (next <= nowLocal.DateTime)
        {
            next = next.AddDays(1);
        }

        return next - nowLocal.DateTime;
    }
}
```

### `Program.cs` (server)
Neatoo is registered in Server mode, which also registers what `UseNeatoo()` needs. Don't add `AddNeatooAspNetCore` after it; that order breaks remote saves (RemoteFactory#117). `TimeProvider` is injected so tests can control the clock.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Data;
using ZCrm.Domain.Contacts;
using ZCrm.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Swap UseSqlServer for whichever provider CrmDbContext already uses.
builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.Configure<DormancyJobOptions>(builder.Configuration.GetSection("DormancyJob"));
builder.Services.AddSingleton<DormancyJob>();
builder.Services.AddHostedService<DormancyJobScheduler>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

`appsettings.json`: `"DormancyJob": { "RunAt": "02:00" }`. For the query, add an index on `Visits(ContactId, VisitedOn)`.

---

## 3. Screen (`ZCrm.Client`): the same rule, with no extra code

The page binds to the entity, and the ViewModel only loads and saves. There is no key-account check in the UI, because the entity already reports it as a validation message.

### `ViewModels/ContactEditViewModel.cs`
`IsSaving` disables the Save button while a save is running. Without it, a double-click sends two saves (Neatoo#103).

```csharp
using ZCrm.Domain.Contacts;

namespace ZCrm.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel(IContactFactory contactFactory)
{
    public IContact? Contact { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task Load(Guid id)
    {
        Contact = await contactFactory.Fetch(id);
        IsLoaded = true;
    }

    public async Task Save()
    {
        if (Contact is null)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Contact.WaitForTasks();
            if (!Contact.IsSavable)
            {
                return; // the validation summary already shows why
            }

            Contact = (IContact)await Contact.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### `Pages/ContactEdit.razor`
```razor
@page "/contacts/{Id:guid}"
@using ZCrm.Client.ViewModels
@using ZCrm.Domain.Contacts
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@implements IDisposable
@inject ContactEditViewModel Vm

@if (!Vm.IsLoaded)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (Vm.Contact is null)
{
    <MudText>Contact not found.</MudText>
}
else
{
    @* @key: Save returns a new instance; rebuild the bound components for it. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Name)]" Class="mb-3" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsKeyAccount)]" Label="Key account" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsDormant)]" Label="Dormant" Class="mb-3" />

        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter] public Guid Id { get; set; }

    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        await Vm.Load(Id);
        Resubscribe();
    }

    // The Save button reads the entity, so the page re-renders on its changes.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    private async Task SaveAsync()
    {
        var saving = Vm.Save();
        StateHasChanged(); // show the button disabled while saving
        await saving;
        Resubscribe();
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```

### `Program.cs` (client)
Neatoo runs in Remote mode on the client, so `Fetch` and `Save` are sent to the server.

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Client;
using ZCrm.Client.ViewModels;
using ZCrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

---

## Decisions for you to confirm

1. **Key accounts are reported every night, not filtered out.** The query leaves them in, so each unvisited key account is refused by the rule and appears under `Rejected` ("A key account cannot be dormant."). That keeps the rule in one place, and the list also shows which key accounts nobody has visited. If you'd rather not see them, add `!c.IsKeyAccount` to the query. The rule still covers the screen, but the job's selection then repeats it.
2. **Contacts that have never had a visit are marked dormant.** I read "no visit in 180 days" literally. If a contact created last week shouldn't qualify, the query needs a creation date, which your `Contact` row may not have.
3. **Bad data already in the database stops the run.** Loading a contact doesn't run its rules; stored data is assumed valid. If you later add other rules to `Contact` (a required field, say) and old rows break them, the job's `IsSavable` check will pass. The `[Update]` safety check will then throw, and the run stops there. That run is logged as failed; earlier saves stay saved.
4. **Multiple server instances.** If the server runs more than one copy, each copy runs the job. The results are the same, but the work is duplicated. If that matters, run the scheduler on one instance only or add a distributed lock.
