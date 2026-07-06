using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies the two-bucket DTO preservation emission (TRIM-001): positional records
/// (no public parameterless ctor) get DtoConstructorRegistry.PreserveType&lt;T&gt;() calls,
/// while plain DTOs keep getting Register&lt;T&gt;(() => new T()) — as return types, as
/// non-service parameters, and nested through property graphs in both directions.
/// </summary>
public class RecordDtoDiscoveryTests
{
    private static string RunAndGetGeneratedSource(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        return string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));
    }

    /// <summary>
    /// Extracts DtoConstructorRegistry.Register type arguments from generated source.
    /// </summary>
    private static HashSet<string> GetRegisteredDtoTypes(string generatedSource)
    {
        var registered = new HashSet<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            generatedSource,
            @"DtoConstructorRegistry\.Register<(.+?)>\(\(\)");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            registered.Add(match.Groups[1].Value);
        }

        return registered;
    }

    /// <summary>
    /// Extracts DtoConstructorRegistry.PreserveType type arguments from generated source.
    /// Anchored to the no-argument call shape so it never cross-captures Register calls.
    /// </summary>
    private static HashSet<string> GetPreservedDtoTypes(string generatedSource)
    {
        var preserved = new HashSet<string>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            generatedSource,
            @"DtoConstructorRegistry\.PreserveType<(.+?)>\(\)");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            preserved.Add(match.Groups[1].Value);
        }

        return preserved;
    }

    [Fact]
    public void PositionalRecordAsReturnType_PreserveTypeEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record StartVisitResult(int VisitId, string Status);

    [Factory]
    public static partial class VisitCommands
    {
        [Remote]
        [Execute]
        internal static Task<StartVisitResult> _StartVisit(int patientId)
            => Task.FromResult(new StartVisitResult(patientId, ""started""));
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.StartVisitResult", GetPreservedDtoTypes(generated));
        Assert.DoesNotContain("global::TestNamespace.StartVisitResult", GetRegisteredDtoTypes(generated));
    }

    [Fact]
    public void PositionalRecordAsParameter_PreserveTypeEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record VisitCommand(int PatientId, string Reason);

    [Factory]
    public static partial class VisitCommands
    {
        [Remote]
        [Execute]
        internal static Task<int> _StartVisit(VisitCommand command)
            => Task.FromResult(command.PatientId);
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.VisitCommand", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void PositionalRecordNestedInClassDto_BothBucketsEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record BannerInfo(string Text, string Severity);

    public class DashboardDto
    {
        public int Id { get; set; }
        public BannerInfo Banner { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal DashboardDto Create() => new DashboardDto();
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.DashboardDto", GetRegisteredDtoTypes(generated));
        Assert.Contains("global::TestNamespace.BannerInfo", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void ClassDtoNestedInPositionalRecord_DescentEntersRecordGraph()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class DetailInfo
    {
        public string Notes { get; set; }
    }

    public record ResultRecord(int Id, DetailInfo Detail);

    [Factory]
    public static partial class Commands
    {
        [Remote]
        [Execute]
        internal static Task<ResultRecord> _Run(int id)
            => Task.FromResult(new ResultRecord(id, new DetailInfo()));
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.ResultRecord", GetPreservedDtoTypes(generated));
        Assert.Contains("global::TestNamespace.DetailInfo", GetRegisteredDtoTypes(generated));
    }

    [Fact]
    public void CollectionOfRecords_UnwrappedAndPreserved()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record ContactResult(int Id, string Name);

    [Factory]
    public static partial class SearchCommands
    {
        [Remote]
        [Execute]
        internal static Task<List<ContactResult>> _Search(string term)
            => Task.FromResult(new List<ContactResult>());
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.ContactResult", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void RecordWithBothCtorShapes_StaysInRegisterBucket()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record FlexibleDto
    {
        public FlexibleDto() { }
        public FlexibleDto(int id) { Id = id; }
        public int Id { get; set; }
    }

    [Factory]
    public static partial class Commands
    {
        [Remote]
        [Execute]
        internal static Task<FlexibleDto> _Run() => Task.FromResult(new FlexibleDto());
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.FlexibleDto", GetRegisteredDtoTypes(generated));
        Assert.DoesNotContain("global::TestNamespace.FlexibleDto", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void PositionalRecordFromInterfaceFactory_PreserveTypeEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record LookupResult(int Id, string Name);

    [Factory]
    public interface ILookupService
    {
        [Remote]
        Task<LookupResult> GetByIdAsync(int id);
    }

    public class LookupService : ILookupService
    {
        public Task<LookupResult> GetByIdAsync(int id)
            => Task.FromResult(new LookupResult(id, ""x""));
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.Contains("global::TestNamespace.LookupResult", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void FactoryAnnotatedType_NoEmissionOfEitherKind()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class OtherEntity
    {
        [Create]
        internal void Create() { }
    }

    [Factory]
    public static partial class Commands
    {
        [Remote]
        [Execute]
        internal static Task<OtherEntity> _Load([Service] OtherEntity entity)
            => Task.FromResult(entity);
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.DoesNotContain("global::TestNamespace.OtherEntity", GetRegisteredDtoTypes(generated));
        Assert.DoesNotContain("global::TestNamespace.OtherEntity", GetPreservedDtoTypes(generated));
    }

    [Fact]
    public void PrivateCtorOnlyType_NoEmissionOfEitherKind()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class Opaque
    {
        private Opaque() { }
        public int Id { get; set; }
    }

    [Factory]
    public static partial class Commands
    {
        [Remote]
        [Execute]
        internal static Task<Opaque> _Run() => Task.FromResult<Opaque>(null);
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.DoesNotContain("global::TestNamespace.Opaque", GetRegisteredDtoTypes(generated));
        Assert.DoesNotContain("global::TestNamespace.Opaque", GetPreservedDtoTypes(generated));
    }
}
