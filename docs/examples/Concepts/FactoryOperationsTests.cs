using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.DocsExamples.Infrastructure;

namespace Neatoo.RemoteFactory.DocsExamples.Concepts;

#region docs-factory-ops-create-constructor
/// <summary>
/// Create operation using a constructor.
/// The [Create] attribute marks a constructor for factory generation.
/// </summary>
[Factory]
public class CreateConstructorExample
{
	[Create]
	public CreateConstructorExample()
	{
		CreatedAt = DateTime.UtcNow;
	}

	public DateTime CreatedAt { get; set; }
}
#endregion docs-factory-ops-create-constructor

#region docs-factory-ops-create-method
/// <summary>
/// Create operation using an instance method.
/// Useful when you need initialization logic with parameters.
/// </summary>
[Factory]
public class CreateMethodExample
{
	public string? InitializedValue { get; set; }

	[Create]
	public CreateMethodExample() { }

	[Create]
	public void Initialize(string value)
	{
		InitializedValue = value;
	}
}
#endregion docs-factory-ops-create-method

#region docs-factory-ops-create-static
/// <summary>
/// Create operation using a static method.
/// Returns a new instance of the class.
/// </summary>
[Factory]
public class CreateStaticExample
{
	public string? Source { get; set; }

	[Create]
	public CreateStaticExample() { }

	[Create]
	public static CreateStaticExample FromSource(string source)
	{
		return new CreateStaticExample { Source = source };
	}
}
#endregion docs-factory-ops-create-static

#region docs-factory-ops-fetch-bool
/// <summary>
/// Fetch operation returning bool.
/// When the method returns false, the factory returns null.
/// </summary>
[Factory]
public class FetchBoolExample
{
	public int Id { get; set; }
	public string? Name { get; set; }

	[Create]
	public FetchBoolExample() { }

	[Remote]
	[Fetch]
	public Task<bool> Fetch(int id)
	{
		// Simulated fetch - in real code, query database
		if (id == 1)
		{
			Id = 1;
			Name = "Found Item";
			return Task.FromResult(true);
		}
		return Task.FromResult(false);
	}
}
#endregion docs-factory-ops-fetch-bool

#region docs-factory-ops-insert-update
/// <summary>
/// Combined Insert and Update operation.
/// Uses IsNew property to determine which operation to perform.
/// </summary>
public interface IInsertUpdateExample : IFactorySaveMeta
{
	int Id { get; set; }
	string? Value { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

[Factory]
public class InsertUpdateExample : IInsertUpdateExample
{
	private static int _nextId = 1000;

	public int Id { get; set; }
	public string? Value { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	[Create]
	public InsertUpdateExample() { }

	[Remote]
	[Insert]
	[Update]
	public Task Save()
	{
		if (IsNew)
		{
			// Insert: assign new ID
			Id = _nextId++;
		}
		// Update: ID already set
		IsNew = false;
		return Task.CompletedTask;
	}
}
#endregion docs-factory-ops-insert-update

#region docs-factory-ops-delete
/// <summary>
/// Delete operation.
/// Called when IsDeleted is true during Save.
/// </summary>
public interface IDeleteExample : IFactorySaveMeta
{
	int Id { get; set; }
	bool WasDeleted { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

[Factory]
public class DeleteExample : IDeleteExample
{
	public int Id { get; set; }
	public bool WasDeleted { get; set; }
	public bool IsNew { get; set; }
	public bool IsDeleted { get; set; }

	[Create]
	public DeleteExample() { }

	[Remote]
	[Fetch]
	public Task<bool> Fetch(int id)
	{
		Id = id;
		IsNew = false;
		return Task.FromResult(true);
	}

	[Remote]
	[Insert]
	[Update]
	public Task Save()
	{
		IsNew = false;
		return Task.CompletedTask;
	}

	[Remote]
	[Delete]
	public Task Delete()
	{
		WasDeleted = true;
		return Task.CompletedTask;
	}
}
#endregion docs-factory-ops-delete

#region docs-factory-ops-execute
/// <summary>
/// Execute operation for static methods.
/// Used for operations that don't require object state.
/// Execute methods must be in a static partial class.
/// Methods are private with underscore prefix - generator creates public delegates.
/// </summary>
[Factory]
public static partial class CalculationService
{
	[Remote]
	[Execute]
	private static Task<int> _CalculateSum(int a, int b)
	{
		return Task.FromResult(a + b);
	}

	[Remote]
	[Execute]
	private static Task<string> _FormatMessage(string template, string value)
	{
		return Task.FromResult(string.Format(template, value));
	}
}
#endregion docs-factory-ops-execute

/// <summary>
/// Tests for Create operations.
/// </summary>
public class CreateOperationsTests : DocsTestBase<ICreateConstructorExampleFactory>
{
	[Fact]
	public void Create_Constructor_SetsCreatedAt()
	{
		var before = DateTime.UtcNow;
		var example = factory.Create();
		var after = DateTime.UtcNow;

		Assert.NotNull(example);
		Assert.True(example.CreatedAt >= before && example.CreatedAt <= after);
	}

	[Fact]
	public void Create_Method_InitializesWithParameter()
	{
		var methodFactory = clientScope.ServiceProvider.GetRequiredService<ICreateMethodExampleFactory>();

		var example = methodFactory.Initialize("test-value");

		Assert.NotNull(example);
		Assert.Equal("test-value", example.InitializedValue);
	}

	[Fact]
	public void Create_Static_ReturnsConfiguredInstance()
	{
		var staticFactory = clientScope.ServiceProvider.GetRequiredService<ICreateStaticExampleFactory>();

		var example = staticFactory.FromSource("my-source");

		Assert.NotNull(example);
		Assert.Equal("my-source", example.Source);
	}
}

/// <summary>
/// Tests for Fetch operations.
/// </summary>
public class FetchOperationsTests : DocsTestBase<IFetchBoolExampleFactory>
{
	[Fact]
	public async Task Fetch_ExistingItem_ReturnsPopulatedObject()
	{
		var example = await factory.Fetch(1);

		Assert.NotNull(example);
		Assert.Equal(1, example.Id);
		Assert.Equal("Found Item", example.Name);
	}

	[Fact]
	public async Task Fetch_NonExistingItem_ReturnsNull()
	{
		var example = await factory.Fetch(999);

		Assert.Null(example);
	}
}

/// <summary>
/// Tests for Insert/Update operations.
/// Note: Factory method is named SaveSave when Delete is not defined.
/// When all three (Insert/Update/Delete) are defined, it becomes Save.
/// </summary>
public class InsertUpdateOperationsTests : DocsTestBase<IInsertUpdateExampleFactory>
{
	[Fact]
	public async Task Insert_NewItem_AssignsId()
	{
		var example = factory.Create();
		example.Value = "New Value";

		Assert.True(example.IsNew);
		Assert.Equal(0, example.Id);

		// Factory method is SaveSave (MethodName + Operation) when Delete is not defined
		var saved = await factory.SaveSave(example);

		Assert.NotNull(saved);
		Assert.True(saved.Id >= 1000);
		Assert.False(saved.IsNew);
	}

	[Fact]
	public async Task Update_ExistingItem_PreservesId()
	{
		var example = factory.Create();
		var saved = await factory.SaveSave(example);
		var originalId = saved.Id;

		saved.Value = "Updated Value";
		var updated = await factory.SaveSave(saved);

		Assert.NotNull(updated);
		Assert.Equal(originalId, updated.Id);
		Assert.Equal("Updated Value", updated.Value);
	}
}

/// <summary>
/// Tests for Delete operations.
/// When all three (Insert/Update/Delete) are defined, factory method is Save.
/// </summary>
public class DeleteOperationsTests : DocsTestBase<IDeleteExampleFactory>
{
	[Fact]
	public async Task Delete_MarkedItem_ExecutesDeleteOperation()
	{
		var example = await factory.Fetch(42);
		Assert.NotNull(example);
		Assert.False(example.IsNew);

		example.IsDeleted = true;
		// Factory.Save handles Insert/Update/Delete based on IsNew and IsDeleted
		var deleted = await factory.Save(example);

		Assert.NotNull(deleted);
		Assert.True(deleted.WasDeleted);
	}
}

/// <summary>
/// Tests for Execute operations.
/// Execute methods generate delegates that are resolved from DI.
/// </summary>
public class ExecuteOperationsTests : DocsTestBase<ICreateConstructorExampleFactory>
{
	[Fact]
	public async Task Execute_CalculateSum_ReturnsCorrectResult()
	{
		// Execute methods are resolved as delegates from DI
		var calculateSum = clientScope.ServiceProvider.GetRequiredService<CalculationService.CalculateSum>();

		var result = await calculateSum(5, 3);

		Assert.Equal(8, result);
	}

	[Fact]
	public async Task Execute_FormatMessage_ReturnsFormattedString()
	{
		var formatMessage = clientScope.ServiceProvider.GetRequiredService<CalculationService.FormatMessage>();

		var result = await formatMessage("Hello, {0}!", "World");

		Assert.Equal("Hello, World!", result);
	}
}
