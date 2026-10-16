# Duplicate-email check on the zCRM contact edit screen

The check is a rule on the `Contact` entity. When the user leaves the Email field, the rule runs, calls the server through a `[Remote, Execute]` command, and puts "Another contact already uses …" under the field. The Save button stays disabled while the check runs and while the message is there. The server runs the same rule again before it writes.

I wrote this from the Neatoo, RemoteFactory and MudNeatoo skill references, and I checked namespaces against the Neatoo Design projects. You asked me not to create files, so none of this has been compiled.

## Assumed: the existing EF Core model (not new code)

I need a key, so I've assumed `Guid Id`. Change the types if yours differ.

```csharp
// ZCrm.Data (existing) - shown only to pin the names the domain code uses.
namespace ZCrm.Data;

public class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
}
```

---

## 1. Domain: `ZCrm.Domain`, loaded on both the client and the server

### `IContact.cs`
**Why here:** the public interface is the only way the page and the ViewModel refer to a contact. It extends `IEntityRoot`, so it has `IsSavable` and `Save()`.

```csharp
using Neatoo;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// Contact aggregate root.
/// </summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}
```

### `ContactEmailCheck.cs`
**Why here:** the rule runs in the browser, so it can't take the DbContext. This static command is the server call the rule makes. `[Remote]` sends the client's call to the server, where `[Service] ZCrmDbContext` is resolved. On the server, the same delegate calls the method directly. Value parameters come before the `[Service]` parameter (RemoteFactory#135).

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using ZCrm.Data;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// Server lookup behind ContactEmailUniqueRule. The client gets the generated
/// ContactEmailCheck.IsEmailInUse delegate; the query runs on the server.
/// </summary>
[Factory]
public static partial class ContactEmailCheck
{
    /// <summary>
    /// True when a contact other than <paramref name="excludeContactId"/> already has
    /// <paramref name="email"/>, case-insensitive.
    /// </summary>
    [Remote, Execute]
    private static Task<bool> _IsEmailInUse(string email, Guid excludeContactId, [Service] ZCrmDbContext db)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return db.Contacts.AnyAsync(c => c.Id != excludeContactId && c.Email.ToLower() == normalized);
    }
}
```

### `ContactEmailUniqueRule.cs`
**Why here:** Neatoo puts logic that a property change drives into a rule on the entity. The rule only takes the command delegate, so `Contact` can still be built on the client. It has a DI interface so the entity gets it from the container and tests can swap it out.

```csharp
using Neatoo;
using Neatoo.Rules;

namespace ZCrm.Domain.Contacts;

internal interface IContactEmailUniqueRule : IRule<Contact> { }

/// <summary>
/// Email must not belong to another contact. Runs when Email is set; IsBusy is
/// true (and IsSavable false) while the server answers.
/// </summary>
internal class ContactEmailUniqueRule : AsyncRuleBase<Contact>, IContactEmailUniqueRule
{
    private readonly ContactEmailCheck.IsEmailInUse _isEmailInUse;

    public ContactEmailUniqueRule(ContactEmailCheck.IsEmailInUse isEmailInUse) : base(t => t.Email)
    {
        _isEmailInUse = isEmailInUse;
    }

    protected override async Task<IRuleMessages> Execute(Contact target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            return None;  // [Required] reports the empty case; no server call
        }

        // Excluding this contact's own Id lets an existing contact keep its email;
        // a new contact's Id (set in Create) matches no row.
        if (await _isEmailInUse(target.Email, target.Id))
        {
            return (nameof(Contact.Email), $"Another contact already uses {target.Email.Trim()}.").AsRuleMessages();
        }

        return None;
    }
}
```

### `Contact.cs`
**Why here:** this is the aggregate root, and its rules and persistence operations live with it.

- `Create` is local and runs the rules, so a blank form shows its required fields.
- `Fetch`, `Insert` and `Update` are `[Remote] internal`, so the DbContext is used only on the server.
- `Insert` and `Update` run every rule again, uniqueness included, and refuse to write an invalid contact. This is the server's safety net for a caller that skips `IsSavable`. It also catches an email someone else took between the field check and the save.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Data;
using ContactRow = ZCrm.Data.Contact;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// Contact aggregate root.
/// </summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services, IContactEmailUniqueRule emailUniqueRule) : base(services)
    {
        RuleManager.AddRule(emailUniqueRule);
    }

    public partial Guid Id { get; set; }

    [DisplayName("First Name")]
    [Required(ErrorMessage = "First name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name")]
    [Required(ErrorMessage = "Last name is required")]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    [Required(ErrorMessage = "Email is required")]
    public partial string? Email { get; set; }

    /// <summary>
    /// Local: a new contact needs nothing from the server. Runs the rules so the
    /// empty form reports its required fields (the email rule skips an empty email).
    /// </summary>
    [Create]
    public async Task Create()
    {
        Id = Guid.NewGuid();
        await RunRules(RunRulesFlag.All);  // no children, so All is safe
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] ZCrmDbContext db)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (row == null)
        {
            return false;
        }

        // Paused inside the operation: baseline, no rules, nothing modified.
        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] ZCrmDbContext db)
    {
        // Server gate: re-run every rule (including the uniqueness check) and refuse
        // an invalid contact. Throw, never return - after Insert returns the
        // framework marks the entity saved.
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

    [Remote, Update]
    internal async Task Update([Service] ZCrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.SingleOrDefaultAsync(c => c.Id == Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        MapTo(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = Email!;
    }
}
```

### `DomainRegistration.cs`
**Why here:** rule types are `internal`, so only the domain assembly can register them. Both tiers call this, because `Contact` takes the rule in its constructor and is built on the client too.

```csharp
using Microsoft.Extensions.DependencyInjection;
using ZCrm.Domain.Contacts;

namespace ZCrm.Domain;

public static class DomainRegistration
{
    /// <summary>Call on BOTH tiers, after AddNeatooServices.</summary>
    public static IServiceCollection AddZCrmDomainRules(this IServiceCollection services)
    {
        // Transient: a rule tracks execution state per entity instance.
        services.AddTransient<IContactEmailUniqueRule, ContactEmailUniqueRule>();
        return services;
    }
}
```

**Domain project file:** the domain references the DbContext project, so these settings keep EF Core out of the browser bundle (RemoteFactory's trimming setup):

```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Neatoo" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
  <ProjectReference Include="..\ZCrm.Data\ZCrm.Data.csproj" PrivateAssets="all" />
</ItemGroup>
```

---

## 2. Server: `ZCrm.Server/Program.cs`

**Why here:** the server is the only tier that registers `ZCrmDbContext`, so every `[Service] ZCrmDbContext` parameter is resolved here. `UseNeatoo()` maps the single endpoint that every factory call and the email command go through, so there are no controllers. Use `AddNeatooServices(Server)` and do not also call `AddNeatooAspNetCore` (RemoteFactory#117).

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using ZCrm.Data;
using ZCrm.Domain;
using ZCrm.Domain.Contacts;

var builder = WebApplication.CreateBuilder(args);

// Your existing DbContext registration; the provider is whatever zCRM uses.
builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZCrm")));

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## 3. Client: `ZCrm.Client`

### `Program.cs`
**Why here:** in Remote mode the generated `IContactFactory` and `ContactEmailCheck.IsEmailInUse` are proxies that send their calls through the keyed `HttpClient`. No DbContext is registered here.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Client;
using ZCrm.Client.ViewModels;
using ZCrm.Domain;
using ZCrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

**Client project file:** this switch lets publishing trim the server-only code out of the domain:

```xml
<ItemGroup>
  <PackageReference Include="Neatoo.Blazor.MudNeatoo" />
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />
</ItemGroup>
```

### `_Imports.razor` (additions)

```razor
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using ZCrm.Domain.Contacts
@using ZCrm.Client.ViewModels
```

### `ViewModels/ContactEditViewModel.cs`
**Why here:** the ViewModel holds the contact and saves it. It decides nothing about email validity. `CanSave` just reads `IsSavable`, which is already false while the check runs or the duplicate message is showing. `IsSaving` stops a double click from sending two Inserts, because `Save()` doesn't mark the entity busy (Neatoo#103).

```csharp
using ZCrm.Domain.Contacts;

namespace ZCrm.Client.ViewModels;

/// <summary>
/// ViewModel for the contact edit screen: holds the aggregate, saves it.
/// </summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded, or when not found.</summary>
    public IContact? Contact { get; private set; }

    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Contact = await _contactFactory.Create();
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
            await Contact.WaitForTasks();  // an email check may still be in flight

            if (!Contact.IsSavable)
            {
                return;  // the messages are already on the fields
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
**Why here:** the page only binds. `MudNeatooTextField` writes Email when the field loses focus, which runs the rule. While the rule waits for the server the field is disabled, and when it finishes the duplicate message appears under the field. The page subscribes to the entity so the Save button re-renders when `IsSavable` changes. `@key` rebuilds the fields against the new instance after a save.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm

<PageTitle>Contact</PageTitle>

@if (_loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (Vm.Contact is null)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else
{
    <div @key="Vm.Contact">
        <MudText Typo="Typo.h5" Class="mb-4">@(Vm.Contact.IsNew ? "New contact" : "Edit contact")</MudText>

        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />

        @* Committing this field (blur or Enter) sets Email, which runs
           ContactEmailUniqueRule: the field is disabled while the server answers,
           then "Another contact already uses ..." shows as its error. *@
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

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

    private bool _loading = true;
    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;

        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        _loading = false;
        Resubscribe();
    }

    // The fields subscribe to their own properties; the Save button reads the
    // entity, so the page re-renders on the entity's change event.
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
        Resubscribe();
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

## Things to know

- **The check runs when the field is committed, not on every keystroke.** That means one server call per email entered. Checking per keystroke would make a server call for every character.
- **Two people saving the same email at the same moment can still both get through.** The field check is a convenience. The server gate narrows the gap, but only a unique index on Email (case-insensitive, to match the query) fully prevents duplicates. If the server gate refuses a save, the user sees an exception, not a message on the field. I didn't add handling for that.
- **Clean up any duplicates already in the table.** Loading a contact doesn't run rules, but saving does. A contact that already shares its email with another would be refused even if the user only changed its first name.
