using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Authorization;

/// <summary>
/// Pins that generated throw sites pass context to <see cref="Neatoo.RemoteFactory.NotAuthorizedException"/>.
/// </summary>
/// <remarks>
/// The runtime supplies a non-empty default on its own, so a consumer who only updates the
/// package stops emitting empty exception messages. These tests cover the second half: a
/// consumer who REBUILDS gets a message naming what was refused, which is the half that has
/// to come from the generator.
///
/// The fixtures declare their own System usings: generated factories name Task,
/// CancellationToken and IServiceProvider unqualified and rely on the consumer's
/// ImplicitUsings, which a raw CSharpCompilation does not have (see issue #97, and the same
/// note on AssemblyAttributeEmissionTests' fixtures).
/// </remarks>
public class NotAuthorizedContextEmissionTests
{
    [Fact]
    public void ClassFactorySave_EmitsOperationAndType()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IOrderAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
        bool CanWrite();
    }

    public class OrderAuth : IOrderAuth
    {
        public bool CanWrite() { return false; }
    }

    public interface IOrder : IFactorySaveMeta { }

    [Factory]
    [AuthorizeFactory<IOrderAuth>]
    public partial class Order : IOrder
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Create]
        public void Create() { }

        [Insert]
        public void Insert() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generated);

        // Positive anchor first, so this cannot pass against an empty or unrelated tree.
        Assert.Contains("public virtual IOrder? Save(", generated);
        Assert.Contains("throw new NotAuthorizedException(authorized, \"operation Save on IOrder\");", generated);

        // The context-less form must be gone from this leg.
        Assert.DoesNotContain("throw new NotAuthorizedException(authorized);", generated);
    }

    [Fact]
    public void InterfaceFactory_EmitsOperationTypeAndTheAuthMethodThatRefused()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IQueryAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        bool CanQuery();

        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        string? CanQueryWithReason();
    }

    public class QueryAuth : IQueryAuth
    {
        public bool CanQuery() { return false; }
        public string? CanQueryWithReason() { return null; }
    }

    [Factory]
    [AuthorizeFactory<IQueryAuth>]
    public interface IQueryService
    {
        Task<string> GetData(Guid id);
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IQueryServiceFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generated);

        Assert.Contains("LocalGetData", generated);

        // One throw per auth method, each naming which check refused -- that is the question
        // an on-call engineer has when a method runs several.
        Assert.Contains("throw new NotAuthorizedException(authorized, \"operation GetData on IQueryService (IQueryAuth.CanQuery)\");", generated);
        Assert.Contains("throw new NotAuthorizedException(authorized, \"operation GetData on IQueryService (IQueryAuth.CanQueryWithReason)\");", generated);

        Assert.DoesNotContain("throw new NotAuthorizedException(authorized);", generated);
    }
}
