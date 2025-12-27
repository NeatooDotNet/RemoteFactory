---
layout: default
title: "WPF Application"
description: "Complete WPF application example with RemoteFactory"
parent: Examples
nav_order: 2
---

# WPF Application Example

This example demonstrates building a WPF desktop application using RemoteFactory with MVVM pattern, connecting to an ASP.NET Core server.

## Solution Structure

```
WpfExample/
├── WpfExample.sln
├── WpfExample.DomainModel/       # Shared domain models
│   ├── WpfExample.DomainModel.csproj
│   ├── Models/
│   │   ├── PersonModel.cs
│   │   └── IPersonModel.cs
│   └── Authorization/
│       ├── IPersonModelAuth.cs
│       └── PersonModelAuth.cs
├── WpfExample.Server/            # ASP.NET Core API
│   ├── WpfExample.Server.csproj
│   └── Program.cs
├── WpfExample.Ef/                # Entity Framework
│   ├── WpfExample.Ef.csproj
│   └── AppDbContext.cs
└── WpfExample.Client/            # WPF Application
    ├── WpfExample.Client.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    └── ViewModels/
        └── PersonViewModel.cs
```

## Domain Model Project

### WpfExample.DomainModel.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" Version="9.*" />
  </ItemGroup>

</Project>
```

### IPersonModel.cs

```csharp
using Neatoo.RemoteFactory;

namespace WpfExample.DomainModel;

public interface IPersonModel : IFactorySaveMeta
{
    int Id { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    string FullName { get; }
}
```

### PersonModel.cs

```csharp
using System.ComponentModel;
using Neatoo.RemoteFactory;
using WpfExample.Ef;

namespace WpfExample.DomainModel;

[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public partial class PersonModel : IPersonModel, INotifyPropertyChanged
{
    public int Id { get; set; }

    private string? _firstName;
    public string? FirstName
    {
        get => _firstName;
        set
        {
            if (_firstName != value)
            {
                _firstName = value;
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    private string? _lastName;
    public string? LastName
    {
        get => _lastName;
        set
        {
            if (_lastName != value)
            {
                _lastName = value;
                OnPropertyChanged(nameof(LastName));
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // IFactorySaveMeta
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Factory Operations
    [Create]
    public PersonModel()
    {
        FirstName = "";
        LastName = "";
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext ctx)
    {
        var entity = await ctx.Persons.FindAsync(id);
        if (entity == null) return false;

        MapFrom(entity);
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert][Update]
    public async Task Upsert([Service] IPersonContext ctx)
    {
        PersonEntity entity;

        if (IsNew)
        {
            entity = new PersonEntity();
            ctx.Persons.Add(entity);
        }
        else
        {
            entity = await ctx.Persons.FindAsync(Id)
                ?? throw new Exception($"Person {Id} not found");
        }

        MapTo(entity);
        await ctx.SaveChangesAsync();
        Id = entity.Id;
        IsNew = false;
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext ctx)
    {
        var entity = await ctx.Persons.FindAsync(Id);
        if (entity != null)
        {
            ctx.Persons.Remove(entity);
            await ctx.SaveChangesAsync();
        }
    }

    // Mapper methods
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
}
```

### Authorization

```csharp
// IPersonModelAuth.cs
using Neatoo.RemoteFactory;

namespace WpfExample.DomainModel;

public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanDelete();
}

// PersonModelAuth.cs
namespace WpfExample.DomainModel;

public class PersonModelAuth : IPersonModelAuth
{
    // For this example, everyone has access
    // In real apps, inject ICurrentUser and check permissions
    public bool CanAccess() => true;
    public bool CanCreate() => true;
    public bool CanFetch() => true;
    public bool CanUpdate() => true;
    public bool CanDelete() => true;
}
```

## Entity Framework Project

### AppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace WpfExample.Ef;

public class PersonEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
}

public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class AppDbContext : DbContext, IPersonContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<PersonEntity> Persons => Set<PersonEntity>();
}
```

## Server Project

### WpfExample.Server.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WpfExample.DomainModel\WpfExample.DomainModel.csproj" />
    <ProjectReference Include="..\WpfExample.Ef\WpfExample.Ef.csproj" />
  </ItemGroup>

</Project>
```

### Program.cs (Server)

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using WpfExample.DomainModel;
using WpfExample.Ef;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IPersonContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// RemoteFactory
builder.Services.AddNeatooAspNetCore(typeof(PersonModel).Assembly);

// Authorization
builder.Services.AddScoped<IPersonModelAuth, PersonModelAuth>();

// CORS for WPF client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure database created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseNeatoo();

app.Run();
```

## WPF Client Project

### WpfExample.Client.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\WpfExample.DomainModel\WpfExample.DomainModel.csproj" />
  </ItemGroup>

</Project>
```

### App.xaml.cs

```csharp
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using WpfExample.DomainModel;

namespace WpfExample.Client;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.DataContext = Services.GetRequiredService<PersonViewModel>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // RemoteFactory in Remote mode
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(PersonModel).Assembly);

        // HTTP Client for remote calls
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5001/")
            };
        });

        // Client-side authorization
        services.AddScoped<IPersonModelAuth, PersonModelAuth>();

        // ViewModels
        services.AddTransient<PersonViewModel>();
    }
}
```

### PersonViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfExample.DomainModel;

namespace WpfExample.Client;

public partial class PersonViewModel : ObservableObject
{
    private readonly IPersonModelFactory _factory;

    public PersonViewModel(IPersonModelFactory factory)
    {
        _factory = factory;

        // Initialize commands
        CreateCommand = new RelayCommand(Create, CanCreate);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        LoadCommand = new AsyncRelayCommand<int>(LoadAsync);

        // Initialize permissions
        RefreshPermissions();
    }

    // Observable Properties
    [ObservableProperty]
    private IPersonModel? _currentPerson;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canCreatePerson;

    [ObservableProperty]
    private bool _canSavePerson;

    [ObservableProperty]
    private bool _canDeletePerson;

    // Commands
    public ICommand CreateCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand<int> LoadCommand { get; }

    // Permission Checks
    private void RefreshPermissions()
    {
        CanCreatePerson = _factory.CanCreate().HasAccess;
        CanSavePerson = _factory.CanSave().HasAccess;
        CanDeletePerson = _factory.CanDelete().HasAccess;
    }

    private bool CanCreate() => CanCreatePerson && !IsBusy;
    private bool CanSave() => CurrentPerson != null && CanSavePerson && !IsBusy;
    private bool CanDelete() => CurrentPerson != null &&
                                !CurrentPerson.IsNew &&
                                CanDeletePerson && !IsBusy;

    // Command Implementations
    private void Create()
    {
        CurrentPerson = _factory.Create();
        StatusMessage = "New person created";
        RefreshCommandStates();
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading...";

            CurrentPerson = await _factory.Fetch(id);

            StatusMessage = CurrentPerson != null
                ? $"Loaded: {CurrentPerson.FullName}"
                : "Person not found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task SaveAsync()
    {
        if (CurrentPerson == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Saving...";

            var result = await _factory.TrySave(CurrentPerson);

            if (result.HasAccess)
            {
                CurrentPerson = result.Result;
                StatusMessage = "Saved successfully";
            }
            else
            {
                StatusMessage = $"Cannot save: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task DeleteAsync()
    {
        if (CurrentPerson == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Deleting...";

            CurrentPerson.IsDeleted = true;
            var result = await _factory.TrySave(CurrentPerson);

            if (result.HasAccess)
            {
                CurrentPerson = null;
                StatusMessage = "Deleted successfully";
            }
            else
            {
                CurrentPerson.IsDeleted = false;
                StatusMessage = $"Cannot delete: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            CurrentPerson.IsDeleted = false;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        (CreateCommand as RelayCommand)?.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }
}
```

### MainWindow.xaml

```xml
<Window x:Class="WpfExample.Client.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Person Editor" Height="400" Width="500">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,20">
            <Button Content="New"
                    Command="{Binding CreateCommand}"
                    IsEnabled="{Binding CanCreatePerson}"
                    Width="80" Margin="0,0,10,0"/>
            <Button Content="Save"
                    Command="{Binding SaveCommand}"
                    IsEnabled="{Binding CanSavePerson}"
                    Width="80" Margin="0,0,10,0"/>
            <Button Content="Delete"
                    Command="{Binding DeleteCommand}"
                    IsEnabled="{Binding CanDeletePerson}"
                    Width="80"/>
        </StackPanel>

        <!-- Form -->
        <Grid Grid.Row="1" IsEnabled="{Binding CurrentPerson, Converter={StaticResource NotNullConverter}}">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="100"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label Grid.Row="0" Grid.Column="0" Content="ID:"/>
            <TextBlock Grid.Row="0" Grid.Column="1"
                       Text="{Binding CurrentPerson.Id}"
                       VerticalAlignment="Center"/>

            <Label Grid.Row="1" Grid.Column="0" Content="First Name:"/>
            <TextBox Grid.Row="1" Grid.Column="1"
                     Text="{Binding CurrentPerson.FirstName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,5"/>

            <Label Grid.Row="2" Grid.Column="0" Content="Last Name:"/>
            <TextBox Grid.Row="2" Grid.Column="1"
                     Text="{Binding CurrentPerson.LastName, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,5"/>

            <Label Grid.Row="3" Grid.Column="0" Content="Email:"/>
            <TextBox Grid.Row="3" Grid.Column="1"
                     Text="{Binding CurrentPerson.Email, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,5"/>
        </Grid>

        <!-- Loading Indicator -->
        <ProgressBar Grid.Row="2"
                     IsIndeterminate="True"
                     Height="4"
                     Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>

        <!-- Status Bar -->
        <StatusBar Grid.Row="3" Margin="0,10,0,0">
            <StatusBarItem Content="{Binding StatusMessage}"/>
        </StatusBar>
    </Grid>
</Window>
```

### MainWindow.xaml.cs

```csharp
using System.Windows;

namespace WpfExample.Client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

## Running the Application

1. **Start the server**:
   ```bash
   cd WpfExample.Server
   dotnet run
   ```

2. **Start the WPF client**:
   ```bash
   cd WpfExample.Client
   dotnet run
   ```

## Key Patterns Demonstrated

### MVVM with RemoteFactory

- ViewModel injects `IPersonModelFactory`
- Commands bound to factory operations
- Can* methods control UI state

### INotifyPropertyChanged

The domain model implements `INotifyPropertyChanged` for WPF data binding:

```csharp
public string? FirstName
{
    get => _firstName;
    set
    {
        if (_firstName != value)
        {
            _firstName = value;
            OnPropertyChanged(nameof(FirstName));
        }
    }
}
```

### Command CanExecute

Commands use Can* methods for enable/disable state:

```csharp
private bool CanSave() => CurrentPerson != null &&
                          _factory.CanSave().HasAccess &&
                          !IsBusy;
```

### Error Handling

TrySave returns authorization status without throwing:

```csharp
var result = await _factory.TrySave(CurrentPerson);
if (!result.HasAccess)
{
    StatusMessage = $"Cannot save: {result.Message}";
}
```

## Next Steps

- **[Blazor Application](blazor-app.md)**: Web application example
- **[Common Patterns](common-patterns.md)**: Reusable patterns and recipes
- **[Factory Pattern](../concepts/factory-pattern.md)**: Understanding factories
