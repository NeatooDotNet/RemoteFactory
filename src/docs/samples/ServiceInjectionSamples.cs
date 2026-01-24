using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/service-injection.md documentation.
/// </summary>
public partial class ServiceInjectionSamples
{
    #region service-injection-basic
    [Factory]
    public partial class BasicServiceInjection
    {
        public Guid Id { get; private set; }
        public string Data { get; private set; } = string.Empty;

        [Create]
        public BasicServiceInjection() { Id = Guid.NewGuid(); }

        [Remote]
        [Fetch]
        public async Task Fetch(Guid id, [Service] IPersonRepository repository)
        {
            // IPersonRepository is injected from DI container on server
            var entity = await repository.GetByIdAsync(id);
            if (entity != null)
            {
                Id = entity.Id;
                Data = $"{entity.FirstName} {entity.LastName}";
            }
        }
    }
    #endregion

    #region service-injection-multiple
    // Multiple service injection with [Execute] - use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class MultipleServiceInjection
    {
        [Remote]
        [Execute]
        private static async Task<string> _ProcessOrder(
            Guid orderId,
            [Service] IOrderRepository orderRepository,
            [Service] IPersonRepository personRepository,
            [Service] IUserContext userContext)
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            var customer = await personRepository.GetByIdAsync(order?.CustomerId ?? Guid.Empty);

            return $"Order {order?.OrderNumber} for {customer?.FirstName} processed by {userContext.Username}";
        }
    }
    #endregion

    #region service-injection-scoped
    public interface IScopedAuditContext
    {
        Guid CorrelationId { get; }
        void LogAction(string action);
    }

    public partial class ScopedAuditContext : IScopedAuditContext
    {
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public List<string> Actions { get; } = new();

        public void LogAction(string action)
        {
            Actions.Add($"[{CorrelationId}] {action}");
        }
    }

    // Scoped service injection with [Execute] - use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ScopedServiceInjection
    {
        [Remote]
        [Execute]
        private static Task<Guid> _ProcessWithAudit(
            string action,
            [Service] IScopedAuditContext auditContext)
        {
            // Scoped service - same instance throughout the request
            auditContext.LogAction(action);
            return Task.FromResult(auditContext.CorrelationId);
        }
    }
    #endregion

    #region service-injection-constructor
    public interface ICalculationService
    {
        decimal Calculate(decimal amount, decimal rate);
    }

    public partial class CalculationService : ICalculationService
    {
        public decimal Calculate(decimal amount, decimal rate) => amount * rate;
    }

    [Factory]
    public partial class ConstructorServiceInjection
    {
        private readonly ICalculationService _calculationService;

        public decimal Result { get; private set; }

        [Create]
        public ConstructorServiceInjection([Service] ICalculationService calculationService)
        {
            _calculationService = calculationService;
        }

        public void Calculate(decimal amount, decimal rate)
        {
            Result = _calculationService.Calculate(amount, rate);
        }
    }
    #endregion

    #region service-injection-server-only
    public interface IServerOnlyDatabaseService
    {
        Task<string> ExecuteQueryAsync(string query);
    }

    public partial class ServerOnlyDatabaseService : IServerOnlyDatabaseService
    {
        public Task<string> ExecuteQueryAsync(string query)
        {
            // This service only exists on the server
            return Task.FromResult($"Query result for: {query}");
        }
    }

    [Factory]
    public partial class ServerOnlyServiceExample
    {
        public string QueryResult { get; private set; } = string.Empty;

        [Create]
        public ServerOnlyServiceExample() { }

        // This method only runs on server where IServerOnlyDatabaseService is registered
        [Remote]
        [Fetch]
        public async Task Fetch(
            string query,
            [Service] IServerOnlyDatabaseService databaseService)
        {
            QueryResult = await databaseService.ExecuteQueryAsync(query);
        }
    }
    #endregion

    #region service-injection-client
    public interface IClientLoggerService
    {
        void Log(string message);
    }

    public partial class ClientLoggerService : IClientLoggerService
    {
        public List<string> Messages { get; } = new();
        public void Log(string message) => Messages.Add(message);
    }

    [Factory]
    public partial class ClientServiceExample
    {
        public bool Logged { get; private set; }

        [Create]
        public ClientServiceExample([Service] IClientLoggerService logger)
        {
            // IClientLoggerService available on both client and server
            logger.Log("ClientServiceExample created");
            Logged = true;
        }
    }
    #endregion

    #region service-injection-matching-name
    // When using RegisterMatchingName:
    // IPersonRepository -> PersonRepository
    // IOrderRepository -> OrderRepository
    // The pattern: Interface name starts with 'I', implementation has same name without 'I'

    public static class MatchingNameRegistration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // Automatically registers IPersonRepository -> PersonRepository
            // if both types exist in the assembly
            services.RegisterMatchingName(typeof(ServiceInjectionSamples).Assembly);
        }
    }
    #endregion

    #region service-injection-mixed
    // Mixed parameters with [Execute] - use static partial class
    public record MixedParametersResult(Guid ProcessedId, string ProcessedBy, bool Cancelled);

    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class MixedParametersExample
    {
        [Remote]
        [Execute]
        private static async Task<MixedParametersResult> _ProcessItem(
            Guid itemId,                              // Value parameter
            string notes,                             // Value parameter
            [Service] IPersonRepository repository,   // Service parameter
            [Service] IUserContext userContext,       // Service parameter
            CancellationToken cancellationToken)      // CancellationToken
        {
            cancellationToken.ThrowIfCancellationRequested();

            await repository.GetByIdAsync(itemId, cancellationToken);

            return new MixedParametersResult(
                itemId,
                userContext.Username,
                cancellationToken.IsCancellationRequested);
        }
    }
    #endregion

    #region service-injection-httpcontext
    // Note: IHttpContextAccessor is available in ASP.NET Core projects
    // This example shows the pattern for accessing HttpContext

    public interface IHttpContextAccessorSimulator
    {
        string? GetUserId();
        string? GetCorrelationId();
    }

    public partial class HttpContextAccessorSimulator : IHttpContextAccessorSimulator
    {
        public string? GetUserId() => "user-123";
        public string? GetCorrelationId() => Guid.NewGuid().ToString();
    }

    [Factory]
    public partial class HttpContextExample
    {
        public string? UserId { get; private set; }
        public string? CorrelationId { get; private set; }

        [Create]
        public HttpContextExample() { }

        [Remote]
        [Fetch]
        public Task Fetch([Service] IHttpContextAccessorSimulator httpContextAccessor)
        {
            // Access HttpContext on server to get user info, headers, etc.
            UserId = httpContextAccessor.GetUserId();
            CorrelationId = httpContextAccessor.GetCorrelationId();
            return Task.CompletedTask;
        }
    }
    #endregion

    #region service-injection-serviceprovider
    // ServiceProvider injection with [Execute] - use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ServiceProviderExample
    {
        [Remote]
        [Execute]
        private static Task<bool> _ResolveServices([Service] IServiceProvider serviceProvider)
        {
            // Dynamically resolve services when needed
            var repository = serviceProvider.GetService<IPersonRepository>();
            var userContext = serviceProvider.GetService<IUserContext>();

            return Task.FromResult(repository != null && userContext != null);
        }
    }
    #endregion

    #region service-injection-lifetimes
    // Service Lifetimes in RemoteFactory:
    //
    // Singleton: One instance for application lifetime
    //   - Use for stateless services, caches, configuration
    //   - Same instance across all requests and scopes
    //
    // Scoped: One instance per request/operation
    //   - Use for DbContext, unit of work, request-specific state
    //   - New instance for each factory operation
    //
    // Transient: New instance every time resolved
    //   - Use for lightweight, stateless services
    //   - New instance each time [Service] parameter is resolved

    public static class ServiceLifetimeRegistration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // Singleton - shared across all operations
            services.AddSingleton<ICalculationService, CalculationService>();

            // Scoped - per-operation instance
            services.AddScoped<IScopedAuditContext, ScopedAuditContext>();

            // Transient - new instance each time
            services.AddTransient<IClientLoggerService, ClientLoggerService>();
        }
    }
    #endregion

    #region service-injection-testing
    // Register test doubles instead of production services
    public static class ServiceInjectionTestSetup
    {
        public static void ConfigureTestServices(IServiceCollection services)
        {
            // In-memory implementations for testing
            services.AddScoped<IPersonRepository, InMemoryPersonRepository>();
            services.AddScoped<IUserContext, TestUserContext>();
        }
    }

    // Simple in-memory repository for tests
    public class InMemoryPersonRepository : IPersonRepository
    {
        private readonly Dictionary<Guid, PersonEntity> _store = new();

        public Task<PersonEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.GetValueOrDefault(id));

        public Task AddAsync(PersonEntity entity, CancellationToken ct = default)
        { _store[entity.Id] = entity; return Task.CompletedTask; }

        public Task<List<PersonEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_store.Values.ToList());

        public Task UpdateAsync(PersonEntity entity, CancellationToken ct = default)
        { _store[entity.Id] = entity; return Task.CompletedTask; }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        { _store.Remove(id); return Task.CompletedTask; }

        public Task SaveChangesAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }

    // Controllable test double for user context
    public class TestUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "testuser";
        public string[] Roles { get; set; } = ["User"];
        public bool IsAuthenticated { get; set; } = true;
        public bool IsInRole(string role) => Roles.Contains(role);
    }
    #endregion

    [Fact]
    public async Task BasicServiceInjection_Works()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IBasicServiceInjectionFactory>();

        var instance = factory.Create();
        Assert.NotNull(instance);
        Assert.NotEqual(Guid.Empty, instance.Id);
    }
}
