using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/authorization.md documentation.
/// </summary>
public partial class AuthorizationSamples
{
    #region authorization-interface
    public interface IDocumentAuthorization
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool CanCreate();

        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead();

        [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
        bool CanWrite();
    }
    #endregion

    #region authorization-implementation
    public partial class DocumentAuthorization : IDocumentAuthorization
    {
        private readonly IUserContext _userContext;

        public DocumentAuthorization(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public bool CanCreate()
        {
            // Any authenticated user can create
            return _userContext.IsAuthenticated;
        }

        public bool CanRead()
        {
            // All authenticated users can read
            return _userContext.IsAuthenticated;
        }

        public bool CanWrite()
        {
            // Only editors and admins can write (update/delete)
            return _userContext.IsInRole("Editor") || _userContext.IsInRole("Admin");
        }
    }
    #endregion

    #region authorization-apply
    [Factory]
    [AuthorizeFactory<IDocumentAuthorization>]
    public partial class Document : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public Document()
        {
            Id = Guid.NewGuid();
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid documentId, [Service] IPersonRepository repository)
        {
            Id = documentId;
            Title = "Sample Document";
            Content = "Document content";
            IsNew = false;
            return Task.FromResult(true);
        }

        [Remote, Insert]
        public Task Insert([Service] IPersonRepository repository)
        {
            IsNew = false;
            return Task.CompletedTask;
        }

        [Remote, Update]
        public Task Update([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }

        [Remote, Delete]
        public Task Delete([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region authorization-generated
    // The generated factory includes Can* methods for client-side checks:
    public partial class GeneratedFactoryExample
    {
        public static async Task CheckAuthorizationBeforeCalling()
        {
            var scopes = SampleTestContainers.Scopes();
            var factory = scopes.client.GetRequiredService<IDocumentFactory>();

            // Check authorization before attempting operation
            if (factory.CanCreate().HasAccess)
            {
                var doc = factory.Create();
                // ... modify doc ...
            }

            var docId = Guid.NewGuid();
            if (factory.CanFetch().HasAccess)
            {
                var doc = await factory.Fetch(docId);
                // ... use doc ...
            }
        }
    }
    #endregion

    #region authorization-combined-flags
    public interface ICombinedFlagsAuthorization
    {
        // Single method covers both Create and Fetch operations
        [AuthorizeFactory(
            AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        bool CanCreateOrFetch();

        // Single method covers all write operations (Insert, Update, Delete)
        [AuthorizeFactory(
            AuthorizeFactoryOperation.Insert |
            AuthorizeFactoryOperation.Update |
            AuthorizeFactoryOperation.Delete)]
        bool CanWrite();
    }

    public partial class CombinedFlagsAuthorization : ICombinedFlagsAuthorization
    {
        private readonly IUserContext _userContext;

        public CombinedFlagsAuthorization(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public bool CanCreateOrFetch() => _userContext.IsAuthenticated;
        public bool CanWrite() => _userContext.IsInRole("Writer") || _userContext.IsInRole("Admin");
    }

    [Factory]
    [AuthorizeFactory<ICombinedFlagsAuthorization>]
    public partial class CombinedFlagsDocument
    {
        public Guid Id { get; private set; }

        [Create]
        public CombinedFlagsDocument() { Id = Guid.NewGuid(); }
    }
    #endregion

    #region authorization-method-level
    public interface IProjectAuthorization
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead();
    }

    public partial class ProjectAuthorization : IProjectAuthorization
    {
        private readonly IUserContext _userContext;
        public ProjectAuthorization(IUserContext userContext) { _userContext = userContext; }
        public bool CanRead() => _userContext.IsAuthenticated;
    }

    [Factory]
    [AuthorizeFactory<IProjectAuthorization>]
    public partial class ProjectWithMethodAuth : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public bool IsArchived { get; private set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public ProjectWithMethodAuth() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            IsNew = false;
            return Task.FromResult(true);
        }

        // Method-level authorization - only admins can archive (using Update)
        [Remote, Update]
        [AspAuthorize(Roles = "Admin")]
        public Task Archive()
        {
            IsArchived = true;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region authorization-policy-config
    public static class AuthorizationPolicyConfig
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("RequireEditor", policy =>
                    policy.RequireRole("Editor", "Admin"));

                options.AddPolicy("RequireAuthenticated", policy =>
                    policy.RequireAuthenticatedUser());
            });
        }
    }
    #endregion

    #region authorization-policy-apply
    [Factory]
    public partial class PolicyProtectedResource
    {
        public Guid Id { get; private set; }
        public string Data { get; private set; } = string.Empty;

        [Create]
        public PolicyProtectedResource() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        [AspAuthorize("RequireAuthenticated")]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            Data = "Fetched data";
            return Task.FromResult(true);
        }

        // For admin-only operations with policy, use Fetch with different parameters
        [Remote, Fetch]
        [AspAuthorize("RequireAdmin")]
        public Task<bool> FetchAdminOnly(Guid id, bool includePrivateData)
        {
            Id = id;
            Data = includePrivateData ? "Private admin data" : "Admin operation completed";
            return Task.FromResult(true);
        }
    }
    #endregion

    #region authorization-policy-multiple
    // For Execute with multiple policies, use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class MultiplePolicyResource
    {
        // Requires both policies to be satisfied
        [Remote, Execute]
        [AspAuthorize("RequireAuthenticated")]
        [AspAuthorize("RequireAdmin")]
        private static Task _SensitiveOperation(Guid resourceId)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region authorization-policy-roles
    [Factory]
    public partial class RoleProtectedResource
    {
        public Guid Id { get; private set; }

        [Create]
        public RoleProtectedResource() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        [AspAuthorize(Roles = "User,Admin,Manager")]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }

    // Role-based Execute operations in static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class RoleProtectedOperations
    {
        [Remote, Execute]
        [AspAuthorize(Roles = "Admin,Manager")]
        private static Task _ManagerOperation(Guid resourceId)
        {
            return Task.CompletedTask;
        }

        [Remote, Execute]
        [AspAuthorize(Roles = "Admin")]
        private static Task _AdminOnlyOperation(Guid resourceId)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region authorization-combined
    public interface IReportAuthorization
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        bool CanAccess();
    }

    public partial class ReportAuthorization : IReportAuthorization
    {
        private readonly IUserContext _userContext;
        public ReportAuthorization(IUserContext userContext) { _userContext = userContext; }
        public bool CanAccess() => _userContext.IsAuthenticated;
    }

    [Factory]
    [AuthorizeFactory<IReportAuthorization>]
    public partial class Report
    {
        public Guid Id { get; private set; }

        [Create]
        public Report() { Id = Guid.NewGuid(); }

        // Custom auth (IReportAuthorization.CanAccess) runs first
        // Then ASP.NET Core policy check
        [Remote, Fetch]
        [AspAuthorize("RequireAuthenticated")]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }

    }

    // Combined auth with Execute in static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ReportOperations
    {
        // Custom auth runs through factory auth, then ASP.NET Core policy
        [Remote, Execute]
        [AspAuthorize(Roles = "Admin")]
        private static Task<string> _GenerateReport(Guid reportId)
        {
            return Task.FromResult($"Report {reportId} generated");
        }
    }
    #endregion

    #region authorization-exception
    public partial class AuthorizationExceptionHandling
    {
        // [Fact]
        public async Task HandleNotAuthorizedException()
        {
            var scopes = SampleTestContainers.Scopes();

            // Configure user without required role
            var userContext = scopes.server.GetRequiredService<MockUserContext>();
            userContext.IsAuthenticated = false;

            var factory = scopes.client.GetRequiredService<IDocumentFactory>();

            try
            {
                // This will throw NotAuthorizedException if user lacks permission
                var doc = factory.Create();
                await Task.CompletedTask;
            }
            catch (NotAuthorizedException ex)
            {
                // Handle unauthorized access
                Assert.NotNull(ex.Message);
            }
        }
    }
    #endregion

    #region authorization-events
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public partial class EventAuthorizationExample
    {
        public Guid Id { get; private set; }

        [Create]
        public EventAuthorizationExample() { Id = Guid.NewGuid(); }

        // Events bypass authorization - use for internal operations
        // that should always execute regardless of user permissions
        [Event]
        public Task NotifyAdmins(
            string message,
            [Service] IEmailService emailService,
            CancellationToken ct)
        {
            return emailService.SendAsync("admins@example.com", "Notification", message, ct);
        }
    }
    #endregion

    #region authorization-testing
    public partial class AuthorizationTests
    {
        // [Fact]
        public void AuthorizedUser_CanCreate()
        {
            var scopes = SampleTestContainers.Scopes();

            // Configure user with authentication
            var userContext = scopes.server.GetRequiredService<MockUserContext>();
            userContext.IsAuthenticated = true;
            userContext.Roles = ["User"];

            var factory = scopes.local.GetRequiredService<IDocumentFactory>();

            // Should succeed
            var canCreate = factory.CanCreate();
            Assert.True(canCreate.HasAccess);

            var doc = factory.Create();
            Assert.NotNull(doc);
        }

        // [Fact]
        public void UnauthorizedUser_CannotDelete()
        {
            var scopes = SampleTestContainers.Scopes();

            // Configure user without Admin role
            var userContext = scopes.server.GetRequiredService<MockUserContext>();
            userContext.IsAuthenticated = true;
            userContext.Roles = ["User"]; // Not Admin

            var factory = scopes.local.GetRequiredService<IDocumentFactory>();

            // Check authorization first - CanDelete checks delete permission
            var canDelete = factory.CanDelete();
            Assert.False(canDelete.HasAccess);
        }
    }
    #endregion

    #region authorization-context
    public interface IAuthContextAuthorization
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead();
    }

    public partial class AuthContextAuthorization : IAuthContextAuthorization
    {
        private readonly IUserContext _userContext;

        public AuthContextAuthorization(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public bool CanRead()
        {
            // Access user claims and context
            var userId = _userContext.UserId;
            var username = _userContext.Username;
            var roles = _userContext.Roles;

            // Check specific claims or roles
            if (!_userContext.IsAuthenticated)
                return false;

            // Custom authorization logic based on claims
            return _userContext.IsInRole("Reader") || _userContext.IsInRole("Admin");
        }
    }

    [Factory]
    [AuthorizeFactory<IAuthContextAuthorization>]
    public partial class AuthContextResource
    {
        public Guid Id { get; private set; }

        [Create]
        public AuthContextResource() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    // [Fact]
    public void AuthorizationInterface_Compiles()
    {
        var userContext = new MockUserContext { IsAuthenticated = true, Roles = ["Admin"] };
        var auth = new DocumentAuthorization(userContext);

        Assert.True(auth.CanCreate());
        Assert.True(auth.CanWrite()); // CanWrite covers Update/Delete
    }

    // [Fact]
    public void Document_WithAuthorization_Works()
    {
        var scopes = SampleTestContainers.Scopes();

        var userContext = scopes.server.GetRequiredService<MockUserContext>();
        userContext.IsAuthenticated = true;
        userContext.Roles = ["Admin"];

        var factory = scopes.local.GetRequiredService<IDocumentFactory>();

        var doc = factory.Create();
        Assert.NotNull(doc);
        Assert.NotEqual(Guid.Empty, doc.Id);
    }
}
