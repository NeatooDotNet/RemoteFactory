# Remote Factory
Neatoo Remote Factory is a Data Mapper Factory for Domain Models powered by Roslyn source generators. It allows Domain Model objects to be 2-Tier client/server and authentication agnostic. Used with [Neatoo](https://github.com/NeatooDotNet/Neatoo) you can create streamlined Rich Domain Models in .NET with authorization, business rules, data-binding and meta properties. All with a single controller and no DTOs*. It is Dependency Injection and Async/Await centric. Plus high performance thanks to the source generators. 

Create streamlined Blazor and WPF applications with Neatoo!

See the [Person Demo example](https://github.com/NeatooDotNet/RemoteFactory/tree/main/src/Examples/Person) shown in the animation below. Given a [domain model object](https://github.com/NeatooDotNet/RemoteFactory/blob/main/src/Examples/Person/Person.DomainModel/PersonModel.cs) and optional [authorization object](https://github.com/NeatooDotNet/RemoteFactory/blob/main/src/Examples/Person/Person.DomainModel/PersonModelAuth.cs) the [factory is generated by Neatoo](https://github.com/NeatooDotNet/RemoteFactory/blob/main/src/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs) using that provides 2-Tier implementation and ties in the authorization.

![Person Demo Gif](https://github.com/NeatooDotNet/RemoteFactory/blob/main/RemoteFactory%20Person.gif "Person Demo")

*No Dtos for the UI to Service/Application layer. Other patterns like repository may require DTOs.

## Getting Started

To get started with Neatoo Remote Factory, follow these steps:

__1. Install the [Neatoo Remote Factory NuGet package](https://www.nuget.org/packages/Neatoo.RemoteFactory) in your client and server projects.__

__2. Register the necessary services in ASP.NET server application. Be sure to include your Domain Model library assembly.__

```csharp
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Local, typeof(IPersonModel).Assembly);
```

__3. Add a single controller to your ASP.NET server application__

```csharp
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request) =>
{
	var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
	return handleRemoteDelegateRequest(request);
});
```

__4. In your client application, register the Neatoo Remote Factory services__
	Note: Update the URL in the following code.
```csharp

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);
builder.Services.AddKeyedScoped(Neatoo.RemoteFactory.RemoteFactoryServices.HttpClientKey, (sp, key) => {
		return new HttpClient { BaseAddress = new Uri("http://localhost:5183/") };
});
```


__5. Create a domain model class and add the [Factory] attribute__
```csharp
[Factory]
public class PersonModel : IPersonModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int Age { get; set; }
}
```

__6. Add Factory Operation methods including Create, Fetch, Update, Insert and Delete__
```csharp
...
[Fetch]
public async Task<bool> Fetch([Service] IPersonContext personContext)
{
	var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
	if (personEntity == null)
	{
		return false;
	}
	this.MapFrom(personEntity);
	this.IsNew = false;
	return true;
}
```
__7. Neato Remote Factory will use Roslyn to automatically generate a corresponding factory class__

```csharp
    public interface IPersonModelFactory
    {
        IPersonModel? Create();
        Task<IPersonModel?> Fetch();
        Task<IPersonModel?> Save(IPersonModel target);
    }
```

## Example
For an example Blazor Standalone application see [here](https://github.com/NeatooDotNet/RemoteFactory/tree/main/src/Examples/Person).
It shows all of the available operations: Create, Fetch, Insert, Update and Delete.
It also shows how to add Authorization.
For now, you do need to load the entire solution. You also need to deploy the [EF database migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli#tabpanel_3_dotnet-core-cli). 

_Please reach out to me if you have any interest. More to come!_
