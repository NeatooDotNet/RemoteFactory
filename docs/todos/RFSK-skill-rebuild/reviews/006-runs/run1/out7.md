I've written the merge feature in five parts: a `Contact` aggregate, an editing model that holds the merge choices, one server operation that does the merge in a transaction, a server-side contact search, and the page with its ViewModel. I haven't compiled any of it, because you asked me not to create files. I checked the Neatoo API details I wasn't sure of against the local Neatoo source: namespaces, the `AddAction` trigger overloads, and `SaveOperationException`.

## Design in one paragraph

The **choices** are what the person edits, any number of times, so they live in `ContactMerge`, an editing model with rules. Its rules work out the merged result as the person clicks. Nothing is saved until they confirm, so it needs no database lifecycle. The **merge itself** updates one contact and deletes another, so it is one `[Remote, Execute]` on `Contact` that returns the contact that survives. Inside one transaction it loads both contacts, applies the chosen values, deletes the duplicate and saves the survivor, each contact writing its own row. The page binds to what the domain computes and makes no decisions of its own.

---

## Domain: `zCRM.Domain` (referenced by both client and server)

**`Data/ContactRow.cs`, `Data/ICrmDbContext.cs`**: the database operations take this interface as a `[Service]` parameter, so they get the server's DbContext without the domain referencing the server project. The client trims these server-only paths out.

```csharp
namespace zCRM.Domain.Data;

/// <summary>Row in the Contacts table.</summary>
public class ContactRow
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace zCRM.Domain.Data;

/// <summary>The part of the CRM DbContext the domain's server operations use.</summary>
public interface ICrmDbContext
{
    DbSet<ContactRow> Contacts { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**`Contacts/ContactCard.cs`**: a record that crosses the wire as-is. It is each side's snapshot on the screen, the search result, and the merged values the server applies.

```csharp
namespace zCRM.Domain.Contacts;

/// <summary>
/// Value object for a contact's mergeable values: each side of the merge,
/// a search result, and the merged values applied to the kept contact.
/// </summary>
public sealed record ContactCard(
    Guid Id,
    string? FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Company,
    string? JobTitle)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Email)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {LastName} <{Email}>".Trim();
}
```

**`Contacts/IContactSearch.cs`**: the picker calls this search service. It is not a repository; its implementation runs on the server.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>Finds contacts by name or email for the merge pickers.</summary>
[Factory]
public interface IContactSearch
{
    Task<IReadOnlyList<ContactCard>> Find(string text);
}
```

**`Contacts/Contact.cs`**: the aggregate root. It owns its own rules, its row, and the merge operation, because the merge's result is this aggregate.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using zCRM.Domain.Data;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    string? Phone { get; set; }
    string? Company { get; set; }
    string? JobTitle { get; set; }

    /// <summary>Takes the values a merge settled on.</summary>
    void ApplyMergedValues(ContactCard values);
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    public partial Guid Id { get; set; }

    [DisplayName("First name")]
    [StringLength(100)]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name")]
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public partial string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "Email is not a valid address")]
    [StringLength(256)]
    public partial string? Email { get; set; }

    [StringLength(50)]
    public partial string? Phone { get; set; }

    [StringLength(200)]
    public partial string? Company { get; set; }

    [DisplayName("Job title")]
    [StringLength(100)]
    public partial string? JobTitle { get; set; }

    // Every rule here is per-field, so a merge of two stored (valid) contacts
    // is valid whichever side each field comes from. A cross-field rule added
    // later can be broken by a merge; Update's gate refuses it and the
    // transaction rolls back.
    public void ApplyMergedValues(ContactCard values)
    {
        FirstName = values.FirstName;
        LastName = values.LastName;
        Email = values.Email;
        Phone = values.Phone;
        Company = values.Company;
        JobTitle = values.JobTitle;
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] ICrmDbContext db)
    {
        var row = await db.Contacts.FindAsync(id);
        if (row is null)
            return false;

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        Phone = row.Phone;
        Company = row.Company;
        JobTitle = row.JobTitle;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] ICrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
            throw new SaveOperationException(SaveFailureReason.IsInvalid);

        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] ICrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
            throw new SaveOperationException(SaveFailureReason.IsInvalid);

        var row = await db.Contacts.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        await db.SaveChangesAsync();
    }

    [Remote, Delete]
    internal async Task Delete([Service] ICrmDbContext db)
    {
        var row = await db.Contacts.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        db.Contacts.Remove(row);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Keeps <paramref name="merged"/>.Id with the merged values and deletes
    /// <paramref name="duplicateId"/>, in one transaction. Returns the kept contact.
    /// </summary>
    /// <remarks>
    /// Runs on the server; the Fetch and Save calls inside it are local and share
    /// the request's scoped DbContext, so each contact's own SaveChangesAsync
    /// joins this transaction. The values applied are the ones the person saw
    /// and confirmed, not values re-resolved from the current rows.
    /// </remarks>
    [Remote, Execute]
    public static async Task<IContact> MergeDuplicate(
        ContactCard merged,
        Guid duplicateId,
        [Service] IContactFactory factory,
        [Service] ICrmDbContext db)
    {
        if (merged.Id == duplicateId)
            throw new ArgumentException("A contact cannot be merged into itself.", nameof(duplicateId));

        await using var transaction = await db.Database.BeginTransactionAsync();

        var kept = await factory.Fetch(merged.Id)
            ?? throw new KeyNotFoundException($"Contact {merged.Id} not found");
        var duplicate = await factory.Fetch(duplicateId)
            ?? throw new KeyNotFoundException($"Contact {duplicateId} not found");

        // The duplicate goes first: the kept contact may be taking its email,
        // and a unique index on Email refuses the update while both rows exist.
        duplicate.Delete();
        await factory.Save(duplicate);

        kept.ApplyMergedValues(merged);
        kept = await factory.Save(kept)
            ?? throw new InvalidOperationException("Saving the kept contact returned nothing.");

        await transaction.CommitAsync();
        return kept;
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName;
        row.LastName = LastName!;
        row.Email = Email;
        row.Phone = Phone;
        row.Company = Company;
        row.JobTitle = JobTitle;
    }
}
```

**`Contacts/ContactMerge.cs`**: the editing model for the merge screen. Its rules (no merging a contact with itself, default choices, the merged preview, and `CanMerge`) live here so they can be unit-tested without a browser.

```csharp
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

/// <summary>Which of the two picked contacts a value comes from.</summary>
public enum MergeSide { A, B }

/// <summary>A merge of two duplicate contacts, before it is confirmed.</summary>
public interface IContactMerge : IValidateBase
{
    ContactCard? A { get; set; }
    ContactCard? B { get; set; }

    /// <summary>The contact that survives; the other is deleted.</summary>
    MergeSide Keep { get; set; }

    MergeSide FirstNameFrom { get; set; }
    MergeSide LastNameFrom { get; set; }
    MergeSide EmailFrom { get; set; }
    MergeSide PhoneFrom { get; set; }
    MergeSide CompanyFrom { get; set; }
    MergeSide JobTitleFrom { get; set; }

    /// <summary>The kept contact's id with the chosen values; null until two different contacts are picked.</summary>
    ContactCard? Result { get; }

    /// <summary>The contact the merge deletes.</summary>
    Guid? DuplicateId { get; }

    /// <summary>Admission for confirming the merge.</summary>
    bool CanMerge { get; }
}

[Factory]
internal partial class ContactMerge : ValidateBase<ContactMerge>, IContactMerge
{
    public ContactMerge(IValidateBaseServices<ContactMerge> services) : base(services)
    {
        RuleManager.AddRule(new DifferentContactsRule());

        // A new pair: each field starts from A, or from B where only B has a value.
        RuleManager.AddAction(t => t.DefaultChoices(), t => t.A, t => t.B);

        RuleManager.AddAction(
            t => t.Compose(),
            new Expression<Func<ContactMerge, object?>>[]
            {
                t => t.A, t => t.B, t => t.Keep,
                t => t.FirstNameFrom, t => t.LastNameFrom, t => t.EmailFrom,
                t => t.PhoneFrom, t => t.CompanyFrom, t => t.JobTitleFrom,
            });
    }

    public partial ContactCard? A { get; set; }
    public partial ContactCard? B { get; set; }
    public partial MergeSide Keep { get; set; }

    public partial MergeSide FirstNameFrom { get; set; }
    public partial MergeSide LastNameFrom { get; set; }
    public partial MergeSide EmailFrom { get; set; }
    public partial MergeSide PhoneFrom { get; set; }
    public partial MergeSide CompanyFrom { get; set; }
    public partial MergeSide JobTitleFrom { get; set; }

    public partial ContactCard? Result { get; set; }
    public partial Guid? DuplicateId { get; set; }
    public partial bool CanMerge { get; set; }

    [Create]
    public void Create()
    {
    }

    private void DefaultChoices()
    {
        FirstNameFrom = Prefer(A?.FirstName, B?.FirstName);
        LastNameFrom = Prefer(A?.LastName, B?.LastName);
        EmailFrom = Prefer(A?.Email, B?.Email);
        PhoneFrom = Prefer(A?.Phone, B?.Phone);
        CompanyFrom = Prefer(A?.Company, B?.Company);
        JobTitleFrom = Prefer(A?.JobTitle, B?.JobTitle);
    }

    private void Compose()
    {
        if (A is null || B is null || A.Id == B.Id)
        {
            Result = null;
            DuplicateId = null;
            CanMerge = false;
            return;
        }

        var kept = Keep == MergeSide.A ? A : B;
        var duplicate = Keep == MergeSide.A ? B : A;

        Result = new ContactCard(
            kept.Id,
            From(FirstNameFrom).FirstName,
            From(LastNameFrom).LastName,
            From(EmailFrom).Email,
            From(PhoneFrom).Phone,
            From(CompanyFrom).Company,
            From(JobTitleFrom).JobTitle);
        DuplicateId = duplicate.Id;
        CanMerge = true;
    }

    private ContactCard From(MergeSide side) => side == MergeSide.A ? A! : B!;

    private static MergeSide Prefer(string? a, string? b) =>
        string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) ? MergeSide.B : MergeSide.A;
}

/// <summary>The two picked contacts must be different contacts.</summary>
internal sealed class DifferentContactsRule : RuleBase<ContactMerge>
{
    public DifferentContactsRule() : base(t => t.A, t => t.B) { }

    protected override IRuleMessages Execute(ContactMerge target)
    {
        return new RuleMessages()
            .If(target.A is not null && target.B is not null && target.A.Id == target.B.Id,
                nameof(ContactMerge.B),
                "Pick two different contacts.");
    }
}
```

---

## Server: `zCRM.Server`

**`Data/CrmDbContext.cs`**: your existing DbContext. The only change is that it now implements `ICrmDbContext`. `Database` and `SaveChangesAsync` already exist on `DbContext`.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Data;

namespace zCRM.Server.Data;

public class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options), ICrmDbContext
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(c => c.Id);
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.FirstName).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.Property(c => c.Company).HasMaxLength(200);
            e.Property(c => c.JobTitle).HasMaxLength(100);
        });
    }
}
```

**`Contacts/ContactSearch.cs`**: implements the search interface on the server, the only tier with a DbContext. It only reads, so it bypasses no rules.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts;
using zCRM.Domain.Data;

namespace zCRM.Server.Contacts;

public class ContactSearch(ICrmDbContext db) : IContactSearch
{
    public async Task<IReadOnlyList<ContactCard>> Find(string text)
    {
        return await db.Contacts
            .AsNoTracking()
            .Where(c => c.LastName.Contains(text)
                     || (c.FirstName != null && c.FirstName.Contains(text))
                     || (c.Email != null && c.Email.Contains(text)))
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Take(20)
            .Select(c => new ContactCard(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.Company, c.JobTitle))
            .ToListAsync();
    }
}
```

**`Program.cs`**: Neatoo is registered in Server mode with `AddNeatooServices` and not `AddNeatooAspNetCore`, because registering both breaks every remote Save (RemoteFactory#117). `UseNeatoo()` is the single endpoint every factory call goes through.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain.Contacts;
using zCRM.Domain.Data;
using zCRM.Server.Contacts;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Your existing registration; the provider is yours.
builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddScoped<ICrmDbContext>(sp => sp.GetRequiredService<CrmDbContext>());

builder.Services.AddScoped<IContactSearch, ContactSearch>();

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## Client: `zCRM.Client`

**`Program.cs`**: Neatoo runs in Remote mode, so the generated factories are proxies to the server. Nothing server-only is registered here.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactMergeViewModel>();

await builder.Build().RunAsync();
```

**`ViewModels/ContactMergeViewModel.cs`**: holds the merge and forwards the person's actions to the domain. It doesn't decide whether merging is allowed (`CanMerge` does) and doesn't hold the transaction (the server operation does). `IsMerging` only stops a double-click from sending the merge twice.

```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Merge duplicates screen.</summary>
internal sealed class ContactMergeViewModel(
    IContactMergeFactory mergeFactory,
    IContactFactory contactFactory,
    IContactSearch contactSearch)
{
    public IContactMerge Merge { get; private set; } = null!;

    public bool IsMerging { get; private set; }

    public bool CanConfirm => Merge.CanMerge && !IsMerging;

    public void Start()
    {
        Merge = mergeFactory.Create();
    }

    public async Task<IEnumerable<ContactCard>> Search(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return await contactSearch.Find(text);
    }

    /// <summary>Runs the merge; returns the kept contact, or null when the merge cannot run.</summary>
    public async Task<IContact?> Confirm()
    {
        await Merge.WaitForTasks();
        if (!CanConfirm)
            return null;

        IsMerging = true;
        try
        {
            var kept = await contactFactory.MergeDuplicate(Merge.Result!, Merge.DuplicateId!.Value);
            Start();
            return kept;
        }
        finally
        {
            IsMerging = false;
        }
    }
}
```

**`Components/MergeFieldRow.razor`**: one reusable row: both values side by side, plus the merged value. The choices are bound by hand because MudNeatoo fields only accept entity properties, and `ContactMerge` is an editing model (Neatoo#95).

```razor
@using Neatoo
@using zCRM.Domain.Contacts

<MudRadioGroup T="MergeSide"
               Value="@Selected"
               ValueChanged="@(async (MergeSide side) => await Choice.SetValue(side))"
               Disabled="@(Choice.IsReadOnly || Choice.IsBusy)">
    <MudGrid Spacing="2" Class="align-center">
        <MudItem xs="3"><MudText Typo="Typo.subtitle2">@Label</MudText></MudItem>
        <MudItem xs="3"><MudRadio T="MergeSide" Value="MergeSide.A">@Show(ValueA)</MudRadio></MudItem>
        <MudItem xs="3"><MudRadio T="MergeSide" Value="MergeSide.B">@Show(ValueB)</MudRadio></MudItem>
        <MudItem xs="3"><MudText>@Show(Result)</MudText></MudItem>
    </MudGrid>
</MudRadioGroup>

@code {
    [Parameter, EditorRequired] public IValidateProperty Choice { get; set; } = default!;
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter] public string? ValueA { get; set; }
    [Parameter] public string? ValueB { get; set; }
    [Parameter] public string? Result { get; set; }

    private MergeSide Selected => Choice.Value is MergeSide side ? side : MergeSide.A;

    private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
```

**`Pages/MergeContacts.razor`**: the page binds to what the domain works out (`CanMerge`, `Result`, the validation messages). It subscribes to the merge so the merged column and the button update on every choice.

```razor
@page "/contacts/merge"
@implements IDisposable
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Client.Components
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts
@inject ContactMergeViewModel Vm
@inject ISnackbar Snackbar

<MudText Typo="Typo.h4" Class="mb-4">Merge duplicates</MudText>

@* @key: a finished merge starts a new IContactMerge; rebuild the bindings on it. *@
<div @key="Vm.Merge">
    <MudGrid Spacing="3" Class="mb-4">
        <MudItem xs="12" md="6">
            <MudAutocomplete T="ContactCard"
                             Label="Contact A"
                             Value="@Vm.Merge.A"
                             ValueChanged="@(async (ContactCard c) => await Prop(nameof(IContactMerge.A)).SetValue(c))"
                             SearchFunc="Vm.Search"
                             ToStringFunc="@(c => c?.DisplayName)"
                             DebounceInterval="300"
                             Variant="Variant.Outlined"
                             Error="@(!Prop(nameof(IContactMerge.A)).IsValid)"
                             ErrorText="@Messages(nameof(IContactMerge.A))" />
        </MudItem>
        <MudItem xs="12" md="6">
            <MudAutocomplete T="ContactCard"
                             Label="Contact B"
                             Value="@Vm.Merge.B"
                             ValueChanged="@(async (ContactCard c) => await Prop(nameof(IContactMerge.B)).SetValue(c))"
                             SearchFunc="Vm.Search"
                             ToStringFunc="@(c => c?.DisplayName)"
                             DebounceInterval="300"
                             Variant="Variant.Outlined"
                             Error="@(!Prop(nameof(IContactMerge.B)).IsValid)"
                             ErrorText="@Messages(nameof(IContactMerge.B))" />
        </MudItem>
    </MudGrid>

    <NeatooValidationSummary Entity="@Vm.Merge" ShowHeader="false" Dense="true" Class="mb-4" />

    @if (Vm.Merge is { CanMerge: true, A: { } a, B: { } b, Result: { } result })
    {
        <MudPaper Class="pa-4 mb-4" Outlined="true">
            <MudGrid Spacing="2" Class="mb-2">
                <MudItem xs="3" />
                <MudItem xs="3"><MudText Typo="Typo.subtitle1">Contact A</MudText></MudItem>
                <MudItem xs="3"><MudText Typo="Typo.subtitle1">Contact B</MudText></MudItem>
                <MudItem xs="3"><MudText Typo="Typo.subtitle1">Merged</MudText></MudItem>
            </MudGrid>

            <MergeFieldRow Label="Keep record" Choice="@Prop(nameof(IContactMerge.Keep))"
                           ValueA="@a.DisplayName" ValueB="@b.DisplayName" Result="@result.DisplayName" />
            <MudDivider Class="my-2" />
            <MergeFieldRow Label="First name" Choice="@Prop(nameof(IContactMerge.FirstNameFrom))"
                           ValueA="@a.FirstName" ValueB="@b.FirstName" Result="@result.FirstName" />
            <MergeFieldRow Label="Last name" Choice="@Prop(nameof(IContactMerge.LastNameFrom))"
                           ValueA="@a.LastName" ValueB="@b.LastName" Result="@result.LastName" />
            <MergeFieldRow Label="Email" Choice="@Prop(nameof(IContactMerge.EmailFrom))"
                           ValueA="@a.Email" ValueB="@b.Email" Result="@result.Email" />
            <MergeFieldRow Label="Phone" Choice="@Prop(nameof(IContactMerge.PhoneFrom))"
                           ValueA="@a.Phone" ValueB="@b.Phone" Result="@result.Phone" />
            <MergeFieldRow Label="Company" Choice="@Prop(nameof(IContactMerge.CompanyFrom))"
                           ValueA="@a.Company" ValueB="@b.Company" Result="@result.Company" />
            <MergeFieldRow Label="Job title" Choice="@Prop(nameof(IContactMerge.JobTitleFrom))"
                           ValueA="@a.JobTitle" ValueB="@b.JobTitle" Result="@result.JobTitle" />
        </MudPaper>
    }

    <MudButton Variant="Variant.Filled"
               Color="Color.Error"
               Disabled="@(!Vm.CanConfirm)"
               OnClick="ConfirmAsync">
        Merge and delete the other contact
    </MudButton>
</div>

@code {
    private IContactMerge? _subscribed;

    protected override void OnInitialized()
    {
        Vm.Start();
        Resubscribe();
    }

    private IValidateProperty Prop(string name) => Vm.Merge[name];

    private string Messages(string name) =>
        string.Join(" ", Prop(name).PropertyMessages.Select(m => m.Message));

    private async Task ConfirmAsync()
    {
        var kept = await Vm.Confirm();
        Resubscribe();

        if (kept is not null)
        {
            Snackbar.Add($"Merged into {kept.FirstName} {kept.LastName}.", Severity.Success);
        }
    }

    // The merged column and the button read the merge, so the page re-renders
    // on its changes; Confirm leaves the ViewModel holding a new merge.
    private void Resubscribe()
    {
        if (_subscribed is not null)
            _subscribed.NeatooPropertyChanged -= OnMergeChanged;

        _subscribed = Vm.Merge;
        _subscribed.NeatooPropertyChanged += OnMergeChanged;
    }

    private Task OnMergeChanged(NeatooPropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
            _subscribed.NeatooPropertyChanged -= OnMergeChanged;
    }
}
```

---

## Decisions you should know about

- **The server saves the values the person saw**, not values it looks up again from the current rows. The trade-off: if someone edits either contact between picking and confirming, that edit is silently lost. A concurrency token on `ContactRow`, checked in `MergeDuplicate`, would close this; I left it out because the Contacts schema wasn't specified.
- **The duplicate is deleted before the survivor is saved**, so a unique index on `Email` won't reject a merge where the survivor takes the duplicate's email.
- **Related records aren't handled.** Any activities, deals or notes pointing at the deleted contact will either be orphaned or block the delete through foreign keys. Repointing them belongs in `MergeDuplicate`, inside the same transaction, but only you know which tables exist.
- **No authorization.** Merging deletes data, so it probably needs `[AspAuthorize]` or an authorization rule on `Contact`.
- **Two deployment details:**
  - If your EF provider has retry-on-failure enabled, a transaction you start yourself has to be wrapped in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`.
  - If the client is published with trimming, `ContactCard` needs a preserve entry, because it is a parameter of a remote call.
