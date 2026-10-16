I've designed the Contact aggregate, its server-side persistence and the edit page below. None of it has been compiled. I followed the patterns in the Neatoo, RemoteFactory and MudNeatoo references, but you should expect to adjust a few `using` lines for extension-method namespaces.

**Assumptions to confirm:**
- **Email is optional.** It is only checked when it has a value.
- **A phone's number is required.** You didn't ask for this, but a blank phone row would otherwise save. Remove the attribute and the `RunRules` in `PhoneNumber.Create` if you don't want it.
- **There is no `[Delete]` on Contact**, because the screen only has Save.
- **The database is SQL Server.** That only matters in `Program.cs`.

## Solution layout

| Project | References | Why |
|---|---|---|
| `zCRM.Dal` | nothing | Row types and the DbContext interface, with no EF package, so the browser never loads EF Core. |
| `zCRM.Ef` | Dal, EF Core | The real DbContext. Only the server references it. |
| `zCRM.Domain` | Dal, Neatoo | The aggregate. It is loaded on both tiers, so there are no DTOs and no controllers. |
| `zCRM.Client` | Domain, Neatoo.Blazor.MudNeatoo | The page and its ViewModel. |
| `zCRM.Server` | Domain, Ef, Client | Hosts the WASM client and the single RemoteFactory endpoint. |

---

## zCRM.Dal

**Why here:** the domain's server-only operations take `IZCrmDbContext` as a `[Service]` parameter. The interface has to be visible to the domain assembly, but the EF implementation must not be.

```csharp
// zCRM.Dal/ContactRow.cs
namespace zCRM.Dal;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public ICollection<PhoneRow> Phones { get; set; } = new List<PhoneRow>();
}
```

```csharp
// zCRM.Dal/PhoneRow.cs
namespace zCRM.Dal;

public class PhoneRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "";
}
```

```csharp
// zCRM.Dal/IZCrmDbContext.cs
namespace zCRM.Dal;

/// <summary>The store the Contact aggregate's server operations write through.</summary>
public interface IZCrmDbContext
{
    /// <summary>The contact row with its phone rows, or null.</summary>
    Task<ContactRow?> FindContactAsync(Guid id);

    void AddContact(ContactRow row);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## zCRM.Ef

**Why here:** this is the only project that references EF Core, and only the server references this project. Removing a `PhoneRow` from `ContactRow.Phones` deletes it as an orphan, because the foreign key is required.

```csharp
// zCRM.Ef/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Dal;

namespace zCRM.Ef;

public class ZCrmDbContext : DbContext, IZCrmDbContext
{
    public ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : base(options) { }

    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<PhoneRow> Phones => Set<PhoneRow>();

    public Task<ContactRow?> FindContactAsync(Guid id) =>
        Contacts.Include(c => c.Phones).SingleOrDefaultAsync(c => c.Id == id);

    public void AddContact(ContactRow row) => Contacts.Add(row);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contact");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();   // the entity sets its own key
            e.Property(c => c.FirstName).IsRequired();
            e.Property(c => c.LastName).IsRequired();
            e.HasMany(c => c.Phones)
             .WithOne()
             .HasForeignKey(p => p.ContactId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneRow>(e =>
        {
            e.ToTable("Phone");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Number).IsRequired();
            e.Property(p => p.Type).HasMaxLength(10).IsRequired();
        });
    }
}
```

## zCRM.Domain

**Why here:** one assembly runs in both the browser and the server. The validation rules run in the browser as the salesperson types. The `[Remote]` operations cross to the server, where `IZCrmDbContext` is available.

The rules work as follows:
- `[Required]` on both names and a rule on Email: a message appears when a field is committed, and `IsSavable` turns false.
- `Contact.Create` runs the rules once, so a new form shows its required fields straight away and Save starts disabled.
- `Insert` and `Update` re-run the rules on the server and refuse an invalid contact. This is a safety net for callers that skip `IsSavable`.

```csharp
// zCRM.Domain/Contacts/PhoneType.cs
namespace zCRM.Domain.Contacts;

public enum PhoneType
{
    Home,
    Mobile,
    Work
}
```

```csharp
// zCRM.Domain/Contacts/IContactInterfaces.cs
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    IPhoneNumberList? PhoneNumbers { get; }
}

/// <summary>A contact's phone number. Saved through the Contact; no Save() of its own.</summary>
public interface IPhoneNumber : IEntityBase
{
    Guid Id { get; }
    string? Number { get; set; }
    PhoneType Type { get; set; }
}

public interface IPhoneNumberList : IEntityListBase<IPhoneNumber>
{
}
```

Each type has a public interface and an `internal` concrete class. The child interface extends `IEntityBase`, which has no `Save()`, so code cannot save a phone outside its contact.

```csharp
// zCRM.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root: names, email, and its phone numbers.</summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    private static readonly Regex EmailFormat =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public partial Guid Id { get; set; }

    [DisplayName("First name")]
    [Required(ErrorMessage = "First name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name")]
    [Required(ErrorMessage = "Last name is required")]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    public partial string? Email { get; set; }

    public partial IPhoneNumberList? PhoneNumbers { get; set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        // Optional, but well formed when present.
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Email) || EmailFormat.IsMatch(t.Email)
                ? string.Empty
                : "Email is not a valid address",
            t => t.Email);
    }

    /// <summary>
    /// A blank contact. Local: no server call. Runs the rules so the empty
    /// form shows its required fields and Save starts disabled; the phone
    /// list is empty, so All is safe.
    /// </summary>
    [Create]
    public async Task Create([Service] IPhoneNumberListFactory phoneListFactory)
    {
        PhoneNumbers = phoneListFactory.Create();
        await RunRules(RunRulesFlag.All);
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(
        Guid id,
        [Service] IZCrmDbContext db,
        [Service] IPhoneNumberListFactory phoneListFactory)
    {
        var row = await db.FindContactAsync(id);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        PhoneNumbers = phoneListFactory.Fetch(row.Phones);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] IZCrmDbContext db,
        [Service] IPhoneNumberListFactory phoneListFactory)
    {
        // Server gate: refuse an invalid aggregate before writing. Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.AddContact(row);

        phoneListFactory.Save(PhoneNumbers!, row.Phones);

        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] IZCrmDbContext db,
        [Service] IPhoneNumberListFactory phoneListFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        phoneListFactory.Save(PhoneNumbers!, row.Phones);

        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;
    }
}
```

```csharp
// zCRM.Domain/Contacts/PhoneNumber.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Contacts;

/// <summary>Child of Contact. Maps itself into the row its list hands it.</summary>
[Factory]
internal partial class PhoneNumber : EntityBase<PhoneNumber>, IPhoneNumber
{
    public partial Guid Id { get; set; }

    [DisplayName("Number")]
    [Required(ErrorMessage = "Number is required")]
    public partial string? Number { get; set; }

    [DisplayName("Type")]
    public partial PhoneType Type { get; set; }

    public PhoneNumber(IEntityBaseServices<PhoneNumber> services) : base(services) { }

    /// <summary>
    /// Public and local: the client builds a phone, then adds it to the list.
    /// Runs the rules so a blank row shows "Number is required" at once.
    /// </summary>
    [Create]
    public async Task Create()
    {
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(PhoneRow row)
    {
        Id = row.Id;
        Number = row.Number;
        Type = Enum.Parse<PhoneType>(row.Type);
    }

    [Insert]
    internal void Insert(PhoneRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(PhoneRow row)
    {
        MapTo(row);
    }

    private void MapTo(PhoneRow row)
    {
        row.Id = Id;
        row.Number = Number!;
        row.Type = Type.ToString();
    }
}
```

Only `Create` is public. Fetch, Insert and Update are `internal` and not `[Remote]`, because only the list calls them, and it does so on the server.

```csharp
// zCRM.Domain/Contacts/PhoneNumberList.cs
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal;

namespace zCRM.Domain.Contacts;

/// <summary>Contact's phone numbers. Keeps the phone rows in line with the list on save.</summary>
[Factory]
internal partial class PhoneNumberList : EntityListBase<IPhoneNumber>, IPhoneNumberList
{
    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<PhoneRow> rows, [Service] IPhoneNumberFactory phoneFactory)
    {
        foreach (var row in rows)
        {
            Add(phoneFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<PhoneRow> rows, [Service] IPhoneNumberFactory phoneFactory)
    {
        foreach (var phone in this.Union(DeletedList))
        {
            if (phone.IsDeleted)
            {
                if (!phone.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == phone.Id));
                }
            }
            else if (phone.IsNew)
            {
                var row = new PhoneRow();
                rows.Add(row);
                phoneFactory.Save(phone, row);
            }
            else if (phone.IsModified)
            {
                phoneFactory.Save(phone, rows.Single(r => r.Id == phone.Id));
            }
        }
    }
}
```

The list coordinates the save. A removed phone is held in `DeletedList` until then and its row is removed. A new phone gets a new row, and a changed phone gets its existing row. Each phone writes its own row through its own factory operation, which is what marks it clean afterwards.

## zCRM.Client

**Why here:** this is the UI layer. The ViewModel turns gestures into calls on the domain. The page binds to the entity's properties and decides nothing.

```csharp
// zCRM.Client/ViewModels/ContactEditViewModel.cs
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;
    private readonly IPhoneNumberFactory _phoneFactory;

    public ContactEditViewModel(IContactFactory contactFactory, IPhoneNumberFactory phoneFactory)
    {
        _contactFactory = contactFactory;
        _phoneFactory = phoneFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded, or when not found.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>What the phone type select offers.</summary>
    public IReadOnlyList<PhoneType> PhoneTypes { get; } = Enum.GetValues<PhoneType>();

    /// <summary>
    /// Set while a save is in flight. Save() does not mark the entity busy
    /// (Neatoo#103), so this keeps a double-click from sending two Inserts.
    /// </summary>
    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Contact = await _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    public async Task AddPhone()
    {
        if (Contact?.PhoneNumbers is null)
        {
            return;
        }

        Contact.PhoneNumbers.Add(await _phoneFactory.Create());
    }

    public void RemovePhone(IPhoneNumber phone)
    {
        Contact?.PhoneNumbers?.Remove(phone);
    }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

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
                return;  // invalid or unchanged; the validation summary shows why
            }

            Contact = (IContact)await Contact.Save();  // reassign: Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

There is no validate step before saving: the rules already ran as each field was set. The handler waits for any rule still running, asks `IsSavable`, saves, and keeps the instance that `Save()` returns. `IsSaving` works around a known Neatoo bug (Neatoo#103): without it, a double-click on Save sends two Inserts.

```razor
@* zCRM.Client/_Imports.razor *@
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Routing
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zCRM.Domain.Contacts
@using zCRM.Client.ViewModels
```

```razor
@* zCRM.Client/Pages/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm

<PageTitle>Contact</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Contact</MudText>

    @if (!_loaded)
    {
        <MudProgressLinear Indeterminate="true" />
    }
    else if (Vm.Contact is null)
    {
        <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
    }
    else
    {
        @* @key: Save returns a new instance; keying on it rebuilds the bound
           components so each field subscribes to the new instance's properties. *@
        <div @key="Vm.Contact">
            <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

            <MudGrid Spacing="2" Class="mb-2">
                <MudItem xs="12" sm="6">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="12" sm="6">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="12">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" />
                </MudItem>
            </MudGrid>

            <MudText Typo="Typo.h6" Class="mt-4 mb-2">Phone numbers</MudText>

            @if (Vm.Contact.PhoneNumbers is not null)
            {
                @foreach (var phone in Vm.Contact.PhoneNumbers)
                {
                    <MudGrid Spacing="2" Class="mb-2">
                        <MudItem xs="7" sm="7">
                            <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPhoneNumber.Number)]" Variant="Variant.Outlined" />
                        </MudItem>
                        <MudItem xs="4" sm="4">
                            <MudNeatooSelect T="PhoneType" EntityProperty="@phone[nameof(IPhoneNumber.Type)]" Variant="Variant.Outlined">
                                @foreach (var type in Vm.PhoneTypes)
                                {
                                    <MudSelectItem T="PhoneType" Value="@type">@type</MudSelectItem>
                                }
                            </MudNeatooSelect>
                        </MudItem>
                        <MudItem xs="1" Class="d-flex align-center">
                            <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Vm.RemovePhone(phone))" />
                        </MudItem>
                    </MudGrid>
                }
            }

            <MudButton Variant="Variant.Outlined"
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="Vm.AddPhone"
                       Class="mb-4">
                Add phone
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
</MudContainer>

@code {
    [Parameter] public Guid? Id { get; set; }

    private bool _loaded;
    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        _loaded = false;

        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        _loaded = true;
        Resubscribe();
    }

    // The input components re-render themselves; the Save button reads the
    // entity, so the page re-renders on any change anywhere in the graph.
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
        await Vm.Save();
        Resubscribe();  // Save left the ViewModel holding a new instance
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```

There is no `EditForm`, no `DataAnnotationsValidator` and no condition the page works out for itself:
- **Field components:** each MudNeatoo component takes its label, error messages, busy state and read-only state from its property.
- **Save button:** bound to `IsSavable`.
- **Subscription:** the page subscribes to the entity so the Save button and the phone rows re-render on any change in the graph.

```csharp
// zCRM.Client/Program.cs
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

// Remote mode: the generated factories are proxies; [Remote] operations cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);

// The HttpClient the proxies call through: the server that hosts this client.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

The client registers Neatoo in Remote mode and nothing server-only. A `[Service] IZCrmDbContext` parameter can therefore only be resolved on the server.

## zCRM.Server

**Why here:** this is the only tier where the DbContext exists. One endpoint carries every factory call.

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Dal;
using zCRM.Domain.Contacts;
using zCRM.Ef;

var builder = WebApplication.CreateBuilder(args);

// Server mode: the generated factories run the operations here.
// Do not also call AddNeatooAspNetCore after this line (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// What the domain's [Service] IZCrmDbContext parameters resolve to.
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IZCrmDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The RemoteFactory endpoint: one route for every factory call, no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

Neatoo is registered with `AddNeatooServices(Server)` plus `UseNeatoo()`. I deliberately didn't call `AddNeatooAspNetCore`: calling it after `AddNeatooServices(Server)` breaks every remote Save (RemoteFactory#117).
