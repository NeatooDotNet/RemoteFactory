I've written this the way the Neatoo / RemoteFactory / MudNeatoo skills say to. It is not compiled. The Neatoo namespaces (`Neatoo`, `Neatoo.RemoteFactory`, `Neatoo.Rules`) are from memory, because the skills don't show them.

## How it works

The duplicate check is an **async rule on the `Contact` entity**, triggered by `Email`. A rule runs in the browser, so it can't take the DbContext. Instead it takes the delegate of a **`[Remote, Execute]` command** that runs the query on the server. Its message lands on the `Email` property, and `MudNeatooTextField` shows property messages under the field, so the page needs no code of its own for this. The same rule runs again on the server inside `Insert`/`Update`, which blocks a save that skipped the check.

Assumptions:
- The existing `zCRM.Data` project holds `ZCrmDbContext` (`DbSet<Contact> Contacts`) and the EF `Contact` class.
- The EF `Contact` has a `Guid Id` key. You didn't say what the key is.
- The domain project references `zCRM.Data`, so the `[Service]` parameter types compile.

---

## 1. Domain (`zCRM.Domain`, loaded by both client and server)

### The command that queries the database

```csharp
// zCRM.Domain/Contacts/ContactEmailUsage.cs
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Data;

namespace zCRM.Domain.Contacts;

/// <summary>
/// Answers whether a contact other than the given one already holds an email.
/// [Remote]: a client call crosses to the server, where the DbContext is.
/// </summary>
[Factory]
public static partial class ContactEmailUsage
{
    // Value parameters before [Service] parameters (RemoteFactory#135).
    [Remote]
    [Execute]
    private static Task<bool> _IsInUse(
        string email,
        Guid excludingContactId,
        [Service] ZCrmDbContext db)
    {
        // Case-insensitivity comes from the column collation (SQL Server's default is CI).
        return db.Contacts
            .AsNoTracking()
            .AnyAsync(c => c.Id != excludingContactId && c.Email == email);
    }
}
```

**Why here:** this is the only code that touches the database for the check. It is a static command in the domain assembly, and the generator turns it into a `ContactEmailUsage.IsInUse` delegate on both tiers. In the browser, calling the delegate sends an HTTP call. On the server, it calls the method directly. Passing `excludingContactId` means a contact doesn't count as a duplicate of itself.

### The rule

```csharp
// zCRM.Domain/Contacts/UniqueContactEmailRule.cs
using System.ComponentModel.DataAnnotations;
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

/// <summary>Contact takes the rule from DI through this interface; tests can substitute it.</summary>
internal interface IUniqueContactEmailRule : IRule<Contact> { }

/// <summary>
/// Email is unique across contacts. Runs whenever Email is set, so the message
/// shows on the field while the user edits, not after Save.
/// </summary>
internal sealed class UniqueContactEmailRule : AsyncRuleBase<Contact>, IUniqueContactEmailRule
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    private readonly ContactEmailUsage.IsInUse _isInUse;

    public UniqueContactEmailRule(ContactEmailUsage.IsInUse isInUse) : base(t => t.Email)
    {
        _isInUse = isInUse;
    }

    protected override async Task<IRuleMessages> Execute(Contact target, CancellationToken? token = null)
    {
        var email = target.Email?.Trim();

        // Empty or malformed: [Required]/[EmailAddress] report that; no server call.
        if (string.IsNullOrEmpty(email) || !EmailFormat.IsValid(email))
        {
            return None;
        }

        if (await _isInUse(email, target.Id))
        {
            return (nameof(Contact.Email), $"Another contact already uses {email}.").AsRuleMessages();
        }

        return None;
    }
}
```

**Why here:** the trigger is a property change, so the logic is a rule on the entity. It takes the command delegate, never the DbContext. The entity is also built in the browser, and a server-only service in its constructor would break that.

### The entity

```csharp
// zCRM.Domain/Contacts/IContact.cs
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}
```

```csharp
// zCRM.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Data;
using ContactRow = zCRM.Data.Contact;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; set; }

    [DisplayName("First name")]
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name")]
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not a valid address")]
    [StringLength(256)]
    public partial string? Email { get; set; }

    public Contact(
        IEntityBaseServices<Contact> services,
        IUniqueContactEmailRule uniqueEmailRule) : base(services)
    {
        RuleManager.AddRule(uniqueEmailRule);
    }

    /// <summary>
    /// A new contact. Local, no server call. The key is set here so the
    /// uniqueness rule can exclude this contact before it is first saved.
    /// </summary>
    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] ZCrmDbContext db)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
    {
        // Server gate: re-runs every rule here, the uniqueness query included
        // (the delegate calls the method directly on the server). Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] ZCrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.SingleOrDefaultAsync(c => c.Id == Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = Email!.Trim();
    }
}
```

**Why here:** `Contact` is a Neatoo entity because a person edits it and the Save button depends on its validity. `[Create]` is local, so a new contact needs no server call. `Fetch`, `Insert` and `Update` are `[Remote] internal`: they are where the client's calls cross to the server, and the DbContext is a `[Service]` parameter resolved there. The EF class is aliased as `ContactRow` because both classes are named `Contact`.

### Rule registration

```csharp
// zCRM.Domain/DomainRegistration.cs
using Microsoft.Extensions.DependencyInjection;
using zCRM.Domain.Contacts;

namespace zCRM.Domain;

/// <summary>
/// The domain's DI-provided rules. Call on BOTH tiers, after AddNeatooServices:
/// Contact takes a rule in its constructor and is built on the client too.
/// </summary>
public static class DomainRegistration
{
    public static IServiceCollection AddZCrmDomainRules(this IServiceCollection services)
    {
        // Transient: each entity instance gets its own rule instance.
        services.AddTransient<IUniqueContactEmailRule, UniqueContactEmailRule>();
        return services;
    }
}
```

**Why here:** the rule types are `internal`, so only the domain assembly can register them.

---

## 2. Server (`zCRM.Server`)

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using zCRM.Data;
using zCRM.Domain;
using zCRM.Domain.Contacts;

var builder = WebApplication.CreateBuilder(args);

// Neatoo + generated factories in Server mode. Do not also call
// AddNeatooAspNetCore after this - it breaks every remote Save (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

// Server-only: what the [Service] parameters resolve.
builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// One endpoint for every factory call, including ContactEmailUsage.IsInUse.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

**Why here:** the DbContext is registered only on the server. There is no controller or DTO for the email check. The command goes through the same `UseNeatoo()` endpoint as `Fetch` and `Save`.

---

## 3. Client (`zCRM.Client`, Blazor WASM)

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remote mode: [Remote] operations and commands cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

### ViewModel

```csharp
// zCRM.Client/ViewModels/ContactEditViewModel.cs
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>Contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    public IContact? Contact { get; private set; }

    /// <summary>Save() does not mark the entity busy (Neatoo#103); this stops a double-click inserting twice.</summary>
    public bool IsSaving { get; private set; }

    public void New()
    {
        Contact = _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
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
            // An email check may still be in flight.
            await Contact.WaitForTasks();

            if (!Contact.IsSavable)
            {
                return; // the duplicate message is already on the Email field
            }

            Contact = (IContact)await Contact.Save(); // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

**Why here:** the ViewModel turns gestures into domain calls and reads the entity's answers. It has no duplicate check of its own. `IsSavable` is already false while the check runs or when it fails.

### `_Imports.razor` (additions)

```razor
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Domain.Contacts
@using zCRM.Client.ViewModels
```

### The edit page

```razor
@* zCRM.Client/Pages/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm

<MudText Typo="Typo.h5" Class="mb-4">Contact</MudText>

@if (Vm.Contact is null)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else
{
    @* @key: Save returns a new instance; rebuild the fields so they bind to it. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />

        @* When the field loses focus, the value commits and UniqueContactEmailRule
           queries the server. The field is disabled while the check runs, and
           "Another contact already uses ..." appears as its error text. *@
        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Contact[nameof(IContact.Email)]"
                            Variant="Variant.Outlined"
                            InputType="InputType.Email"
                            Class="mb-3" />

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)"
                   OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

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
            Vm.New();
        }

        Resubscribe();
    }

    // The fields subscribe to their own properties; the Save button and the
    // summary read the entity, so the page re-renders on the graph-wide event.
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
        StateHasChanged(); // show the button disabled while IsSaving
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

**Why here:** the page only binds. `MudNeatooTextField` already shows the Email property's messages and disables itself while the check runs. The Save button binds to `CanSave`, which comes from the entity's `IsSavable`. No `EditForm` or `DataAnnotationsValidator` is needed.

---

## Things to know

- **"Straight away" means when the user leaves the field, not on every keystroke.** `MudNeatooTextField` commits on blur, so each finished value costs one server call. Checking per keystroke would send a query on every character.
- **The pre-save check doesn't cover a race.** Two users could pass the check and then save the same email at the same moment. The server gate shrinks that window but can't close it. Only a unique index on `Contacts.Email` closes it. That's a schema change, so I haven't included it.
- **Case-insensitive matching depends on the database.** The query uses `==`, so it relies on the column's collation (SQL Server's default ignores case). On PostgreSQL, use `citext` or compare normalised values.
- **The client gets the EF Core assembly through its domain reference, but no query code runs there.** The only method bodies that use the DbContext are `[Remote]` or `internal`, so they run on the server. With trimming and `IsServerRuntime=false`, they are removed from the published client.
