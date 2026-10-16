Below is the whole feature. The merge itself is the editing session: a `ValidateBase` input model that holds both contacts' values and one choice per field. Rules on it recompute the merged result every time a choice changes. Confirming sends the input model to a `[Remote, Execute]` command. The command deletes one contact and updates the other through `Contact`'s own factory, inside one EF transaction. None of this has been compiled; see the notes at the end.

## Domain (`Zcrm.Domain`, loaded on both client and server)

**Persistence contract.** The domain's server operations take this as a `[Service]`. It lives in the domain so those operations can name it, and the server implements it. Those operation bodies are trimmed out of the published client.

```csharp
// Zcrm.Domain/Data/ContactRow.cs
namespace Zcrm.Domain.Data;

/// Row in the Contacts table.
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
// Zcrm.Domain/Data/IZcrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Zcrm.Domain.Data;

/// The server's EF Core context, as the domain's operations see it.
public interface IZcrmDbContext
{
    DbSet<ContactRow> Contacts { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Value object.** This is one contact's mergeable values. It is a `record` because it is immutable and compared by value. That value comparison is what the command uses to detect that a contact changed after the merge was opened.

```csharp
// Zcrm.Domain/Contacts/ContactValues.cs
namespace Zcrm.Domain.Contacts;

/// Value object: the mergeable values of one contact.
public sealed record ContactValues(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Company,
    string? JobTitle)
{
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
```

**Contact aggregate root.** The merge never writes a contact row itself. It fetches both contacts, calls a verb on the one it keeps, marks the other deleted, and saves both through `IContactFactory`. Each contact writes its own row, so the validity check in `Update` also protects the merge. `[Create]`/`[Insert]` belong to the contact edit screen and are left out here.

```csharp
// Zcrm.Domain/Contacts/IContact.cs
using Neatoo;

namespace Zcrm.Domain.Contacts;

/// Contact aggregate root.
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    string? Phone { get; set; }
    string? Company { get; set; }
    string? JobTitle { get; set; }

    /// The contact's current mergeable values.
    ContactValues Values { get; }

    /// Takes every mergeable value at once; used when this contact survives a merge.
    void TakeValues(ContactValues values);
}
```

```csharp
// Zcrm.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Domain.Data;

namespace Zcrm.Domain.Contacts;

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Email) || MailAddress.TryCreate(t.Email, out _)
                ? string.Empty
                : "Email is not a valid address.",
            t => t.Email);
    }

    public partial Guid Id { get; set; }

    [DisplayName("First name")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name"), Required]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    public partial string? Email { get; set; }

    [DisplayName("Phone")]
    public partial string? Phone { get; set; }

    [DisplayName("Company")]
    public partial string? Company { get; set; }

    [DisplayName("Job title")]
    public partial string? JobTitle { get; set; }

    public ContactValues Values => new(FirstName, LastName, Email, Phone, Company, JobTitle);

    public void TakeValues(ContactValues values)
    {
        FirstName = values.FirstName;
        LastName = values.LastName;
        Email = values.Email;
        Phone = values.Phone;
        Company = values.Company;
        JobTitle = values.JobTitle;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IZcrmDbContext db)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
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

    [Remote, Update]
    internal async Task Update([Service] IZcrmDbContext db)
    {
        // Server gate: refuse an invalid contact from any caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
            throw new SaveOperationException(SaveFailureReason.IsInvalid);

        var row = await db.Contacts.SingleAsync(c => c.Id == Id);
        row.FirstName = FirstName;
        row.LastName = LastName!;
        row.Email = Email;
        row.Phone = Phone;
        row.Company = Company;
        row.JobTitle = JobTitle;
        await db.SaveChangesAsync();
    }

    [Remote, Delete]
    internal async Task Delete([Service] IZcrmDbContext db)
    {
        var row = await db.Contacts.SingleAsync(c => c.Id == Id);
        db.Contacts.Remove(row);
        await db.SaveChangesAsync();
    }
}
```

**Read model for the picker.** The list is server data shown to the user, which nothing edits. So it is a plain `[Factory]` class with only a `[Fetch]`: no DTOs and no controller.

```csharp
// Zcrm.Domain/Contacts/ContactDirectory.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Zcrm.Domain.Data;

namespace Zcrm.Domain.Contacts;

/// One row of the contact picker.
public sealed record ContactListItem(Guid Id, string? FirstName, string LastName, string? Email, string? Company)
{
    public string Name => $"{FirstName} {LastName}".Trim();
}

/// Read model: contacts matching a search, for picking merge candidates.
public interface IContactDirectory
{
    IReadOnlyList<ContactListItem> Contacts { get; }
}

[Factory]
internal partial class ContactDirectory : IContactDirectory
{
    public IReadOnlyList<ContactListItem> Contacts { get; internal set; } = [];

    [Remote, Fetch]
    internal async Task Fetch(string search, [Service] IZcrmDbContext db, CancellationToken cancellationToken)
    {
        var query = db.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c =>
                c.LastName.Contains(s)
                || (c.FirstName != null && c.FirstName.Contains(s))
                || (c.Email != null && c.Email.Contains(s))
                || (c.Company != null && c.Company.Contains(s)));
        }

        Contacts = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Take(100)
            .Select(c => new ContactListItem(c.Id, c.FirstName, c.LastName, c.Email, c.Company))
            .ToListAsync(cancellationToken);
    }
}
```

**The merge input model.** This is where "change their choices any number of times" lives. Each choice is a tracked property, and rules recompute `Merged` and the kept/deleted names on every change, with no server call. It is a `ValidateBase` rather than an `EntityBase` because it has no row of its own; the user edits it, and a command consumes it. Its `[Create]` is `[Remote]` because building it means reading both contacts on the server. Inside the operation rules don't fire, so `Create` computes the derived values itself using the same methods the rules call.

```csharp
// Zcrm.Domain/Contacts/Merge/MergeSide.cs
namespace Zcrm.Domain.Contacts.Merge;

public enum MergeSide { Left, Right }
```

```csharp
// Zcrm.Domain/Contacts/Merge/IContactMerge.cs
using Neatoo;

namespace Zcrm.Domain.Contacts.Merge;

/// Merge of two duplicate contacts: which record survives and which value wins per field.
public interface IContactMerge : IValidateBase
{
    Guid LeftId { get; }
    Guid RightId { get; }
    ContactValues? LeftContact { get; }
    ContactValues? RightContact { get; }

    MergeSide Survivor { get; set; }
    MergeSide FirstNameFrom { get; set; }
    MergeSide LastNameFrom { get; set; }
    MergeSide EmailFrom { get; set; }
    MergeSide PhoneFrom { get; set; }
    MergeSide CompanyFrom { get; set; }
    MergeSide JobTitleFrom { get; set; }

    /// The surviving contact's values under the current choices.
    ContactValues? Merged { get; }
    string? SurvivorName { get; }
    string? DiscardedName { get; }

    /// Applies the current choices to a pair of contact values.
    ContactValues Resolve(ContactValues left, ContactValues right);
}
```

```csharp
// Zcrm.Domain/Contacts/Merge/ContactMerge.cs
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Zcrm.Domain.Contacts.Merge;

[Factory]
internal partial class ContactMerge : ValidateBase<ContactMerge>, IContactMerge
{
    public ContactMerge(IValidateBaseServices<ContactMerge> services) : base(services)
    {
        RuleManager.AddAction(
            t => t.ComputeMerged(),
            new Expression<Func<ContactMerge, object?>>[]
            {
                t => t.FirstNameFrom, t => t.LastNameFrom, t => t.EmailFrom,
                t => t.PhoneFrom, t => t.CompanyFrom, t => t.JobTitleFrom
            });

        RuleManager.AddAction(t => t.ComputeSurvivor(), t => t.Survivor);
    }

    public partial Guid LeftId { get; set; }
    public partial Guid RightId { get; set; }
    public partial ContactValues? LeftContact { get; set; }
    public partial ContactValues? RightContact { get; set; }

    public partial MergeSide Survivor { get; set; }
    public partial MergeSide FirstNameFrom { get; set; }
    public partial MergeSide LastNameFrom { get; set; }
    public partial MergeSide EmailFrom { get; set; }
    public partial MergeSide PhoneFrom { get; set; }
    public partial MergeSide CompanyFrom { get; set; }
    public partial MergeSide JobTitleFrom { get; set; }

    public partial ContactValues? Merged { get; set; }
    public partial string? SurvivorName { get; set; }
    public partial string? DiscardedName { get; set; }

    [Remote, Create]
    internal async Task Create(Guid leftId, Guid rightId, [Service] IContactFactory contactFactory)
    {
        if (leftId == rightId)
            throw new ArgumentException("A contact cannot be merged with itself.", nameof(rightId));

        var left = await contactFactory.Fetch(leftId)
            ?? throw new InvalidOperationException($"Contact {leftId} not found.");
        var right = await contactFactory.Fetch(rightId)
            ?? throw new InvalidOperationException($"Contact {rightId} not found.");

        var l = left.Values;
        var r = right.Values;

        LeftId = leftId;
        RightId = rightId;
        LeftContact = l;
        RightContact = r;

        // Default: keep the left record; take its value unless it is blank and the right one is not.
        Survivor = MergeSide.Left;
        FirstNameFrom = Prefer(l.FirstName, r.FirstName);
        LastNameFrom = Prefer(l.LastName, r.LastName);
        EmailFrom = Prefer(l.Email, r.Email);
        PhoneFrom = Prefer(l.Phone, r.Phone);
        CompanyFrom = Prefer(l.Company, r.Company);
        JobTitleFrom = Prefer(l.JobTitle, r.JobTitle);

        // Paused inside the operation: compute the derived values the rules would.
        ComputeMerged();
        ComputeSurvivor();
    }

    public ContactValues Resolve(ContactValues left, ContactValues right) => new(
        Pick(FirstNameFrom, left.FirstName, right.FirstName),
        Pick(LastNameFrom, left.LastName, right.LastName),
        Pick(EmailFrom, left.Email, right.Email),
        Pick(PhoneFrom, left.Phone, right.Phone),
        Pick(CompanyFrom, left.Company, right.Company),
        Pick(JobTitleFrom, left.JobTitle, right.JobTitle));

    private void ComputeMerged()
    {
        Merged = LeftContact is null || RightContact is null
            ? null
            : Resolve(LeftContact, RightContact);
    }

    private void ComputeSurvivor()
    {
        var keepLeft = Survivor == MergeSide.Left;
        SurvivorName = (keepLeft ? LeftContact : RightContact)?.DisplayName;
        DiscardedName = (keepLeft ? RightContact : LeftContact)?.DisplayName;
    }

    private static MergeSide Prefer(string? left, string? right) =>
        string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) ? MergeSide.Right : MergeSide.Left;

    private static string? Pick(MergeSide side, string? left, string? right) =>
        side == MergeSide.Left ? left : right;
}
```

**The confirm command.** This step works across two aggregate instances and needs the server, so it is a static `[Remote, Execute]` command. That keeps it out of a verb or a `Save`.
- The command uses only the user's choices from the client. It recomputes the values from freshly fetched contacts with `Resolve`.
- If either contact changed or disappeared since the merge was opened, client and server disagree, so it throws.
- It deletes before it updates, so a unique email index can't collide when the kept contact takes the other one's email.

```csharp
// Zcrm.Domain/Contacts/Merge/ContactMergeCommands.cs
using Neatoo.RemoteFactory;
using Zcrm.Domain.Data;

namespace Zcrm.Domain.Contacts.Merge;

[Factory]
public static partial class ContactMergeCommands
{
    /// Keeps the surviving contact with the chosen values and deletes the other, in one transaction.
    /// Returns the surviving contact's id.
    [Remote, Execute]
    private static async Task<Guid> _MergeContacts(
        IContactMerge merge,
        [Service] IContactFactory contactFactory,
        [Service] IZcrmDbContext db)
    {
        var left = await contactFactory.Fetch(merge.LeftId)
            ?? throw new InvalidOperationException("A contact in this merge was deleted. Pick the contacts again.");
        var right = await contactFactory.Fetch(merge.RightId)
            ?? throw new InvalidOperationException("A contact in this merge was deleted. Pick the contacts again.");

        if (left.Values != merge.LeftContact || right.Values != merge.RightContact)
            throw new InvalidOperationException("A contact changed after the merge was opened. Open the merge again.");

        var (survivor, discarded) = merge.Survivor == MergeSide.Left ? (left, right) : (right, left);
        var survivorId = survivor.Id;

        survivor.TakeValues(merge.Resolve(left.Values, right.Values));
        await survivor.WaitForTasks();
        discarded.Delete();

        await using var transaction = await db.Database.BeginTransactionAsync();

        await contactFactory.Save(discarded);
        if (survivor.IsModified)
            await contactFactory.Save(survivor);

        await transaction.CommitAsync();
        return survivorId;
    }
}
```

## Server (`Zcrm.Server`)

The concrete `DbContext` and the wiring live only on the server. `AddNeatooServices(Server)` is the only Neatoo registration. Do not also call `AddNeatooAspNetCore` (RemoteFactory#117). Both `IZcrmDbContext` and `ZcrmDbContext` resolve to the same scoped instance, so the command's transaction covers both contacts' `SaveChangesAsync` calls.

```csharp
// Zcrm.Server/Data/ZcrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using Zcrm.Domain.Data;

namespace Zcrm.Server.Data;

public sealed class ZcrmDbContext(DbContextOptions<ZcrmDbContext> options) : DbContext(options), IZcrmDbContext
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

```csharp
// Zcrm.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Domain.Contacts;
using Zcrm.Domain.Data;
using Zcrm.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ZcrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Zcrm")));
builder.Services.AddScoped<IZcrmDbContext>(sp => sp.GetRequiredService<ZcrmDbContext>());

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## Client (`Zcrm.Client`, Blazor WebAssembly)

```csharp
// Zcrm.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Client;
using Zcrm.Client.ViewModels;
using Zcrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<MergeContactsViewModel>();

await builder.Build().RunAsync();
```

**ViewModel.** It turns the user's clicks (search, compare, back, confirm) into domain calls. The only state it holds itself is the screen's: the search text and which rows are selected. It doesn't decide what a merge produces; the merge object does. The `IsMerging` flag stops a double-click from sending a second merge.

```csharp
// Zcrm.Client/ViewModels/MergeContactsViewModel.cs
using Zcrm.Domain.Contacts;
using Zcrm.Domain.Contacts.Merge;

namespace Zcrm.Client.ViewModels;

/// ViewModel for the Merge duplicates screen: picks two contacts, holds the merge, confirms it.
internal sealed class MergeContactsViewModel
{
    private readonly IContactDirectoryFactory _directoryFactory;
    private readonly IContactMergeFactory _mergeFactory;
    private readonly ContactMergeCommands.MergeContacts _mergeContacts;

    public MergeContactsViewModel(
        IContactDirectoryFactory directoryFactory,
        IContactMergeFactory mergeFactory,
        ContactMergeCommands.MergeContacts mergeContacts)
    {
        _directoryFactory = directoryFactory;
        _mergeFactory = mergeFactory;
        _mergeContacts = mergeContacts;
    }

    public string Search { get; set; } = string.Empty;
    public IContactDirectory? Directory { get; private set; }
    public HashSet<ContactListItem> Selected { get; set; } = [];

    /// The merge the comparison binds to. Null while picking.
    public IContactMerge? Merge { get; private set; }
    public bool IsMerging { get; private set; }

    public bool CanCompare => Selected.Count == 2;
    public bool CanConfirm => Merge is { IsValid: true, IsBusy: false } && !IsMerging;

    public async Task SearchAsync()
    {
        Directory = await _directoryFactory.Fetch(Search);
        Selected = [];
    }

    public async Task CompareAsync()
    {
        if (!CanCompare || Directory is null)
            return;

        // Columns read in list order.
        var pair = Directory.Contacts.Where(Selected.Contains).ToList();
        Merge = await _mergeFactory.Create(pair[0].Id, pair[1].Id);
    }

    public void BackToPicker() => Merge = null;

    /// Returns the surviving contact's id, or null when the merge could not be confirmed.
    public async Task<Guid?> ConfirmAsync()
    {
        if (Merge is null)
            return null;

        await Merge.WaitForTasks();
        if (!CanConfirm)
            return null;

        IsMerging = true;
        try
        {
            var survivorId = await _mergeContacts(Merge);
            Merge = null;
            await SearchAsync();
            return survivorId;
        }
        finally
        {
            IsMerging = false;
        }
    }
}
```

**One comparison row.** MudNeatoo components only bind properties of `EntityBase` objects (Neatoo#95), and `IContactMerge` is a `ValidateBase`. So this component binds the choice by hand and commits it through `SetValue`, which runs the merge's rules the same way MudNeatoo would. The component exists so that manual binding is written once instead of seven times.

```razor
@* Zcrm.Client/Components/MergeChoiceRow.razor *@
<MudRadioGroup T="MergeSide"
               Value="@Current"
               ValueChanged="@((MergeSide side) => Choice.SetValue(side))"
               Disabled="@(Choice.IsBusy || Choice.IsReadOnly)">
    <MudGrid Spacing="2" Class="align-center py-1">
        <MudItem xs="12" md="2">
            <MudText Typo="Typo.subtitle2">@Label</MudText>
        </MudItem>
        <MudItem xs="12" md="4">
            <MudRadio T="MergeSide" Value="MergeSide.Left" Color="Color.Primary">@Show(LeftValue)</MudRadio>
        </MudItem>
        <MudItem xs="12" md="4">
            <MudRadio T="MergeSide" Value="MergeSide.Right" Color="Color.Primary">@Show(RightValue)</MudRadio>
        </MudItem>
        <MudItem xs="12" md="2">
            <MudText Typo="Typo.body2"><b>@Show(Result)</b></MudText>
        </MudItem>
    </MudGrid>
</MudRadioGroup>

@code {
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter, EditorRequired] public IValidateProperty Choice { get; set; } = default!;
    [Parameter] public string? LeftValue { get; set; }
    [Parameter] public string? RightValue { get; set; }
    [Parameter] public string? Result { get; set; }

    private MergeSide Current => Choice.Value is MergeSide side ? side : MergeSide.Left;

    private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
```

**The page.** It binds facts the merge computed: `Merged`, `SurvivorName`, `DiscardedName` and `IsValid`. The page subscribes to `NeatooPropertyChanged`, which re-renders it after a choice changes. That is needed because a choice is set inside a row component, and the Result column and the warning live on the page. `@key` rebuilds the rows whenever a new merge object replaces the old one.

```razor
@* Zcrm.Client/Pages/MergeContacts.razor *@
@page "/contacts/merge"
@implements IDisposable
@inject MergeContactsViewModel Vm
@inject ISnackbar Snackbar

<PageTitle>Merge duplicates</PageTitle>
<MudText Typo="Typo.h4" Class="mb-4">Merge duplicates</MudText>

@if (Vm.Merge is null)
{
    <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-3">
        <MudTextField T="string" @bind-Value="Vm.Search" Label="Search contacts" Variant="Variant.Outlined" />
        <MudButton Variant="Variant.Outlined" OnClick="SearchAsync">Search</MudButton>
    </MudStack>

    @if (Vm.Directory is not null)
    {
        <MudTable T="ContactListItem"
                  Items="@Vm.Directory.Contacts"
                  MultiSelection="true"
                  @bind-SelectedItems="Vm.Selected"
                  Dense="true"
                  Hover="true"
                  Class="mb-3">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Email</MudTh>
                <MudTh>Company</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.Name</MudTd>
                <MudTd>@context.Email</MudTd>
                <MudTd>@context.Company</MudTd>
            </RowTemplate>
        </MudTable>
    }

    <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanCompare)" OnClick="CompareAsync">
        Compare selected
    </MudButton>
    <MudText Typo="Typo.caption" Class="ml-2">Select exactly two contacts.</MudText>
}
else
{
    var m = Vm.Merge;
    <div @key="m">
        <MudGrid Spacing="2" Class="mb-2">
            <MudItem xs="12" md="2"><MudText Typo="Typo.overline">Field</MudText></MudItem>
            <MudItem xs="12" md="4"><MudText Typo="Typo.overline">@m.LeftContact?.DisplayName</MudText></MudItem>
            <MudItem xs="12" md="4"><MudText Typo="Typo.overline">@m.RightContact?.DisplayName</MudText></MudItem>
            <MudItem xs="12" md="2"><MudText Typo="Typo.overline">Result</MudText></MudItem>
        </MudGrid>

        <MergeChoiceRow Label="Keep record"
                        Choice="@m[nameof(IContactMerge.Survivor)]"
                        LeftValue="@m.LeftContact?.DisplayName"
                        RightValue="@m.RightContact?.DisplayName"
                        Result="@m.SurvivorName" />
        <MudDivider Class="my-2" />

        <MergeChoiceRow Label="First name" Choice="@m[nameof(IContactMerge.FirstNameFrom)]"
                        LeftValue="@m.LeftContact?.FirstName" RightValue="@m.RightContact?.FirstName" Result="@m.Merged?.FirstName" />
        <MergeChoiceRow Label="Last name" Choice="@m[nameof(IContactMerge.LastNameFrom)]"
                        LeftValue="@m.LeftContact?.LastName" RightValue="@m.RightContact?.LastName" Result="@m.Merged?.LastName" />
        <MergeChoiceRow Label="Email" Choice="@m[nameof(IContactMerge.EmailFrom)]"
                        LeftValue="@m.LeftContact?.Email" RightValue="@m.RightContact?.Email" Result="@m.Merged?.Email" />
        <MergeChoiceRow Label="Phone" Choice="@m[nameof(IContactMerge.PhoneFrom)]"
                        LeftValue="@m.LeftContact?.Phone" RightValue="@m.RightContact?.Phone" Result="@m.Merged?.Phone" />
        <MergeChoiceRow Label="Company" Choice="@m[nameof(IContactMerge.CompanyFrom)]"
                        LeftValue="@m.LeftContact?.Company" RightValue="@m.RightContact?.Company" Result="@m.Merged?.Company" />
        <MergeChoiceRow Label="Job title" Choice="@m[nameof(IContactMerge.JobTitleFrom)]"
                        LeftValue="@m.LeftContact?.JobTitle" RightValue="@m.RightContact?.JobTitle" Result="@m.Merged?.JobTitle" />

        <MudAlert Severity="Severity.Warning" Class="my-4">
            @m.DiscardedName will be deleted. @m.SurvivorName is kept with the values in the Result column.
        </MudAlert>

        <MudButton Variant="Variant.Text" OnClick="Back" Class="mr-2">Back</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Error" Disabled="@(!Vm.CanConfirm)" OnClick="ConfirmAsync">
            Confirm merge
        </MudButton>
    </div>
}

@code {
    private IContactMerge? _subscribed;

    protected override async Task OnInitializedAsync()
    {
        await Vm.SearchAsync();
    }

    private Task SearchAsync() => Vm.SearchAsync();

    private async Task CompareAsync()
    {
        await Vm.CompareAsync();
        Resubscribe();
    }

    private void Back()
    {
        Vm.BackToPicker();
        Resubscribe();
    }

    private async Task ConfirmAsync()
    {
        try
        {
            if (await Vm.ConfirmAsync() is not null)
                Snackbar.Add("Contacts merged.", Severity.Success);
        }
        catch (Exception ex)
        {
            // Application failure: a contact changed or was deleted after the merge was opened.
            Snackbar.Add($"The merge was not applied. {ex.Message}", Severity.Error);
        }
        Resubscribe();
    }

    private void Resubscribe()
    {
        if (_subscribed is not null)
            _subscribed.NeatooPropertyChanged -= OnMergeChanged;

        _subscribed = Vm.Merge;

        if (_subscribed is not null)
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

```razor
@* Zcrm.Client/_Imports.razor additions *@
@using MudBlazor
@using Neatoo
@using Zcrm.Domain.Contacts
@using Zcrm.Domain.Contacts.Merge
@using Zcrm.Client.ViewModels
@using Zcrm.Client.Components
```

## Notes

- **Not compiled.** I wrote this from the Neatoo, RemoteFactory and MudNeatoo skill references. Three things I couldn't confirm there are worth checking first:
  - that a `ValidateBase` can be passed as a `[Remote, Execute]` parameter;
  - that `IValidateBase` exposes `NeatooPropertyChanged`;
  - that `MudRadio` takes child content in your MudBlazor version.
- **Small race window.** The "contact changed" check runs before the transaction. An edit landing between that check and the commit isn't caught. A `rowversion` concurrency token on `Contacts`, checked in `Contact.Update`/`Delete`, would close the gap.
- **References to the deleted contact.** You only described a Contacts table. If activities or deals point at contacts, those references must be moved to the kept contact. Do it inside the same transaction, through those aggregates' own factories.
- **Leftovers.**
  - The domain project references EF Core so it can name `IZcrmDbContext`. Publish the client trimmed (`IsServerRuntime=false`) so the server-only code is removed from it.
  - The merge deletes data, so it should get `[AspAuthorize]`/`[AuthorizeFactory]`. I didn't add authorization.
