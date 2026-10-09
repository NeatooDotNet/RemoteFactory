using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies DTO discovery through dictionary entries and public fields (DICT-002).
/// </summary>
/// <remarks>
/// <para>
/// Before DICT-002 the walk unwrapped a collection through its single
/// <c>IEnumerable&lt;T&gt;</c> argument, so <c>Dictionary&lt;K,V&gt;</c> became
/// <c>KeyValuePair&lt;K,V&gt;</c> — a <c>System</c> type the walk rejects — and neither
/// <c>K</c> nor <c>V</c> was visited. It also walked properties only, although the
/// serializer runs with <c>IncludeFields = true</c>. A type reachable only through either
/// shape got no preservation and was trimmed from Blazor WebAssembly clients
/// (zTreatment's <c>BodyAssessmentInfo.Locations</c>, confirmed 2026-09-10).
/// </para>
/// <para>
/// The walk is one seam shared by three callers — factory signatures, <c>[Factory]</c>
/// entity properties, and event-record graphs — so the dictionary-value shape is pinned
/// once per caller rather than assumed to flow through. The shape tests were run against
/// the unchanged generator first and observed red; the safety tests at the bottom were
/// green before and must stay green after.
/// </para>
/// <para>
/// Sorted, concurrent, immutable, and consumer-defined generic dictionaries reach the walk
/// through the same <c>IEnumerable&lt;KeyValuePair&lt;K,V&gt;&gt;</c> interface and are pinned
/// in <c>MoreDictionaryShapes_ValueDtoRegistered</c>. Those six framework cases were red
/// before the helper compilation referenced the System.Collections assemblies, because
/// their types bound as error types, which shows the assertion is not vacuous.
/// </para>
/// </remarks>
public class DictionaryAndFieldDtoDiscoveryTests
{
    /// <summary>
    /// Runs the generator. The helper fails the run if the generator threw, so no negative
    /// assertion in this file can pass against empty output (DICT-002 test review,
    /// 2026-10-06; the guard moved into the helper afterwards).
    /// </summary>
    private static GeneratorDriverRunResult Run(string source)
        => DiagnosticTestHelper.RunGenerator(source).RunResult;

    private static string AllTrees(GeneratorDriverRunResult runResult)
        => string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));

    /// <summary>
    /// Generated tree(s) for exactly one factory, anchored to ".{hint}.g.cs" so a hint
    /// cannot substring-match another factory's tree. Fails when no tree matches, so a
    /// negative assertion can never run against empty text.
    /// </summary>
    private static string FactoryTree(GeneratorDriverRunResult runResult, string factoryFileHint)
        => RequireTree(runResult, $".{factoryFileHint}.g.cs");

    private static string EventRegistrarTree(GeneratorDriverRunResult runResult)
        => RequireTree(runResult, ".NeatooEventPreservation.g.cs");

    private static string RequireTree(GeneratorDriverRunResult runResult, string fileSuffix)
    {
        var tree = string.Join("\n", runResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith(fileSuffix, StringComparison.Ordinal))
            .Select(t => t.GetText()?.ToString() ?? ""));
        Assert.False(string.IsNullOrEmpty(tree), $"No generated tree ends with {fileSuffix}.");
        return tree;
    }

    /// <summary>
    /// Type arguments of every <c>DtoConstructorRegistry.Register&lt;T&gt;</c> in the text. The
    /// lazy group ends at the first <c>&gt;</c> followed by <c>(()</c>, so a generic argument
    /// such as <c>Node&lt;ValueDto&gt;</c> is captured whole.
    /// </summary>
    private static HashSet<string> Registered(string generated)
        => Regex.Matches(generated, @"DtoConstructorRegistry\.Register<(.+?)>\(\(\)")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Type arguments of every <c>DtoConstructorRegistry.PreserveType&lt;T&gt;()</c>.</summary>
    private static HashSet<string> Preserved(string generated)
        => Regex.Matches(generated, @"DtoConstructorRegistry\.PreserveType<(.+?)>\(\)")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    #region Dictionary values — one test per caller (AC-1)

    [Fact]
    public void SignatureReturn_DictionaryValueDto_Registered()
    {
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ValueDto
    {
        public string Text { get; set; }
    }

    [Factory]
    public partial class Lookup
    {
        [Create]
        internal Dictionary<string, ValueDto> Create() => new Dictionary<string, ValueDto>();
    }
}
";
        Assert.Contains("global::TestNamespace.ValueDto", Registered(AllTrees(Run(source))));
    }

    [Fact]
    public void SignatureParameter_DictionaryValueDto_Registered()
    {
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ParamDto
    {
        public string Text { get; set; }
    }

    [Factory]
    public partial class Importer
    {
        [Fetch]
        internal void Fetch(Dictionary<string, ParamDto> input) { }
    }
}
";
        Assert.Contains("global::TestNamespace.ParamDto", Registered(AllTrees(Run(source))));
    }

    [Fact]
    public void EntityProperty_DictionaryValueDto_RegisteredInEntityRegistrar()
    {
        // The zTreatment BodyAssessmentInfo.Locations shape: a plain sealed class with an
        // implicit parameterless constructor, reachable only as a dictionary value.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public sealed class LocationAssessment
    {
        public string Region { get; set; }
        public int Severity { get; set; }
    }

    [Factory]
    public class BodyAssessment
    {
        public Dictionary<string, LocationAssessment> Locations { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "BodyAssessmentFactory");

        Assert.Contains("global::TestNamespace.LocationAssessment", Registered(tree));
    }

    [Fact]
    public void EventRecordProperty_DictionaryValueDto_RegisteredInEventRegistrar()
    {
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class PayloadDto
    {
        public string Note { get; set; }
    }

    public record ReadingsTakenEvent(int VisitId) : FactoryEventBase
    {
        public Dictionary<string, PayloadDto> Readings { get; set; }
    }
}
";
        var tree = EventRegistrarTree(Run(source));

        Assert.Contains("global::TestNamespace.ReadingsTakenEvent", Preserved(tree));
        Assert.Contains("global::TestNamespace.PayloadDto", Registered(tree));
    }

    #endregion

    #region Dictionary values — shapes and buckets (AC-1)

    [Theory]
    [InlineData("Dictionary<string, ValueDto>")]
    [InlineData("IDictionary<string, ValueDto>")]
    [InlineData("IReadOnlyDictionary<string, ValueDto>")]
    public void DictionaryShapes_ValueDtoRegistered(string propertyType)
    {
        var source = $@"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{{
    public class ValueDto
    {{
        public string Text {{ get; set; }}
    }}

    [Factory]
    public class Carrier
    {{
        public {propertyType} Entries {{ get; set; }}

        [Create]
        internal void Create() {{ }}
    }}
}}
";
        Assert.Contains("global::TestNamespace.ValueDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Fact]
    public void DictionaryValueRecord_PreservedNotRegistered()
    {
        // A positional record has no public parameterless constructor, so it belongs in
        // the PreserveType bucket — the same rule a list element follows.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record BannerRecord(string Text, string Severity);

    [Factory]
    public class Carrier
    {
        public Dictionary<string, BannerRecord> Banners { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "CarrierFactory");

        Assert.Contains("global::TestNamespace.BannerRecord", Preserved(tree));
        Assert.DoesNotContain("global::TestNamespace.BannerRecord", Registered(tree));
    }

    [Fact]
    public void DictionaryValueWrappedInList_ElementRegistered()
    {
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ValueDto
    {
        public string Text { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public Dictionary<string, List<ValueDto>> Groups { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.ValueDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Fact]
    public void DictionaryValueDto_ItsOwnGraphStillWalked()
    {
        // Reaching the value type must hand it to the ordinary graph walk, so a DTO nested
        // inside the value is discovered too.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class InnerDto
    {
        public int Id { get; set; }
    }

    public class ValueDto
    {
        public InnerDto Inner { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public Dictionary<string, ValueDto> Entries { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var registered = Registered(FactoryTree(Run(source), "CarrierFactory"));

        Assert.Contains("global::TestNamespace.ValueDto", registered);
        Assert.Contains("global::TestNamespace.InnerDto", registered);
    }

    #endregion

    #region Dictionary keys (AC-2)

    [Fact]
    public void DictionaryKeyDto_Registered()
    {
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class KeyDto
    {
        public int Id { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public Dictionary<KeyDto, string> ByKey { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.KeyDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    #endregion

    #region Public fields (AC-6)

    [Fact]
    public void PublicFieldOnDto_FieldTypeRegistered()
    {
        // The DICT-001 harness shape: the host is reached as an entity property and
        // carries the DTO in a public field.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class FieldDto
    {
        public string Text { get; set; }
    }

    public class HostDto
    {
        public FieldDto Carried;
    }

    [Factory]
    public class Carrier
    {
        public HostDto Host { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var registered = Registered(FactoryTree(Run(source), "CarrierFactory"));

        Assert.Contains("global::TestNamespace.HostDto", registered);
        Assert.Contains("global::TestNamespace.FieldDto", registered);
    }

    [Fact]
    public void PublicFieldOnEntity_FieldTypeRegistered()
    {
        // An entity serialized in Named format goes through the reflection serializer with
        // IncludeFields on, so its own public fields are on the wire too.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class FieldDto
    {
        public string Text { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public FieldDto Carried;

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.FieldDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Fact]
    public void PublicFieldRecord_PreservedNotRegistered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record BannerRecord(string Text, string Severity);

    public class HostDto
    {
        public BannerRecord Banner;
    }

    [Factory]
    public class Carrier
    {
        public HostDto Host { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "CarrierFactory");

        Assert.Contains("global::TestNamespace.BannerRecord", Preserved(tree));
        Assert.DoesNotContain("global::TestNamespace.BannerRecord", Registered(tree));
    }

    [Fact]
    public void NonPublicStaticAndConstFields_NotWalked()
    {
        // Only public instance fields reach the wire. A private field, a static field, and
        // a const are not serialized, so their types must not be preserved.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class PrivateDto
    {
        public int Id { get; set; }
    }

    public class StaticDto
    {
        public int Id { get; set; }
    }

    public class HostDto
    {
        private PrivateDto _hidden;
        public static StaticDto Shared;
        public const int Label = 1;

        public string Name { get; set; }

        public PrivateDto Peek() => _hidden;
    }

    [Factory]
    public class Carrier
    {
        public HostDto Host { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var registered = Registered(FactoryTree(Run(source), "CarrierFactory"));

        Assert.Contains("global::TestNamespace.HostDto", registered);
        Assert.DoesNotContain("global::TestNamespace.PrivateDto", registered);
        Assert.DoesNotContain("global::TestNamespace.StaticDto", registered);
    }

    #endregion

    #region Safety — green before DICT-002 and must stay green

    [Fact]
    public void SelfEnumerableGeneric_TerminatesWithPreChangeRegistration()
    {
        // Node<T> enumerates Node<T>. Recursive unwrapping would loop forever without a
        // seen-set. Before DICT-002 single-level unwrapping stopped at Node<ValueDto>,
        // registered it, and walked its Value property; that output must not change.
        var source = @"
using System.Collections;
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ValueDto
    {
        public int Id { get; set; }
    }

    public class Node<T> : IEnumerable<Node<T>>
    {
        public T Value { get; set; }

        public IEnumerator<Node<T>> GetEnumerator() => throw new System.NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Factory]
    public class Tree
    {
        public Node<ValueDto> Root { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var registered = Registered(FactoryTree(Run(source), "TreeFactory"));

        Assert.Contains("global::TestNamespace.Node<global::TestNamespace.ValueDto>", registered);
        Assert.Contains("global::TestNamespace.ValueDto", registered);
    }

    [Fact]
    public void ExpandingGenericEnumerable_TerminatesAtDepthCap()
    {
        // Weird<T> enumerates Weird<Weird<T>>, so every unwrap step yields a NEW type and
        // the path check never fires; only the walker's depth cap stops the recursion.
        // Without the cap this overflows the stack and takes the test host down — observed
        // by lifting the cap (DICT-002 evidence). Recursion is new in DICT-002, so there is
        // no pre-change red for this test; the negative control is its proof.
        var source = @"
using System.Collections;
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ValueDto
    {
        public int Id { get; set; }
    }

    public class Weird<T> : IEnumerable<Weird<Weird<T>>>
    {
        public IEnumerator<Weird<Weird<T>>> GetEnumerator() => throw new System.NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Factory]
    public class Tree
    {
        public Weird<ValueDto> Root { get; set; }
        public ValueDto Sibling { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var registered = Registered(FactoryTree(Run(source), "TreeFactory"));

        // The generator finished, and the walk carried on past Root to the next member.
        Assert.Contains("global::TestNamespace.ValueDto", registered);
    }

    [Fact]
    public void SystemOnlyDictionaries_RegisterNothingOfTheirOwn()
    {
        // Expanding a KeyValuePair must hand its arguments to the ordinary candidate check,
        // never register the pair itself or any other System type. AnchorDto proves the
        // walk ran over this entity's members, so "nothing else registered" is a real
        // observation rather than the result of an empty tree.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class AnchorDto
    {
        public int Id { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public AnchorDto Anchor { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public Dictionary<string, List<string>> Tags { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "CarrierFactory");
        var registered = Registered(tree);

        Assert.Single(registered);
        Assert.Contains("global::TestNamespace.AnchorDto", registered);
        Assert.Empty(Preserved(tree));
    }

    #endregion

    #region Dictionary values — nullable and consumer collections (AC-1, gate round 1)

    [Fact]
    public void NullableDictionaryValue_ValueDtoRegistered()
    {
        // A nullable-enabled consumer writes Dictionary<string, Dto?>. The annotation sits
        // on the type argument, so it is stripped during the recursive unwrap, not only at
        // the top of the property type.
        var source = @"
#nullable enable
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ValueDto
    {
        public string? Text { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public Dictionary<string, ValueDto?>? Entries { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.ValueDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Theory]
    [InlineData("PagedList<ValueDto>")]
    [InlineData("List<PagedList<ValueDto>>")]
    [InlineData("Dictionary<string, PagedList<ValueDto>>")]
    public void ConsumerGenericCollection_CollectionAndElementBothRegistered(string propertyType)
    {
        // The serializer constructs a consumer's own generic collection as well as its
        // elements, so both constructors need preserving. Before DICT-002 a NESTED one was
        // registered but its element was not; the first cut of DICT-002 swapped that,
        // registering the element and dropping the collection (code review, 2026-10-06).
        // A top-level one never had its own constructor registered at all.
        var source = $@"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{{
    public class ValueDto
    {{
        public string Text {{ get; set; }}
    }}

    public class PagedList<T> : List<T>
    {{
        public int PageNumber {{ get; set; }}
    }}

    [Factory]
    public class Carrier
    {{
        public {propertyType} Items {{ get; set; }}

        [Create]
        internal void Create() {{ }}
    }}
}}
";
        var registered = Registered(FactoryTree(Run(source), "CarrierFactory"));

        Assert.Contains("global::TestNamespace.PagedList<global::TestNamespace.ValueDto>", registered);
        Assert.Contains("global::TestNamespace.ValueDto", registered);
    }

    #endregion

    #region Side effect — nested collections

    [Fact]
    public void NestedListOfLists_InnerElementRegistered()
    {
        // Missed before DICT-002 for the same reason as dictionaries: the inner List<T>
        // was rejected as a System type. Recursive unwrapping reaches the element.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class CellDto
    {
        public int Value { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public List<List<CellDto>> Grid { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.CellDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Fact]
    public void JaggedArray_InnerElementRegistered()
    {
        // An array of arrays recurses through the array branch twice.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class CellDto
    {
        public int Value { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public CellDto[][] Grid { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.CellDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    [Fact]
    public void NullableStructElement_StructRegistered()
    {
        // List<PointDto?> enumerates Nullable<PointDto>, a System type. The nullable is
        // stripped during the recursive unwrap, so the struct itself is reached.
        var source = @"
using System.Collections.Generic;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public struct PointDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Factory]
    public class Carrier
    {
        public List<PointDto?> Points { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        Assert.Contains("global::TestNamespace.PointDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    #endregion

    #region Dictionary values — framework dictionaries outside CoreLib, and a consumer's own

    [Theory]
    [InlineData("SortedDictionary<string, ValueDto>")]
    [InlineData("SortedList<string, ValueDto>")]
    [InlineData("ConcurrentDictionary<string, ValueDto>")]
    [InlineData("ImmutableDictionary<string, ValueDto>")]
    [InlineData("ImmutableSortedDictionary<string, ValueDto>")]
    [InlineData("IImmutableDictionary<string, ValueDto>")]
    [InlineData("LookupMap<ValueDto>")]
    public void MoreDictionaryShapes_ValueDtoRegistered(string propertyType)
    {
        // The published docs claim sorted, concurrent, immutable, and custom generic
        // dictionaries are walked, because each enumerates KeyValuePair<K,V> through a generic
        // IEnumerable<T>. These live in System.Collections, System.Collections.Concurrent, and
        // System.Collections.Immutable, which the helper compilation references for that reason
        // — without them the property type is an error type and nothing is walked.
        var source = $@"
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Neatoo.RemoteFactory;

namespace TestNamespace
{{
    public class ValueDto
    {{
        public string Text {{ get; set; }}
    }}

    public class LookupMap<TValue> : Dictionary<string, TValue>
    {{
    }}

    [Factory]
    public class Carrier
    {{
        public {propertyType} Entries {{ get; set; }}

        [Create]
        internal void Create() {{ }}
    }}
}}
";
        Assert.Contains("global::TestNamespace.ValueDto", Registered(FactoryTree(Run(source), "CarrierFactory")));
    }

    #endregion
}
