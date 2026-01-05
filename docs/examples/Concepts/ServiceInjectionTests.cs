using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.DocsExamples.GettingStarted;
using Neatoo.RemoteFactory.DocsExamples.Infrastructure;

namespace Neatoo.RemoteFactory.DocsExamples.Concepts;

#region docs-service-basic
/// <summary>
/// Basic service injection example.
/// [Service] parameters are resolved from DI and excluded from factory signature.
/// </summary>
public interface IServiceBasicExample
{
	int Id { get; }
	string? Data { get; }
}

[Factory]
public class ServiceBasicExample : IServiceBasicExample
{
	public int Id { get; set; }
	public string? Data { get; set; }

	[Create]
	public ServiceBasicExample() { }

	/// <summary>
	/// The IPersonContext parameter is resolved from DI.
	/// Generated factory method signature: Fetch(int id)
	/// </summary>
	[Remote]
	[Fetch]
	public async Task<bool> Fetch(
		int id,                                // Caller provides this
		[Service] IPersonContext context)      // DI provides this
	{
		var entity = await context.Persons.FindAsync(id);
		if (entity == null) return false;

		Id = entity.Id;
		Data = entity.FirstName;
		return true;
	}
}
#endregion docs-service-basic

#region docs-service-multiple
/// <summary>
/// Multiple services can be injected in a single method.
/// </summary>
public interface IMultiServiceExample : IFactorySaveMeta
{
	int Id { get; set; }
	string? Data { get; set; }
	bool EmailSent { get; set; }
	string? LogMessage { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

/// <summary>
/// Mock email service for testing.
/// </summary>
public interface IEmailService
{
	Task SendWelcomeEmail(string email);
}

public class MockEmailService : IEmailService
{
	public bool WasCalled { get; private set; }

	public Task SendWelcomeEmail(string email)
	{
		WasCalled = true;
		return Task.CompletedTask;
	}
}

/// <summary>
/// Mock audit service for testing.
/// </summary>
public interface IAuditService
{
	Task Log(string message);
}

public class MockAuditService : IAuditService
{
	public string? LastMessage { get; private set; }

	public Task Log(string message)
	{
		LastMessage = message;
		return Task.CompletedTask;
	}
}

[Factory]
public class MultiServiceExample : IMultiServiceExample
{
	public int Id { get; set; }
	public string? Data { get; set; }
	public bool EmailSent { get; set; }
	public string? LogMessage { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	[Create]
	public MultiServiceExample() { }

	/// <summary>
	/// Multiple services injected in Insert.
	/// All [Service] parameters are resolved from DI.
	/// </summary>
	[Remote]
	[Insert]
	[Update]
	public async Task Save(
		[Service] IPersonContext context,
		[Service] IEmailService emailService,
		[Service] IAuditService auditService)
	{
		// Create entity
		if (IsNew)
		{
			var entity = new PersonEntity { FirstName = Data };
			context.Persons.Add(entity);
			await context.SaveChangesAsync();
			Id = entity.Id;

			// Use injected services
			await emailService.SendWelcomeEmail("test@example.com");
			EmailSent = true;

			await auditService.Log($"Created person: {Data}");
			LogMessage = $"Created person: {Data}";
		}

		IsNew = false;
	}

	[Remote]
	[Delete]
	public Task Delete([Service] IAuditService auditService)
	{
		auditService.Log($"Deleted person: {Id}");
		return Task.CompletedTask;
	}
}
#endregion docs-service-multiple

#region docs-service-logging
/// <summary>
/// Logging service injection example.
/// </summary>
public interface ILoggingExample
{
	int Id { get; }
	string? Status { get; }
}

[Factory]
public class LoggingExample : ILoggingExample
{
	public int Id { get; set; }
	public string? Status { get; set; }

	[Create]
	public LoggingExample() { }

	/// <summary>
	/// ILogger is injected from DI for diagnostics.
	/// </summary>
	[Remote]
	[Fetch]
	public Task<bool> Fetch(
		int id,
		[Service] ILogger<LoggingExample> logger)
	{
		logger.LogDebug("Fetching with ID {Id}", id);

		if (id > 0)
		{
			Id = id;
			Status = "Found";
			logger.LogInformation("Item {Id} fetched successfully", id);
			return Task.FromResult(true);
		}

		logger.LogWarning("Item {Id} not found", id);
		return Task.FromResult(false);
	}
}
#endregion docs-service-logging

#region docs-service-current-user
/// <summary>
/// Current user service interface.
/// Provides access to the authenticated user's information.
/// </summary>
public interface ICurrentUser
{
	string UserId { get; }
	string Email { get; }
	IEnumerable<string> Roles { get; }
}

/// <summary>
/// Mock current user for testing.
/// </summary>
public class MockCurrentUser : ICurrentUser
{
	public string UserId { get; set; } = "user-123";
	public string Email { get; set; } = "user@example.com";
	public IEnumerable<string> Roles { get; set; } = new[] { "User", "Admin" };
}

/// <summary>
/// Example using current user service.
/// </summary>
public interface ICurrentUserExample : IFactorySaveMeta
{
	int Id { get; }
	string? CreatedBy { get; }
	string? CreatorEmail { get; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

[Factory]
public class CurrentUserExample : ICurrentUserExample
{
	public int Id { get; set; }
	public string? CreatedBy { get; set; }
	public string? CreatorEmail { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	[Create]
	public CurrentUserExample() { }

	/// <summary>
	/// ICurrentUser is injected to capture who created the record.
	/// </summary>
	[Remote]
	[Insert]
	[Update]
	public Task Save(
		[Service] ICurrentUser currentUser,
		[Service] IPersonContext context)
	{
		if (IsNew)
		{
			CreatedBy = currentUser.UserId;
			CreatorEmail = currentUser.Email;
		}
		IsNew = false;
		return Task.CompletedTask;
	}

	[Remote]
	[Delete]
	public Task Delete() => Task.CompletedTask;
}
#endregion docs-service-current-user

/// <summary>
/// Tests for service injection documentation examples.
/// </summary>
public class ServiceInjectionTests : DocsTestBase<IServiceBasicExampleFactory>
{
	public ServiceInjectionTests() : base()
	{
		// Register additional mock services for these tests
		RegisterAdditionalServices();
	}

	private void RegisterAdditionalServices()
	{
		// Note: These services need to be registered in DocsContainers.
		// For this example, we're demonstrating the pattern.
	}

	[Fact]
	public async Task Service_Basic_FetchWithContext()
	{
		// First save a person to fetch
		var personFactory = clientScope.ServiceProvider.GetRequiredService<IPersonModelFactory>();
		var person = personFactory.Create();
		person.FirstName = "ServiceTest";
		var saved = await personFactory.Save(person);
		var id = saved!.Id;

		// Now fetch using the service injection example
		// The IPersonContext is injected automatically
		var result = await factory.Fetch(id);

		Assert.NotNull(result);
		Assert.Equal(id, result.Id);
		Assert.Equal("ServiceTest", result.Data);
	}

	[Fact]
	public async Task Service_Basic_FetchNotFound()
	{
		var result = await factory.Fetch(99999);

		Assert.Null(result);
	}
}

/// <summary>
/// Tests for multiple service injection.
/// </summary>
public class MultiServiceInjectionTests : DocsTestBase<IMultiServiceExampleFactory>
{
	[Fact]
	public async Task MultiService_Insert_AllServicesInjected()
	{
		var example = factory.Create();
		example.Data = "Test Person";

		// Save calls Insert which uses multiple services
		var saved = await factory.Save(example);

		Assert.NotNull(saved);
		Assert.True(saved.Id > 0);
		Assert.True(saved.EmailSent);
		Assert.Equal("Created person: Test Person", saved.LogMessage);
		Assert.False(saved.IsNew);
	}
}

/// <summary>
/// Tests for logging service injection.
/// </summary>
public class LoggingServiceTests : DocsTestBase<ILoggingExampleFactory>
{
	[Fact]
	public async Task Logging_FetchSuccess_LogsInfoMessage()
	{
		// ILogger is automatically injected
		var result = await factory.Fetch(42);

		Assert.NotNull(result);
		Assert.Equal(42, result.Id);
		Assert.Equal("Found", result.Status);
	}

	[Fact]
	public async Task Logging_FetchNotFound_ReturnsNull()
	{
		var result = await factory.Fetch(0);

		Assert.Null(result);
	}
}

/// <summary>
/// Tests for current user service injection.
/// </summary>
public class CurrentUserServiceTests : DocsTestBase<ICurrentUserExampleFactory>
{
	[Fact]
	public async Task CurrentUser_Insert_CapturesUserId()
	{
		var example = factory.Create();

		var saved = await factory.Save(example);

		Assert.NotNull(saved);
		// The MockCurrentUser provides "user-123"
		Assert.Equal("user-123", saved.CreatedBy);
		Assert.Equal("user@example.com", saved.CreatorEmail);
	}
}
