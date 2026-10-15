using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// AddNeatooRemoteFactory is safe to call more than once (#117).
/// </summary>
/// <remarks>
/// A Neatoo server reaches it twice: AddNeatooServices calls it with the domain assembly plus
/// Neatoo.dll and then swaps the JSON converter factory for its own; AddNeatooAspNetCore calls it
/// again with the domain assembly. The second call must not replace the assembly list or bring
/// the swapped-out converter factory back. The tests stand Neatoo's half in with the same steps.
/// </remarks>
public class RepeatedRegistrationTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(RepeatedRegistrationTests).Assembly;

    /// <summary>An assembly the second call does not name, standing in for Neatoo.dll.</summary>
    private static readonly System.Reflection.Assembly Library = typeof(FactAttribute).Assembly;

    private sealed class HostConverterFactory : NeatooJsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => false;

        public override System.Text.Json.Serialization.JsonConverter? CreateConverter(Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => null;
    }

    /// <summary>The steps Neatoo's AddNeatooServices takes around its own call.</summary>
    private static void AddLikeNeatoo(IServiceCollection services)
    {
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain, Library);
        services.RemoveAll<NeatooJsonConverterFactory>();
        services.AddTransient<NeatooJsonConverterFactory, HostConverterFactory>();
    }

    [Fact]
    public void SecondCall_KeepsTheAssembliesTheFirstRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddLikeNeatoo(services);

        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        using var provider = services.BuildServiceProvider();
        var assemblies = provider.GetRequiredService<IServiceAssemblies>();
        Assert.Single(services, d => d.ServiceType == typeof(IServiceAssemblies));
        Assert.Equal(typeof(FactAttribute), assemblies.FindType(typeof(FactAttribute).FullName!));
        Assert.True(assemblies.HasType(typeof(RepeatedRegistrationTests)));
    }

    [Fact]
    public void SecondCall_DoesNotBringBackAReplacedConverterFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddLikeNeatoo(services);

        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        var converter = Assert.Single(services, d => d.ServiceType == typeof(NeatooJsonConverterFactory));
        Assert.Equal(typeof(HostConverterFactory), converter.ImplementationType);
    }

    [Fact]
    public void SecondCall_WithANewAssembly_AddsItToTheExistingList()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        services.AddNeatooRemoteFactory(NeatooFactory.Server, Library);

        using var provider = services.BuildServiceProvider();
        var assemblies = provider.GetRequiredService<IServiceAssemblies>();
        Assert.Single(services, d => d.ServiceType == typeof(IServiceAssemblies));
        Assert.True(assemblies.HasType(typeof(RepeatedRegistrationTests)));
        Assert.True(assemblies.HasType(typeof(FactAttribute)));
    }

    [Fact]
    public void SecondCall_WithTheSameAssemblies_RegistersNothing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);
        var afterFirst = services.Count;

        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        Assert.Equal(afterFirst, services.Count);
    }

    [Fact]
    public void SecondCall_WithoutOptions_KeepsTheRegisteredOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, new NeatooSerializationOptions { Format = SerializationFormat.Named }, Domain);

        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        using var provider = services.BuildServiceProvider();
        Assert.Single(services, d => d.ServiceType == typeof(NeatooSerializationOptions));
        Assert.Equal(SerializationFormat.Named, provider.GetRequiredService<NeatooSerializationOptions>().Format);
    }

    [Fact]
    public void SecondCall_WithOptions_ReplacesTheRegisteredOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        services.AddNeatooRemoteFactory(NeatooFactory.Server, new NeatooSerializationOptions { Format = SerializationFormat.Named }, Domain);

        using var provider = services.BuildServiceProvider();
        Assert.Single(services, d => d.ServiceType == typeof(NeatooSerializationOptions));
        Assert.Equal(SerializationFormat.Named, provider.GetRequiredService<NeatooSerializationOptions>().Format);
    }

    [Fact]
    public void SecondCall_WithADifferentMode_Throws()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Domain);

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddNeatooRemoteFactory(NeatooFactory.Remote, Domain));

        Assert.Contains("NeatooFactory.Server", ex.Message);
        Assert.Contains("NeatooFactory.Remote", ex.Message);
    }
}
