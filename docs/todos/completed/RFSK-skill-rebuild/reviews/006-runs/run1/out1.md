I've written the Contact screen across three projects. I haven't compiled any of it, so treat it as unbuilt until `dotnet build` passes. The patterns come from the Neatoo, RemoteFactory and MudNeatoo references. I also checked Neatoo's source to confirm that `[EmailAddress]` becomes a rule automatically.

**Layout:** `zCRM.Domain` is one class library referenced by both the client and the server, so the same classes run on both sides. `zCRM.Server` is ASP.NET Core with EF Core. `zCRM.Client` is Blazor WebAssembly with MudBlazor.

---

## 1. Domain (`zCRM.Domain`, used by both client and server)

The domain is in the shared assembly so the rules run in the browser as the salesperson types, and the server runs the same rules again before it writes. Each concrete class is `internal` and has a public interface. The child interfaces extend `IEntityBase`, which has no `Save()`, so a phone can only be saved through its contact.

### `Persistence.cs`: row shapes and the store contract

These sit in the domain because the factory methods take them as parameters. They are plain classes with no EF reference, so the browser bundle never pulls in EF Core. The server implements the interface.

```csharp
namespace zCRM.Domain;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public ICollection<PhoneRow> Phones { get; set; } = new List<PhoneRow>();
}

public class PhoneRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Number { get; set; } = "";
    public PhoneType Type { get; set; }
}

/// <summary>
/// The store the Contact aggregate reads and writes. Implemented on the server by the EF Core DbContext.
/// </summary>
public interface IContactDbContext
{
    /// <summary>The contact row with its phone rows, or null if there is none.</summary>
    Task<ContactRow?> FindContactAsync(Guid id);

    void AddContact(ContactRow row);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### `PhoneType.cs`

```csharp
namespace zCRM.Domain;

public enum PhoneType
{
    Home,
    Mobile,
    Work
}
```

### `Contact.cs`: the aggregate root

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

/// <summary>
/// Contact aggregate root.
/// </summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    IPhoneList? Phones { get; }
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; set; }

    [DisplayName("First name")]
    [Required(ErrorMessage = "First name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name")]
    [Required(ErrorMessage = "Last name is required")]
    public partial string? LastName { get; set; }

    // Optional, but well formed when present: the EmailAddress rule passes null and empty.
    [DisplayName("Email")]
    [EmailAddress(ErrorMessage = "Email is not a valid address")]
    public partial string? Email { get; set; }

    public partial IPhoneList? Phones { get; set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
    }

    /// <summary>
    /// A blank contact. Local: no server call.
    /// </summary>
    /// <remarks>
    /// Runs the rules so the required fields show their messages at once and Save stays
    /// disabled until they are filled. The phone list is empty, so All touches nothing else.
    /// </remarks>
    [Create]
    public async Task Create([Service] IPhoneListFactory phoneListFactory)
    {
        Phones = phoneListFactory.Create();
        await RunRules(RunRulesFlag.All);
    }

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IContactDbContext db,
        [Service] IPhoneListFactory phoneListFactory)
    {
        var row = await db.FindContactAsync(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;

        Phones = phoneListFactory.Fetch(row.Phones);
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IContactDbContext db,
        [Service] IPhoneListFactory phoneListFactory)
    {
        // Server gate: refuses an invalid aggregate from a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new ContactRow();
        MapTo(row);
        db.AddContact(row);

        phoneListFactory.Save(Phones!, row.Phones);

        await db.SaveChangesAsync();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IContactDbContext db,
        [Service] IPhoneListFactory phoneListFactory)
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

        phoneListFactory.Save(Phones!, row.Phones);

        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Id = Id;
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;
    }
}
```

### `Phone.cs`: the child entity

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

/// <summary>
/// A contact's phone number. Saved through the Contact.
/// </summary>
public interface IPhone : IEntityBase
{
    Guid Id { get; }
    string? Number { get; set; }
    PhoneType Type { get; set; }
}

[Factory]
internal partial class Phone : EntityBase<Phone>, IPhone
{
    public partial Guid Id { get; set; }

    [DisplayName("Number")]
    [Required(ErrorMessage = "Phone number is required")]
    public partial string? Number { get; set; }

    [DisplayName("Type")]
    public partial PhoneType Type { get; set; }

    public Phone(IEntityBaseServices<Phone> services) : base(services)
    {
    }

    /// <summary>
    /// A blank phone. Public and local: the client builds it before it is added.
    /// Runs its rules so the empty number shows its message and blocks Save.
    /// </summary>
    [Create]
    public async Task Create()
    {
        Type = PhoneType.Mobile;
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(PhoneRow row)
    {
        Id = row.Id;
        Number = row.Number;
        Type = row.Type;
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
        row.Type = Type;
    }
}
```

### `PhoneList.cs`: the child list

The list matches its phones to the contact row's `Phones` collection. Each phone writes its own row, and a removed phone's row is taken out of the collection. `AddPhone()` lives on the list because `Phone.Create` takes no arguments, so the ViewModel never needs a phone factory. `IPhoneFactory` arrives through the constructor because it is registered on both client and server.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

public interface IPhoneList : IEntityListBase<IPhone>
{
    /// <summary>Creates a blank phone and adds it to the contact.</summary>
    Task<IPhone> AddPhone();
}

[Factory]
internal partial class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    private readonly IPhoneFactory _phoneFactory;

    public PhoneList(IPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    [Create]
    public void Create()
    {
    }

    public async Task<IPhone> AddPhone()
    {
        var phone = await _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    [Fetch]
    internal void Fetch(IEnumerable<PhoneRow> rows)
    {
        foreach (var row in rows)
        {
            Add(_phoneFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<PhoneRow> rows)
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
                _phoneFactory.Save(phone, row);
            }
            else if (phone.IsModified)
            {
                _phoneFactory.Save(phone, rows.Single(r => r.Id == phone.Id));
            }
        }
    }
}
```

---

## 2. Server (`zCRM.Server`)

### `ZCrmDbContext.cs`

The DbContext is on the server only. It implements the domain's `IContactDbContext`, so the domain never references EF.
- **`ValueGeneratedNever()` is required.** The domain assigns its own Guid keys. Without this setting, EF assumes a new `PhoneRow` with a key already set is an existing row, tries to update it, and fails.
- **Cascade delete is required.** It makes EF delete a phone row when the list removes it from `row.Phones`.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain;

namespace zCRM.Server;

public sealed class ZCrmDbContext : DbContext, IContactDbContext
{
    public ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : base(options)
    {
    }

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
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.FirstName).IsRequired();
            e.Property(c => c.LastName).IsRequired();
            e.HasMany(c => c.Phones)
                .WithOne()
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneRow>(e =>
        {
            e.ToTable("Phone");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Number).IsRequired();
            e.Property(p => p.Type).HasConversion<string>().HasMaxLength(10);
        });
    }
}
```

### `Program.cs`

This registers Neatoo in Server mode and the database the `[Service]` parameters use. `UseNeatoo()` maps the single endpoint that every factory call goes through, so there are no controllers.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Domain;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

// Neatoo in Server mode. Do not also call AddNeatooAspNetCore after this (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// Swap UseSqlServer for your provider.
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IContactDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## 3. Client (`zCRM.Client`)

### `Program.cs`

This registers Neatoo in Remote mode, which turns the generated factories into proxies, plus the `HttpClient` those proxies use and the ViewModel. Nothing server-only is registered here.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

### `_Imports.razor` (relevant lines)

```razor
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zCRM.Domain
@using zCRM.Client.ViewModels
```

### `ViewModels/ContactEditViewModel.cs`

The ViewModel holds the live contact, passes the salesperson's actions (add phone, remove phone, save) to the domain, and reads the domain's answers. It never checks validity itself; Save is enabled by `IsSavable`. `IsSaving` exists because `Save()` does not mark the entity busy (Neatoo#103), so a double-click would otherwise send two inserts.

```csharp
using zCRM.Domain;

namespace zCRM.Client.ViewModels;

/// <summary>
/// ViewModel for the Contact edit screen: holds the aggregate, forwards phone gestures, saves.
/// </summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>The requested contact does not exist.</summary>
    public bool NotFound { get; private set; }

    /// <summary>A save is in flight; Save() does not mark the entity busy (Neatoo#103).</summary>
    public bool IsSaving { get; private set; }

    /// <summary>What the phone-type select offers.</summary>
    public IReadOnlyList<PhoneType> PhoneTypes { get; } = Enum.GetValues<PhoneType>();

    public async Task New()
    {
        Contact = await _contactFactory.Create();
        NotFound = false;
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
        NotFound = Contact is null;
    }

    public async Task AddPhone()
    {
        if (Contact?.Phones is null)
        {
            return;
        }

        await Contact.Phones.AddPhone();
    }

    public void RemovePhone(IPhone phone)
    {
        Contact?.Phones?.Remove(phone);
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
                return;  // the field messages already say why
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

### `Pages/ContactEdit.razor`

The page only binds. The MudNeatoo fields take the label, the validation messages and the busy and read-only state from the entity's own property objects, so there is no `EditForm`, `MudForm` or `DataAnnotationsValidator`. `@key` rebuilds the fields when `Save` returns a new instance. The page subscribes to the entity so the Save button updates as you type.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@inject ContactEditViewModel Vm
@implements IDisposable

<PageTitle>Contact</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium">
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h4" Class="mb-4">Contact</MudText>

        @if (Vm.NotFound)
        {
            <MudAlert Severity="Severity.Warning">This contact does not exist.</MudAlert>
        }
        else if (Vm.Contact is null)
        {
            <MudText>Loading...</MudText>
        }
        else
        {
            <div @key="Vm.Contact">
                <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

                <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
                <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
                <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

                <MudText Typo="Typo.h6" Class="mt-4 mb-2">Phone numbers</MudText>

                @if (Vm.Contact.Phones is not null)
                {
                    @foreach (var phone in Vm.Contact.Phones)
                    {
                        <MudGrid @key="phone" Spacing="2" Class="mb-2">
                            <MudItem xs="12" sm="6">
                                <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPhone.Number)]" Variant="Variant.Outlined" />
                            </MudItem>
                            <MudItem xs="10" sm="5">
                                <MudNeatooSelect T="PhoneType" EntityProperty="@phone[nameof(IPhone.Type)]">
                                    @foreach (var type in Vm.PhoneTypes)
                                    {
                                        <MudSelectItem T="PhoneType" Value="@type">@type</MudSelectItem>
                                    }
                                </MudNeatooSelect>
                            </MudItem>
                            <MudItem xs="2" sm="1" Class="d-flex align-center">
                                @* A fetched phone moves to DeletedList; its row is removed on Save *@
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
    </MudPaper>
</MudContainer>

@code {
    [Parameter] public Guid? Id { get; set; }

    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        Resubscribe();
    }

    // Each field re-renders itself, but the Save button reads the root's IsSavable,
    // so the page listens for changes anywhere in the graph.
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

---

## Decisions to check

- **Phone number is required.** Your spec only required first and last name. I added this because a phone row with no number makes no sense; remove the `[Required]` on `Phone.Number` if you disagree.
- **Email is optional.** Your spec only said "well formed", so a blank email is allowed. Add `[Required]` to `Email` if every contact must have one.
- **New phones start as Mobile.** That's an arbitrary default; change it in `Phone.Create`.
- **No delete.** The screen has no Delete button, so `Contact` has no `[Delete]` operation.
- **A new contact stays on `/contacts/new` after saving.** The contact is saved and the form shows it, but the URL doesn't change. Navigate to `/contacts/{Id}` after the first save if you want it to.
- **Not included:** the MudBlazor layout setup (`MudThemeProvider` and friends) and the IL-trimming settings for publishing the client.
