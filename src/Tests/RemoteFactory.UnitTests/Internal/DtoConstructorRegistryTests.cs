using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

public class DtoConstructorRegistryTests
{
    public record ParameterizedRecord(int X);

    [Fact]
    public void PreserveType_DoesNotRegisterConstructor()
    {
        DtoConstructorRegistry.PreserveType<ParameterizedRecord>();

        Assert.False(DtoConstructorRegistry.TryCreate(typeof(ParameterizedRecord), out _));
    }

    [Fact]
    public void PreserveType_IsIdempotent()
    {
        DtoConstructorRegistry.PreserveType<ParameterizedRecord>();
        DtoConstructorRegistry.PreserveType<ParameterizedRecord>();
        DtoConstructorRegistry.PreserveType<ParameterizedRecord>();

        Assert.False(DtoConstructorRegistry.TryCreate(typeof(ParameterizedRecord), out _));
    }
}
