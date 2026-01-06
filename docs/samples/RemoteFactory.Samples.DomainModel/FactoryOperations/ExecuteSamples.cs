/// <summary>
/// Code samples for docs/concepts/factory-operations.md - Execute operations
///
/// NOTE: [Execute] methods use underscore prefix (_MethodName) to avoid
/// naming conflicts with generated delegate types (MethodName).
/// </summary>

using Neatoo.RemoteFactory;
using Microsoft.EntityFrameworkCore;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations.ExecuteExamples;

#region docs:concepts/factory-operations:execute-operations
[Factory]
public static partial class PersonOperationsExample
{
    [Remote]
    [Execute]
    private static async Task<int> _GetPersonCount([Service] IPersonContext context)
    {
        return await context.Persons.CountAsync();
    }

    [Remote]
    [Execute]
    private static async Task<List<string>> _GetAllEmails([Service] IPersonContext context)
    {
        return await context.Persons
            .Where(p => p.Email != null)
            .Select(p => p.Email!)
            .ToListAsync();
    }
}
#endregion

// Note: The generated factory interface is shown in the docs as:
// public interface IPersonOperationsFactory
// {
//     Task<int> GetPersonCount();
//     Task<List<string>> GetAllEmails();
// }

// Commands & Queries Pattern samples

#region docs:concepts/factory-operations:query-request-response
// Request (criteria value object)
public class GetUserQuery
{
    public int UserId { get; set; }
}

// Response (result value object)
public class UserResult
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}
#endregion

#region docs:concepts/factory-operations:query-handler
// The query handler
[Factory]
public static partial class UserQueriesExample
{
    [Remote]
    [Execute]
    private static async Task<UserResult?> _GetUser(
        GetUserQuery query,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(query.UserId);
        if (user == null) return null;

        return new UserResult
        {
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive
        };
    }
}
#endregion

// Note: The generated factory interface is shown in the docs as:
// public interface IUserQueriesFactory
// {
//     Task<UserResult?> GetUser(GetUserQuery query);
// }

#region docs:concepts/factory-operations:command-request-response
// Command request
public class DeactivateUserCommand
{
    public int UserId { get; set; }
    public string? Reason { get; set; }
}

// Command result
public class CommandResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
#endregion

#region docs:concepts/factory-operations:command-handler
[Factory]
public static partial class UserCommandsExample
{
    [Remote]
    [Execute]
    private static async Task<CommandResult> _DeactivateUser(
        DeactivateUserCommand command,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(command.UserId);
        if (user == null)
        {
            return new CommandResult { Success = false, Message = "User not found" };
        }

        user.IsActive = false;
        user.DeactivationReason = command.Reason;
        await ctx.SaveChangesAsync();

        return new CommandResult { Success = true, Message = "User deactivated" };
    }
}
#endregion

#region docs:concepts/factory-operations:search-criteria
public class ProductSearchCriteria
{
    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ProductSearchResults
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
}
#endregion

#region docs:concepts/factory-operations:search-handler
[Factory]
public static partial class ProductSearchExample
{
    [Remote]
    [Execute]
    private static async Task<ProductSearchResults> _Search(
        ProductSearchCriteria criteria,
        [Service] IProductContext ctx)
    {
        var query = ctx.Products.AsQueryable();

        if (!string.IsNullOrEmpty(criteria.Keyword))
            query = query.Where(p => p.Name!.Contains(criteria.Keyword));

        if (criteria.MinPrice.HasValue)
            query = query.Where(p => p.Price >= criteria.MinPrice.Value);

        if (criteria.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= criteria.MaxPrice.Value);

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToListAsync();

        return new ProductSearchResults
        {
            Products = products,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)criteria.PageSize)
        };
    }
}
#endregion

#region docs:concepts/factory-operations:blazor-query-usage
public class BlazorQueryUsageExample
{
    private readonly IUserQueriesFactory _userQueries;
    private UserResult? _user;

    public BlazorQueryUsageExample(IUserQueriesFactory userQueries)
    {
        _userQueries = userQueries;
    }

    public async Task LoadUser(int userId)
    {
        _user = await _userQueries.GetUser(new GetUserQuery { UserId = userId });
    }
}
#endregion

// Supporting interfaces and entities
public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
}

public class PersonEntity
{
    public int Id { get; set; }
    public string? Email { get; set; }
}

public interface IUserContext
{
    DbSet<UserEntity> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UserEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string? DeactivationReason { get; set; }
}

public interface IProductContext
{
    DbSet<ProductEntity> Products { get; }
}

public class ProductEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
}

// Generated factory interface for sample usage
public interface IUserQueriesFactory
{
    Task<UserResult?> GetUser(GetUserQuery query);
}
