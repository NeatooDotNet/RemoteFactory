I haven't compiled any of this. It follows the Neatoo and RemoteFactory patterns, but two generator details are unverified (see the end).

**How the rule works on both paths.** "A key account cannot be dormant" is one validation rule on the `Contact` entity. The edit screen and the nightly job both use that same class, so the rule runs in both. The job never checks `IsKeyAccount` itself. It calls a `MarkDormant()` method on each contact, and the rule decides whether the contact is saved or reported.

## Assumed (existing, not written here)

```cs
// ZCrm.Data (server-only project)
// ZCrmDbContext : DbContext { DbSet<Contact> Contacts; DbSet<Visit> Visits; }
// Contact { int Id; string Name; bool IsKeyAccount; bool IsDormant; }
// Visit   { int Id; int ContactId; DateTime VisitDate /* UTC */; }
```

I read "existing Contact entity" as the EF table class. The domain `Contact` below is a separate class that loads from and writes to that row.

---

## 1. Domain project (`ZCrm.Domain`, loaded by both client and server)

**Why here:** the rule has to run in the browser while someone edits, and on the server during the job. Only an assembly loaded on both sides can hold it.

**`Contact.cs`**: the contact entity, its `MarkDormant()` method and the rule.

```cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using ZCrm.Data;
using ContactRow = ZCrm.Data.Contact;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// Contact aggregate root.
/// </summary>
public interface IContact : IEntityRoot
{
    int Id { get; }
    string? Name { get; set; }
    bool IsKeyAccount { get; set; }
    bool IsDormant { get; set; }

    /// <summary>Marks the contact dormant. Whether that is allowed is the rules' answer, not this method's.</summary>
    void MarkDormant();
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial bool IsKeyAccount { get; set; }
    public partial bool IsDormant { get; set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddRule(new KeyAccountCannotBeDormantRule());
    }

    public void MarkDormant()
    {
        IsDormant = true;
    }

    [Create]
    public void Create()
    {
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(int id, [Service] ZCrmDbContext db, CancellationToken cancellationToken)
    {
        var row = await db.Contacts.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        Name = row.Name;
        IsKeyAccount = row.IsKeyAccount;
        IsDormant = row.IsDormant;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
    {
        await RefuseIfInvalid();

        var row = new ContactRow();
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();

        Id = row.Id;
    }

    [Remote, Update]
    internal async Task Update([Service] ZCrmDbContext db)
    {
        await RefuseIfInvalid();

        var row = await db.Contacts.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        await db.SaveChangesAsync();
    }

    // Server gate: a direct factory.Save skips IsSavable, so the rules run
    // again here. Throw, never return: after the operation returns, the
    // entity is marked saved.
    private async Task RefuseIfInvalid()
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }
    }

    private void MapTo(ContactRow row)
    {
        row.Name = Name ?? string.Empty;
        row.IsKeyAccount = IsKeyAccount;
        row.IsDormant = IsDormant;
    }
}

/// <summary>
/// A key account cannot be dormant. Runs when either flag changes, so it fires for
/// MarkDormant() in the job, for ticking Dormant on screen, and for ticking
/// Key account on a contact that is already dormant.
/// </summary>
internal sealed class KeyAccountCannotBeDormantRule : RuleBase<Contact>
{
    public KeyAccountCannotBeDormantRule() : base(t => t.IsKeyAccount, t => t.IsDormant) { }

    protected override IRuleMessages Execute(Contact target)
    {
        return new RuleMessages()
            .If(target.IsKeyAccount && target.IsDormant,
                nameof(Contact.IsDormant),
                "A key account cannot be dormant.");
    }
}
```

The rule takes two triggers, so it is a rule class: the inline `AddValidation` form accepts only one. Its message is attached to `IsDormant`, which is the flag to clear in either case.

**`DormancySweep.cs`**: the job itself, plus the report it returns.

```cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Data;
using ZCrm.Domain.Contacts;

namespace ZCrm.Domain.Jobs;

/// <summary>A contact the rules refused, with the messages a screen would have shown.</summary>
public sealed record RejectedContact(int ContactId, string? Name, IReadOnlyList<string> Messages);

/// <summary>What one nightly sweep did.</summary>
public interface IDormancySweep
{
    DateTime CutoffUtc { get; }
    IReadOnlyList<int> MarkedDormant { get; }
    IReadOnlyList<RejectedContact> Rejected { get; }
    IReadOnlyList<int> Unchanged { get; }
}

/// <summary>
/// Nightly job: marks contacts with no visit since the cutoff as dormant. It reuses
/// the Contact aggregate, its MarkDormant method and its rules.
/// </summary>
[Factory]
internal partial class DormancySweep : IDormancySweep
{
    public const int InactivityDays = 180;

    public DateTime CutoffUtc { get; internal set; }
    public IReadOnlyList<int> MarkedDormant { get; internal set; } = [];
    public IReadOnlyList<RejectedContact> Rejected { get; internal set; } = [];
    public IReadOnlyList<int> Unchanged { get; internal set; } = [];

    [Create]
    internal void Create(
        DateTime cutoffUtc,
        IReadOnlyList<int> markedDormant,
        IReadOnlyList<RejectedContact> rejected,
        IReadOnlyList<int> unchanged)
    {
        CutoffUtc = cutoffUtc;
        MarkedDormant = markedDormant;
        Rejected = rejected;
        Unchanged = unchanged;
    }

    // internal static, no [Remote]: server-only. The client cannot call it,
    // and its body is trimmed from the published WASM client.
    [Execute]
    internal static async Task<IDormancySweep> Run(
        DateTime asOfUtc,
        [Service] ZCrmDbContext db,
        [Service] IContactFactory contactFactory,
        [Service] IDormancySweepFactory sweepFactory,
        CancellationToken cancellationToken)
    {
        var cutoff = asOfUtc.AddDays(-InactivityDays);

        // The query selects by visits only. It does not filter out key accounts;
        // the contact's rule decides that.
        var candidateIds = await db.Contacts
            .Where(c => !c.IsDormant
                && !db.Visits.Any(v => v.ContactId == c.Id && v.VisitDate >= cutoff))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var marked = new List<int>();
        var rejected = new List<RejectedContact>();
        var unchanged = new List<int>();

        foreach (var id in candidateIds)
        {
            var contact = await contactFactory.Fetch(id, cancellationToken);
            if (contact is null)
            {
                continue;  // deleted since the query; nothing to mark
            }

            contact.MarkDormant();      // the rules run here
            await contact.WaitForTasks();

            if (contact.IsSavable)
            {
                contact = (IContact)await contact.Save();
                marked.Add(contact.Id);
            }
            else if (!contact.IsValid)
            {
                // Report it, do not save it, do not throw.
                rejected.Add(new RejectedContact(
                    contact.Id,
                    contact.Name,
                    contact.PropertyMessages.Select(m => $"{m.Property.Name}: {m.Message}").ToList()));
            }
            else
            {
                unchanged.Add(id);      // valid but not modified: someone marked it dormant meanwhile
            }
        }

        return sweepFactory.Create(cutoff, marked, rejected, unchanged);
    }
}
```

**Why it's a server-only `[Execute]` on a report class, not a `[Remote]` command:** no client ever calls this job. A `[Remote]` command would open a public endpoint that any browser could call. A plain static `[Execute]` would ship the EF query to the client. `internal static [Execute]` with no `[Remote]` runs only on the server and is trimmed from the client.

**`AssemblyInfo.cs`**: lets the server's background service call the internal `Run`.

```cs
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ZCrm.Server")]
```

**Project settings** that keep EF and the job off the client:

```xml
<!-- ZCrm.Domain.csproj -->
<PropertyGroup><IsTrimmable>true</IsTrimmable></PropertyGroup>
<ItemGroup>
  <PackageReference Include="Neatoo" />
  <PackageReference Include="Neatoo.RemoteFactory" />
  <ProjectReference Include="..\ZCrm.Data\ZCrm.Data.csproj" PrivateAssets="all" />
</ItemGroup>

<!-- ZCrm.Client.csproj -->
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />
  <TrimmableAssembly Include="ZCrm.Data" />
</ItemGroup>
```

---

## 2. How the job is started (`ZCrm.Server`)

**Why here:** the schedule belongs to the server host. It holds no business logic; it only calls `DormancySweep.Run` once a night.

**`NightlyDormancyJob.cs`**

```cs
using Microsoft.Extensions.Options;
using ZCrm.Domain.Jobs;

namespace ZCrm.Server.Jobs;

public sealed class DormancyJobOptions
{
    /// <summary>Time of day (UTC) the sweep runs.</summary>
    public TimeSpan RunAtUtc { get; set; } = new(2, 0, 0);
}

/// <summary>
/// Runs the dormancy sweep once a night, in its own DI scope (its own DbContext).
/// </summary>
internal sealed class NightlyDormancyJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;
    private readonly IOptions<DormancyJobOptions> _options;
    private readonly ILogger<NightlyDormancyJob> _logger;

    public NightlyDormancyJob(
        IServiceScopeFactory scopeFactory,
        TimeProvider clock,
        IOptions<DormancyJobOptions> options,
        ILogger<NightlyDormancyJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(UntilNextRun(), _clock, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnce(stoppingToken);
        }
    }

    private TimeSpan UntilNextRun()
    {
        var now = _clock.GetUtcNow();
        var next = new DateTimeOffset(now.UtcDateTime.Date + _options.Value.RunAtUtc, TimeSpan.Zero);
        if (next <= now)
        {
            next = next.AddDays(1);
        }
        return next - now;
    }

    private async Task RunOnce(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sweeps = scope.ServiceProvider.GetRequiredService<IDormancySweepFactory>();

        try
        {
            var report = await sweeps.Run(_clock.GetUtcNow().UtcDateTime, stoppingToken);

            _logger.LogInformation(
                "Dormancy sweep (cutoff {Cutoff:u}): {Marked} marked dormant, {Rejected} rejected, {Unchanged} unchanged",
                report.CutoffUtc, report.MarkedDormant.Count, report.Rejected.Count, report.Unchanged.Count);

            foreach (var r in report.Rejected)
            {
                _logger.LogWarning(
                    "Contact {ContactId} ({Name}) not marked dormant: {Messages}",
                    r.ContactId, r.Name, string.Join("; ", r.Messages));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Failures such as the database being down, not validation. An unhandled
            // exception in a BackgroundService stops the host; log it and try tomorrow.
            _logger.LogError(ex, "Dormancy sweep failed");
        }
    }
}
```

**`Program.cs`**

```cs
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Domain.Contacts;
using ZCrm.Server.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Existing: builder.Services.AddDbContext<ZCrmDbContext>(...);

// Neatoo in Server mode. Do not also call AddNeatooAspNetCore (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<DormancyJobOptions>(builder.Configuration.GetSection("DormancyJob"));
builder.Services.AddHostedService<NightlyDormancyJob>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## 3. The same rule on screen (`ZCrm.Client`)

**Why there's no screen-side rule code:** the page binds directly to the `Contact` entity. When someone ticks Dormant on a key account, the message appears and Save is disabled. The server checks the rules again before writing.

**`Program.cs`** (client registrations)

```cs
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();
```

**`ContactEditViewModel.cs`**

```cs
using ZCrm.Domain.Contacts;

namespace ZCrm.Client.ViewModels;

/// <summary>
/// ViewModel for the Contact edit screen: holds the aggregate and saves it.
/// </summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    public IContact? Contact { get; private set; }

    /// <summary>Save() does not mark the entity busy (Neatoo#103), so a save in flight blocks a second click.</summary>
    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public void New()
    {
        Contact = _contactFactory.Create();
    }

    public async Task Load(int id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    public async Task Save()
    {
        if (Contact is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Contact.WaitForTasks();
            if (!Contact.IsSavable)
            {
                return;  // the validation summary shows why
            }

            Contact = (IContact)await Contact.Save();  // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

**`Pages/ContactEdit.razor`**

```razor
@page "/contacts/new"
@page "/contacts/{Id:int}"
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using ZCrm.Client.ViewModels
@using ZCrm.Domain.Contacts
@implements IDisposable
@inject ContactEditViewModel Vm

@if (!_loaded)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (Vm.Contact is null)
{
    <MudText>Contact not found.</MudText>
}
else
{
    @* @key: Save returns a new instance; rebuild the fields against it. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Name)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsKeyAccount)]" Label="Key account" Class="mb-3" />
        <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Contact[nameof(IContact.IsDormant)]" Label="Dormant" Class="mb-3" />

        <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter] public int? Id { get; set; }

    private bool _loaded;
    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is int id)
        {
            await Vm.Load(id);
        }
        else
        {
            Vm.New();
        }

        _loaded = true;
        Resubscribe();
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    // The fields subscribe to their own properties; the Save button reads
    // IsSavable, so the page re-renders on the entity's change event.
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

---

## Decisions for you to confirm

1. **Key accounts show up in the nightly report.** "Contacts that fail validation are reported" covers them, so every key account with no visit in 180 days is listed every night with "A key account cannot be dormant." If you'd rather skip them quietly, add a `CanMarkDormant` property (computed by a rule) to `Contact`. The job would check it and list those contacts as skipped instead of rejected.
2. **Contacts that have never had a visit count as inactive.** That includes one created yesterday. If that's wrong, the query needs a created date to compare against.
3. **"Reported" currently means log entries.** If someone needs to act on them, save the `IDormancySweep` result to a table.
4. **Not covered by the spec:** nothing clears `IsDormant` when a new visit is recorded. Also, if the server runs on more than one instance, each one runs the job; you'd need a lock or a single designated instance.
5. **An invalid stored contact stops that night's run.** `Update` re-runs all rules before writing. If you later add other rules and a stored contact breaks one of them, `Update` throws. The rest of that night's run stops there, and the error is logged.

**Not verified:** I haven't built this. Two details of the code generator are untested:
- that the server project can call the internal `IDormancySweepFactory.Run` through `InternalsVisibleTo`;
- that a static `[Execute]` defined on a class accepts a `CancellationToken`.

The first build will settle both.
